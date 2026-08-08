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

        public static PolyMesh Capsule(int sides = 24, int capSegments = 8, float cylinderHeight = 2f,
            float radius = 1f)
        {
            sides = Mathf.Max(3, sides);
            capSegments = Mathf.Max(1, capSegments);
            cylinderHeight = Mathf.Max(0f, cylinderHeight);
            radius = Mathf.Max(.0001f, radius);

            var profile = new List<Vector2> { new(0f, -cylinderHeight * .5f - radius) };
            for (var segment = 1; segment <= capSegments; segment++)
            {
                var angle = -Mathf.PI * .5f + Mathf.PI * .5f * segment / capSegments;
                profile.Add(new Vector2(
                    Mathf.Cos(angle) * radius,
                    -cylinderHeight * .5f + Mathf.Sin(angle) * radius
                ));
            }
            if (cylinderHeight > .000001f)
                profile.Add(new Vector2(radius, cylinderHeight * .5f));
            for (var segment = 1; segment < capSegments; segment++)
            {
                var angle = Mathf.PI * .5f * segment / capSegments;
                profile.Add(new Vector2(
                    Mathf.Cos(angle) * radius,
                    cylinderHeight * .5f + Mathf.Sin(angle) * radius
                ));
            }
            profile.Add(new Vector2(0f, cylinderHeight * .5f + radius));
            return Lathe(profile, sides);
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
