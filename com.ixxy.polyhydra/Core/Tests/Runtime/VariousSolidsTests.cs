using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Polyhydra.Core.Tests
{
    public class VariousSolidsTests
    {
        private static PolyMesh CreateCube()
        {
            var vertices = new[]
            {
                new Vector3(-1, -1, -1), new Vector3(1, -1, -1),
                new Vector3(1, -1, 1), new Vector3(-1, -1, 1),
                new Vector3(-1, 1, -1), new Vector3(1, 1, -1),
                new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
            };
            var faces = new List<IEnumerable<int>>
            {
                new[] { 0, 1, 2, 3 }, new[] { 4, 7, 6, 5 },
                new[] { 0, 4, 5, 1 }, new[] { 1, 5, 6, 2 },
                new[] { 2, 6, 7, 3 }, new[] { 3, 7, 4, 0 }
            };
            return new PolyMesh(vertices, faces);
        }

        private static void AssertClosed(PolyMesh poly)
        {
            Assert.That(poly.IsValid, Is.True);
            Assert.That(poly.Halfedges.All(edge => edge.Pair != null), Is.True);
        }

        [Test]
        public void ClosedGeneratorsHaveNoBoundaryEdges()
        {
            AssertClosed(VariousSolids.Capsule(12, 4, 2f));
            AssertClosed(VariousSolids.Capsule(12, 4, 0f));
            AssertClosed(VariousSolids.ChamferedBox(1, 1f, .2f));
            AssertClosed(VariousSolids.ChamferedBox(4, 2f, .2f));
            AssertClosed(VariousSolids.HollowHemisphere(12, 6, .2f));
            AssertClosed(VariousSolids.ChamferedCylinder(12, 2, .2f));
            AssertClosed(VariousSolids.PartialTorus(12, 6, 25f, 180f));
            AssertClosed(VariousSolids.WireframeBox(.1f));
            AssertClosed(VariousSolids.WireframeBox(1f));
        }

        [Test]
        public void HollowHemisphereIsAClosedShellWithTwoSkinsAndAnEquatorialRim()
        {
            const int sides = 12;
            const int rows = 6;
            const float thickness = .2f;
            var poly = VariousSolids.HollowHemisphere(sides, rows, thickness);

            AssertClosed(poly);
            Assert.That(poly.Vertices.Count, Is.EqualTo(2 * sides * rows + 2));
            Assert.That(poly.Faces.Count, Is.EqualTo(sides * (2 * rows + 1)));
            Assert.That(poly.Vertices.Count(vertex => Mathf.Approximately(vertex.Position.y, 1f)), Is.EqualTo(1));
            Assert.That(poly.Vertices.Count(vertex =>
                Mathf.Approximately(vertex.Position.y, 1f - thickness)), Is.EqualTo(1));
        }

        [Test]
        public void ChamferedBoxCreatesFaceEdgeAndCornerSurfaces()
        {
            var poly = VariousSolids.ChamferedBox(1, 1f, .2f);

            Assert.That(poly.Vertices.Count, Is.EqualTo(24));
            Assert.That(poly.Faces.Count, Is.EqualTo(6 + 12 + 8));

            var profiled = VariousSolids.ChamferedBox(3, 2f, .2f);
            Assert.That(profiled.Faces.Count, Is.EqualTo(6 + 12 * 3 + 8 * 3 * 3));
        }

        [Test]
        public void PartialTorusAddsTwoEndCaps()
        {
            const int pathSteps = 12;
            const int shapeSides = 6;
            var partial = VariousSolids.PartialTorus(pathSteps, shapeSides, 25f, 180f);
            var complete = VariousSolids.PartialTorus(pathSteps, shapeSides, 25f, 360f);

            Assert.That(partial.Faces.Count, Is.EqualTo(pathSteps * shapeSides + 2));
            Assert.That(complete.Faces.Count, Is.EqualTo(pathSteps * shapeSides));
        }

        [Test]
        public void TriangleAndFilledArcExtrudeToClosedSolids()
        {
            var wedge = Shapes.Triangle().LayeredExtrude(1, 1f, Axis.Y);
            var halfCylinder = Shapes.Arc(8, 1f, 0f, 180f).LayeredExtrude(1, 1f, Axis.Z);

            AssertClosed(wedge);
            AssertClosed(halfCylinder);
            Assert.That(wedge.Faces.Count, Is.EqualTo(5));
            Assert.That(halfCylinder.Faces.Count, Is.EqualTo(8 + 4));
        }

        [TestCase(.25f, 10)]
        [TestCase(.5f, 10)]
        [TestCase(1f, 8)]
        [TestCase(-1f, 8)]
        public void FilledArcUsesOneFaceWithoutDuplicateVertices(float turns, int vertexCount)
        {
            var arc = Shapes.Build(ShapeTypes.Arc, 8f, 0f, turns);

            Assert.That(arc.Faces.Count, Is.EqualTo(1));
            Assert.That(arc.Vertices.Count, Is.EqualTo(vertexCount));
            Assert.That(arc.Vertices.Select(v => v.Position).Distinct().Count(), Is.EqualTo(vertexCount));
            if (Mathf.Abs(turns) < 1f)
            {
                var endpoint = arc.Vertices[vertexCount - 1].Position;
                Assert.That(endpoint.x, Is.EqualTo(Mathf.Cos(turns * 2f * Mathf.PI)).Within(.0001f));
                Assert.That(endpoint.y, Is.EqualTo(Mathf.Sin(turns * 2f * Mathf.PI)).Within(.0001f));
            }
        }

        [Test]
        public void FilledRingMatchesFullFilledArcAndExtrudesToAClosedSolid()
        {
            var ring = Shapes.Build(ShapeTypes.Ring, 8f, 0f);
            var arc = Shapes.Build(ShapeTypes.Arc, 8f, 0f, 1f);

            Assert.That(ring.Faces.Count, Is.EqualTo(1));
            Assert.That(ring.Vertices.Count, Is.EqualTo(8));
            Assert.That(ring.Vertices.Select(vertex => vertex.Position).Distinct().Count(), Is.EqualTo(8));
            Assert.That(ring.Vertices.Select(vertex => vertex.Position),
                Is.EqualTo(arc.Vertices.Select(vertex => vertex.Position)));
            AssertClosed(ring.LayeredExtrude(1, 1f, Axis.Z));
        }

        [Test]
        public void TriangleUsesBaseAnglesInRadians()
        {
            var right = Shapes.Build(ShapeTypes.Triangle, 1f, Mathf.PI * .5f, Mathf.PI * .25f);
            var isoscelesRight = Shapes.Build(ShapeTypes.Triangle, 1f, Mathf.PI * .25f, Mathf.PI * .25f);
            var rightLeft = right.Vertices[2].Position - right.Vertices[0].Position;
            var isoscelesApex = isoscelesRight.Vertices[2].Position -
                                isoscelesRight.Vertices[0].Position;

            Assert.That(rightLeft.x, Is.EqualTo(0f).Within(.0001f));
            Assert.That(rightLeft.z, Is.EqualTo(1f).Within(.0001f));
            Assert.That(isoscelesApex.x, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(isoscelesApex.z, Is.EqualTo(.5f).Within(.0001f));
        }

        [Test]
        public void TriangleFrequencyCreatesABarycentricLattice()
        {
            const int frequency = 3;
            var triangle = Shapes.Build(
                ShapeTypes.Triangle, frequency, Mathf.PI * .25f, Mathf.PI * .25f);

            Assert.That(triangle.IsValid, Is.True);
            Assert.That(triangle.Vertices.Count, Is.EqualTo((frequency + 1) * (frequency + 2) / 2));
            Assert.That(triangle.Faces.Count, Is.EqualTo(frequency * frequency));
            Assert.That(triangle.Faces.All(face => face.Sides == 3), Is.True);
            Assert.That(triangle.Halfedges.Count(edge => edge.Pair == null), Is.EqualTo(3 * frequency));
        }

        [Test]
        public void WireframeBoxIsOneConnectedManifoldSolid()
        {
            var poly = VariousSolids.WireframeBox(.1f);
            var visited = new HashSet<Face>();
            var pending = new Stack<Face>();
            pending.Push(poly.Faces[0]);

            while (pending.Count > 0)
            {
                var face = pending.Pop();
                if (!visited.Add(face)) continue;
                foreach (var edge in face.GetHalfedges())
                {
                    if (edge.Pair != null && !visited.Contains(edge.Pair.Face))
                        pending.Push(edge.Pair.Face);
                }
            }

            Assert.That(poly.Halfedges.All(edge => edge.Pair != null), Is.True);
            Assert.That(visited.Count, Is.EqualTo(poly.Faces.Count));
            Assert.That(poly.vef, Is.EqualTo((64, 144, 72)));
        }

        [Test]
        public void FilletEdgesProducesConnectedClosedTopologyOnTrivalentMesh()
        {
            var source = CreateCube();
            var filleted = source.FilletEdges(new OpParams(.15f, 3f));

            AssertClosed(filleted);
            Assert.That(filleted.Faces.Count, Is.GreaterThan(source.Faces.Count));
        }

        [Test]
        public void RevolveCreatesCappedPartialAndClosedFullTori()
        {
            var profile = Shapes.Polygon(8);
            foreach (var vertex in profile.Vertices)
                vertex.Position += Vector3.right * 2f;

            var partial = profile.Revolve(new OpParams(.5f, 12f), Axis.Z);
            var complete = profile.Revolve(new OpParams(1f, 12f), Axis.Z);

            AssertClosed(partial);
            AssertClosed(complete);
            Assert.That(partial.Faces.Count, Is.EqualTo(12 * 8 + 2));
            Assert.That(complete.Faces.Count, Is.EqualTo(12 * 8));
        }

        [Test]
        public void RevolveWeldsProfileVerticesOnTheAxis()
        {
            var profile = new PolyMesh(
                new[]
                {
                    new Vector3(0f, 0f, -1f), new Vector3(1f, 0f, -1f),
                    new Vector3(1f, 0f, 1f), new Vector3(0f, 0f, 1f)
                },
                new List<IEnumerable<int>> { new[] { 0, 3, 2, 1 } });

            var cylinder = profile.Revolve(new OpParams(1f, 12f), Axis.Z);

            AssertClosed(cylinder);
            Assert.That(cylinder.Vertices.Count, Is.EqualTo(12 * 2 + 2));
            Assert.That(cylinder.Faces.Count, Is.EqualTo(12 * 3));
        }

        [Test]
        public void PipeBoundaryCreatesClosedTubesAroundPolygonAndStar()
        {
            var polygonPipe = Shapes.Polygon(12).PipeBoundary(new OpParams(.2f, 8f));
            var starPipe = Shapes.Polygon(10, stellate: .5f).PipeBoundary(new OpParams(.15f, 6f));

            AssertClosed(polygonPipe);
            AssertClosed(starPipe);
            Assert.That(polygonPipe.vef, Is.EqualTo((12 * 8, 12 * 8 * 2, 12 * 8)));
            Assert.That(starPipe.Faces.Count, Is.EqualTo(10 * 6));
        }
        [Test]
        public void RectangularFrameHasAHoleAndUniformBorder()
        {
            var frame = Shapes.Frame(4f, 2f, .2f);
            var area = frame.Faces.Sum(face =>
            {
                var points = face.GetVertices().Select(vertex => vertex.Position).ToList();
                var faceArea = 0f;
                for (var index = 1; index < points.Count - 1; index++)
                    faceArea += Vector3.Cross(points[index] - points[0], points[index + 1] - points[0]).magnitude * .5f;
                return faceArea;
            });

            Assert.That(area, Is.EqualTo(8f - 3.6f * 1.6f).Within(.0001f));
            Assert.That(frame.Vertices.Count, Is.EqualTo(8));
            Assert.That(frame.Faces.Count, Is.EqualTo(4));
            Assert.That(frame.Faces.All(face => face.Sides == 4), Is.True);
            Assert.That(frame.Kis(new OpParams(.1f)).Faces.Count, Is.EqualTo(16));
            Assert.That(frame.Halfedges.Count(edge => edge.Pair == null), Is.EqualTo(8));
            Assert.That(frame.Vertices.Count - frame.EdgeCount + frame.Faces.Count, Is.Zero);
            Assert.That(frame.Faces.All(face => face.Normal.y > .99f), Is.True);
            AssertClosed(frame.Shell(new OpParams(.1f)));
        }

        [TestCase(.4f, 4)]
        [TestCase(.1f, 4)]
        [TestCase(1f, 4)]
        [TestCase(.4f, 0)]
        public void RoundedFramesShellIntoClosedRims(float radius, int segments)
        {
            var frame = Shapes.Frame(4f, 2f, .2f, radius, segments);
            var rim = frame.Shell(new OpParams(.1f));

            AssertClosed(rim);
            Assert.That(rim.Vertices.Count - rim.EdgeCount + rim.Faces.Count, Is.Zero);
            Assert.That(frame.GetBounds().size.x, Is.EqualTo(4f).Within(.0001f));
            Assert.That(frame.GetBounds().size.z, Is.EqualTo(2f).Within(.0001f));
        }

        [Test]
        public void RoundedFrameUsesQuadsAndOnlyCollapsedCornersUseTriangles()
        {
            var rounded = Shapes.Frame(4f, 2f, .2f, .4f, 4);
            Assert.That(rounded.Faces.Count, Is.EqualTo(20));
            Assert.That(rounded.Faces.All(face => face.Sides == 4), Is.True);

            var collapsed = Shapes.Frame(4f, 2f, .2f, .1f, 4);
            Assert.That(collapsed.Faces.Count(face => face.Sides == 4), Is.EqualTo(4));
            Assert.That(collapsed.Faces.Count(face => face.Sides == 3), Is.EqualTo(16));
            Assert.That(collapsed.Faces.All(face => face.Normal.y > .99f), Is.True);
        }

        [Test]
        public void FrameBuildUsesHeightBorderRadiusAndSegments()
        {
            var frame = Shapes.Build(ShapeTypes.Frame, .75f, .1f, .2f, d: 5f);

            Assert.That(frame.GetBounds().size.x, Is.EqualTo(1f).Within(.0001f));
            Assert.That(frame.GetBounds().size.z, Is.EqualTo(.75f).Within(.0001f));
            Assert.That(frame.Halfedges.Count(edge => edge.Pair == null), Is.EqualTo(48));
            Assert.That(Shapes.Frame(borderWidth: 0f).Faces.Count, Is.Zero);
            Assert.That(Shapes.Frame(borderWidth: .5f).Faces.Count, Is.EqualTo(1));
        }
        [TestCase(-.25f)]
        [TestCase(-1f)]
        public void NegativeRevolutionsAreClosedAndFaceOutward(float turns)
        {
            var profile = new PolyMesh(new[]
            {
                new Vector3(0f, -.5f, 0f), new Vector3(1f, -.5f, 0f),
                new Vector3(1f, .5f, 0f), new Vector3(0f, .5f, 0f)
            }, new List<IEnumerable<int>> { new[] { 0, 1, 2, 3 } });
            var solid = profile.Revolve(new OpParams(turns, 12f), Axis.Y);

            AssertClosed(solid);
            var signedVolume = 0f;
            foreach (var face in solid.Faces)
            {
                var points = face.GetVertices().Select(vertex => vertex.Position).ToList();
                for (var index = 1; index < points.Count - 1; index++)
                    signedVolume += Vector3.Dot(points[0],
                        Vector3.Cross(points[index], points[index + 1])) / 6f;
            }
            Assert.That(signedVolume, Is.GreaterThan(0f));
            if (turns > -1f)
            {
                Assert.That(solid.GetBounds().min.z, Is.GreaterThanOrEqualTo(-.0001f));
                Assert.That(solid.GetBounds().max.z, Is.EqualTo(1f).Within(.0001f));
                Assert.That(solid.Faces.Count(face => solid.FaceRoles[solid.Faces.IndexOf(face)] == Roles.Existing),
                    Is.EqualTo(2));
            }
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AxisTranslationLeavesOtherCoordinatesUnchanged(int axis)
        {
            var source = Shapes.Rectangle(4f, 2f);
            var before = source.Vertices.Select(vertex => vertex.Position).ToList();
            var translated = source.Translate(new OpParams(.25f), axis);

            for (var index = 0; index < before.Count; index++)
            {
                var expected = before[index];
                expected[axis] += .25f;
                Assert.That((translated.Vertices[index].Position - expected).sqrMagnitude,
                    Is.LessThan(1e-12f));
            }
        }
        [TestCase(false)]
        [TestCase(true)]
        public void TransformLoftUsesTheProfileCentroidAndPreservesItsInput(bool triangulateSides)
        {
            var profile = Shapes.Rectangle(4f, 2f);
            profile.Transform(new Vector3(3f, 2f, -4f));
            var before = profile.Vertices.Select(vertex => vertex.Position).ToList();
            var pivot = new Vector3(3f, 2f, -4f);
            var translation = new Vector3(.3f, 1f, -.2f);
            var rotation = Quaternion.Euler(15f, 20f, 10f);
            var scale = new Vector3(.5f, 1f, 1.5f);
            var solid = profile.Loft(translation, rotation, scale, triangulateSides: triangulateSides);

            AssertClosed(solid);
            Assert.That(solid.Vertices.Count, Is.EqualTo(8));
            var sideFaceCount = triangulateSides ? 8 : 4;
            Assert.That(solid.Faces.Count, Is.EqualTo(sideFaceCount + 2));
            Assert.That(solid.Faces.Take(sideFaceCount).All(face =>
                face.Sides == (triangulateSides ? 3 : 4)), Is.True);
            Assert.That(solid.Faces.Skip(sideFaceCount).All(face => face.Sides == 4), Is.True);
            for (var index = 0; index < before.Count; index++)
            {
                Assert.That(profile.Vertices[index].Position, Is.EqualTo(before[index]));
                var expected = pivot + translation + rotation * Vector3.Scale(before[index] - pivot, scale);
                Assert.That(solid.Vertices.Any(vertex => (vertex.Position - expected).sqrMagnitude < 1e-10f), Is.True);
            }
        }

        [TestCase(1f, false)]
        [TestCase(-1f, false)]
        [TestCase(1f, true)]
        [TestCase(-1f, true)]
        public void TransformLoftCapsFaceOutwardInEitherDirection(float height, bool triangulateSides)
        {
            var solid = Shapes.Polygon(8).Loft(Vector3.up * height, Quaternion.identity, Vector3.one,
                triangulateSides: triangulateSides);
            AssertClosed(solid);
            foreach (var face in solid.Faces)
                Assert.That(Vector3.Dot(face.Normal, face.Centroid - Vector3.up * height * .5f),
                    Is.GreaterThan(0f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TransformLoftSupportsOpenEndsAndCollapsedApex(bool triangulateSides)
        {
            var profile = Shapes.Polygon(8);
            var open = profile.Loft(Vector3.up, Quaternion.identity, Vector3.one, capEnds: false,
                triangulateSides: triangulateSides);
            Assert.That(open.Faces.Count, Is.EqualTo(triangulateSides ? 16 : 8));
            Assert.That(open.Faces.All(face => face.Sides == (triangulateSides ? 3 : 4)), Is.True);
            Assert.That(open.Halfedges.Count(edge => edge.Pair == null), Is.EqualTo(16));

            var cone = profile.Loft(Vector3.up, Quaternion.identity, Vector3.zero,
                triangulateSides: triangulateSides);
            AssertClosed(cone);
            Assert.That(cone.Vertices.Count, Is.EqualTo(9));
            Assert.That(cone.Faces.Count(face => face.Sides == 3), Is.EqualTo(8));
            Assert.That(cone.Faces.Count(face => face.Sides == 8), Is.EqualTo(1));
        }
        [Test]
        public void ZeroSweepKeepsThinWedgeDefaultAndOffersExplicitAlternatives()
        {
            var profile = Shapes.Rectangle();
            profile.Transform(Vector3.right * 2f);
            var settings = new OpParams(0f, 8f);
            var defaultResult = profile.Revolve(settings, Axis.Z);
            var wedge = profile.Revolve(settings, Axis.Z, PolyMesh.ZeroSweepBehavior.ThinWedge);
            AssertClosed(defaultResult);
            Assert.That(defaultResult.Faces.Count, Is.EqualTo(34));
            Assert.That(defaultResult.Vertices.Select(vertex => vertex.Position),
                Is.EqualTo(wedge.Vertices.Select(vertex => vertex.Position)));

            var flat = profile.Revolve(settings, Axis.Z, PolyMesh.ZeroSweepBehavior.DoubleSidedProfile);
            Assert.That(flat.Faces.Count, Is.EqualTo(2));
            Assert.That(flat.Vertices.Count, Is.EqualTo(4));
            Assert.That(Vector3.Dot(flat.Faces[0].Normal, flat.Faces[1].Normal),
                Is.EqualTo(-1f).Within(.0001f));
            Assert.That(flat.Vertices.Select(vertex => vertex.Position),
                Is.EquivalentTo(profile.Vertices.Select(vertex => vertex.Position)));

            var empty = profile.Revolve(settings, Axis.Z, PolyMesh.ZeroSweepBehavior.Empty);
            Assert.That(empty.Vertices.Count, Is.Zero);
            Assert.That(empty.Faces.Count, Is.Zero);
        }
        // Independent analytic samples used only as a volume oracle. Geometry is
        // generated by the library-owned Shapes.ConicProfile API.
        private static List<Vector3> SampleConicForRevolve(int curve, float bottom, float top)
        {
            var samples = new List<Vector3>();
            for (var row = 0; row <= 8; row++)
            {
                var t = Mathf.Lerp(bottom, top, row / 8f);
                var u = -Mathf.Cos(Mathf.PI * t);
                var height = curve == 1 ? .4f * u : .5f * u;
                var radius = curve switch
                {
                    0 => .5f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)),
                    1 => .6f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)),
                    2 => .3f * Mathf.Sqrt(1f + u * u),
                    _ => .2f + .5f * u * u
                };
                samples.Add(new Vector3(radius, height, 0f));
            }
            return samples;
        }

        [TestCase(0)] // Circle
        [TestCase(1)] // Ellipse
        [TestCase(2)] // Hyperbola
        [TestCase(3)] // Parabola
        public void SampledConicsRevolveWithCorrectVolumeAndClosedCaps(int curve)
        {
            const int sides = 12;
            foreach (var clipped in new[] { false, true })
            foreach (var turns in new[] { 1f, .25f, -.25f })
            {
                var samples = SampleConicForRevolve(curve, clipped ? .2f : 0f, clipped ? .8f : 1f);
                var majorAxis = curve switch { 1 => 1.2f, 2 => .6f, 3 => .25f, _ => 1f };
                var profile = Shapes.ConicProfile((Shapes.ConicType)curve, segments: 8,
                    radius: .5f, majorAxis: majorAxis, minorAxis: curve == 1 ? .8f : 1f,
                    centerOffset: .4f, bottomClip: clipped ? .2f : 0f, topClip: clipped ? .8f : 1f);
                Assert.That(profile.Faces.Count, Is.EqualTo(1));
                Assert.That(profile.Faces[0].Normal.z, Is.GreaterThan(.99f));
                var solid = profile.Revolve(new OpParams(turns, sides), Axis.Y);
                AssertClosed(solid);
                Assert.That(solid.Vertices.Count - solid.EdgeCount + solid.Faces.Count, Is.EqualTo(2));

                // Exact volume of the piecewise-linear profile's polygonal frusta.
                var expectedVolume = 0f;
                for (var row = 1; row < samples.Count; row++)
                {
                    var a = samples[row - 1];
                    var b = samples[row];
                    expectedVolume += (b.y - a.y) * (a.x * a.x + a.x * b.x + b.x * b.x);
                }
                expectedVolume *= sides * Mathf.Sin(Mathf.Abs(turns) * Mathf.PI * 2f / sides) / 6f;
                var mesh = solid.BuildMeshData(useSmoothing: false);
                var renderedVolume = 0f;
                for (var triangle = 0; triangle < mesh.meshTriangles.Count; triangle += 3)
                {
                    var a = mesh.meshVertices[mesh.meshTriangles[triangle]];
                    var b = mesh.meshVertices[mesh.meshTriangles[triangle + 1]];
                    var c = mesh.meshVertices[mesh.meshTriangles[triangle + 2]];
                    renderedVolume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                }
                Assert.That(renderedVolume, Is.EqualTo(expectedVolume).Within(.0001f),
                    $"Curve {curve}, clipped {clipped}, turns {turns}");
            }
        }
        [Test]
        public void CapsuleProfileMatchesTheExistingFullCapsule()
        {
            var profile = Shapes.CapsuleProfile(4, 2f, .5f);
            var revolved = profile.Revolve(new OpParams(-1f, 12f), Axis.Y);
            var capsule = VariousSolids.Capsule(12, 4, 2f, .5f);
            AssertClosed(revolved);
            AssertClosed(capsule);
            Assert.That(revolved.Vertices.Count, Is.EqualTo(capsule.Vertices.Count));
            Assert.That(revolved.Faces.Count, Is.EqualTo(capsule.Faces.Count));
            foreach (var point in capsule.Vertices.Select(vertex => vertex.Position))
                Assert.That(revolved.Vertices.Any(vertex => (vertex.Position - point).sqrMagnitude < 1e-10f), Is.True);
        }

        [TestCase(90f)]
        [TestCase(-90f)]
        [TestCase(-360f)]
        public void CapsuleGeneratorSupportsSignedSweeps(float angle)
        {
            var capsule = VariousSolids.Capsule(12, 4, 2f, .5f, sweepAngle: angle);
            AssertClosed(capsule);
            Assert.That(capsule.Vertices.Count - capsule.EdgeCount + capsule.Faces.Count, Is.EqualTo(2));
            Assert.That(capsule.GetBounds().size.y, Is.EqualTo(3f).Within(.0001f));
            if (angle == 90f)
                Assert.That(capsule.GetBounds().min.z, Is.GreaterThanOrEqualTo(-.0001f));
            if (angle == -90f)
                Assert.That(capsule.GetBounds().max.z, Is.LessThanOrEqualTo(.0001f));
        }

        [TestCase(360f, 1f, .5f)]
        [TestCase(-360f, 1f, 0f)]
        [TestCase(300f, 0f, 1f)]
        [TestCase(-300f, 1f, .5f)]
        [TestCase(90f, 1f, 0f)]
        [TestCase(-90f, 1f, 0f)]
        public void CylinderSweepsAreClosedAndHaveCorrectRenderedVolume(float sweep,
            float radiusBottom, float radiusTop)
        {
            const int sides = 6;
            const float height = 2f;
            var mesh = VariousSolids.Cylinder(sides, height, radiusBottom, radiusTop, sweep);
            AssertClosed(mesh);
            Assert.That(mesh.Vertices.Count - mesh.Halfedges.Count / 2 + mesh.Faces.Count, Is.EqualTo(2));
            var data = mesh.BuildMeshData();
            var volume = 0f;
            for (var index = 0; index < data.meshTriangles.Count; index += 3)
            {
                var a = data.meshVertices[data.meshTriangles[index]];
                var b = data.meshVertices[data.meshTriangles[index + 1]];
                var c = data.meshVertices[data.meshTriangles[index + 2]];
                volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
            }
            var expected = sides * Mathf.Sin(Mathf.Abs(sweep) * Mathf.Deg2Rad / sides) * height / 6f *
                (radiusBottom * radiusBottom + radiusBottom * radiusTop + radiusTop * radiusTop);
            Assert.That(volume, Is.EqualTo(expected).Within(.0001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CylinderTransformsEndsIndependentlyWithoutShiftingTheSectorCentre(bool triangulate)
        {
            const int sides = 4;
            var offset = new Vector2(.3f, -.2f);
            var rotation = Quaternion.Euler(15f, 30f, 10f);
            var lowerScale = new Vector2(2f, .5f);
            var upperScale = new Vector2(.75f, 1.5f);
            var mesh = VariousSolids.Cylinder(sides, 2f, 1f, .6f, 90f,
                offset, rotation, lowerScale, upperScale, triangulateSides: triangulate);
            AssertClosed(mesh);
            var wallCount = sides * (triangulate ? 2 : 1);
            Assert.That(mesh.Faces.Count, Is.EqualTo(wallCount + 4));
            Assert.That(mesh.Faces.Take(wallCount).All(face => face.Sides == (triangulate ? 3 : 4)), Is.True);
            // Neither profile rotation nor partial-sweep centroid moves the end centres.
            Assert.That(mesh.Vertices.Any(vertex => vertex.Position == Vector3.down), Is.True);
            Assert.That(mesh.Vertices.Any(vertex => vertex.Position == new Vector3(.3f, 1f, -.2f)), Is.True);
            for (var index = 0; index <= sides; index++)
            {
                var angle = 90f * Mathf.Deg2Rad * index / sides;
                var expectedLower = Vector3.down + new Vector3(Mathf.Cos(angle) * 2f, 0f,
                    Mathf.Sin(angle) * .5f);
                var expectedUpper = new Vector3(.3f, 1f, -.2f) + rotation * new Vector3(
                    Mathf.Cos(angle) * .6f * .75f, 0f, Mathf.Sin(angle) * .6f * 1.5f);
                Assert.That(mesh.Vertices.Any(vertex => (vertex.Position - expectedLower).sqrMagnitude < 1e-10f), Is.True);
                Assert.That(mesh.Vertices.Any(vertex => (vertex.Position - expectedUpper).sqrMagnitude < 1e-10f), Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CylinderSmoothsOnlyTheWallEvenWhenCutAnglesAreSmaller(bool triangulate)
        {
            var mesh = VariousSolids.Cylinder(3, sweepAngle: 300f, smoothSides: true,
                triangulateSides: triangulate);
            AssertClosed(mesh);
            var walls = new HashSet<Face>(mesh.Faces.Take(triangulate ? 6 : 3));
            foreach (var edge in mesh.Halfedges)
                Assert.That(edge.IsEdgeSmooth, Is.EqualTo(walls.Contains(edge.Face) && walls.Contains(edge.Pair.Face)));
            Assert.That(mesh.Halfedges.Any(edge => edge.IsEdgeSmooth && edge.DihedralAngle > 99f), Is.True);
            Assert.That(mesh.Halfedges.Any(edge => !edge.IsEdgeSmooth && edge.DihedralAngle < 41f), Is.True);
            var faceted = VariousSolids.Cylinder(3, sweepAngle: 300f, triangulateSides: triangulate);
            Assert.That(faceted.Halfedges.All(edge => !edge.IsEdgeSmooth), Is.True);
        }

        [Test]
        public void CylinderKeepsSmallNonzeroAndNearFullSweepsDistinctFromTheirLimits()
        {
            Assert.That(VariousSolids.Cylinder(sweepAngle: 0f).Faces, Is.Empty);
            Assert.That(VariousSolids.Cylinder(radiusBottom: 0f, radiusTop: 0f).Faces, Is.Empty);
            var tiny = VariousSolids.Cylinder(3, sweepAngle: .01f);
            Assert.That(tiny.GetBounds().max.z, Is.EqualTo(.5f * Mathf.Sin(.01f * Mathf.Deg2Rad)).Within(1e-7f));
            var almostFull = VariousSolids.Cylinder(3, sweepAngle: 359.9999f);
            Assert.That(almostFull.Faces.Count, Is.EqualTo(7));
            Assert.That(VariousSolids.Cylinder(3).Faces.Count, Is.EqualTo(5));
        }

        [TestCase(Shapes.ConicType.Circle, 360f, 0f, 1f)]
        [TestCase(Shapes.ConicType.Circle, -300f, 0f, 1f)]
        [TestCase(Shapes.ConicType.Ellipse, 300f, .2f, .8f)]
        [TestCase(Shapes.ConicType.Hyperbola, -360f, 0f, 1f)]
        [TestCase(Shapes.ConicType.Parabola, 300f, 0f, 1f)]
        public void ConicSolidMatchesProfileRevolutionAndIsClosed(Shapes.ConicType type,
            float sweep, float bottom, float top)
        {
            var solid = VariousSolids.Conic(type, 12, 8, bottomClip: bottom, topClip: top,
                sweepAngle: sweep);
            var revolved = Shapes.ConicProfile(type, 8, bottomClip: bottom, topClip: top)
                .Revolve(new OpParams(-sweep / 360f, 12), Axis.Y);

            AssertClosed(solid);
            // End-cap tessellation may differ; sampled surface positions must agree.
            foreach (var vertex in solid.Vertices)
                Assert.That(revolved.Vertices.Any(other =>
                    Vector3.Distance(vertex.Position, other.Position) < .00001f), Is.True);
            // Full caps use polygons rather than a fan with an extra axis vertex.
            foreach (var vertex in revolved.Vertices.Where(vertex => Mathf.Abs(sweep) < 360f ||
                         new Vector2(vertex.Position.x, vertex.Position.z).sqrMagnitude > .0000000001f))
                Assert.That(solid.Vertices.Any(other =>
                    Vector3.Distance(vertex.Position, other.Position) < .00001f), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConicSmoothsTheBodyWithoutSmoothingItsShallowerCuts(bool triangulate)
        {
            var solid = VariousSolids.Conic(Shapes.ConicType.Circle, 3, 2,
                sweepAngle: 300f, smooth: true, triangulateSides: triangulate);
            AssertClosed(solid);
            var body = new HashSet<Face>(solid.Faces.Where((face, index) =>
                solid.FaceRoles[index] != Roles.Existing));
            var bodyEdges = solid.Halfedges.Where(edge => edge.Pair != null &&
                body.Contains(edge.Face) && body.Contains(edge.Pair.Face)).ToList();
            var cutEdges = solid.Halfedges.Where(edge => edge.Pair != null &&
                body.Contains(edge.Face) != body.Contains(edge.Pair.Face)).ToList();

            Assert.That(bodyEdges, Is.Not.Empty);
            Assert.That(cutEdges, Is.Not.Empty);
            Assert.That(bodyEdges.All(edge => edge.IsSmooth), Is.True);
            Assert.That(cutEdges.All(edge => !edge.IsSmooth), Is.True);
            Assert.That(bodyEdges.Max(edge => edge.DihedralAngle),
                Is.GreaterThan(cutEdges.Min(edge => edge.DihedralAngle)));
        }

        [TestCase(-.1f, -1f, Axis.Y)]
        [TestCase(-.1f, 1f, Axis.Y)]
        [TestCase(-.1f, -.25f, Axis.Y)]
        [TestCase(-.1f, .25f, Axis.Y)]
        [TestCase(-.125f, -1f, Axis.Y)]
        [TestCase(-.125f, -.25f, Axis.Y)]
        [TestCase(-.3f, -1f, Axis.Y)]
        [TestCase(-.3f, -.25f, Axis.Y)]
        [TestCase(-.1f, -.25f, Axis.X)]
        [TestCase(-.1f, .25f, Axis.Z)]
        public void SignedParabolaRevolvePreservesEdgePairingAndNegativeRadii(float offset, float turns, Axis axis)
        {
            var profile = Shapes.ConicProfile(Shapes.ConicType.Parabola, 16, radius: .5f,
                majorAxis: 1f, centerOffset: offset);
            Assert.That(profile.Vertices.Any(vertex => vertex.Position.x < 0f), Is.True);
            if (offset > -.25f)
                Assert.That(profile.Vertices.Any(vertex => vertex.Position.x > 0f), Is.True);
            var rotation = axis == Axis.X ? new Vector3(0f, 0f, -90f)
                : axis == Axis.Z ? new Vector3(90f, 0f, 0f) : Vector3.zero;
            profile.Transform(Vector3.zero, rotation);
            var before = profile.Vertices.Select(vertex => vertex.Position).ToList();
            var solid = profile.Revolve(new OpParams(turns, 12f), axis);
            // Edge pairing is separate from geometric self-intersection: the folded
            // parameterization is retained, rather than silently clamping the radius.
            AssertClosed(solid);
            foreach (var position in before)
                Assert.That(solid.Vertices.Any(vertex => (vertex.Position - position).sqrMagnitude < 1e-10f), Is.True);
        }
    }
}
