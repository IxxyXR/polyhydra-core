using System.Collections.Generic;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        /// <summary>
        /// Marks every paired edge whose face-normal angle is at most
        /// <paramref name="maxDihedralAngle"/> as smooth. Other edges are made hard.
        /// Boundary edges are always hard.
        /// </summary>
        public void AutoSmooth(float maxDihedralAngle)
        {
            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                edge.IsSmooth = edge.Pair != null && edge.DihedralAngle <= maxDihedralAngle;
            }
        }

        internal bool HasSmoothEdges()
        {
            for (var i = 0; i < Halfedges.Count; i++)
            {
                if (Halfedges[i].IsEdgeSmooth)
                {
                    return true;
                }
            }

            return false;
        }

        private void CopySmoothingTo(PolyMesh target)
        {
            var faceCount = Mathf.Min(Faces.Count, target.Faces.Count);
            for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
            {
                var sourceEdges = Faces[faceIndex].GetHalfedges();
                var targetEdges = target.Faces[faceIndex].GetHalfedges();
                if (sourceEdges.Count != targetEdges.Count)
                {
                    continue;
                }

                for (var edgeIndex = 0; edgeIndex < sourceEdges.Count; edgeIndex++)
                {
                    targetEdges[edgeIndex].IsSmooth = sourceEdges[edgeIndex].IsSmooth;
                }
            }
        }

        /// <summary>
        /// Calculates the render normal at the corner where a halfedge ends.
        /// Faces are included only when they are connected to this corner through
        /// smooth edges at the same vertex.
        /// </summary>
        internal Vector3 GetCornerNormal(Halfedge corner)
        {
            return GetCornerNormal(corner, null);
        }

        internal Vector3 GetCornerNormal(Halfedge corner, Dictionary<Halfedge, Vector3> cache)
        {
            if (corner == null || corner.Face == null)
            {
                return Vector3.zero;
            }

            if (cache != null && cache.TryGetValue(corner, out var cachedNormal))
            {
                return cachedNormal;
            }

            if (!corner.IsEdgeSmooth && (corner.Next == null || !corner.Next.IsEdgeSmooth))
            {
                var faceNormal = corner.Face.Normal;
                cache?.Add(corner, faceNormal);
                return faceNormal;
            }

            var pending = new Stack<Halfedge>();
            var visitedCorners = new HashSet<Halfedge>();
            var visitedFaces = new HashSet<Face>();
            pending.Push(corner);

            var normal = Vector3.zero;
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || current.Vertex != corner.Vertex || !visitedCorners.Add(current))
                {
                    continue;
                }

                if (current.Face != null && visitedFaces.Add(current.Face))
                {
                    normal += current.Face.Normal;
                }

                // The current halfedge ends at the corner vertex. Across that edge,
                // the equivalent corner is the preceding edge in the paired face.
                if (current.IsEdgeSmooth)
                {
                    pending.Push(current.Pair.Prev);
                }

                // The next halfedge starts at the corner vertex. Its pair ends at it.
                var outgoing = current.Next;
                if (outgoing != null && outgoing.IsEdgeSmooth)
                {
                    pending.Push(outgoing.Pair);
                }
            }

            var result = normal.sqrMagnitude > 0f ? normal.normalized : corner.Face.Normal;
            if (cache != null)
            {
                foreach (var visitedCorner in visitedCorners)
                {
                    cache[visitedCorner] = result;
                }
            }

            return result;
        }
    }
}
