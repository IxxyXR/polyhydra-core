using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        /// <summary>
        /// Replaces every edge of a closed, convex, trivalent mesh with a segmented quadratic profile.
        /// This intentionally narrow first version provides deterministic manifold topology while more
        /// general edge selection and high-valence corner miters are developed.
        /// </summary>
        public PolyMesh FilletEdges(OpParams o)
        {
            var width = Mathf.Clamp(o.OriginalParamA, .0001f, .4999f);
            var segments = Mathf.Clamp(Mathf.RoundToInt(o.OriginalParamB), 1, 16);
            if (Halfedges.Any(edge => edge.Pair == null) ||
                Vertices.Any(vertex => vertex.GetVertexFaces().Count != 3))
            {
                Debug.LogWarning("[PolyhydraFilletEdges] FilletEdges currently requires a closed, trivalent mesh.");
                return Duplicate();
            }

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<List<int>>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();
            var insetIndices = new Dictionary<(Face face, Vertex vertex), int>();
            var cornerArcs = Vertices.ToDictionary(vertex => vertex, vertex => new List<List<int>>());

            int AddVertex(Vector3 point)
            {
                vertexPoints.Add(point);
                vertexRoles.Add(Roles.New);
                return vertexPoints.Count - 1;
            }

            void AddFace(IEnumerable<int> source, Vector3 expectedNormal, Roles role)
            {
                var face = source.ToList();
                if (face.Count < 3) return;
                var a = vertexPoints[face[0]];
                var b = vertexPoints[face[1]];
                var c = vertexPoints[face[2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - b), expectedNormal) < 0f) face.Reverse();
                faceIndices.Add(face);
                faceRoles.Add(role);
            }

            foreach (var face in Faces)
            {
                var insetFace = new List<int>();
                foreach (var edge in face.GetHalfedges())
                {
                    var point = edge.Vertex.Position + (face.Centroid - edge.Vertex.Position) * width;
                    var index = AddVertex(point);
                    insetIndices[(face, edge.Vertex)] = index;
                    insetFace.Add(index);
                }
                AddFace(insetFace, face.Normal, Roles.Existing);
            }

            var visitedEdges = new HashSet<(System.Guid, System.Guid)?>();
            foreach (var edge in Halfedges)
            {
                if (!visitedEdges.Add(edge.PairedName)) continue;

                var start = edge.Prev.Vertex;
                var end = edge.Vertex;
                var startCurve = new List<int>();
                var endCurve = new List<int>();
                for (var segment = 0; segment <= segments; segment++)
                {
                    var t = (float)segment / segments;
                    var oneMinusT = 1f - t;

                    int CurveVertex(Vertex vertex)
                    {
                        if (segment == 0) return insetIndices[(edge.Face, vertex)];
                        if (segment == segments) return insetIndices[(edge.Pair.Face, vertex)];
                        var a = vertexPoints[insetIndices[(edge.Face, vertex)]];
                        var b = vertex.Position;
                        var c = vertexPoints[insetIndices[(edge.Pair.Face, vertex)]];
                        return AddVertex(oneMinusT * oneMinusT * a +
                                         2f * oneMinusT * t * b + t * t * c);
                    }

                    startCurve.Add(CurveVertex(start));
                    endCurve.Add(CurveVertex(end));
                }

                var edgeNormal = (edge.Face.Normal + edge.Pair.Face.Normal).normalized;
                for (var segment = 0; segment < segments; segment++)
                {
                    AddFace(new[]
                    {
                        startCurve[segment], endCurve[segment],
                        endCurve[segment + 1], startCurve[segment + 1]
                    }, edgeNormal, Roles.New);
                }

                cornerArcs[start].Add(startCurve);
                cornerArcs[end].Add(endCurve);
            }

            foreach (var vertex in Vertices)
            {
                var unusedArcs = new List<List<int>>(cornerArcs[vertex]);
                var boundary = new List<int>(unusedArcs[0]);
                unusedArcs.RemoveAt(0);
                while (unusedArcs.Count > 0)
                {
                    var last = boundary[boundary.Count - 1];
                    var nextIndex = unusedArcs.FindIndex(arc => arc[0] == last || arc[arc.Count - 1] == last);
                    if (nextIndex < 0)
                    {
                        Debug.LogWarning("[PolyhydraFilletEdges] Could not order a corner boundary.");
                        return Duplicate();
                    }
                    var next = unusedArcs[nextIndex];
                    unusedArcs.RemoveAt(nextIndex);
                    if (next[next.Count - 1] == last) next.Reverse();
                    boundary.AddRange(next.Skip(1));
                }
                if (boundary[boundary.Count - 1] == boundary[0]) boundary.RemoveAt(boundary.Count - 1);

                var center = AddVertex(vertex.Position);
                for (var index = 0; index < boundary.Count; index++)
                {
                    AddFace(new[] { center, boundary[index], boundary[(index + 1) % boundary.Count] },
                        vertex.Normal, Roles.NewAlt);
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles);
        }
    }
}
