using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public enum ShapeTypes
    {
        Polygon,
        Star,
        C_Shape,
        L_Shape,
        H_Shape,
        Arc,
        Arch,
        GothicArch,
        Ring,
        Triangle,
        Rectangle,
        Frame,
    }

    public class Shapes
    {
        public enum ConicType
        {
            Circle,
            Ellipse,
            Hyperbola,
            Parabola
        }

        public enum Method
        {
            Concave,
            Convex,
            Grid,
        }
        public static PolyMesh Build(ShapeTypes type, float a=0.5f, float b=0.5f, float c=0.5f, Method method = Method.Concave, float d = 4f)
        {
            return type switch
            {
                ShapeTypes.Polygon => Polygon(Mathf.FloorToInt(Mathf.Max(a, 3))),
                ShapeTypes.Star => Polygon(Mathf.FloorToInt(Mathf.Max(a * 2, 3)), stellate: b),
                ShapeTypes.C_Shape => C_Shape(a, b, c, method),
                ShapeTypes.L_Shape => L_Shape(a, b, c, method),
                ShapeTypes.H_Shape => H_Shape(a, b, c, method),
                ShapeTypes.Arc => Arc(Mathf.FloorToInt(a), 1, b, 360 * c),
                ShapeTypes.Arch => Arch(Mathf.FloorToInt(a), 1, b, c),
                ShapeTypes.GothicArch => GothicArch(sides: Mathf.FloorToInt(a), width: 1, thickness: b, height: c),
                ShapeTypes.Ring => Arc(Mathf.FloorToInt(a), 1, b, 360, true),
                ShapeTypes.Triangle => Triangle(Mathf.FloorToInt(a), b, c),
                // Unit width: A = height, B = corner radius, C = corner segments.
                ShapeTypes.Rectangle => Rectangle(1f, a, b, Mathf.FloorToInt(c)),
                // Unit width: A = height, B = border width, C = outer radius, D = corner segments.
                ShapeTypes.Frame => Frame(1f, a, b, c, Mathf.FloorToInt(d)),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        /// <summary>
        /// Creates a centered rectangle in the XZ plane, facing up.
        /// Radius is measured in shape units and clamped to half the shorter side.
        /// A zero radius or zero corner segments produces square corners.
        /// </summary>
        public static PolyMesh Rectangle(float width = 1f, float height = 1f,
            float radius = 0f, int cornerSegments = 4)
        {
            width = Mathf.Max(.0001f, width);
            height = Mathf.Max(.0001f, height);
            var vertices = RectangleContour(width, height, radius, cornerSegments);
            return new PolyMesh(vertices, new List<List<int>>
            {
                Enumerable.Range(0, vertices.Count).ToList()
            });
        }

        // Retaining corner samples lets Frame pair corresponding points even when
        // an inner corner collapses to a single vertex. Rectangle removes duplicates.
        private static List<Vector3> RectangleContour(float width, float height,
            float radius, int cornerSegments, bool retainCornerSamples = false)
        {
            var halfWidth = width * .5f;
            var halfHeight = height * .5f;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(halfWidth, halfHeight));
            if (cornerSegments <= 0 || (radius == 0f && !retainCornerSamples))
                return new List<Vector3>
                {
                    new Vector3(-halfWidth, 0f, -halfHeight),
                    new Vector3(-halfWidth, 0f, halfHeight),
                    new Vector3(halfWidth, 0f, halfHeight),
                    new Vector3(halfWidth, 0f, -halfHeight)
                };

            cornerSegments = Mathf.Clamp(cornerSegments, 1, 128);
            var centers = new[]
            {
                new Vector2(halfWidth - radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, halfHeight - radius),
                new Vector2(-halfWidth + radius, -halfHeight + radius),
                new Vector2(halfWidth - radius, -halfHeight + radius)
            };
            var vertices = new List<Vector3>();
            var tolerance = Mathf.Min(width, height) * .000001f;
            var toleranceSquared = tolerance * tolerance;
            for (var corner = 0; corner < centers.Length; corner++)
            for (var segment = 0; segment <= cornerSegments; segment++)
            {
                var angle = (corner + (float)segment / cornerSegments) * Mathf.PI * .5f;
                var point = new Vector3(
                    centers[corner].x + radius * Mathf.Cos(angle), 0f,
                    centers[corner].y + radius * Mathf.Sin(angle));
                if (retainCornerSamples || vertices.Count == 0 ||
                    (vertices[vertices.Count - 1] - point).sqrMagnitude > toleranceSquared)
                    vertices.Add(point);
            }
            if (!retainCornerSamples &&
                (vertices[0] - vertices[vertices.Count - 1]).sqrMagnitude <= toleranceSquared)
                vertices.RemoveAt(vertices.Count - 1);
            vertices.Reverse();
            return vertices;
        }

        /// <summary>
        /// Creates a centered rectangular frame in the XZ plane, facing up.
        /// Border width is measured inward from the outer boundary. Radius is the
        /// outer corner radius; the inner radius is reduced by the border width.
        /// Corresponding contour points form quads, with triangles only where a
        /// corner collapses. Use extrusion or Shell to give the frame thickness.
        /// </summary>
        public static PolyMesh Frame(float width = 1f, float height = 1f,
            float borderWidth = .1f, float radius = 0f, int cornerSegments = 4)
        {
            width = Mathf.Max(.0001f, width);
            height = Mathf.Max(.0001f, height);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * .5f);
            if (borderWidth <= 0f) return new PolyMesh();
            if (borderWidth >= Mathf.Min(width, height) * .5f)
                return Rectangle(width, height, radius, cornerSegments);
            if (radius == 0f) cornerSegments = 0;
            var innerWidth = Mathf.Max(.0001f, width - borderWidth * 2f);
            var innerHeight = Mathf.Max(.0001f, height - borderWidth * 2f);
            var outer = RectangleContour(width, height, radius, cornerSegments, true);
            var inner = RectangleContour(innerWidth, innerHeight,
                Mathf.Max(0f, radius - borderWidth), cornerSegments, true);
            var vertices = new List<Vector3>();
            var faces = new List<List<int>>();

            List<int> AddLoop(List<Vector3> points, float tolerance)
            {
                var indices = new List<int>();
                var toleranceSquared = tolerance * tolerance;
                foreach (var point in points)
                {
                    if (indices.Count > 0 &&
                        (vertices[indices[indices.Count - 1]] - point).sqrMagnitude <= toleranceSquared)
                        indices.Add(indices[indices.Count - 1]);
                    else if (indices.Count > 0 &&
                        (vertices[indices[0]] - point).sqrMagnitude <= toleranceSquared)
                        indices.Add(indices[0]);
                    else
                    {
                        indices.Add(vertices.Count);
                        vertices.Add(point);
                    }
                }
                return indices;
            }

            var outerIndices = AddLoop(outer, Mathf.Min(width, height) * .000001f);
            var innerIndices = AddLoop(inner, Mathf.Min(innerWidth, innerHeight) * .000001f);
            for (var index = 0; index < outerIndices.Count; index++)
            {
                var next = (index + 1) % outerIndices.Count;
                // Both contours face up; follow the outer edge and return along the inner edge.
                var face = new List<int>
                {
                    outerIndices[index], outerIndices[next], innerIndices[next], innerIndices[index]
                };
                for (var corner = face.Count - 1; corner > 0; corner--)
                    if (face[corner] == face[corner - 1]) face.RemoveAt(corner);
                if (face.Count > 1 && face[0] == face[face.Count - 1]) face.RemoveAt(face.Count - 1);
                if (face.Count >= 3) faces.Add(face);
            }
            return new PolyMesh(vertices, faces);
        }

        /// <summary>
        /// Creates a closed radial profile in the XY plane, facing +Z, for revolution about Y.
        /// Radius uniformly scales the profile. Ellipse axes are its horizontal/vertical
        /// semiaxes; hyperbola uses majorAxis*sqrt(1+y*y/(minorAxis*minorAxis));
        /// parabola uses y*y/(4*majorAxis)+centerOffset before uniform scaling.
        /// Clip values are fractions of the sampling angle from 0 to PI, not of height.
        /// Parabola offsets retain signed radial coordinates; negative offsets can create
        /// self-intersecting profiles and folded surfaces when revolved. They are not repaired.
        /// </summary>
        public static PolyMesh ConicProfile(ConicType type, int segments = 16,
            float radius = 1f, float majorAxis = 1f, float minorAxis = 1f,
            float centerOffset = .2f, float bottomClip = 0f, float topClip = 1f)
        {
            return ProfilePolygon(ConicProfilePoints(type, segments, radius, majorAxis, minorAxis,
                centerOffset, bottomClip, topClip));
        }

        internal static List<Vector2> ConicProfilePoints(ConicType type, int segments, float radius,
            float majorAxis, float minorAxis, float centerOffset, float bottomClip, float topClip)
        {
            segments = Mathf.Max(2, segments);
            if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            if (type != ConicType.Circle && majorAxis <= 0f)
                throw new ArgumentOutOfRangeException(nameof(majorAxis));
            if ((type == ConicType.Ellipse || type == ConicType.Hyperbola) && minorAxis <= 0f)
                throw new ArgumentOutOfRangeException(nameof(minorAxis));
            bottomClip = Mathf.Clamp01(bottomClip);
            topClip = Mathf.Clamp01(topClip);
            if (topClip <= bottomClip)
                throw new ArgumentException("The top clip must be greater than the bottom clip.");

            var points = new List<Vector2>();
            for (var row = 0; row <= segments; row++)
            {
                var fraction = Mathf.Lerp(bottomClip, topClip, (float)row / segments);
                var angle = Mathf.PI * fraction;
                var height = -Mathf.Cos(angle);
                // Explicit poles avoid a residual radius from floating-point sin(PI).
                var circularRadius = fraction == 0f || fraction == 1f ? 0f : Mathf.Sin(angle);
                var radial = type switch
                {
                    ConicType.Circle => circularRadius,
                    ConicType.Ellipse => majorAxis * circularRadius,
                    ConicType.Hyperbola => majorAxis * Mathf.Sqrt(1f + height * height / (minorAxis * minorAxis)),
                    ConicType.Parabola => height * height / (4f * majorAxis) + centerOffset,
                    _ => throw new ArgumentOutOfRangeException(nameof(type))
                };
                if (type == ConicType.Ellipse) height *= minorAxis;
                points.Add(new Vector2(radial * radius, height * radius));
            }
            return points;
        }

        /// <summary>
        /// Creates a closed half-capsule outline in the XY plane, facing +Z.
        /// Cylinder height is the straight section; total height adds two radii.
        /// </summary>
        public static PolyMesh CapsuleProfile(int capSegments = 8, float cylinderHeight = 2f,
            float radius = 1f)
        {
            return ProfilePolygon(CapsuleProfilePoints(capSegments, cylinderHeight, radius));
        }

        internal static List<Vector2> CapsuleProfilePoints(int capSegments, float cylinderHeight, float radius)
        {
            capSegments = Mathf.Max(1, capSegments);
            cylinderHeight = Mathf.Max(0f, cylinderHeight);
            radius = Mathf.Max(.0001f, radius);
            var points = new List<Vector2> { new Vector2(0f, -cylinderHeight * .5f - radius) };
            for (var segment = 1; segment <= capSegments; segment++)
            {
                var angle = -Mathf.PI * .5f + Mathf.PI * .5f * segment / capSegments;
                points.Add(new Vector2(Mathf.Cos(angle) * radius,
                    -cylinderHeight * .5f + Mathf.Sin(angle) * radius));
            }
            if (cylinderHeight > .000001f) points.Add(new Vector2(radius, cylinderHeight * .5f));
            for (var segment = 1; segment < capSegments; segment++)
            {
                var angle = Mathf.PI * .5f * segment / capSegments;
                points.Add(new Vector2(Mathf.Cos(angle) * radius,
                    cylinderHeight * .5f + Mathf.Sin(angle) * radius));
            }
            points.Add(new Vector2(0f, cylinderHeight * .5f + radius));
            return points;
        }

        internal static PolyMesh ProfilePolygon(IReadOnlyList<Vector2> points)
        {
            var vertices = new List<Vector3> { new Vector3(0f, points[0].y, 0f) };
            foreach (var point in points)
            {
                var position = new Vector3(point.x, point.y, 0f);
                if (!position.Equals(vertices[vertices.Count - 1])) vertices.Add(position);
            }
            var topCenter = new Vector3(0f, points[points.Count - 1].y, 0f);
            if (!topCenter.Equals(vertices[vertices.Count - 1])) vertices.Add(topCenter);
            return new PolyMesh(vertices, new List<List<int>>
            {
                Enumerable.Range(0, vertices.Count).ToList()
            });
        }

        public static PolyMesh Triangle(int frequency = 1, float leftAngleRadians = .7853982f,
            float rightAngleRadians = .7853982f)
        {
            frequency = Mathf.Clamp(frequency, 1, 64);
            const float minimumAngle = .01f;
            var maximumAngleSum = Mathf.PI - minimumAngle;
            leftAngleRadians = Mathf.Clamp(leftAngleRadians, minimumAngle, maximumAngleSum);
            rightAngleRadians = Mathf.Clamp(rightAngleRadians, minimumAngle, maximumAngleSum);
            var angleSum = leftAngleRadians + rightAngleRadians;
            if (angleSum > maximumAngleSum)
            {
                var scale = maximumAngleSum / angleSum;
                leftAngleRadians *= scale;
                rightAngleRadians *= scale;
            }

            var apexAngle = Mathf.PI - leftAngleRadians - rightAngleRadians;
            var leftSideLength = Mathf.Sin(rightAngleRadians) / Mathf.Sin(apexAngle);
            var apex = new Vector3(
                leftSideLength * Mathf.Cos(leftAngleRadians),
                0f,
                leftSideLength * Mathf.Sin(leftAngleRadians));
            var centroid = (Vector3.zero + Vector3.right + apex) / 3f;
            var corners = new[]
            {
                Vector3.zero - centroid,
                Vector3.right - centroid,
                apex - centroid
            };
            var vertices = new List<Vector3>();
            var indices = new Dictionary<(int alongBase, int towardApex), int>();
            for (var towardApex = 0; towardApex <= frequency; towardApex++)
            for (var alongBase = 0; alongBase <= frequency - towardApex; alongBase++)
            {
                var baseWeight = (float)alongBase / frequency;
                var apexWeight = (float)towardApex / frequency;
                indices[(alongBase, towardApex)] = vertices.Count;
                vertices.Add(corners[0] +
                             (corners[1] - corners[0]) * baseWeight +
                             (corners[2] - corners[0]) * apexWeight);
            }

            var faces = new List<List<int>>();
            for (var towardApex = 0; towardApex < frequency; towardApex++)
            for (var alongBase = 0; alongBase < frequency - towardApex; alongBase++)
            {
                faces.Add(new List<int>
                {
                    indices[(alongBase, towardApex)],
                    indices[(alongBase, towardApex + 1)],
                    indices[(alongBase + 1, towardApex)]
                });
                if (alongBase < frequency - towardApex - 1)
                {
                    faces.Add(new List<int>
                    {
                        indices[(alongBase + 1, towardApex)],
                        indices[(alongBase, towardApex + 1)],
                        indices[(alongBase + 1, towardApex + 1)]
                    });
                }
            }

            return new PolyMesh(vertices, faces);
        }

        public static PolyMesh Polygon(int sides, bool flip = false, float angleOffset = 0,
            float heightOffset = 0, float radius = 1, float stellate = 0)
        {
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();

            faceIndices.Add(new int[sides]);

            float theta = Mathf.PI * 2 / sides;

            int start, end, inc;

            if (flip)
            {
                start = 0;
                end = sides;
                inc = 1;
            }
            else
            {
                start = sides - 1;
                end = -1;
                inc = -1;
            }

            for (int i = start; i != end; i += inc)
            {
                float angle = theta * i + (theta * angleOffset);
                vertexPoints.Add(new Vector3(Mathf.Cos(angle) * radius, heightOffset, Mathf.Sin(angle) * radius));
                faceIndices[0][i] = i;
            }

            var poly = new PolyMesh(vertexPoints, faceIndices);
            if (stellate!=0) poly.VertexStellate(new OpParams(stellate));
            return poly;
        }

        public static PolyMesh L_Shape(float a, float b, float c, Method method = Method.Convex)
        {

            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 2, 3);
                poly = poly.FaceRemove(false, new List<int>{0, 2});
                return poly;
            }

            List<List<int>> faces;
            List<Vector3> verts = new()
            {
                // Base
                new(0, 0, 0),
                new(b, 0, 0),
                new(b, 0, -c),
                new(-c, 0, -c),
                new(-c, 0, a),
                new(0, 0, a),
            };

            if (method == Method.Concave)
            {
                faces = new(){ Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 3 },
                    new() { 0, 3, 4, 5 },
                };
            }
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh C_Shape(float a, float b, float c = 0.25f, Method method = Method.Convex)
        {

            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 2, 3);
                poly = poly.FaceRemove(false, new List<int>{2});
                return poly;
            }

            List<List<int>> faces;
            List<Vector3> verts = new()
            {
                new (a, 0, -b),
                new (a, 0, -(b+c)),
                new (-c, 0, -(b+c)),
                new (-c, 0, b+c),
                new (a, 0, b+c),
                new (a, 0, b),
                new (0, 0, b),
                new (0, 0, -b),
            };

            if (method == Method.Concave)
            {
                faces = new() { Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 7 },
                    new() { 2, 3, 6, 7 },
                    new() {3, 4, 5, 6}
                };
            }
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh H_Shape(float a, float b, float c, Method method = Method.Convex)
        {

            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 3, 3);
                poly = poly.FaceRemove(false, new List<int>{1, 7});
                return poly;
            }

            List<List<int>> faces;
            List<Vector3> verts = new()
            {
                // Top right
                new (a, 0, b+c),
                new (a+b, 0, b+c),

                // Base right
                new (a+b, 0, -(b+c)),
                new (a, 0, -(b+c)),

                // Crossbar bottom
                new (a, 0, -b/2f),
                new (-a, 0, -b/2f),

                // Base left
                new (-a, 0, -(b+c)),
                new (-(a+b), 0, -(b+c)),

                // Top left
                new (-(a+b), 0, b+c),
                new (-a, 0, b+c),

                // Crossbar top
                new (-a, 0, b/2f),
                new (a, 0, b/2f),
            };

            if (method == Method.Concave)
            {
                faces = new() { Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 3, 4, 11 },
                    new () { 11, 4, 5, 10 },
                    new() { 5, 6, 7, 8, 9, 10 },
                };
            }
            return new PolyMesh(verts, faces);
        }

        public static (List<Vector3> verts, List<List<int>> faces) _CalcArc(
            int sides, float radius, float thickness, float arcAngle, bool joined = false)
        {
            List<Vector3> verts = new();
            List<List<int>> faces = new();

            float theta = ((arcAngle / 360f) * (Mathf.PI * 2f)) / sides;
            int end, inc;
            end = sides;
            inc = 1;

            for (int i = 0; i <= end; i += inc)
            {
                bool closing = i == end && joined;
                float angle = theta * i;
                if (!closing)
                {
                    verts.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
                    verts.Add(new Vector3(Mathf.Cos(angle) * radius * thickness, Mathf.Sin(angle) * radius * thickness, 0));
                }
                int lastIndex = verts.Count - 1;
                if (i == 0) continue;
                List<int> face;
                if (closing)
                {
                    face = new List<int> { 0, 1, lastIndex, lastIndex - 1 };
                }
                else
                {
                    face = new List<int> { lastIndex - 1, lastIndex, lastIndex - 2, lastIndex - 3 };
                }
                if (thickness > 1) face.Reverse();
                faces.Add(face);
            }

            return (verts, faces);
        }

        public static PolyMesh Arc(int sides, float radius, float thickness, float angle, bool joined = false)
        {
            if (thickness == 0f)
            {
                angle = Mathf.Clamp(angle, -360f, 360f);
                if (angle == 0f || radius == 0f) return new PolyMesh();

                bool fullCircle = Mathf.Abs(angle) == 360f;
                sides = Mathf.Max(fullCircle ? 3 : 2, sides);
                var vertices = new List<Vector3>();
                if (!fullCircle) vertices.Add(Vector3.zero);
                int count = fullCircle ? sides : sides + 1;
                for (int i = 0; i < count; i++)
                {
                    float radians = angle * Mathf.Deg2Rad * i / sides;
                    vertices.Add(new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f));
                }
                return new PolyMesh(vertices, new List<List<int>>
                {
                    Enumerable.Range(0, vertices.Count).ToList()
                });
            }

            var (verts, faces) = _CalcArc(sides, radius, thickness, angle, joined);
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh Arch(int sides, float radius, float thickness, float height)
        {
            var (verts, faces) = _CalcArc(sides, radius, thickness, 180);

            verts = verts.Select(v => v + Vector3.up * height).ToList();

            verts.Add(new Vector3(radius, 0, 0));
            verts.Add(new Vector3(radius * thickness, 0, 0));
            int finalIndex = verts.Count - 1;
            var uprightFaceL = new List<int> { finalIndex - 1, 0, 1, finalIndex };
            if (thickness > 1) uprightFaceL.Reverse();
            faces.Insert(0, uprightFaceL);

            verts.Add(new Vector3(-radius, 0, 0));
            verts.Add(new Vector3(-radius * thickness, 0, 0));
            finalIndex = verts.Count - 1;
            var uprightFaceR = new List<int> { finalIndex, finalIndex - 4, finalIndex - 5, finalIndex - 1 };
            if (thickness > 1) uprightFaceR.Reverse();
            faces.Add(uprightFaceR);

            return new PolyMesh(verts, faces);
        }
        public static PolyMesh GothicArch(int sides, float width, float thickness, float height)
        {
            float curveX = 0.5f;
            float curveY = 0.75f;

            var pointA = new Vector2(-width, 0);
            var pointB = new Vector2(-(width * curveX), height * curveY);
            var pointC = new Vector2(0, height);
            var pointD = new Vector2(width * curveX, height * curveY);
            var pointE = new Vector2(width, 0);
            var arcLeft = ThreePointArc(pointA, pointB, pointC, sides);
            var arcRight = ThreePointArc(pointC, pointD, pointE, sides).Skip(1).ToList();

            List<Vector2> gothicArchPoints = new List<Vector2>();
            gothicArchPoints.AddRange(arcLeft);
            gothicArchPoints.AddRange(arcRight);

            var offsetPath = PolyMesh.PathOffset(gothicArchPoints, thickness, miterLimit: 8)[0];
            var verts = offsetPath.Select(v => new Vector3(v.x, v.y, 0)).ToList();
            var face = Enumerable.Range(0, verts.Count).ToList();
            var faces = new List<List<int>>{face};
            var polyMesh = new PolyMesh(verts, faces);
            return polyMesh;
        }

        static List<Vector2> ThreePointArc(Vector2 pointA, Vector2 pointB, Vector2 pointC, int sides)
        {
            // Calculate the slope of the lines
            float slopeAB = (pointB.y - pointA.y) / (pointB.x - pointA.x);
            float slopeBC = (pointC.y - pointB.y) / (pointC.x - pointB.x);

            // Calculate the center of the arc
            float centerX = (slopeAB * slopeBC * (pointA.y - pointC.y) + slopeBC * (pointA.x + pointB.x) - slopeAB * (pointB.x + pointC.x)) / (2 * (slopeBC - slopeAB));
            float centerY = -1 * (centerX - (pointA.x + pointB.x) / 2) / slopeAB + (pointA.y + pointB.y) / 2;
            Vector2 center = new Vector2(centerX, centerY);

            // Calculate the radius of the arc
            float radius = Vector2.Distance(pointA, center);

            // Calculate the start and end angles
            float startAngle = (float)(Math.Atan2(pointA.y - centerY, pointA.x - centerX) * (180 / Math.PI));
            float endAngle = (float)(Math.Atan2(pointC.y - centerY, pointC.x - centerX) * (180 / Math.PI));

            List<Vector2> arcPoints = new List<Vector2>();
            float sweepAngle = endAngle - startAngle;
            var sweepAngle2 = Mathf.Min(sweepAngle, 360 - sweepAngle);
            if (Mathf.Min(Mathf.Abs(sweepAngle), Mathf.Abs(sweepAngle2)) == sweepAngle2)
            {
                sweepAngle = -sweepAngle2;
            }
            float angleStep = sweepAngle / (sides - 1);

            for (int i = 0; i < sides; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                float radianAngle = currentAngle * (float)Math.PI / 180;

                float x = center.x + radius * (float)Math.Cos(radianAngle);
                float y = center.y + radius * (float)Math.Sin(radianAngle);

                arcPoints.Add(new Vector2(x, y));
            }

            return arcPoints;
        }

        public static PolyMesh Rhombus(float angle, float edgeLength)
        {
            float angleRadians = angle * Mathf.Deg2Rad;
            float halfLength = edgeLength / 2f;
            float offsetX = Mathf.Cos(angleRadians) * halfLength;
            float offsetY = Mathf.Sin(angleRadians) * halfLength;

            Vector3 V1 = new Vector2(halfLength, 0);
            Vector3 V2 = new Vector2(offsetX, offsetY);
            Vector3 V3 = new Vector2(-halfLength, 0);
            Vector3 V4 = new Vector2(-offsetX, -offsetY);

            var vertices = new List<Vector2> { V1, V2, V3, V4 };
            return new PolyMesh(vertices);
        }
    }
}
