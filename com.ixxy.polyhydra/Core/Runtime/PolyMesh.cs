using System;
using System.Collections.Generic;
using System.Linq;
using AsImpL;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        private const float TOLERANCE = 0.02f;

        public static Color[] DefaultFaceColors =
        {
            new Color(1.0f, 0.5f, 0.5f),
            new Color(0.8f, 0.85f, 0.9f),
            new Color(0.5f, 0.6f, 0.6f),
            new Color(1.0f, 0.94f, 0.9f),
            new Color(0.66f, 0.2f, 0.2f),
            new Color(0.6f, 0.0f, 0.0f),
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.6f, 0.6f, 0.6f),
            new Color(0.5f, 1.0f, 0.5f),
            new Color(0.5f, 0.5f, 1.0f),
            new Color(0.5f, 1.0f, 1.0f),
            new Color(1.0f, 0.5f, 1.0f),
        };

        public enum TagType
        {
            Introvert,
            Extrovert
        }

        public enum UVMethods
        {
            FirstEdge,
            BestEdge,
            FirstVertex,
            BestVertex,
            ObjectAligned
        }

        public enum ColorMethods
        {
            BySides,
            ByRole,
            ByFaceDirection,
            ByTags,
        }

        public List<Roles> FaceRoles;
        public List<Roles> VertexRoles;
        public List<HashSet<Tuple<string, TagType>>> FaceTags;


        public MeshHalfedgeList Halfedges { get; private set; }
        public MeshVertexList Vertices { get; set; }
        public MeshFaceList Faces { get; private set; }

        public enum Roles
        {
            Ignored,
            Existing,
            New,
            NewAlt,
            ExistingAlt,
        }

        public bool IsValid
        {
            get
            {
                if (Halfedges.Count == 0)
                {
                    return false;
                }

                if (Vertices.Count == 0)
                {
                    return false;
                }

                if (Faces.Count == 0)
                {
                    return false;
                }

                // TODO: beef this up (check for a valid mesh)

                return true;
            }
        }

        public int[] GetFaceCountsByType()
        {
            var faceCountsByType = new int[8];
            foreach (var face in Faces)
            {
                int sides = face.Sides;
                if (faceCountsByType.Length < sides + 1)
                {
                    Array.Resize(ref faceCountsByType, sides + 1);
                }

                faceCountsByType[sides]++;
            }

            return faceCountsByType;
        }

        public (int v, int e, int f) vef
        {
            get
            {
                int v = Vertices.Count;
                int e = EdgeCount;
                int f = Faces.Count;
                return (v, e, f);
            }
        }

        public int EdgeCount
        {
            get
            {
                var nakedEdges = Halfedges.Count(x => x.Pair == null);
                var fullEdges = (Halfedges.Count - nakedEdges) / 2;
                return nakedEdges + fullEdges;
            }
        }

        public PolyMesh()
        {
            Halfedges = new MeshHalfedgeList(this);
            Vertices = new MeshVertexList(this);
            Faces = new MeshFaceList(this);
            FaceRoles = new List<Roles>();
            VertexRoles = new List<Roles>();
        }


        public PolyMesh(
            IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices,
            IEnumerable<Roles> faceRoles,
            IEnumerable<Roles> vertexRoles
        ) : this()
        {
            if (faceRoles.Count() != facesByVertexIndices.Count())
            {
                throw new ArgumentException(
                    $"Incorrect FaceRole array: {faceRoles.Count()} instead of {facesByVertexIndices.Count()}",
                    "faceRoles"
                );
            }

            FaceRoles = faceRoles.ToList();
            VertexRoles = vertexRoles.ToList();
            InitIndexed(verticesByPoints, facesByVertexIndices);

            CullUnusedVertices();
        }

        private PolyMesh(
            IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices,
            IEnumerable<Roles> faceRoles,
            IEnumerable<Roles> vertexRoles,
            List<HashSet<Tuple<string, TagType>>> newFaceTags
        ) : this(verticesByPoints, facesByVertexIndices, faceRoles, vertexRoles)
        {
            FaceTags = newFaceTags;
        }

        /// <summary>
        /// Removes all vertices that are currently not used by the Halfedge list.
        /// </summary>
        /// <returns>The number of unused vertices that were removed.</returns>
        public int CullUnusedVertices()
        {
            var orig = new List<Vertex>(Vertices);
            var origVertexRoles = new List<Roles>(VertexRoles);
            Vertices.Clear();
            VertexRoles.Clear();
            // re-add vertices which reference a halfedge
            for (var vertIndex = 0; vertIndex < orig.Count; vertIndex++)
            {
                var vertex = orig[vertIndex];
                if (vertex.Halfedge != null)
                {
                    Vertices.Add(vertex);
                    VertexRoles.Add(origVertexRoles[vertIndex]);
                }
            }

            return orig.Count - Vertices.Count;
        }


        /// <summary>
        /// A string representation of the mesh.
        /// </summary>
        /// <returns>a string representation of the mesh</returns>
        public override string ToString()
        {
            return base.ToString() + String.Format(" (V:{0} F:{1})", Vertices.Count, Faces.Count);
        }

        /// <summary>
        /// Gets the positions of all mesh vertices. Note that points are duplicated.
        /// </summary>
        /// <returns>a list of vertex positions</returns>
        public Vector3[] ListVerticesByPoints()
        {
            Vector3[] points = new Vector3[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 pos = Vertices[i].Position;
                points[i] = new Vector3(pos.x, pos.y, pos.z);
            }

            return points;
        }

        public List<List<Halfedge>> FindBoundaries()
        {
            var looped = new HashSet<Halfedge>();
            var loops = new List<List<Halfedge>>();

            foreach (var startHalfedge in Halfedges)
            {
                // If it's not a bare edge or we've already checked it
                if (startHalfedge.Pair != null || looped.Contains(startHalfedge)) continue;

                var loop = new List<Halfedge>();
                var currLoopEdge = startHalfedge;
                int escapeClause = 0;
                do
                {
                    loop.Add(currLoopEdge);
                    looped.Add(currLoopEdge);
                    Halfedge nextLoopEdge = null;
                    var possibleEdges = currLoopEdge.Prev.Vertex.Halfedges;
                    //possibleEdges.Reverse();
                    foreach (var edgeToTest in possibleEdges)
                    {
                        if (currLoopEdge != edgeToTest && edgeToTest.Pair == null)
                        {
                            nextLoopEdge = edgeToTest;
                            break;
                        }
                    }

                    if (nextLoopEdge != null)
                    {
                        currLoopEdge = nextLoopEdge;
                    }

                    escapeClause++;
                } while (currLoopEdge != startHalfedge && escapeClause < 1000);

                if (loop.Count >= 3)
                {
                    loops.Add(loop);
                }
            }

            return loops;
        }

        /// <summary>
        /// Gets the indices of vertices in each face loop (i.e. index face-vertex data structure).
        /// Used for duplication and conversion to other mesh types, such as Rhino's.
        /// </summary>
        /// <returns>An array of lists of vertex indices.</returns>
        public List<int>[] ListFacesByVertexIndices()
        {
            var fIndex = new List<int>[Faces.Count];
            var vlookup = new Dictionary<Guid, int>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }

            for (int i = 0; i < Faces.Count; i++)
            {
                var vertIndices = new List<int>();
                var vs = Faces[i].GetVertices();
                for (var vertIndex = 0; vertIndex < vs.Count; vertIndex++)
                {
                    Vertex v = vs[vertIndex];
                    vertIndices.Add(vlookup[v.Name]);
                }

                fIndex[i] = vertIndices;
            }

            return fIndex;
        }

        public bool HasNaked()
        {
            return Halfedges.Select((item, ii) => ii).Where(i => Halfedges[i].Pair == null).ToList().Count > 0;
        }

        private void InitIndexed(IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices)
        {
            var newRoles = new List<Roles>();

            // Add vertices
            foreach (Vector3 p in verticesByPoints)
            {
                Vertices.Add(new Vertex(p));
            }

            // Add faces
            var faces = facesByVertexIndices.ToList();
            for (int counter = 0; counter < faces.Count(); counter++)
            {
                List<int> indices = faces[counter].ToList();

                bool faceAdded;

                faceAdded = Faces.Add(indices.Select(i => Vertices[i]));

                if (!faceAdded)
                {
                    indices.Reverse();
                    faceAdded = Faces.Add(indices.Select(i => Vertices[i]));
                }

                if (faceAdded)
                {
                    newRoles.Add(FaceRoles[counter]);
                }
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
            FaceRoles = newRoles;
        }

        public Bounds GetBounds()
        {
            var bounds = new Bounds();
            foreach (var vert in Vertices)
            {
                bounds.Encapsulate(vert.Position);
            }

            return bounds;
        }

        public Mesh BuildUnityMesh(
            bool generateSubmeshes = false,
            Color[] colors = null,
            ColorMethods colorMethod = ColorMethods.ByRole,
            UVMethods uvMethod = UVMethods.FirstEdge,
            bool largeMeshFormat = true
        )
        {
            Vector2 calcUV(Vector3 point, Vector3 xAxis, Vector3 yAxis)
            {
                float u, v;
                u = Vector3.Project(point, xAxis).magnitude;
                u *= Vector3.Dot(point, xAxis) > 0 ? 1 : -1;
                v = Vector3.Project(point, yAxis).magnitude;
                v *= Vector3.Dot(point, yAxis) > 0 ? 1 : -1;
                return new Vector2(u, v);
            }

            if (colors == null) colors = DefaultFaceColors;
            var target = new Mesh();
            if (largeMeshFormat)
            {
                target.indexFormat = IndexFormat.UInt32;
            }

            var meshTriangles = new List<int>();
            var meshVertices = new List<Vector3>();
            var meshNormals = new List<Vector3>();
            var meshColors = new List<Color32>();
            var meshUVs = new List<Vector2>();
            var edgeUVs = new List<Vector2>();
            var barycentricUVs = new List<Vector3>();
            var miscUVs1 = new List<Vector4>();
            var miscUVs2 = new List<Vector4>();

            List<PolyMesh.Roles> uniqueRoles = null;
            List<string> uniqueTags = null;

            var submeshTriangles = new List<List<int>>();

            // TODO
            // var hasNaked = conway.HasNaked();

            // Strip down to Face-Vertex structure
            var points = ListVerticesByPoints();
            var faceIndices = ListFacesByVertexIndices();

            // Add faces
            int index = 0;

            if (generateSubmeshes)
            {
                switch (colorMethod)
                {
                    case ColorMethods.ByRole:
                        uniqueRoles = new HashSet<Roles>(FaceRoles).ToList();
                        for (int i = 0; i < uniqueRoles.Count; i++) submeshTriangles.Add(new List<int>());
                        break;
                    case ColorMethods.BySides:
                        for (int i = 0; i < colors.Length; i++) submeshTriangles.Add(new List<int>());
                        break;
                    case ColorMethods.ByFaceDirection:
                        for (int i = 0; i < colors.Length; i++) submeshTriangles.Add(new List<int>());
                        break;
                    case ColorMethods.ByTags:
                        var flattenedTags = FaceTags.SelectMany(d => d.Select(i => i.Item1));
                        uniqueTags = new HashSet<string>(flattenedTags).ToList();
                        for (int i = 0; i < uniqueTags.Count + 1; i++) submeshTriangles.Add(new List<int>());
                        break;
                }
            }

            for (var i = 0; i < faceIndices.Length; i++)
            {
                var faceIndex = faceIndices[i];
                var face = Faces[i];
                var faceNormal = face.Normal;
                var faceCentroid = face.Centroid;

                Roles faceRole = FaceRoles[i];

                // Axes for UV mapping

                Vector3 xAxis = Vector3.right;
                Vector3 yAxis = Vector3.up;
                switch (uvMethod)
                {
                    case UVMethods.FirstEdge:
                        xAxis = face.Halfedge.Vector;
                        yAxis = Vector3.Cross(xAxis, faceNormal);
                        break;
                    case UVMethods.BestEdge:
                        xAxis = face.GetBestEdge().Vector;
                        yAxis = Vector3.Cross(xAxis, faceNormal);
                        break;
                    case UVMethods.FirstVertex:
                        yAxis = face.Centroid - face.GetVertices()[0].Position;
                        xAxis = Vector3.Cross(yAxis, faceNormal);
                        break;
                    case UVMethods.BestVertex:
                        yAxis = face.Centroid - face.GetBestEdge().Vertex.Position;
                        xAxis = Vector3.Cross(yAxis, faceNormal);
                        break;
                    case UVMethods.ObjectAligned:
                        // Align towards the highest vertex or edge midpoint (measured in the y direction)
                        Vertex chosenVert = face.GetVertices().OrderBy(vert => vert.Position.y).First();
                        Halfedge chosenEdge = face.GetHalfedges().OrderBy(edge => edge.Midpoint.y).First();
                        Vector3 chosenPoint;
                        if (chosenVert.Position.y > chosenEdge.Midpoint.y + 0.01f) // favour edges slightly
                            chosenPoint = chosenVert.Position;
                        else
                            chosenPoint = chosenEdge.Midpoint;

                        yAxis = face.Centroid - chosenPoint;
                        xAxis = Vector3.Cross(yAxis, faceNormal);
                        break;
                }

                Color32 color = CalcFaceColor(colors, colorMethod, i);

                float faceScale = 0;
                foreach (var v in face.GetVertices())
                {
                    faceScale += Vector3.Distance(v.Position, faceCentroid);
                }

                faceScale /= face.Sides;

                var miscUV1 = new Vector4(faceScale, face.Sides, faceCentroid.magnitude,
                    ((float)i) / faceIndices.Length);
                var miscUV2 = new Vector4(faceCentroid.x, faceCentroid.y, faceCentroid.z, i);

                var faceTris = new List<int>();

                if (face.Sides > 3)
                {
                    var newTris = Triangulator.Triangulate(face);

                    for (int t = 0; t < newTris.Count; t++)
                    {
                        meshVertices.Add(newTris[t].v1.Position);
                        meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                        faceTris.Add(index++);
                        edgeUVs.Add(new Vector2(0, 0));
                        barycentricUVs.Add(new Vector3(0, 0, 1));

                        meshVertices.Add(newTris[t].v2.Position);
                        meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                        faceTris.Add(index++);
                        edgeUVs.Add(new Vector2(1, 1));
                        barycentricUVs.Add(new Vector3(0, 1, 0));

                        meshVertices.Add(newTris[t].v3.Position);
                        meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                        faceTris.Add(index++);
                        edgeUVs.Add(new Vector2(1, 1));
                        barycentricUVs.Add(new Vector3(1, 0, 0));

                        meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
                        meshColors.AddRange(Enumerable.Repeat(color, 3));
                        miscUVs1.AddRange(Enumerable.Repeat(miscUV1, 3));
                        miscUVs2.AddRange(Enumerable.Repeat(miscUV2, 3));
                    }
                }
                else
                {
                    meshVertices.Add(points[faceIndex[0]]);
                    meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                    faceTris.Add(index++);
                    barycentricUVs.Add(new Vector3(0, 0, 1));

                    meshVertices.Add(points[faceIndex[1]]);
                    meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                    faceTris.Add(index++);
                    barycentricUVs.Add(new Vector3(0, 1, 0));

                    meshVertices.Add(points[faceIndex[2]]);
                    meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                    faceTris.Add(index++);
                    barycentricUVs.Add(new Vector3(1, 0, 0));

                    edgeUVs.AddRange(Enumerable.Repeat(new Vector2(1, 1), 3));
                    meshNormals.AddRange(Enumerable.Repeat(faceNormal, 3));
                    meshColors.AddRange(Enumerable.Repeat(color, 3));
                    miscUVs1.AddRange(Enumerable.Repeat(miscUV1, 3));
                    miscUVs2.AddRange(Enumerable.Repeat(miscUV2, 3));
                }

                if (generateSubmeshes)
                {
                    switch (colorMethod)
                    {
                        case ColorMethods.ByRole:
                            int uniqueRoleIndex = uniqueRoles.IndexOf(faceRole);
                            submeshTriangles[uniqueRoleIndex].AddRange(faceTris);
                            break;
                        case ColorMethods.BySides:
                            submeshTriangles[face.Sides].AddRange(faceTris);
                            break;
                        case ColorMethods.ByFaceDirection:
                            submeshTriangles[CalcDirectionIndex(face, colors.Length - 1)].AddRange(faceTris);
                            break;
                        case ColorMethods.ByTags:
                            if (FaceTags[i].Count > 0)
                            {
                                string htmlColor = FaceTags[i].First(t => t.Item1.StartsWith("#")).Item1;
                                int uniqueTagIndex = uniqueTags.IndexOf(htmlColor);
                                submeshTriangles[uniqueTagIndex + 1].AddRange(faceTris);
                            }
                            else
                            {
                                submeshTriangles[0].AddRange(faceTris);
                            }

                            break;
                    }
                }
                else
                {
                    meshTriangles.AddRange(faceTris);
                }
            }
            
            // TODO Do we really want to jitter verts here?
            // It was a quick fix for z-fighting but I haven't really tested how effective it is
            // or looked into alternatives
            target.vertices = meshVertices.Select(x => Jitter(x)).ToArray();
            
            target.normals = meshNormals.ToArray();
            
            if (generateSubmeshes)
            {
                target.subMeshCount = submeshTriangles.Count;
                for (var i = 0; i < submeshTriangles.Count; i++)
                {
                    target.SetTriangles(submeshTriangles[i], i);
                }
            }
            else
            {
                target.triangles = meshTriangles.ToArray();
            }

            target.colors32 = meshColors.ToArray();
            target.SetUVs(0, meshUVs);
            target.SetUVs(1, edgeUVs);
            target.SetUVs(2, barycentricUVs);
            target.SetUVs(3, miscUVs1);
            target.SetUVs(4, miscUVs2);

            target.RecalculateTangents();
            return target;
        }


        private static int CalcDirectionIndex(Face face, int range)
        {
            var angles = new[]
            {
                Vector3.Angle(face.Normal, Vector3.up),
                Vector3.Angle(face.Normal, Vector3.down),
                Vector3.Angle(face.Normal, Vector3.left),
                Vector3.Angle(face.Normal, Vector3.right),
                Vector3.Angle(face.Normal, Vector3.forward),
                Vector3.Angle(face.Normal, Vector3.back),
            };
            float angle = angles.Min();
            return Mathf.FloorToInt((angle / 90f) * range);
        }

        public Color32 CalcFaceColor(Color[] colors, ColorMethods colorMethod, int i)
        {
            Color32 color;
            var face = Faces[i];
            var faceRole = FaceRoles[i];
            switch (colorMethod)
            {
                case ColorMethods.ByRole:
                    color = colors[(int)faceRole];
                    break;
                case ColorMethods.BySides:
                    color = colors[face.Sides % colors.Length];
                    break;
                case ColorMethods.ByFaceDirection:
                    color = colors[CalcDirectionIndex(face, colors.Length - 1)];
                    break;
                case ColorMethods.ByTags:
                    var c = new Color();
                    if (FaceTags[i].Count > 0)
                    {
                        string htmlColor = FaceTags[i].First(t => t.Item1.StartsWith("#")).Item1;
                        if (!(ColorUtility.TryParseHtmlString(htmlColor, out c)))
                        {
                            if (!ColorUtility.TryParseHtmlString(htmlColor.Replace("#", ""), out c))
                            {
                                c = Color.white;
                            }
                        }

                        color = c;
                    }
                    else
                    {
                        color = Color.white;
                    }

                    break;
                default:
                    color = Color.white;
                    break;
            }

            return color;
        }
        
        private static Vector3 Jitter(Vector3 val)
        {
            // Used to reduce Z fighting for coincident faces
            float jitter = 0.0002f;
            return val + new Vector3(Random.value * jitter, Random.value * jitter, Random.value * jitter);
        }
    }
}