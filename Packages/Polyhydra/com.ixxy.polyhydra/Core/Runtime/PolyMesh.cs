using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AsImpL;
using Polyhydra.Wythoff;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace Polyhydra.Core
{

    public enum Axis {X,Y,Z}

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

    public enum Roles
    {
        Ignored,
        Existing,
        New,
        NewAlt,
        ExistingAlt,
    }

    public partial class PolyMesh
    {
        private PointOctree<Vertex> octree;

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

        // Mod functions that treat negative values sanely
        public static int ActualMod(int x, int m) => (x % m + m) % m;
        public static float ActualMod(float x, float m) => (x % m + m) % m;

        public float ScalingFactor = 1f;
        public List<Roles> FaceRoles;
        public List<Roles> VertexRoles;
        public List<HashSet<string>> FaceTags;

        public MeshHalfedgeList Halfedges { get; private set; }
        public MeshVertexList Vertices { get; set; }
        public MeshFaceList Faces { get; private set; }

        public void SetFaceRoles(Roles role)
        {
            FaceRoles = Enumerable.Repeat(role, Faces.Count).ToList();
        }

        public void SetVertexRoles(Roles role)
        {
            VertexRoles = Enumerable.Repeat(role, Vertices.Count).ToList();
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

        public List<Vector3> DebugVerts;

        public PolyMesh()
        {
            Halfedges = new MeshHalfedgeList(this);
            Vertices = new MeshVertexList(this);
            Faces = new MeshFaceList(this);
            FaceRoles = new List<Roles>();
            VertexRoles = new List<Roles>();
            InitTags();
        }

        public PolyMesh(
            IEnumerable<Vector3> verts,
            IEnumerable<IEnumerable<int>> faceIndices
        ) : this()
        {
            var faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count());
            var vertexRoles = Enumerable.Repeat(Roles.New, verts.Count());
            FaceRoles = faceRoles.ToList();
            VertexRoles = vertexRoles.ToList();
            InitIndexed(verts, faceIndices);
            CullUnusedVertices();
            InitTags();
        }

        public enum SeedShape
        {
            Tetrahedron,
            Octahedron,
            Cube,
            Icosahedron,
            Dodecahedron,
            Prism,
            Antiprism,
            Pyramid,
            Polygon,
            Uniform,
            Johnson,
            Grid,
            Random,
        }

        public static PolyMesh FromConwayString(string conwayString)
        {
            // Seed types
            var seedMap = new Dictionary<string, SeedShape>
            {
              {"T",  SeedShape.Tetrahedron},
              {"O",  SeedShape.Octahedron},
              {"C",  SeedShape.Cube},
              {"I",  SeedShape.Icosahedron},
              {"D",  SeedShape.Dodecahedron},
              {"P",  SeedShape.Prism},
              {"A",  SeedShape.Antiprism},
              {"Y",  SeedShape.Pyramid},
              {"Z",  SeedShape.Polygon},
              {"J",  SeedShape.Johnson},
              {"U",  SeedShape.Uniform},
              {"R",  SeedShape.Random},
              {"K",  SeedShape.Grid},
            };

            var operatorMap = new Dictionary<string, Operation>
            {

                {"a", Operation.Ambo},
                {"b", Operation.Bevel},
                {"d", Operation.Dual},
                {"e", Operation.Expand},
                {"g", Operation.Gyro},
                {"j", Operation.Join},
                {"k", Operation.Kis},
                {"m", Operation.Meta},
                {"o", Operation.Ortho},
                {"p", Operation.Propeller},
                {"s", Operation.Snub},
                {"t", Operation.Truncate},
                {"c", Operation.Chamfer},
                {"G", Operation.OppositeLace},
                {"K", Operation.Stake},
                {"L", Operation.Lace},
                {"l", Operation.Loft},
                {"M", Operation.Medial},
                {"n", Operation.Needle},
                {"q", Operation.Quinto},
                {"u", Operation.Subdivide},
                {"w", Operation.Whirl},
                {"X", Operation.Cross},
                {"z", Operation.Zip},
                {"C", Operation.Canonicalize},

                // {"r", Operation.Reflect},
                // {"O", Operation.Quadsub},
                // {"u", Operation.Trisub},
                // {"H", Operation.Hollow},
                // {"Z", Operation.Triangulate},
                // {"A", Operation.AdjustXYZ},
            };

            var allTokens = new HashSet<string>(seedMap.Keys);
            allTokens.UnionWith(operatorMap.Keys);

            bool IsToken(string c) {return allTokens.Contains(c);}
            bool isNumeric(string c){return "1234567890.".Contains(c);}

            var tokens = new List<(string, float)>();
            int pointer = 0;

            while (pointer < conwayString.Length)
            {
              var opString = conwayString.Substring(pointer, 1);
              if (!IsToken(opString))
              {
                  Debug.LogError($"Unexpected token: {opString} in input {conwayString} at {pointer}");
                  pointer++;
                  continue;
              }
              var start_n = ++pointer;
              while (pointer < conwayString.Length && isNumeric(conwayString.Substring(pointer, 1)))
              {
                  ++pointer;
              }
              float floatString;
              var floatValue = float.TryParse(conwayString.Substring(start_n, pointer-start_n), out floatString);
              tokens.Add((opString, floatValue ? floatString : float.NaN));
            }

            tokens.Reverse();
            const string parameterizedSeeds = "PAYZJKU";
            if (parameterizedSeeds.IndexOf(tokens[0].Item1, StringComparison.Ordinal) >= 0)
            {
                if(tokens[0].Item2 < 3) {
                  throw new Exception("Invalid number of faces for seed");
                }
            }
            else if (!float.IsNaN(tokens[0].Item2))
            {
                throw new Exception("Seed "  + tokens[0].Item1 + " does not use a parameter");
            }

            var seed = seedMap[tokens[0].Item1];
            var poly = GenerateConwaySeedShape(seed, tokens[0].Item2);

            // Apply operators
            for (var i = 1; i < tokens.Count; ++i)
            {
                Filter filter = null;
                float val = 0;
                var op = operatorMap[tokens[i].Item1];
                OpParams p = new OpParams();

                if (!float.IsNaN(tokens[i].Item2))
                {
                    if (
                        op==Operation.Loft || op==Operation.Needle ||
                        op==Operation.Kis || op==Operation.Lace || op==Operation.Stake)
                    {
                        int sides = Mathf.FloorToInt(tokens[i].Item2);
                        filter = Filter.NumberOfSides(sides);
                    }
                    else if (op==Operation.Truncate)
                    {
                        int vertexEdges = Mathf.FloorToInt(tokens[i].Item2);
                        filter = Filter.NumberOfSides(vertexEdges);
                    }
                }

                if (op==Operation.Lace && tokens[i].Item2 == 0)
                {
                    // Antiprism's L0 op
                    // TODO actual Antiprism syntax is L_0
                    op = Operation.OppositeLace;
                }

                if (op == Operation.Chamfer)
                {
                    val = 0.5f;
                }
                else if (op == Operation.Join || op == Operation.Propeller || op == Operation.Needle)
                {
                    val = 0;
                }
                else
                {
                    val = 1 / 3f;
                }

                if (filter == null)
                {
                    p = new OpParams(val);
                }
                else
                {
                    p = new OpParams(val, filter);
                }

                if (op==Operation.Canonicalize)
                {
                    poly = poly.Canonicalize(tokens[i].Item2, tokens[i].Item2);
                }
                else
                {
                    poly = poly.AppyOperation(op, p);
                }

                if (op==Operation.Propeller || op==Operation.Quinto)
                {
                    poly = poly.Canonicalize(2, 2); // Antiprism compatibility
                }

            }
            return poly;
        }

        public static PolyMesh GenerateConwaySeedShape(SeedShape seedShape, float param = 0)
        {
            var gridTypeMap = new Dictionary<string, GridEnums.GridTypes>
            {
                {"333333", GridEnums.GridTypes.K_3_3_3_3_3_3},
                {"4444", GridEnums.GridTypes.K_4_4_4_4},
                {"666", GridEnums.GridTypes.K_6_6_6},
                {"33336", GridEnums.GridTypes.K_3_3_3_3_6},
                {"33344", GridEnums.GridTypes.K_3_3_3_4_4},
                {"33434", GridEnums.GridTypes.K_3_3_4_3_4},
                {"3464", GridEnums.GridTypes.K_3_4_6_4},
                {"3636", GridEnums.GridTypes.K_3_6_3_6},
                {"31212", GridEnums.GridTypes.K_3_12_12},
                {"4612", GridEnums.GridTypes.K_4_6_12},
                {"488", GridEnums.GridTypes.K_4_8_8},
                {"33412333333", GridEnums.GridTypes.K_3_3_4_12__3_3_3_3_3_3},
                {"33663636", GridEnums.GridTypes.K_3_3_6_6__3_6_3_6},
                {"3431231212", GridEnums.GridTypes.K_3_4_3_12__3_12_12},
                {"34463636", GridEnums.GridTypes.K_3_4_4_6__3_6_3_6},
            };

            PolyMesh poly = null;
            WythoffPoly wythoff;

            int paramInt = Mathf.FloorToInt(param);
            int sides = Mathf.Max(paramInt, 3);

            switch (seedShape)
            {
                case SeedShape.Tetrahedron:
                    wythoff = new WythoffPoly(UniformTypes.Tetrahedron);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Octahedron:
                    wythoff = new WythoffPoly(UniformTypes.Octahedron);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Cube:
                    wythoff = new WythoffPoly(UniformTypes.Cube);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Icosahedron:
                    wythoff = new WythoffPoly(UniformTypes.Icosahedron);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Dodecahedron:
                    wythoff = new WythoffPoly(UniformTypes.Dodecahedron);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Prism:
                    poly = RadialSolids.Prism(sides);
                    break;
                case SeedShape.Antiprism:
                    poly = RadialSolids.Antiprism(sides);
                    break;
                case SeedShape.Pyramid:
                    poly = RadialSolids.Pyramid(sides);
                    break;
                case SeedShape.Polygon:
                    poly = Shapes.Polygon(sides);
                    break;
                case SeedShape.Johnson:
                    poly = JohnsonSolids.Build(paramInt);
                    break;
                case SeedShape.Uniform:
                    wythoff = new WythoffPoly(Uniform.FromCoxeter(sides).Coxeter);
                    wythoff.Build();
                    poly = wythoff.Build();
                    break;
                case SeedShape.Random:
                    int c = Mathf.FloorToInt(Random.Range(0, 7));
                    int root = Mathf.FloorToInt(Random.Range(1, 60));
                    poly = WatermanPoly.Build(1, root, c, true);
                    break;
                case SeedShape.Grid:
                    var typeString = paramInt.ToString();
                    var gridType = gridTypeMap[typeString];
                    poly = Grids.Build(gridType, GridEnums.GridShapes.Plane, 5, 5);
                    break;
            }

            return poly;
        }

        public PolyMesh(
			IEnumerable<Vector3> verts,
            IEnumerable<IEnumerable<int>> faceIndices,
            IEnumerable<Roles> faceRoles,
            IEnumerable<Roles> vertexRoles
		) : this()
        {
            if (faceRoles.Count() != faceIndices.Count())
            {
                throw new ArgumentException(
                    $"Incorrect FaceRole array: {faceRoles.Count()} instead of {faceIndices.Count()}",
                    "faceRoles"
                );
            }
            FaceRoles = faceRoles.ToList();
            VertexRoles = vertexRoles.ToList();
            InitIndexed(verts, faceIndices);
            CullUnusedVertices();
            InitTags();
        }

        public PolyMesh(
            IEnumerable<Vector3> verticesByPoints,
            IEnumerable<IEnumerable<int>> facesByVertexIndices,
            IEnumerable<Roles> faceRoles,
            IEnumerable<Roles> vertexRoles,
            List<HashSet<string>> newFaceTags
        ) : this(verticesByPoints, facesByVertexIndices, faceRoles, vertexRoles)
        {
            FaceTags = newFaceTags;
        }

        public PolyMesh(TextReader reader) : this()
        {
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();

            // Read all non-empty, non-comment lines into a list
            var lines = reader
                .ReadToEnd()
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !(string.IsNullOrWhiteSpace(line) || line.StartsWith("#")))
                .ToList();

            var metrics = Regex.Split(lines[1], "\\s+");
            int NVertices = int.Parse(metrics[0]);
            int NFaces = int.Parse(metrics[1]);

            // Vertices should start after header and metrics lines
            for (int i = 2; i < NVertices + 2; i++)
            {
                // Split on any whitespace
                string[] vertex = Regex.Split(lines[i], "\\s+");

                var v = new Vector3(
                    float.Parse(vertex[0]),
                    float.Parse(vertex[1]),
                    float.Parse(vertex[2])
                );
                vertexPoints.Add(v);
            }

            FaceTags = new List<HashSet<string>>();

            // Faces should immediately follow all vertices
            int firstFaceLine = NVertices + 2;
            for (int i = firstFaceLine; i < firstFaceLine + NFaces; i++)
            {

                // Split on any whitespace
                var faceString = Regex.Split(lines[i], "\\s+");
                int sides = int.Parse(faceString[0]);
                if (sides < 3) continue;
                var face = new int[sides];
                for (int j = 0; j < sides; j++)
                {
                    face[sides - j - 1] = int.Parse(faceString[j + 1]);
                }

                // Assume any line with more than 3 values also has colours
                if (faceString.Length > sides + 3)
                {
                    // Read the colour (we ignore alpha)
                    var faceColor = new Color(
                        float.Parse(faceString[sides + 1]),
                        float.Parse(faceString[sides + 2]),
                        float.Parse(faceString[sides + 3])
                    );
                    var tags = new HashSet<string>();
                    tags.Add($"#{(int)faceColor.r:X2}{(int)faceColor.g:X2}{(int)faceColor.b:X2}");
                    FaceTags.Add(tags);
                }

                face = face.ToArray();
                faceIndices.Add(face);
            }

            var faceRoles = Enumerable.Repeat(Roles.Existing, faceIndices.Count);
            var vertexRoles = Enumerable.Repeat(Roles.Existing, NVertices);

            FaceRoles = faceRoles.ToList();
            VertexRoles = vertexRoles.ToList();
            InitIndexed(vertexPoints, faceIndices);

            CullUnusedVertices();

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
                    VertexRoles.Add(vertIndex < origVertexRoles.Count ? origVertexRoles[vertIndex]: Roles.Ignored);
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
            return FindBoundaries(Halfedges);
        }

        public List<List<Halfedge>> FindBoundaries(IEnumerable<Halfedge> edges)
        {
            var looped = new HashSet<Halfedge>();
            var loops = new List<List<Halfedge>>();

            foreach (var startHalfedge in edges)
            {
                // If it's not a bare edge or we've already checked it
                if (startHalfedge.Pair != null || looped.Contains(startHalfedge)) continue;

                var loop = GetBoundaryLoop(startHalfedge);
                looped.UnionWith(loop);

                if (loop.Count >= 3)
                {
                    loops.Add(loop);
                }
            }

            return loops;
        }

        public List<Halfedge> GetBoundaryLoop(Halfedge startHalfedge)
        {
            var loop = new List<Halfedge>();
            var currLoopEdge = startHalfedge;
            int failsafe = 0;
            do
            {
                loop.Add(currLoopEdge);
                currLoopEdge = currLoopEdge.Prev.Vertex.Halfedges.First(e=>e.Pair==null && e!=currLoopEdge);
            } while (currLoopEdge != startHalfedge && failsafe++ < 1000);

            if (failsafe >= 1000)
            {
                Debug.LogError($"Failed to close boundary loop");
                loop.Clear();
            }
            return loop;
        }

        public void InitOctree()
        {
            octree = new PointOctree<Vertex>(1, Vector3.zero, 1);
            for (var i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                octree.Add(v, v.Position);
            }
        }

        public Vertex[] FindNeighbours(Vertex v, float distance)
        {
            return octree.GetNearby(v.Position, distance);
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

        public MeshData BuildMeshData(
            bool generateSubmeshes = false,
            Color[] colors = null,
            ColorMethods colorMethod = ColorMethods.ByRole,
            UVMethods uvMethod = UVMethods.FirstEdge,
            bool largeMeshFormat = true)
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

            var meshTriangles = new List<int>();
            var meshVertices = new List<Vector3>();
            var meshNormals = new List<Vector3>();
            var meshColors = new List<Color32>();
            var meshUVs = new List<Vector2>();
            var edgeUVs = new List<Vector2>();
            var barycentricUVs = new List<Vector3>();
            var miscUVs1 = new List<Vector4>();
            var miscUVs2 = new List<Vector4>();

            List<Roles> uniqueRoles = null;
            List<string> uniqueTags = null;

            var submeshTriangles = new List<List<int>>();

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
                        var flattenedTags = FaceTags.SelectMany(d => d);
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
                    if (face.AreAllVertsVisibleFromCentroid())
                    {
                        for (var edgeIndex = 0; edgeIndex < faceIndex.Count; edgeIndex++)
                        {
                            // Convex faces can use fan triangulation
                            // It's fast at the cost of an extra triangle per face
                            meshVertices.Add(faceCentroid);
                            meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                            faceTris.Add(index++);
                            edgeUVs.Add(new Vector2(0, 0));
                            barycentricUVs.Add(new Vector3(0, 0, 1));

                            meshVertices.Add(points[faceIndex[edgeIndex]]);
                            meshUVs.Add(calcUV(meshVertices[index], xAxis, yAxis));
                            faceTris.Add(index++);
                            edgeUVs.Add(new Vector2(1, 1));
                            barycentricUVs.Add(new Vector3(0, 1, 0));

                            meshVertices.Add(points[faceIndex[(edgeIndex + 1) % face.Sides]]);
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
                        // Concave faces need ear clipping triangulation
                        // This doesn't work well for complex (self-intersecting) faces
                        // but those are mostly Wythoff Uniform polyhedra
                        // and therefore have convex faces
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

                        faceTris.Reverse();
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
                                string htmlColor = FaceTags[i].Last(t => t.StartsWith("#"));
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

            var meshData = new MeshData
            {
                meshVertices = meshVertices,
                meshNormals = meshNormals,
                submeshTriangles = submeshTriangles,
                meshTriangles = meshTriangles,
                meshColors = meshColors,
                generateSubmeshes = generateSubmeshes,
                largeMeshFormat = largeMeshFormat,
                meshUVs = meshUVs,
                edgeUVs = edgeUVs,
                barycentricUVs = barycentricUVs,
                miscUVs1 = miscUVs1,
                miscUVs2 = miscUVs2
            };

            return meshData;
        }

        public struct MeshData
        {
            public List<Vector3> meshVertices;
            public List<Vector3> meshNormals;
            public List<List<int>> submeshTriangles;
            public List<int> meshTriangles;
            public List<Color32> meshColors;
            public bool generateSubmeshes;
            public bool largeMeshFormat;
            public List<Vector2> meshUVs;
            public List<Vector2> edgeUVs;
            public List<Vector3> barycentricUVs;
            public List<Vector4> miscUVs1;
            public List<Vector4> miscUVs2;
        }

        public Mesh BuildUnityMesh(MeshData meshData)
        {
            var target = new Mesh();
            if (meshData.largeMeshFormat)
            {
                target.indexFormat = IndexFormat.UInt32;
            }

            // Scale verts by scaling factor and apply some jitter to reduce z-fighting
            // TODO Do we really want to jitter verts here?
            // It was a quick fix for z-fighting but I haven't really tested how effective it is
            // or looked into alternatives
            const float jitter = 0.0002f;
            for (var i = 0; i < meshData.meshVertices.Count; i++)
            {
                meshData.meshVertices[i] = meshData.meshVertices[i]
                    * ScalingFactor
                    + new Vector3(
                        Random.value * jitter,
                        Random.value * jitter,
                        Random.value * jitter
                    );
            }

            target.vertices = meshData.meshVertices.ToArray();
            target.normals = meshData.meshNormals.ToArray();

            if (meshData.generateSubmeshes)
            {
                target.subMeshCount = meshData.submeshTriangles.Count;
                for (var i = 0; i < meshData.submeshTriangles.Count; i++)
                {
                    target.SetTriangles(meshData.submeshTriangles[i], i);
                }
            }
            else
            {
                target.triangles = meshData.meshTriangles.ToArray();
            }

            target.colors32 = meshData.meshColors.ToArray();
            target.SetUVs(0, meshData.meshUVs);
            target.SetUVs(1, meshData.edgeUVs);
            target.SetUVs(2, meshData.barycentricUVs);
            target.SetUVs(3, meshData.miscUVs1);
            target.SetUVs(4, meshData.miscUVs2);

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
            Color32 color = Color.white;
            var face = Faces[i];
            var faceRole = FaceRoles[i];
            switch (colorMethod)
            {
                case ColorMethods.ByRole:
                    color = colors[(int)faceRole];
                    break;
                case ColorMethods.BySides:
                    color = colors[(face.Sides - 3) % colors.Length];
                    break;
                case ColorMethods.ByFaceDirection:
                    color = colors[CalcDirectionIndex(face, colors.Length - 1)];
                    break;
                case ColorMethods.ByTags:
                    if (i < FaceTags.Count && FaceTags[i].Count > 0)
                    {
                        string htmlColor = FaceTags[i].LastOrDefault(t => t!=null && t.StartsWith("#"));
                        color = htmlColor != null ? ParseHexColor(htmlColor) : Color.white;
                    }
                    break;
            }

            return color;
        }

        private Color ParseHexColor(string htmlColor)
        {
            int hex = Int32.Parse(htmlColor.Replace("#", ""), NumberStyles.HexNumber);
            return new Color(
                (hex & 0xff0000)>> 0x10,
                (hex & 0xff00)>> 8,
                hex & 0xff
            );
        }

        public void InitTags(Color color)
        {
            InitTags($"#{ColorUtility.ToHtmlStringRGB(color)}");
        }

        public void InitTags(string tag=null)
        {
            FaceTags = new List<HashSet<string>>();
            for (var i = 0; i < Faces.Count; i++)
            {
                FaceTags.Add(new HashSet<string> { string.IsNullOrEmpty(tag) ? null : tag });
            }
        }

        // For a given number of vertices, returns the face index data structure
        // to connect it as a strip of quad faces
        public static List<List<int>> GenerateQuadStripIndices(int vertexCount)
        {
            var faceIndices = new List<List<int>>();
            for (int i = 0; i < vertexCount - 2; i += 2)
            {
                faceIndices.Add(new List<int>
                {
                    i, i+2, i+3, i+1
                });
            }
            return faceIndices;
        }

        public enum Operation
        {

            // Conway Operators

            Identity = 0,
            Kis = 1,
            Ambo = 2,
            Zip = 3,
            Expand = 4,
            Bevel = 5,
            Join = 6,
            Needle = 7,
            Ortho = 8,
            Meta = 9,
            Truncate = 10,
            Dual = 11,
            Gyro = 12,
            Snub = 13,
            Subdivide = 14,
            Loft = 15,
            Chamfer = 16,
            Quinto = 17,
            Lace = 18,
            JoinedLace = 19,
            OppositeLace = 20,
            JoinKisKis = 21,
            Stake = 22,
            JoinStake = 23,
            Medial = 24,
            EdgeMedial = 25,
            Propeller = 26,
            Whirl = 27,
            Volute = 28,
            Exalt = 29,
            Yank = 30,
            Squall = 31,
            JoinSquall = 32,
            Cross = 33,
            Subdiv = 96,
            Ortho3 = 97,

            // Alternating Operators

            SplitFaces = 34,
            Gable = 35,

            // Thickening Operators

            Extrude = 36,
            Shell = 37,
            Segment = 79,
            Skeleton = 38,

            // Object Transforms

            ScaleX = 98,
            ScaleY = 99,
            ScaleZ = 100,
            Recenter = 64,
            SitLevel = 65,

            // Face Transforms

            FaceOffset = 39,
            FaceScale = 40,
            FaceRotateX = 41,
            FaceRotateY = 42,
            FaceRotateZ = 43,
            FaceSlide = 44,
            // Hinge = 48,

            // Affine Vertex Transforms

            VertexScale = 45,
            VertexRotate = 46,
            VertexOffset = 47,
            VertexStellate = 81,

            // PolarOffset,   TODO

            // Shape Replication

            // AddDual = 49,
            // AddCopyX = 50,
            // AddCopyY = 51,
            // AddCopyZ = 52,
            // AddMirrorX = 53,
            // AddMirrorY = 54,
            // AddMirrorZ = 55,
            // Stack = 72,
            // Layer = 73,

            // Face/Vertex Deletion

            FaceRemove = 56,
            VertexRemove = 58,

            // Topology Manipulation

            FillHoles = 60,
            // ExtendBoundaries = 61,
            // ConnectFaces = 80,
            // FaceMerge = 62,
            Weld = 63,
            ConvexHull = 68,

            // Non-Affine Vertex Transforms

            // Stretch = 66,
            Spherize = 69,
            Cylinderize = 70,
            Bulge = 93,
            Wave = 94,
            Canonicalize = 71,
            PerlinNoiseX = 86,
            PerlinNoiseY = 87,
            PerlinNoiseZ = 88,

            // Store/Recall

            // Stash = 74,
            // Unstash = 75,
            // UnstashToVerts = 76,
            // UnstashToFaces = 77,
            AddTag = 90,
            RemoveTag = 91,
            ClearTags = 92,

            // Generator Ops

            Sweep = 95,
        }

        public PolyMesh AppyOperation(Operation op, OpParams p)
        {

            if (Faces.Count != FaceTags.Count)
            {
                Debug.LogWarning($"{Faces.Count} faces but {FaceTags.Count} tags");
                return this;
            }
            PolyMesh polyMesh = this;

            switch (op)
            {
                // Conway Operators

                case Operation.Identity:
                    polyMesh = Duplicate();
                    break;
                case Operation.Kis:
                    polyMesh = polyMesh.Kis(p);
                    break;
                case Operation.Ambo:
                    polyMesh = polyMesh.Ambo();
                    break;
                case Operation.Zip:
                    polyMesh = polyMesh.Zip(p);
                    break;
                case Operation.Expand:
                    polyMesh = polyMesh.Expand(p);
                    break;
                case Operation.Bevel:
                    polyMesh = polyMesh.Bevel(p);
                    break;
                case Operation.Join:
                    polyMesh = polyMesh.Join(p);
                    break;
                case Operation.Needle:
                    polyMesh = polyMesh.Needle(p);
                    break;
                case Operation.Ortho:
                    polyMesh = polyMesh.Ortho(p);
                    break;
                case Operation.Subdiv:
                    polyMesh = polyMesh.Ortho(p, true);
                    break;
                case Operation.Meta:
                    polyMesh = polyMesh.Meta(p);
                    break;
                case Operation.Truncate:
                    polyMesh = polyMesh.Truncate(p);
                    break;
                case Operation.Dual:
                    polyMesh = polyMesh.Dual();
                    break;
                case Operation.Gyro:
                    polyMesh = polyMesh.Gyro(p);
                    break;
                case Operation.Snub:
                    polyMesh = polyMesh.Gyro(p);
                    polyMesh = polyMesh.Dual();
                    break;
                case Operation.Subdivide:
                    polyMesh = polyMesh.Subdivide(p);
                    break;
                case Operation.Loft:
                    polyMesh = polyMesh.Loft(p);
                    break;
                case Operation.Chamfer:
                    polyMesh = polyMesh.Chamfer(p);
                    break;
                case Operation.Quinto:
                    polyMesh = polyMesh.Quinto(p);
                    break;
                case Operation.Lace:
                    polyMesh = polyMesh.Lace(p);
                    break;
                case Operation.JoinedLace:
                    polyMesh = polyMesh.JoinedLace(p);
                    break;
                case Operation.OppositeLace:
                    polyMesh = polyMesh.OppositeLace(p);
                    break;
                case Operation.JoinKisKis:
                    polyMesh = polyMesh.JoinKisKis(p);
                    break;
                case Operation.Stake:
                    polyMesh = polyMesh.Stake(p);
                    break;
                case Operation.JoinStake:
                    polyMesh = polyMesh.Stake(p, @join: true);
                    break;
                case Operation.Medial:
                    polyMesh = polyMesh.Medial(p);
                    break;
                case Operation.EdgeMedial:
                    polyMesh = polyMesh.EdgeMedial(p);
                    break;
                case Operation.Propeller:
                    polyMesh = polyMesh.Propeller(p);
                    break;
                case Operation.Whirl:
                    polyMesh = polyMesh.Whirl(p);
                    break;
                case Operation.Volute:
                    polyMesh = polyMesh.Volute(p);
                    break;
                case Operation.Exalt:
                    polyMesh = polyMesh.Exalt(p);
                    break;
                case Operation.Yank:
                    polyMesh = polyMesh.Yank(p);
                    break;
                case Operation.Squall:
                    polyMesh = polyMesh.Squall(p);
                    break;
                case Operation.JoinSquall:
                    polyMesh = polyMesh.Squall(p, @join: true);
                    break;
                case Operation.Cross:
                    polyMesh = polyMesh.Cross(p);
                    break;
                case Operation.Ortho3:
                    polyMesh = polyMesh.Ortho3(p);
                    break;

                // Alternating Operators

                case Operation.SplitFaces:
                    polyMesh = polyMesh.SplitFaces(p);
                    break;
                case Operation.Gable:
                    polyMesh = polyMesh.Gable(p);
                    break;

                // Thickening Operators

                case Operation.Extrude:
                    polyMesh = polyMesh.Extrude(p);
                    break;
                case Operation.Shell:
                    polyMesh = polyMesh.Shell(p);
                    break;
                case Operation.Segment:
                    polyMesh = polyMesh.Segment(p);
                    break;
                case Operation.Skeleton:
                    polyMesh = Loft(p);
                    polyMesh = polyMesh.FaceRemove(new OpParams(Filter.Role(Roles.Existing)));
                    polyMesh = polyMesh.Shell(new OpParams(p.funcA), false);
                    break;

                // Object Transforms

                case Operation.ScaleX:
                    polyMesh = polyMesh.Scale(p, (int)Axis.X);
                    break;
                case Operation.ScaleY:
                    polyMesh = polyMesh.Scale(p, (int)Axis.Y);
                    break;
                case Operation.ScaleZ:
                    polyMesh = polyMesh.Scale(p, (int)Axis.Z);
                    break;

                case Operation.Recenter:
                    polyMesh.Recenter();
                    break;
                case Operation.SitLevel:
                    polyMesh.SitLevel();
                    break;

                // Face Transforms

                case Operation.FaceOffset:
                    polyMesh = polyMesh.FaceOffset(p);
                    break;
                case Operation.FaceScale:
                    polyMesh = polyMesh.FaceScale(p);
                    break;
                case Operation.FaceRotateX:
                    polyMesh = polyMesh.FaceRotate(p, (int)Axis.X);
                    break;
                case Operation.FaceRotateY:
                    polyMesh = polyMesh.FaceRotate(p, (int)Axis.Y);
                    break;
                case Operation.FaceRotateZ:
                    polyMesh = polyMesh.FaceRotate(p, (int)Axis.Z);
                    break;
                case Operation.FaceSlide:
                    polyMesh = polyMesh.FaceSlide(p);
                    break;
                // case Operation.Hinge:
                //     polyMesh = polyMesh.Hinge(p);
                //     break;

                // Affine Vertex Transforms

                case Operation.VertexScale:
                    polyMesh = polyMesh.VertexScale(p);
                    break;
                case Operation.VertexRotate:
                    polyMesh = polyMesh.VertexRotate(p);
                    break;
                case Operation.VertexOffset:
                    polyMesh = polyMesh.VertexOffset(p);
                    break;
                case Operation.VertexStellate:
                    polyMesh.VertexStellate(p);
                    break;

                // PolarOffset,   TODO

                // Shape Replication

                // case Operation.AddDual:
                //     polyMesh = polyMesh.AddDual(p);
                //     break;
                // case Operation.AddCopyX:
                //     polyMesh = polyMesh.AddCopy(p);
                //     break;
                // case Operation.AddCopyY:
                //     polyMesh = polyMesh.AddCopyY(p);
                //     break;
                // case Operation.AddCopyZ:
                //     polyMesh = polyMesh.AddCopyZ(p);
                //     break;
                // case Operation.AddMirrorX:
                //     polyMesh = polyMesh.AddMirrorX(p);
                //     break;
                // case Operation.AddMirrorY:
                //     polyMesh = polyMesh.AddMirrorY(p);
                //     break;
                // case Operation.AddMirrorZ:
                //     polyMesh = polyMesh.AddMirrorZ(p);
                //     break;
                // case Operation.Stack:
                //     polyMesh = polyMesh.Stack(p);
                //     break;
                // case Operation.Layer:
                //     polyMesh = polyMesh.Layer(p);
                //     break;

                // Face/Vertex Deletion

                case Operation.FaceRemove:
                    polyMesh = polyMesh.FaceRemove(p);
                    break;
                case Operation.VertexRemove:
                    polyMesh = polyMesh.VertexRemove(p, false);
                    break;

                // Topology Manipulation

                case Operation.FillHoles:
                    polyMesh = polyMesh.FillHoles();
                    break;
                // case Operation.ExtendBoundaries:
                //     polyMesh = polyMesh.ExtendBoundaries(p);
                //     break;
                // case Operation.ConnectFaces:
                //     polyMesh = polyMesh.ConnectFaces(p);
                //     break;
                // case Operation.FaceMerge:
                //     polyMesh = polyMesh.FaceMerge(p);
                //     break;
                case Operation.Weld:
                    polyMesh = polyMesh.Weld(p.OriginalParamA);
                    break;
                case Operation.ConvexHull:
                    polyMesh = polyMesh.ConvexHull(true);
                    break;

                // Non-Affine Vertex Transforms

                // case Operation.Stretch:
                //     polyMesh = polyMesh.Stretch(p);
                //     break;
                case Operation.Spherize:
                    polyMesh = polyMesh.Spherize(p);
                    break;
                case Operation.Cylinderize:
                    polyMesh = polyMesh.Cylinderize(p);
                    break;
                case Operation.Bulge:
                    polyMesh.Bulge(Vector3.up, p.OriginalParamA, p.OriginalParamB);
                    break;
                case Operation.Wave:
                    polyMesh.Wave(Vector3.up, p.OriginalParamA, p.OriginalParamB);
                    break;
                case Operation.Canonicalize:
                    polyMesh = polyMesh.Canonicalize(
                        // TODO - iterations or tolerance?
                        // Mathf.FloorToInt(p.OriginalParamA),
                        // Mathf.FloorToInt(p.OriginalParamB)
                        p.OriginalParamA,
                        p.OriginalParamB
                    );
                    break;
                case Operation.PerlinNoiseX:
                    polyMesh.PerlinNoise(Vector3.left, p.OriginalParamA, p.OriginalParamB, p.OriginalParamB);
                    break;
                case Operation.PerlinNoiseY:
                    polyMesh.PerlinNoise(Vector3.up, p.OriginalParamA, p.OriginalParamB, p.OriginalParamB);
                    break;
                case Operation.PerlinNoiseZ:
                    polyMesh.PerlinNoise(Vector3.forward,p.OriginalParamA, p.OriginalParamB, p.OriginalParamB);
                    break;

                case Operation.AddTag:
                    polyMesh.AddTag(p);
                    break;
                case Operation.RemoveTag:
                    polyMesh.RemoveTag(p);
                    break;
                case Operation.ClearTags:
                    polyMesh.ClearTags(p);
                    break;

                case Operation.Sweep:
                    polyMesh = polyMesh.Sweep(p);
                    break;

                default:
                    Debug.LogError($"Unrecognised operation: {op}");
                    break;
            }

            return polyMesh;

        }

    }
}