using System;
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

        // The angle between this edge and the next one
        public float Angle => Vector3.Angle(Vector, Next.Vector);

        // The angle between the faces shared by this edge. Naked edges return Infinity
        public float DihedralAngle => Pair != null ? Vector3.Angle(Face.Normal, Pair.Face.Normal) : Mathf.Infinity;

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