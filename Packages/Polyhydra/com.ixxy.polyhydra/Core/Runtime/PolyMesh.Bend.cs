using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        /// <summary>
        /// Bends a sampled mesh in an explicit reference frame, without modifying the input.
        /// X bends Y towards +Z, Y bends X towards +Z, Z bends X towards -Y.
        /// Angles are signed degrees across the corresponding reference-bounds span.
        /// Supply the SAME original bounds for an ordered two-axis bend. This operation
        /// does not add samples; nonplanar quads are split along their 0-to-2 diagonal.
        /// Existing smoothing boundaries remain, with new internal diagonals smooth.
        /// </summary>
        public PolyMesh Bend(float angle, Axis axis, Bounds referenceBounds)
        {
            if (float.IsNaN(angle) || float.IsInfinity(angle)) throw new ArgumentOutOfRangeException(nameof(angle));
            Vector3 along, depth;
            switch (axis)
            {
                case Axis.X: along = Vector3.up; depth = Vector3.forward; break;
                case Axis.Y: along = Vector3.right; depth = Vector3.forward; break;
                case Axis.Z: along = Vector3.right; depth = Vector3.down; break;
                default: throw new ArgumentOutOfRangeException(nameof(axis));
            }
            var copy = Duplicate();
            copy.ScalingFactor = ScalingFactor;
            if (angle == 0f) return copy;
            var size = referenceBounds.size;
            var center = referenceBounds.center;
            for (var i = 0; i < 3; i++)
                if (float.IsNaN(center[i]) || float.IsInfinity(center[i]) ||
                    float.IsNaN(size[i]) || float.IsInfinity(size[i]) || size[i] < 0f)
                    throw new ArgumentException("Reference bounds must be finite with nonnegative dimensions.", nameof(referenceBounds));
            double span = Vector3.Dot(size, along);
            if (span <= 0.0) throw new ArgumentException("The bend reference span must be positive.", nameof(referenceBounds));
            double radians = angle * Math.PI / 180.0;
            foreach (var vertex in copy.Vertices)
            {
                var relative = vertex.Position - center;
                double u = Vector3.Dot(relative, along);
                double v = Vector3.Dot(relative, depth);
                double theta = radians * u / span;
                double sine = Math.Sin(theta);
                double cosine = Math.Cos(theta);
                // Stable near zero: avoid subtracting two very large bend radii.
                double halfSine = Math.Sin(theta * .5);
                double bentU = u * (theta == 0.0 ? 1.0 : sine / theta) - v * sine;
                double bentV = span / radians * 2.0 * halfSine * halfSine + v * cosine;
                vertex.Position += along * (float)(bentU - u) + depth * (float)(bentV - v);
            }
            return copy.SplitBentQuads();
        }

        private PolyMesh SplitBentQuads()
        {
            if (!Faces.Any(face =>
            {
                if (face.Sides != 4) return false;
                var points = face.GetVertices().Select(vertex => vertex.Position).ToArray();
                return IsNonplanarQuad(points[0], points[1], points[2], points[3]);
            })) return this;
            var vertexIds = Vertices.Select((vertex, index) => (vertex, index))
                .ToDictionary(item => item.vertex, item => item.index);
            var originalEdges = new Dictionary<(int, int), bool>();
            foreach (var edge in Halfedges)
            {
                var a = vertexIds[edge.Vertex];
                var b = vertexIds[edge.Prev.Vertex];
                originalEdges[a < b ? (a, b) : (b, a)] = edge.IsEdgeSmooth;
            }
            var faces = new List<List<int>>();
            var roles = new List<Roles>();
            var tags = new List<HashSet<string>>();
            for (var i = 0; i < Faces.Count; i++)
            {
                var points = Faces[i].GetVertices().Select(vertex => vertexIds[vertex]).ToList();
                if (points.Count == 4 && IsNonplanarQuad(Vertices[points[0]].Position,
                    Vertices[points[1]].Position, Vertices[points[2]].Position, Vertices[points[3]].Position))
                {
                    faces.Add(new List<int> { points[0], points[1], points[2] });
                    faces.Add(new List<int> { points[0], points[2], points[3] });
                    roles.Add(FaceRoles[i]);
                    tags.Add(new HashSet<string>(FaceTags[i]));
                }
                else faces.Add(points);
                roles.Add(FaceRoles[i]);
                tags.Add(new HashSet<string>(FaceTags[i]));
            }
            var result = new PolyMesh(ListVerticesByPoints(), faces, roles, VertexRoles, tags);
            result.ScalingFactor = ScalingFactor;
            var ids = result.Vertices.Select((vertex, index) => (vertex, index))
                .ToDictionary(item => item.vertex, item => item.index);
            foreach (var edge in result.Halfedges)
            {
                var a = ids[edge.Vertex];
                var b = ids[edge.Prev.Vertex];
                edge.IsSmooth = edge.Pair != null &&
                    (!originalEdges.TryGetValue(a < b ? (a, b) : (b, a), out var smooth) || smooth);
            }
            return result;
        }

        internal static bool IsNonplanarQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var ab = b - a;
            var ac = c - a;
            var ad = d - a;
            var normal = Vector3.Cross(ab, ac);
            var extent = Mathf.Max(ab.magnitude, ac.magnitude, ad.magnitude);
            return Mathf.Abs(Vector3.Dot(normal, ad)) > 1e-5f * normal.magnitude * extent;
        }
    }
}
