using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public static partial class VariousSolids
    {
        /// <summary>
        /// A centred XY panel with depth along Z. Corner radius shapes the outline;
        /// edge radius shapes the front/back transitions independently. One segment
        /// produces a flat chamfer; more segments produce a rounded transition.
        /// Width/height segments sample the cap interior and straight wall regions.
        /// Use FaceRemove before deformation to omit a rear surface.
        /// </summary>
        public static PolyMesh Panel(float width = 1f, float height = 1f, float depth = .1f,
            float cornerRadius = 0f, float edgeRadius = 0f,
            RectangleCorners roundedCorners = RectangleCorners.All,
            int cornerSegments = 8, int edgeSegments = 8, int widthSegments = 1, int heightSegments = 1)
        {
            if (!(width > 0f) || float.IsInfinity(width)) throw new ArgumentOutOfRangeException(nameof(width));
            if (!(height > 0f) || float.IsInfinity(height)) throw new ArgumentOutOfRangeException(nameof(height));
            if (!(depth > 0f) || float.IsInfinity(depth)) throw new ArgumentOutOfRangeException(nameof(depth));
            if (!(cornerRadius >= 0f) || float.IsInfinity(cornerRadius)) throw new ArgumentOutOfRangeException(nameof(cornerRadius));
            if (!(edgeRadius >= 0f) || float.IsInfinity(edgeRadius)) throw new ArgumentOutOfRangeException(nameof(edgeRadius));
            if ((roundedCorners & ~RectangleCorners.All) != 0) throw new ArgumentOutOfRangeException(nameof(roundedCorners));
            cornerRadius = Mathf.Min(cornerRadius, Mathf.Min(width, height) * .5f);
            edgeRadius = Mathf.Min(edgeRadius, Mathf.Min(Mathf.Min(width, height), depth) * .5f);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, 128);
            edgeSegments = Mathf.Clamp(edgeSegments, 1, 128);
            widthSegments = Mathf.Max(1, widthSegments);
            heightSegments = Mathf.Max(1, heightSegments);
            var vertices = new List<Vector3>();
            var vertexIds = new Dictionary<Vector3, int>();
            var faces = new List<List<int>>();
            var roles = new List<Roles>();
            var hardEdges = new HashSet<(int, int)>();

            int Vertex(Vector3 point)
            {
                if (vertexIds.TryGetValue(point, out var index)) return index;
                index = vertices.Count;
                vertexIds.Add(point, index);
                vertices.Add(point);
                return index;
            }

            void Face(IReadOnlyList<int> points, Roles role, bool[] smooth = null)
            {
                var indices = new List<int>();
                foreach (var index in points)
                    if (indices.Count == 0 || indices[indices.Count - 1] != index) indices.Add(index);
                if (indices.Count > 1 && indices[0] == indices[indices.Count - 1]) indices.RemoveAt(indices.Count - 1);
                if (indices.Count < 3) return;
                var area = Vector3.zero;
                var origin = vertices[indices[0]];
                for (var i = 1; i < indices.Count - 1; i++)
                    area += Vector3.Cross(vertices[indices[i]] - origin, vertices[indices[i + 1]] - origin);
                if (area.sqrMagnitude == 0f) return;
                faces.Add(indices);
                roles.Add(role);
                if (smooth == null) return;
                for (var i = 0; i < points.Count; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Count];
                    if (a != b && !smooth[i]) hardEdges.Add(a < b ? (a, b) : (b, a));
                }
            }

            PanelContour Contour(float inset) => MakePanelContour(Mathf.Max(0f, width - 2f * inset),
                Mathf.Max(0f, height - 2f * inset), Mathf.Max(0f, cornerRadius - inset),
                roundedCorners, cornerSegments, widthSegments, heightSegments);
            var outside = Contour(0f);
            var sections = new List<(float inset, float z)>();
            if (edgeRadius == 0f)
            {
                sections.Add((0f, -depth * .5f));
                sections.Add((0f, depth * .5f));
            }
            else
            {
                for (var i = edgeSegments; i >= 0; i--)
                {
                    var angle = i * Mathf.PI * .5f / edgeSegments;
                    var inset = i == edgeSegments ? edgeRadius : edgeRadius * (1f - Mathf.Cos(angle));
                    var z = i == edgeSegments ? depth * .5f : depth * .5f - edgeRadius + edgeRadius * Mathf.Sin(angle);
                    sections.Add((inset, -z));
                }
                for (var i = 0; i <= edgeSegments; i++)
                {
                    var angle = i * Mathf.PI * .5f / edgeSegments;
                    var inset = i == edgeSegments ? edgeRadius : edgeRadius * (1f - Mathf.Cos(angle));
                    var z = i == edgeSegments ? depth * .5f : depth * .5f - edgeRadius + edgeRadius * Mathf.Sin(angle);
                    sections.Add((inset, z));
                }
            }
            var rings = sections.Select(section => Contour(section.inset).Points
                .Select(point => Vertex(new Vector3(point.x, point.y, section.z))).ToArray()).ToList();
            bool smoothDepth = edgeRadius > 0f && edgeSegments > 1;
            for (var layer = 0; layer < rings.Count - 1; layer++)
            {
                var lower = rings[layer];
                var upper = rings[layer + 1];
                for (var i = 0; i < lower.Length; i++)
                {
                    var next = (i + 1) % lower.Length;
                    Face(new[] { lower[i], lower[next], upper[next], upper[i] }, Roles.New,
                        new[] { smoothDepth, outside.Smooth[next], smoothDepth, outside.Smooth[i] });
                }
            }

            void Cap(bool front)
            {
                var contour = Contour(edgeRadius);
                var z = (front ? 1f : -1f) * depth * .5f;
                int Point(float x, float y) => Vertex(new Vector3(x, y, z));
                void Patch(params Vector2[] points)
                {
                    var indices = points.Select(point => Point(point.x, point.y)).ToArray();
                    if (!front) Array.Reverse(indices);
                    Face(indices, Roles.Existing);
                }
                float X(int i) => PanelCoordinate(contour.Inner.x, i, widthSegments);
                float Y(int i) => PanelCoordinate(contour.Inner.y, i, heightSegments);
                for (var x = 0; x < widthSegments; x++)
                for (var y = 0; y < heightSegments; y++)
                    Patch(new Vector2(X(x), Y(y)), new Vector2(X(x + 1), Y(y)),
                        new Vector2(X(x + 1), Y(y + 1)), new Vector2(X(x), Y(y + 1)));
                for (var x = 0; x < widthSegments; x++)
                {
                    Patch(new Vector2(X(x), contour.Inner.y), new Vector2(X(x + 1), contour.Inner.y),
                        new Vector2(X(x + 1), contour.Half.y), new Vector2(X(x), contour.Half.y));
                    Patch(new Vector2(X(x), -contour.Half.y), new Vector2(X(x + 1), -contour.Half.y),
                        new Vector2(X(x + 1), -contour.Inner.y), new Vector2(X(x), -contour.Inner.y));
                }
                for (var y = 0; y < heightSegments; y++)
                {
                    Patch(new Vector2(-contour.Half.x, Y(y)), new Vector2(-contour.Inner.x, Y(y)),
                        new Vector2(-contour.Inner.x, Y(y + 1)), new Vector2(-contour.Half.x, Y(y + 1)));
                    Patch(new Vector2(contour.Inner.x, Y(y)), new Vector2(contour.Half.x, Y(y)),
                        new Vector2(contour.Half.x, Y(y + 1)), new Vector2(contour.Inner.x, Y(y + 1)));
                }
                for (var corner = 0; corner < 4; corner++)
                {
                    var center = PanelCorner(contour.Inner, corner);
                    var points = contour.Corners[corner];
                    for (var i = 0; i < points.Count - 1; i++) Patch(center, points[i], points[i + 1]);
                }
            }
            Cap(false);
            Cap(true);
            // Collapsed cap regions can have no faces. Retain only referenced vertices.
            var remap = new Dictionary<int, int>();
            var used = new List<Vector3>();
            foreach (var face in faces)
            for (var i = 0; i < face.Count; i++)
            {
                var old = face[i];
                if (!remap.TryGetValue(old, out var mapped))
                {
                    mapped = used.Count;
                    used.Add(vertices[old]);
                    remap.Add(old, mapped);
                }
                face[i] = mapped;
            }
            var mappedHardEdges = new HashSet<(int, int)>();
            foreach (var edge in hardEdges)
                if (remap.TryGetValue(edge.Item1, out var a) && remap.TryGetValue(edge.Item2, out var b))
                    mappedHardEdges.Add(a < b ? (a, b) : (b, a));
            var mesh = new PolyMesh(used, faces, roles, Enumerable.Repeat(Roles.New, used.Count));
            var ids = mesh.Vertices.Select((vertex, index) => (vertex, index)).ToDictionary(item => item.vertex, item => item.index);
            foreach (var edge in mesh.Halfedges)
            {
                var a = ids[edge.Vertex];
                var b = ids[edge.Prev.Vertex];
                edge.IsSmooth = edge.Pair != null && !mappedHardEdges.Contains(a < b ? (a, b) : (b, a));
            }
            return mesh;
        }

        private sealed class PanelContour
        {
            public Vector2 Half;
            public Vector2 Inner;
            public readonly List<Vector2> Points = new List<Vector2>();
            public readonly List<bool> Smooth = new List<bool>();
            public readonly List<Vector2>[] Corners = new List<Vector2>[4];
        }

        private static float PanelCoordinate(float extent, int index, int segments) =>
            index == 0 ? -extent : index == segments ? extent : -extent + 2f * extent * index / segments;

        private static Vector2 PanelCorner(Vector2 half, int corner) => new Vector2(
            (corner == 0 || corner == 3 ? 1f : -1f) * half.x,
            (corner < 2 ? 1f : -1f) * half.y);

        private static PanelContour MakePanelContour(float width, float height, float radius,
            RectangleCorners roundedCorners, int cornerSegments, int widthSegments, int heightSegments)
        {
            radius = Mathf.Min(radius, Mathf.Min(width, height) * .5f);
            var contour = new PanelContour
            {
                Half = new Vector2(width, height) * .5f,
                Inner = new Vector2(width * .5f - radius, height * .5f - radius)
            };
            // Reuse Rectangle's corner arcs; only add the straight-edge samples here.
            var raw = Shapes.RectangleContour(width, height, radius, cornerSegments, true, roundedCorners);
            raw.Reverse();
            for (var corner = 0; corner < 4; corner++)
            {
                bool rounded = ((int)roundedCorners & (1 << corner)) != 0;
                var points = new List<Vector2>();
                var smooth = new List<bool>();
                if (rounded)
                {
                    for (var sample = 0; sample <= cornerSegments; sample++)
                    {
                        var point = raw[corner * (cornerSegments + 1) + sample];
                        points.Add(new Vector2(point.x, point.z));
                        smooth.Add(radius > 0f && cornerSegments > 1);
                    }
                }
                else
                {
                    var center = PanelCorner(contour.Inner, corner);
                    var outer = PanelCorner(contour.Half, corner);
                    points.Add(corner % 2 == 0 ? new Vector2(outer.x, center.y) : new Vector2(center.x, outer.y));
                    points.Add(outer);
                    points.Add(corner % 2 == 0 ? new Vector2(center.x, outer.y) : new Vector2(outer.x, center.y));
                    smooth.Add(radius > 0f);
                    smooth.Add(false);
                    smooth.Add(radius > 0f);
                }
                contour.Corners[corner] = points;
                contour.Points.AddRange(points);
                contour.Smooth.AddRange(smooth);
                var segments = corner % 2 == 0 ? widthSegments : heightSegments;
                for (var i = 1; i < segments; i++)
                {
                    var point = corner == 0 ? new Vector2(PanelCoordinate(contour.Inner.x, segments - i, segments), contour.Half.y) :
                        corner == 1 ? new Vector2(-contour.Half.x, PanelCoordinate(contour.Inner.y, segments - i, segments)) :
                        corner == 2 ? new Vector2(PanelCoordinate(contour.Inner.x, i, segments), -contour.Half.y) :
                        new Vector2(contour.Half.x, PanelCoordinate(contour.Inner.y, i, segments));
                    contour.Points.Add(point);
                    contour.Smooth.Add(true);
                }
            }
            return contour;
        }
    }
}
