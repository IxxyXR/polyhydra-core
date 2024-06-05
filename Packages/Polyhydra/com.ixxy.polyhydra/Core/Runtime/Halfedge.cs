using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Polyhydra.Core {

    public class Halfedge {

        #region constructors

            public Halfedge(Vertex vertex) {
                Vertex = vertex;
            }

            public Halfedge(Vertex vertex, Halfedge next, Halfedge prev, Face face) : this(vertex) {
                Next = next;
                Prev = prev;
                Face = face;
            }

            public Halfedge(Vertex vertex, Halfedge next, Halfedge prev, Face face, Halfedge pair)  : this(vertex, next, prev, face) {
                Pair = pair;
            }

        #endregion

        #region properties

        public Halfedge Next { get; set; }
        public Halfedge Prev { get; set; }
        public Halfedge Pair { get; set; }
        public Vertex Vertex { get; private set; }
        public Face Face { get; set; }

        public (Guid, Guid)? Name {
            get
            {
                (Guid, Guid)? name;
                if (Vertex == null || Prev == null || Prev.Vertex == null)
                {
                    name = null;
                }
                else
                {
                    name = (Vertex.Name, Prev.Vertex.Name);
                }
                return name;
            }
        }

        public (Guid, Guid)? PairedName {
            // A unique name for the half-edge pair
            // A half-edge will have the same PairedName as it's pair
            get
            {
                (Guid, Guid)? pairedName;
                // Return a joining of the names with consistent ordering
                if (Vertex.Name.CompareTo(Prev.Vertex.Name) > 0)
                {
                    pairedName = (Prev.Vertex.Name, Vertex.Name);
                }
                else
                {
                    pairedName = (Vertex.Name, Prev.Vertex.Name);
                }
                return pairedName;
            }
        }

        public Vector3 Midpoint => Vertex.Position + -0.5f * (Vertex.Position - Prev.Vertex.Position);

        public Vector3 OneThirdPoint => Vertex.Position + -0.333333f * (Vertex.Position - Prev.Vertex.Position);

        public Vector3 TwoThirdsPoint => Vertex.Position + -0.6666666f * (Vertex.Position - Prev.Vertex.Position);

        public Vector3 Vector => Vertex.Position - Prev.Vertex.Position;

        public Vector3 Tangent => Vector3.Cross(Face.Normal, Vector).normalized;

        // Opposite based purely on index
        public Halfedge OppositeByIndex
        {
            get
            {
                var edges = Face.GetHalfedges();
                var index = edges.IndexOf(this);
                int oppositeIndex = index + (edges.Count / 2);
                return edges[oppositeIndex % edges.Count];
            }
        }

        // Opposite based on angle
        public Halfedge OppositeByTangent => Face.GetHalfedges().OrderBy(i => Vector3.Angle(i.Tangent, Tangent)).First();

        // A weighted blend of both angle and index
        // Not tested but written with the help of ChatGPT so I thought I'd keep it for reference
        public Halfedge GuessOpposite
        {
            get
            {
                var halfEdges = Face.GetHalfedges().ToList();
                int numberOfEdges = halfEdges.Count;
                int midpointIndex = numberOfEdges / 2; // Determine the midpoint index

                // Calculate scores for each edge
                var scoredEdges = halfEdges.Select((edge, index) =>
                    {
                        float angleDifference = Vector3.Angle(edge.Tangent, this.Tangent);
                        // Calculate the index's distance to the midpoint, normalized by the total number of edges
                        float indexDistanceToMidpoint = Math.Abs(index - midpointIndex) / (float)numberOfEdges;

                        // Combine the angle and index distance into a single score
                        // Here, you might need to adjust weights based on how you want to balance these factors
                        float score = angleDifference + indexDistanceToMidpoint * 100; // Assuming angle is the primary factor, adjust multiplier for index distance as needed

                        return new { Edge = edge, Score = score };
                    })
                    .OrderBy(x => x.Score) // Order by score, lowest first
                    .ToList();

                return scoredEdges.First().Edge; // Select the edge with the lowest score
            }
        }

        // The angle between this edge and the next one
        public float Angle => 180f - Vector3.Angle(Vector, Next.Vector);

        // The angle between the faces shared by this edge. Naked edges return Infinity
        public float DihedralAngle => Pair != null ? Vector3.Angle(Face.Normal, Pair.Face.Normal) : Mathf.Infinity;

        public FaceLoop FaceLoop => FaceLoop.FromHalfEdge(this);
        private Dictionary<(Guid, Guid), FaceLoop> _cachedFaceLoops = new ();
        public FaceLoop CachedFaceLoop
        {
            get
            {
                if (!Name.HasValue) return null;
                if (_cachedFaceLoops.ContainsKey(Name.Value))
                {
                    return _cachedFaceLoops[Name.Value];
                }
                var faceLoop = FaceLoop.FromHalfEdge(this);
                _cachedFaceLoops[Name.Value] = faceLoop;
                return faceLoop;
            }
        }

        public Vector3 PointAlongEdge(float n)
        {
            return Vertex.Position + -n * (Vertex.Position - Prev.Vertex.Position);
        }

        #endregion

        public void SetVertex(Vertex newVertex)
        {
            Vertex = newVertex;
        }
    }
}