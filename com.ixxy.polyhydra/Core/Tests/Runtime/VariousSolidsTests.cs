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
        public void TriangleAndSectorExtrudeToClosedSolids()
        {
            var wedge = Shapes.Triangle().LayeredExtrude(1, 1f, Axis.Y);
            var halfCylinder = Shapes.Sector(8, .5f).LayeredExtrude(1, 1f, Axis.Y);

            AssertClosed(wedge);
            AssertClosed(halfCylinder);
            Assert.That(wedge.Faces.Count, Is.EqualTo(5));
            Assert.That(halfCylinder.Faces.Count, Is.EqualTo(8 + 4));
        }

        [Test]
        public void SectorUsesNormalizedAngleAndHalfTurnProducesASemicircle()
        {
            var quarter = Shapes.Build(ShapeTypes.Sector, 8f, 0f, .25f);
            var half = Shapes.Build(ShapeTypes.Sector, 8f, 0f, .5f);

            Assert.That(quarter.Vertices[1].Position.x, Is.EqualTo(0f).Within(.0001f));
            Assert.That(quarter.Vertices[1].Position.z, Is.EqualTo(1f).Within(.0001f));
            Assert.That(half.Vertices[1].Position.x, Is.EqualTo(-1f).Within(.0001f));
            Assert.That(half.Vertices[1].Position.z, Is.EqualTo(0f).Within(.0001f));
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
    }
}
