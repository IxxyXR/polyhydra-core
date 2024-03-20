using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Polyhydra.Core
{

    public class Face
    {

        #region constructors

        public Face(Halfedge edge)
        {
            Halfedge = edge;
            Name = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public Face()
        {
            Name = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        #endregion

        #region properties

        public Halfedge Halfedge { get; set; }
        public String Name { get; private set; }

        private Vector3 _cachedNormal;
        // private bool _hasCachedNormal;

        public Vector3 GetPolarPoint(float angle, float position)
        {
            // Returns a point that lies on the face
            // And is measured in polar coordinates
            // (angle in degrees, position is 0 to 1 with 0 being on the centroid and 1 being on the edge
            // The face can be concave but the algorithm assumes so overhangs or "ears"
            // As it starts by doing a fan triangulation from the centroid.

            var verts = GetVertices();
            var centroid = Centroid;
            angle = angle % 360;
            float previousAngle = 0;
            Vector3 result = centroid;
            for (int i = 1; i <= verts.Count; i++)
            {
                Vector3 a = centroid;
                Vector3 b = verts[i % verts.Count].Position;
                Vector3 c = verts[i - 1].Position;
                float currentAngle = previousAngle + Vector3.Angle(a - b, a - c);
                if (currentAngle > angle)
                {
                    float angleInTri = angle - previousAngle;
                    float ratio = Mathf.InverseLerp(previousAngle, currentAngle, previousAngle + angleInTri);
                    Vector3 pointOnEdge = Vector3.Lerp(b, c, ratio);
                    result = Vector3.LerpUnclamped(a, pointOnEdge, position);
                    break;
                }
                previousAngle = currentAngle;
            }

            return result;
        }

        public float GetArea()
        {
            float area = 0;
            var verts = GetVertices();

            if (verts.Count == 3)
            {
                Vector3 v = Vector3.Cross(
                    verts[0].Position - verts[1].Position,
                    verts[0].Position - verts[2].Position
                );
                area += v.magnitude * 0.5f;
            }
            else
            {
                var centroid = Centroid;
                for (int i = 1; i < verts.Count; i ++)
                {
                    Vector3 a = centroid;
                    Vector3 b = verts[i % verts.Count].Position;
                    Vector3 c = verts[i - 1].Position;
                    Vector3 v = Vector3.Cross(a - b, a - c);
                    area += v.magnitude * 0.5f;
                }
            }

            return area;
        }

        public Vector3 Centroid
        {
            get
            {
                List<Vertex> vertices = GetVertices();
                Vector3 avg = new Vector3();
                var vcount = vertices.Count;
                for (var i = 0; i < vcount; i++)
                {
                    Vertex v = vertices[i];
                    avg.x += v.Position.x;
                    avg.y += v.Position.y;
                    avg.z += v.Position.z;
                }

                avg.x /= vcount;
                avg.y /= vcount;
                avg.z /= vcount;

                return avg;
            }
        }

        public (Vector3, Vector3) GetTangents()
        {
            Vector3 t1, t2;


            t1 = Tangent;

            t2 = Vector3.Cross(Normal, t1);
            return (t1, t2.normalized);

        }

        public Vector3 GetInteriorCentroid()
        {
            // WIP - not currently correct.
            // See https://stackoverflow.com/questions/9692448/how-can-you-find-the-centroid-of-a-concave-irregular-polygon-in-javascript


            List<Vector2> vertices = Get2DVertices();
            vertices.Add(vertices[0]);
            float twicearea = 0;
            float x = 0, y = 0;
            int nPts = vertices.Count;
            Vector3 p1, p2;
            float f;
            for (int i = 0, j = nPts - 1; i < nPts; j = i++)
            {
                p1 = vertices[i];
                p2 = vertices[j];
                f = p1.x * p2.y - p2.x * p1.y;
                twicearea += f;
                x += (p1.x + p2.x) * f;
                y += (p1.y + p2.y) * f;
            }
            f = twicearea * 3f;
            var centroid2D = new Vector3(x / f, y / f);
            var (tangentLeft, tangentUp) = GetTangents();
            var centroid = -(tangentUp * centroid2D.y) + (tangentLeft * centroid2D.x);
            return centroid;
        }

        /// <summary>
        /// Get the face normal (unit vector).
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                Vector3 normal = new Vector3(0, 0, 0);
                var centroid = Centroid;
                Halfedge edge = Halfedge;
                do
                {
                    Vector3 crossTmp = Vector3.Cross(edge.Vector - centroid, edge.Next.Vector - centroid);
                    normal += crossTmp;
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);
                return new Vector3(normal.x, normal.y, normal.z).normalized;
            }
        }


        public int Sides {
            get { return GetVertices().Count; }
        }

        public Vector3 Tangent => (Centroid - Halfedge.Vertex.Position).normalized;

        public static Vector2 Vector3toVector2(Vector3 v, Vector3 tangent)
        {
            return new Vector2(
                Vector3.Dot(v, tangent),
                Vector3.Dot(v, Quaternion.Euler(0, 90, 0) * tangent)
            );
        }

        // Gets the vertices of this face as a list of Vector2s
        // in the plane of the face relative to it's centroid
        public List<Vector2> Get2DVertices()
        {
            var origin = Centroid;
            var tangents = GetTangents();
            var verts = GetVertices();
            return verts.Select(v => new Vector2(
                    Vector3.Dot(v.Position - origin, tangents.Item1),
                    Vector3.Dot(v.Position - origin, tangents.Item2)
            )).ToList();
        }

        public bool IsClockwise
        {
            get
            {
                var verts = Get2DVertices();
                float total = 0;
                for (var i = 0; i < verts.Count; i++)
                {
                    var vert1 = verts[i];
                    var vert2 = verts[(i + 1) % verts.Count];
                    total += (vert2.x - vert1.x) * (vert2.y - vert1.y);
                }
                return total >= 0;
            }
        }

        public bool IsConvex
        {
            get
            {
                List<Vector2> verts = Get2DVertices();
                if (verts.Count < 4) return true;
                bool sign = false;
                int n = verts.Count;
                for(int i = 0; i < n; i++)
                {
                    float dx1 = verts[(i + 2) % n].x - verts[(i + 1) % n].x;
                    float dy1 = verts[(i + 2) % n].y - verts[(i + 1) % n].y;
                    float dx2 = verts[i].x - verts[(i + 1) % n].x;
                    float dy2 = verts[i].y - verts[(i + 1) % n].y;
                    float zcrossproduct = dx1 * dy2 - dy1 * dx2;

                    if (i == 0) sign = zcrossproduct > 0;
                    else if (sign != zcrossproduct > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool AreAllVertsVisibleFromCentroid()
        {
            // Is every vertex reachable from the given point
            // Cycles through vertices and returns false if the
            // angle between vertex and center changes direction

            var centroid = Centroid;
            var normal = Normal;
            var verts = GetHalfedges().Select(e => e.Vertex.Position).ToList();
            if (verts.Count < 4) return true;
            float lastSign = 0;
            int n = verts.Count;
            for(int i = 1; i <= n; i++)
            {
                float angle = Vector3.SignedAngle(verts[i % n] - centroid, verts[i - 1] - centroid, normal);
                var sign = Mathf.Sign(angle);
                if (lastSign != 0 && sign != lastSign) return false;
                lastSign = sign;
            }
            return true;
        }

        #endregion

        #region methods

            public List<Vertex> GetVertices() {
                List<Vertex> vertices = new List<Vertex>();
                Halfedge edge = Halfedge;
                do {
                    vertices.Add(edge.Vertex); // add vertex to list
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);

                return vertices;
            }

            public List<Halfedge> GetHalfedges()
            {
                List<Halfedge> halfedges = new List<Halfedge>();
                Halfedge edge = Halfedge;
                do {
                    halfedges.Add(edge); // add halfedge to list
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);

                return halfedges;
            }

            public void Split(Vertex v1, Vertex v2, out Face f_new, out Halfedge he_new, out Halfedge he_new_pair) {

                Halfedge e1 = Halfedge;
                while (e1.Vertex != v1) {
                    e1 = e1.Next;
                }

                if (v2 == e1.Next.Vertex) {
                    throw new Exception("Vertices adjacent");
                }

                if (v2 == e1.Prev.Vertex) {
                    throw new Exception("Vertices adjacent");
                }

                f_new = new Face(e1.Next);

                Halfedge e2 = e1;
                while (e2.Vertex != v2) {
                    e2 = e2.Next;
                    e2.Face = f_new;
                }

                he_new = new Halfedge(v1, e1.Next, e2, f_new);
                he_new_pair = new Halfedge(v2, e2.Next, e1, this, he_new);
                he_new.Pair = he_new_pair;

                e1.Next.Prev = he_new;
                e1.Next = he_new_pair;
                e2.Next.Prev = he_new_pair;
                e2.Next = he_new;
            }

            public IEnumerable<Halfedge> NakedEdges()
            {
                return GetHalfedges().Where(i=>i.Pair==null);
            }

            public bool HasNakedEdge()
            {
                return GetHalfedges().Any(i=>i.Pair==null);
            }

        #endregion

        public PolyMesh DetachCopy()
        {

            IEnumerable<Vector3> verts = GetVertices().Select(i => i.Position);
            IEnumerable<IEnumerable<int>> faces = new List<List<int>>
            {Enumerable.Range(0, verts.Count()).ToList()};
            return new PolyMesh(verts, faces);
        }

        public Halfedge GetBestEdge()
        {
            // Useful for deciding on an orientation for the face
            // i.e. UV mapping etc.
            // Fairly arbitrary choice of "best"
            // I've gone with "So the edge that is at the top - of forwards if the face is flat"
            // The vector from the center to this edge midpoint
            // will at least always point in a consistent direction.
            // TODO "highest midpoint by y coord" is a fairly poor interpretation of edge direction
            // Should probably calculate a Vector2 angle based on one pair of possible coords
            var faceNormal = Normal;
            Halfedge bestEdge = null;
            float bestScore = -9999999;
            var list = GetHalfedges();
            // How nearly facing up or down are we?
            var upness = Mathf.Abs((new Vector3(Mathf.Abs(faceNormal.x), Mathf.Abs(faceNormal.y), Mathf.Abs(faceNormal.z)) - Vector3.up).magnitude);
            for (var j = 0; j < list.Count; j++)
            {
                var edge = list[j];
                var mid = edge.Midpoint;
                // Pick a desired direction. Up for most faces but "forwards" for nearly up or down faces
                var edgeCoord = (upness<.01f) ? mid.z : mid.y;
                // Add a bit of another vector as a "tie-break". Favour "left"
                edgeCoord += (-mid.x * .001f);
                if (edgeCoord > bestScore)
                {
                    bestScore = edgeCoord;
                    bestEdge = edge;
                }
            }

            return bestEdge;
        }

        public Halfedge GetHalfEdge(int index)
        {
            Halfedge edge = Halfedge;

            for (int i = 0; i < index % Sides; i++)
            {
                edge = edge.Next;
            }

            return edge;
        }
    }
}