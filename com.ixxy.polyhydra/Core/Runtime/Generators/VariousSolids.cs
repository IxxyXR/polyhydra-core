using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public enum VariousSolidTypes
    {
        UvSphere = 0,
        UvHemisphere = 1,
        Box = 2,
        Stairs = 3,
        Torus = 4,
        StarTorus = 5,
        Capsule = 6,
        ChamferedBox = 7,
        HollowHemisphere = 8,
        ChamferedCylinder = 9,
        PartialTorus = 10,
        WireframeBox = 11
    }

    public static class VariousSolids
    {

        private static PolyMesh BuildMesh(List<Vector3> vertices, List<List<int>> faces)
        {
            var faceRoles = faces
                .Select((face, index) => index % 2 == 0 ? Roles.New : Roles.NewAlt)
                .ToList();
            var vertexRoles = Enumerable.Repeat(Roles.Existing, vertices.Count);
            return new PolyMesh(vertices, faces, faceRoles, vertexRoles);
        }

        private static PolyMesh Lathe(IReadOnlyList<Vector2> profile, int sides, bool closeProfile = false)
        {
            sides = Mathf.Max(3, sides);
            var vertices = new List<Vector3>();
            var rows = new List<List<int>>();

            foreach (var point in profile)
            {
                var row = new List<int>();
                if (Mathf.Approximately(point.x, 0f))
                {
                    row.Add(vertices.Count);
                    vertices.Add(new Vector3(0f, point.y, 0f));
                }
                else
                {
                    for (var side = 0; side < sides; side++)
                    {
                        var angle = Mathf.PI * 2f * side / sides;
                        row.Add(vertices.Count);
                        vertices.Add(new Vector3(
                            Mathf.Cos(angle) * point.x,
                            point.y,
                            Mathf.Sin(angle) * point.x
                        ));
                    }
                }
                rows.Add(row);
            }

            var faces = new List<List<int>>();
            var transitionCount = closeProfile ? rows.Count : rows.Count - 1;
            for (var rowIndex = 0; rowIndex < transitionCount; rowIndex++)
            {
                var lower = rows[rowIndex];
                var upper = rows[(rowIndex + 1) % rows.Count];
                if (lower.Count == 1 && upper.Count == 1) continue;
                for (var side = 0; side < sides; side++)
                {
                    var next = (side + 1) % sides;
                    if (lower.Count == 1)
                    {
                        faces.Add(new List<int> { lower[0], upper[side], upper[next] });
                    }
                    else if (upper.Count == 1)
                    {
                        faces.Add(new List<int> { lower[side], upper[0], lower[next] });
                    }
                    else
                    {
                        faces.Add(new List<int>
                        {
                            lower[side], upper[side], upper[next], lower[next]
                        });
                    }
                }
            }

            return BuildMesh(vertices, faces);
        }

        /// <summary>
        /// Creates a capsule with optional signed sweep in degrees, positive toward +Z.
        /// Existing full-sweep calls retain their original lathe construction.
        /// Optional smoothing blends the entire capsule surface, keeping sweep-cut faces sharp.
        /// </summary>
        public static PolyMesh Capsule(int sides = 24, int capSegments = 8, float cylinderHeight = 2f,
            float radius = 1f, float sweepAngle = 360f,
            PolyMesh.ZeroSweepBehavior zeroSweepBehavior = PolyMesh.ZeroSweepBehavior.ThinWedge,
            bool smooth = false)
        {
            sides = Mathf.Max(3, sides);
            var profile = Shapes.CapsuleProfilePoints(capSegments, cylinderHeight, radius);
            sweepAngle = Mathf.Clamp(sweepAngle, -360f, 360f);
            var result = sweepAngle == 360f
                ? Lathe(profile, sides)
                : Shapes.ProfilePolygon(profile).Revolve(new OpParams(-sweepAngle / 360f, sides),
                    Axis.Y, zeroSweepBehavior);
            if (smooth)
            {
                // Revolve labels sweep-cut faces Existing; Lathe contains only surface faces.
                var surfaceFaces = new HashSet<Face>(result.Faces.Where((face, index) =>
                    result.FaceRoles[index] != Roles.Existing));
                foreach (var edge in result.Halfedges)
                    edge.IsSmooth = edge.Pair != null && surfaceFaces.Contains(edge.Face) &&
                                    surfaceFaces.Contains(edge.Pair.Face);
            }
            return result;
        }

        /// <summary>
        /// Creates a cylinder, tapered cylinder or cone about Y, centred on its height.
        /// Each end has an independent X radius and aspect ratio (Z radius / X radius).
        /// The top is rotated about its own centre before applying its XZ offset.
        /// Positive sweep runs from +X toward +Z.
        /// Sides counts strips across the entire sweep. Partial sweeps have radial cuts.
        /// Smooth sides blend neighbouring wall strips, keeping both end caps and cuts sharp.
        /// Planar strips remain quads. Nonplanar strips are triangulated, with diagonals
        /// from bottom i to top i+1 when smooth, or top i to bottom i+1 when faceted.
        /// Triangular strips always smooth across their internal diagonal.
        /// Zero sweep or two zero radii produce an empty mesh. Aspect ratios must be positive.
        /// Arbitrary end transforms may produce self-intersections.
        /// </summary>
        public static PolyMesh Cylinder(int sides = 24, float height = 1f,
            float radiusBottom = .5f, float aspectBottom = 1f,
            float radiusTop = .5f, float aspectTop = 1f, float sweepAngle = 360f,
            Vector2? topOffset = null, Quaternion? topRotation = null,
            bool smoothSides = false)
        {
            if (height <= 0f) throw new System.ArgumentOutOfRangeException(nameof(height));
            if (radiusBottom < 0f) throw new System.ArgumentOutOfRangeException(nameof(radiusBottom));
            if (radiusTop < 0f) throw new System.ArgumentOutOfRangeException(nameof(radiusTop));
            if (aspectBottom <= 0f) throw new System.ArgumentOutOfRangeException(nameof(aspectBottom));
            if (aspectTop <= 0f) throw new System.ArgumentOutOfRangeException(nameof(aspectTop));
            sweepAngle = Mathf.Clamp(sweepAngle, -360f, 360f);
            if (sweepAngle == 0f || (radiusBottom == 0f && radiusTop == 0f)) return new PolyMesh();
            var offset = topOffset ?? Vector2.zero;
            return SweepSections(new[]
            {
                new SweepSection(radiusBottom, Vector3.down * height * .5f, new Vector2(1f, aspectBottom), Quaternion.identity),
                new SweepSection(radiusTop, new Vector3(offset.x, height * .5f, offset.y), new Vector2(1f, aspectTop),
                    topRotation ?? Quaternion.identity)
            }, sides, sweepAngle, smoothSides);
        }

        /// <summary>
        /// Creates a conic solid about Y from a sampled circle, ellipse, hyperbola or parabola.
        /// Positive sweep runs from +X toward +Z, matching Cylinder. Smooth blends only the
        /// curved body; end caps and sweep cuts remain sharp. Planar wall strips remain quads.
        /// Signed parabola radii are retained and can produce self-intersections.
        /// Zero sweep uses the same explicit policy as Capsule and Revolve.
        /// </summary>
        public static PolyMesh Conic(Shapes.ConicType type, int sides = 24, int verticalSegments = 16,
            float radius = 1f, float majorAxis = 1f, float minorAxis = 1f, float centerOffset = .2f,
            float bottomClip = 0f, float topClip = 1f, float sweepAngle = 360f,
            bool smooth = false,
            PolyMesh.ZeroSweepBehavior zeroSweepBehavior = PolyMesh.ZeroSweepBehavior.ThinWedge)
        {
            var profile = Shapes.ConicProfilePoints(type, verticalSegments, radius, majorAxis, minorAxis,
                centerOffset, bottomClip, topClip);
            sweepAngle = Mathf.Clamp(sweepAngle, -360f, 360f);
            if (sweepAngle == 0f)
            {
                if (zeroSweepBehavior != PolyMesh.ZeroSweepBehavior.ThinWedge)
                    return Shapes.ProfilePolygon(profile).Revolve(new OpParams(0f, sides), Axis.Y,
                        zeroSweepBehavior);
                sweepAngle = .036f;
            }
            var sections = profile.Select(point => new SweepSection(point.x,
                new Vector3(0f, point.y, 0f), Vector2.one, Quaternion.identity)).ToList();
            return SweepSections(sections, sides, sweepAngle, smooth);
        }

        private readonly struct SweepSection
        {
            public readonly float Radius;
            public readonly Vector3 Centre;
            public readonly Vector2 Scale;
            public readonly Quaternion Rotation;

            public SweepSection(float radius, Vector3 centre, Vector2 scale, Quaternion rotation)
            {
                Radius = radius;
                Centre = centre;
                Scale = scale;
                Rotation = rotation;
            }
        }

        // The supplied sections run from bottom to top. Construction owns the distinction
        // between the body, end caps and sweep cuts; no intent is stored on input boundaries.
        private static PolyMesh SweepSections(IReadOnlyList<SweepSection> sections, int sides,
            float sweepAngle, bool smoothBody)
        {
            sides = Mathf.Max(3, sides);
            var closed = Mathf.Abs(sweepAngle) == 360f;
            var ringCount = closed ? sides : sides + 1;
            var vertices = new List<Vector3>();
            var rings = new List<List<int>>();
            foreach (var section in sections)
            {
                var ring = new List<int>();
                if (section.Radius == 0f)
                {
                    var apex = vertices.Count;
                    vertices.Add(section.Centre);
                    ring.AddRange(Enumerable.Repeat(apex, ringCount));
                }
                else
                {
                    for (var index = 0; index < ringCount; index++)
                    {
                        var angle = sweepAngle * Mathf.Deg2Rad * index / sides;
                        var point = new Vector3(Mathf.Cos(angle) * section.Radius * section.Scale.x, 0f,
                            Mathf.Sin(angle) * section.Radius * section.Scale.y);
                        ring.Add(vertices.Count);
                        vertices.Add(section.Centre + section.Rotation * point);
                    }
                }
                rings.Add(ring);
            }

            var faces = new List<List<int>>();
            var roles = new List<Roles>();
            var faceStrips = new List<int>();
            void AddFace(IEnumerable<int> points, Roles role, int strip = -1)
            {
                var indices = new List<int>();
                foreach (var index in points)
                    if (indices.Count == 0 || indices[indices.Count - 1] != index) indices.Add(index);
                if (indices.Count > 1 && indices[0] == indices[indices.Count - 1])
                    indices.RemoveAt(indices.Count - 1);
                if (indices.Count < 3) return;
                if (sweepAngle < 0f) indices.Reverse();
                faces.Add(indices);
                roles.Add(role);
                faceStrips.Add(strip);
            }

            for (var row = 0; row < rings.Count - 1; row++)
            {
                var lower = rings[row];
                var upper = rings[row + 1];
                for (var index = 0; index < sides; index++)
                {
                    var next = (index + 1) % ringCount;
                    var role = (row + index) % 2 == 0 ? Roles.New : Roles.NewAlt;
                    var strip = row * sides + index;
                    bool nonplanar = sections[row].Radius != 0f && sections[row + 1].Radius != 0f &&
                        IsNonplanarQuad(vertices[lower[index]], vertices[upper[index]],
                            vertices[upper[next]], vertices[lower[next]]);
                    if (!nonplanar)
                        AddFace(new[] { lower[index], upper[index], upper[next], lower[next] }, role, strip);
                    else if (smoothBody)
                    {
                        AddFace(new[] { lower[index], upper[index], upper[next] }, role, strip);
                        AddFace(new[] { lower[index], upper[next], lower[next] }, role, strip);
                    }
                    else
                    {
                        AddFace(new[] { lower[index], upper[index], lower[next] }, role, strip);
                        AddFace(new[] { upper[index], upper[next], lower[next] }, role, strip);
                    }
                }
            }
            var bodyFaceCount = faces.Count;
            var bottom = rings[0];
            var top = rings[rings.Count - 1];
            if (closed)
            {
                AddFace(bottom, Roles.Existing);
                AddFace(top.AsEnumerable().Reverse(), Roles.Existing);
            }
            else
            {
                int Centre(int row)
                {
                    if (sections[row].Radius == 0f) return rings[row][0];
                    var index = vertices.Count;
                    vertices.Add(sections[row].Centre);
                    return index;
                }
                var bottomCentre = Centre(0);
                var topCentre = Centre(rings.Count - 1);
                AddFace(bottom.Concat(new[] { bottomCentre }), Roles.Existing);
                AddFace(top.Concat(new[] { topCentre }).Reverse(), Roles.Existing);
                AddFace(new[] { bottomCentre, topCentre }.Concat(rings.AsEnumerable().Reverse()
                    .Select(ring => ring[0])), Roles.Existing);
                AddFace(rings.Select(ring => ring[ringCount - 1])
                    .Concat(new[] { topCentre, bottomCentre }), Roles.Existing);
            }
            var mesh = new PolyMesh(vertices, faces, roles, Enumerable.Repeat(Roles.Existing, vertices.Count));
            var bodyStrips = mesh.Faces.Take(bodyFaceCount)
                .Select((face, index) => (face, strip: faceStrips[index]))
                .ToDictionary(entry => entry.face, entry => entry.strip);
            foreach (var edge in mesh.Halfedges)
                edge.IsSmooth = edge.Pair != null && bodyStrips.TryGetValue(edge.Face, out var strip) &&
                    bodyStrips.TryGetValue(edge.Pair.Face, out var pairedStrip) &&
                    (smoothBody || strip == pairedStrip);
            return mesh;
        }

        private static bool IsNonplanarQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var ab = b - a;
            var ac = c - a;
            var ad = d - a;
            var normal = Vector3.Cross(ab, ac);
            // Relative plane-distance tolerance avoids splitting planar faces merely
            // because their coordinates have accumulated floating-point rounding.
            var extent = Mathf.Max(ab.magnitude, ac.magnitude, ad.magnitude);
            return Mathf.Abs(Vector3.Dot(normal, ad)) > 1e-5f * normal.magnitude * extent;
        }

        public static PolyMesh ChamferedBox(int profileSegments = 1, float profile = 1f,
            float chamfer = .2f)
        {
            profileSegments = Mathf.Max(1, profileSegments);
            profile = Mathf.Clamp(profile, 1f, 8f);
            chamfer = Mathf.Clamp(chamfer, .0001f, .4999f);
            var inner = 1f - chamfer;
            var vertices = new List<Vector3>();
            var faces = new List<List<int>>();
            var indices = new Dictionary<(int x, int y, int z), int>();

            int VertexIndex(Vector3 point)
            {
                const float precision = 1000000f;
                (int x, int y, int z) key = (
                    Mathf.RoundToInt(point.x * precision),
                    Mathf.RoundToInt(point.y * precision),
                    Mathf.RoundToInt(point.z * precision)
                );
                if (!indices.TryGetValue(key, out var index))
                {
                    index = vertices.Count;
                    indices.Add(key, index);
                    vertices.Add(new Vector3(key.x / precision, key.y / precision, key.z / precision));
                }
                return index;
            }

            void AddOutwardFace(params Vector3[] points)
            {
                var face = points.Select(VertexIndex).ToArray();
                var a = vertices[face[0]];
                var b = vertices[face[1]];
                var c = vertices[face[2]];
                var normal = Vector3.Cross(b - a, c - b);
                var centroid = face.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) / face.Length;
                if (Vector3.Dot(normal, centroid) < 0f) face = face.Reverse().ToArray();
                faces.Add(face.ToList());
            }

            Vector3 ProfilePoint(float x, float y, float z)
            {
                var inverseProfile = 1f / profile;
                var length = Mathf.Pow(
                    Mathf.Pow(Mathf.Abs(x), profile) +
                    Mathf.Pow(Mathf.Abs(y), profile) +
                    Mathf.Pow(Mathf.Abs(z), profile),
                    inverseProfile
                );
                return length > 0f ? new Vector3(x, y, z) / length : Vector3.zero;
            }

            Vector3 EdgePoint(int axisA, int signA, int axisB, int signB, int axisC,
                float axisCPosition, Vector3 profilePoint)
            {
                var point = Vector3.zero;
                point[axisA] = signA * (inner + chamfer * profilePoint.x);
                point[axisB] = signB * (inner + chamfer * profilePoint.y);
                point[axisC] = axisCPosition;
                return point;
            }

            for (var sign = -1; sign <= 1; sign += 2)
            {
                AddOutwardFace(
                    new Vector3(sign, -inner, -inner), new Vector3(sign, inner, -inner),
                    new Vector3(sign, inner, inner), new Vector3(sign, -inner, inner));
                AddOutwardFace(
                    new Vector3(-inner, sign, -inner), new Vector3(inner, sign, -inner),
                    new Vector3(inner, sign, inner), new Vector3(-inner, sign, inner));
                AddOutwardFace(
                    new Vector3(-inner, -inner, sign), new Vector3(inner, -inner, sign),
                    new Vector3(inner, inner, sign), new Vector3(-inner, inner, sign));
            }

            void AddEdgeStrip(int axisA, int axisB, int axisC, int signA, int signB)
            {
                for (var segment = 0; segment < profileSegments; segment++)
                {
                    var t0 = (float)segment / profileSegments;
                    var t1 = (float)(segment + 1) / profileSegments;
                    var p0 = ProfilePoint(1f - t0, t0, 0f);
                    var p1 = ProfilePoint(1f - t1, t1, 0f);
                    AddOutwardFace(
                        EdgePoint(axisA, signA, axisB, signB, axisC, -inner, p0),
                        EdgePoint(axisA, signA, axisB, signB, axisC, inner, p0),
                        EdgePoint(axisA, signA, axisB, signB, axisC, inner, p1),
                        EdgePoint(axisA, signA, axisB, signB, axisC, -inner, p1)
                    );
                }
            }

            for (var signA = -1; signA <= 1; signA += 2)
            for (var signB = -1; signB <= 1; signB += 2)
            {
                AddEdgeStrip(0, 1, 2, signA, signB);
                AddEdgeStrip(0, 2, 1, signA, signB);
                AddEdgeStrip(1, 2, 0, signA, signB);
            }

            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = new Dictionary<(int i, int j), Vector3>();
                for (var i = 0; i <= profileSegments; i++)
                for (var j = 0; j <= profileSegments - i; j++)
                {
                    var k = profileSegments - i - j;
                    var profilePoint = ProfilePoint(i, j, k);
                    corner[(i, j)] = new Vector3(
                        x * (inner + chamfer * profilePoint.x),
                        y * (inner + chamfer * profilePoint.y),
                        z * (inner + chamfer * profilePoint.z)
                    );
                }

                for (var i = 0; i < profileSegments; i++)
                for (var j = 0; j < profileSegments - i; j++)
                {
                    AddOutwardFace(corner[(i, j)], corner[(i + 1, j)], corner[(i, j + 1)]);
                    if (j < profileSegments - i - 1)
                    {
                        AddOutwardFace(
                            corner[(i + 1, j)], corner[(i + 1, j + 1)], corner[(i, j + 1)]
                        );
                    }
                }
            }

            return BuildMesh(vertices, faces);
        }

        public static PolyMesh HollowHemisphere(int verticalLines = 24, int horizontalLines = 12,
            float thickness = .1f)
        {
            verticalLines = Mathf.Max(3, verticalLines);
            horizontalLines = Mathf.Max(1, horizontalLines);
            thickness = Mathf.Clamp(thickness, .0001f, .9999f);
            var innerRadius = 1f - thickness;
            var profile = new List<Vector2>();

            // Outer equator to outer pole.
            for (var line = 0; line <= horizontalLines; line++)
            {
                var angle = Mathf.PI * .5f * line / horizontalLines;
                var radius = line == horizontalLines ? 0f : Mathf.Cos(angle);
                profile.Add(new Vector2(radius, Mathf.Sin(angle)));
            }

            // Inner pole back to inner equator. The two axial pole points intentionally have no
            // surface between them; the inner and outer skins are joined by the equatorial rim.
            for (var line = horizontalLines; line >= 0; line--)
            {
                var angle = Mathf.PI * .5f * line / horizontalLines;
                var radius = line == horizontalLines ? 0f : Mathf.Cos(angle) * innerRadius;
                profile.Add(new Vector2(radius, Mathf.Sin(angle) * innerRadius));
            }

            return Lathe(profile, verticalLines, true);
        }

        public static PolyMesh ChamferedCylinder(int sides = 24, int bevelSegments = 3,
            float chamfer = .2f, float height = 2f, float radius = 1f)
        {
            sides = Mathf.Max(3, sides);
            bevelSegments = Mathf.Max(1, bevelSegments);
            radius = Mathf.Max(.0001f, radius);
            height = Mathf.Max(.0001f, height);
            var maxChamfer = Mathf.Max(.0001f, Mathf.Min(radius, height * .5f) - .0001f);
            chamfer = Mathf.Clamp(chamfer, .0001f, maxChamfer);

            var halfHeight = height * .5f;
            var profile = new List<Vector2> { new(0f, -halfHeight), new(radius - chamfer, -halfHeight) };
            for (var segment = 1; segment <= bevelSegments; segment++)
            {
                var angle = -Mathf.PI * .5f + Mathf.PI * .5f * segment / bevelSegments;
                profile.Add(new Vector2(
                    radius - chamfer + Mathf.Cos(angle) * chamfer,
                    -halfHeight + chamfer + Mathf.Sin(angle) * chamfer
                ));
            }
            profile.Add(new Vector2(radius, halfHeight - chamfer));
            for (var segment = 1; segment <= bevelSegments; segment++)
            {
                var angle = Mathf.PI * .5f * segment / bevelSegments;
                profile.Add(new Vector2(
                    radius - chamfer + Mathf.Cos(angle) * chamfer,
                    halfHeight - chamfer + Mathf.Sin(angle) * chamfer
                ));
            }
            profile.Add(new Vector2(0f, halfHeight));
            return Lathe(profile, sides);
        }

        public static PolyMesh PartialTorus(int pathSteps = 24, int shapeSides = 8,
            float scale = 25f, float angle = 180f)
        {
            pathSteps = Mathf.Max(1, pathSteps);
            shapeSides = Mathf.Max(3, shapeSides);
            angle = Mathf.Clamp(angle, .001f, 360f);
            var tubeRadius = Mathf.Max(.0001f, scale / 100f);
            var closed = Mathf.Approximately(angle, 360f);
            var ringCount = closed ? pathSteps : pathSteps + 1;
            var vertices = new List<Vector3>();

            for (var pathStep = 0; pathStep < ringCount; pathStep++)
            {
                var pathAngle = angle * Mathf.Deg2Rad * pathStep / pathSteps;
                var radial = new Vector3(Mathf.Cos(pathAngle), 0f, Mathf.Sin(pathAngle));
                for (var shapeStep = 0; shapeStep < shapeSides; shapeStep++)
                {
                    var shapeAngle = Mathf.PI * 2f * shapeStep / shapeSides;
                    vertices.Add(radial * (1f + Mathf.Cos(shapeAngle) * tubeRadius) +
                                 Vector3.up * (Mathf.Sin(shapeAngle) * tubeRadius));
                }
            }

            var faces = new List<List<int>>();
            for (var pathStep = 0; pathStep < pathSteps; pathStep++)
            {
                var nextRing = (pathStep + 1) % ringCount;
                for (var shapeStep = 0; shapeStep < shapeSides; shapeStep++)
                {
                    var nextShape = (shapeStep + 1) % shapeSides;
                    faces.Add(new List<int>
                    {
                        pathStep * shapeSides + shapeStep,
                        pathStep * shapeSides + nextShape,
                        nextRing * shapeSides + nextShape,
                        nextRing * shapeSides + shapeStep
                    });
                }
            }
            if (!closed)
            {
                faces.Add(Enumerable.Range(0, shapeSides).Reverse().ToList());
                faces.Add(Enumerable.Range((ringCount - 1) * shapeSides, shapeSides).ToList());
            }
            return BuildMesh(vertices, faces);
        }

        public static PolyMesh WireframeBox(float thickness = .1f, float size = 2f)
        {
            size = Mathf.Max(.0001f, size);
            var thicknessEpsilon = size * .0001f;
            thickness = Mathf.Clamp(thickness, thicknessEpsilon, size * .5f - thicknessEpsilon);
            var half = size * .5f;
            var inner = half - thickness;
            var grid = new[] { -half, -inner, inner, half };
            var vertices = new List<Vector3>();
            var faces = new List<List<int>>();
            var vertexIndices = new Dictionary<Vector3, int>();

            bool IsOccupied(int x, int y, int z)
            {
                if (x < 0 || x > 2 || y < 0 || y > 2 || z < 0 || z > 2) return false;
                var outerAxisCount = (x == 1 ? 0 : 1) + (y == 1 ? 0 : 1) + (z == 1 ? 0 : 1);
                return outerAxisCount >= 2;
            }

            int VertexIndex(Vector3 point)
            {
                if (vertexIndices.TryGetValue(point, out var index)) return index;
                index = vertices.Count;
                vertexIndices.Add(point, index);
                vertices.Add(point);
                return index;
            }

            void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                faces.Add(new List<int>
                {
                    VertexIndex(a), VertexIndex(b), VertexIndex(c), VertexIndex(d)
                });
            }

            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
            for (var z = 0; z < 3; z++)
            {
                if (!IsOccupied(x, y, z)) continue;

                var x0 = grid[x];
                var x1 = grid[x + 1];
                var y0 = grid[y];
                var y1 = grid[y + 1];
                var z0 = grid[z];
                var z1 = grid[z + 1];

                var v000 = new Vector3(x0, y0, z0);
                var v100 = new Vector3(x1, y0, z0);
                var v101 = new Vector3(x1, y0, z1);
                var v001 = new Vector3(x0, y0, z1);
                var v010 = new Vector3(x0, y1, z0);
                var v110 = new Vector3(x1, y1, z0);
                var v111 = new Vector3(x1, y1, z1);
                var v011 = new Vector3(x0, y1, z1);

                if (!IsOccupied(x, y - 1, z)) AddFace(v000, v100, v101, v001);
                if (!IsOccupied(x, y + 1, z)) AddFace(v010, v011, v111, v110);
                if (!IsOccupied(x, y, z - 1)) AddFace(v000, v010, v110, v100);
                if (!IsOccupied(x + 1, y, z)) AddFace(v100, v110, v111, v101);
                if (!IsOccupied(x, y, z + 1)) AddFace(v101, v111, v011, v001);
                if (!IsOccupied(x - 1, y, z)) AddFace(v001, v011, v010, v000);
            }

            return BuildMesh(vertices, faces);
        }

        public static PolyMesh StarTorus(int pathSteps, int shapeSides, float radius, float scale)
        {
            var shape = Shapes.Build(ShapeTypes.Polygon, shapeSides);
            shape = shape.FaceScale(new OpParams(scale/100f));
            var path = Shapes.Build(ShapeTypes.Star, pathSteps, radius);
            return path.Sweep(path.Faces[0].Get2DVertices(), shape.Faces[0].Get2DVertices(), true);
        }

        public static PolyMesh Torus(int pathSteps, int shapeSides, float scale)
        {
            var shape = Shapes.Build(ShapeTypes.Polygon, shapeSides);
            shape = shape.FaceScale(new OpParams(scale/100f));
            var path = Shapes.Build(ShapeTypes.Polygon, pathSteps);
            return path.Sweep(path.Faces[0].Get2DVertices(), shape.Faces[0].Get2DVertices(), true);
        }

        public static PolyMesh Box(int x, int y, int z)
        {
            var shape = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, x, z);

            shape = shape.LayeredExtrude(y, 1.4142f);

            // Nice patterns
            var capRoles = new List<Roles>();
            var newFaceRoles = new List<Roles>();

            // Top and bottom roles
            int facesOnCap = x * z;
            for (var i = 0; i < facesOnCap; i++)
            {
                int alt = 0;
                if (x % 2 == 0)
                {
                    alt = Mathf.FloorToInt((i / x) % 2);
                }

                capRoles.Add((i + alt) % 2 == 0 ? Roles.New : Roles.NewAlt);
            }

            newFaceRoles.AddRange(capRoles);

            // Roles per vertical layer
            int facesPerLayer = x * 2 + z * 2;
            for (var i = 0; i < y * facesPerLayer; i++)
            {
                int alt = (i / facesPerLayer) % 2 == 0 ? 0 : 1;
                newFaceRoles.Add((i + alt) % 2 == 0 ? Roles.New : Roles.NewAlt);
            }

            newFaceRoles.AddRange(capRoles);

            shape.FaceRoles = newFaceRoles;
            return shape;
        }

        public static PolyMesh UvSphere(int verticalLines = 24, int horizontalLines = 24, float hemi = 1)
        {
            var faceRoles = new List<Roles>();

            horizontalLines = Mathf.Clamp(horizontalLines, 3, 24);
            verticalLines = Mathf.Clamp(verticalLines, 3, 24);

            var verts = new List<Vector3>();
            for (float v = 0; v <= horizontalLines; v++)
            {
                for (float u = 0; u < verticalLines; u++)
                {
                    var vv = v / horizontalLines;
                    var uu = u / verticalLines;
                    // Avoid coincident vertices at the tip
                    // as this caused weird glitches on Lace
                    if (vv == 0) vv = 0.0001f;

                    float x = Mathf.Sin(Mathf.PI * vv) * Mathf.Cos(2f * Mathf.PI * uu);
                    float y = Mathf.Sin(Mathf.PI * vv) * Mathf.Sin(2f * Mathf.PI * uu);
                    float z = Mathf.Cos(Mathf.PI * vv);
                    verts.Add(new Vector3(x, z, y));
                }
            }

            var faces = new List<List<int>>();
            for (int v = 0; v < horizontalLines * hemi; v += 1)
            {
                for (int u = 0; u < verticalLines; u += 1)
                {
                    faces.Add(new List<int>
                    {
                        (v * verticalLines) + u,
                        (v * verticalLines) + ((u + 1) % verticalLines),
                        ((v + 1) * verticalLines) + ((u + 1) % verticalLines),
                        ((v + 1) * verticalLines) + u
                    });
                    faceRoles.Add((u + v) % 2 == 0 ? Roles.New : Roles.NewAlt);
                }
            }

            var vertexRoles = Enumerable.Repeat(Roles.Existing, verts.Count);
            var poly = new PolyMesh(verts, faces, faceRoles, vertexRoles);
            return poly;
        }

        public static PolyMesh UvHemisphere(int verticalLines = 24, int horizontalLines = 24)
        {
            var poly = UvSphere(verticalLines, horizontalLines, 0.5f);
            poly = poly.FillHoles();
            return poly;
        }

        public static PolyMesh Stairs(int steps, float width, float height, bool splitAlongWidth=false)
        {
            PolyMesh poly;
            OpFunc func;

            if (splitAlongWidth)
            {
                // Uses an x/z grid to create multiple x segments - so width will be cast to int
                poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, Mathf.FloorToInt(width), steps);
                func = new OpFunc(x => x.index / width / (1f / height) + height);
            }
            else
            {
                // A single width division
                poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 1, steps);
                // Scale in x to give the desired width
                poly.Scale(new Vector3(width, 1, 1));
                func = new OpFunc(x => x.index / (1f / height) + height);
            }
            return poly.Extrude(new OpParams(func));
        }
    }
}
