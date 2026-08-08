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
        public void TriangleUsesTwoNormalizedBaseAnglesAndIgnoresC()
        {
            var right = Shapes.Build(ShapeTypes.Triangle, .25f, .125f, 0f);
            var isoscelesRight = Shapes.Build(ShapeTypes.Triangle, .125f, .125f, 0f);
            var differentC = Shapes.Build(ShapeTypes.Triangle, .125f, .125f, 4f);
            var rightLeft = right.Vertices[2].Position - right.Vertices[0].Position;
            var isoscelesApex = isoscelesRight.Vertices[2].Position -
                                isoscelesRight.Vertices[0].Position;

            Assert.That(rightLeft.x, Is.EqualTo(0f).Within(.0001f));
            Assert.That(rightLeft.z, Is.EqualTo(1f).Within(.0001f));
            Assert.That(isoscelesApex.x, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(isoscelesApex.z, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(differentC.Vertices.Select(vertex => vertex.Position),
                Is.EqualTo(isoscelesRight.Vertices.Select(vertex => vertex.Position)));
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
    }
}
