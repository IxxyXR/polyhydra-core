using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Polyhydra.Core.Tests
{
    public class PanelAndBendTests
    {
        [TestCase(0f, 0f, RectangleCorners.All, 8, 8)]
        [TestCase(.2f, .08f, RectangleCorners.All, 8, 8)]
        [TestCase(.2f, .08f, RectangleCorners.None, 8, 8)]
        [TestCase(.2f, .08f, RectangleCorners.TopLeft | RectangleCorners.BottomRight, 8, 1)]
        [TestCase(.2f, .08f, RectangleCorners.TopLeft | RectangleCorners.BottomRight, 1, 8)]
        [TestCase(.1f, .3f, RectangleCorners.All, 8, 8)]
        public void PanelHasClosedNondegenerateTopology(float corner, float edge,
            RectangleCorners corners, int cornerSegments, int edgeSegments)
        {
            var panel = VariousSolids.Panel(1.4f, .8f, .6f, corner, edge, corners,
                cornerSegments, edgeSegments, 3, 2);
            Assert.That(panel.Halfedges.All(halfedge => halfedge.Pair != null), Is.True);
            Assert.That(panel.Vertices.All(vertex => vertex.Halfedges.Count > 0), Is.True);
            Assert.That(panel.Faces.All(face => face.Normal.sqrMagnitude > 0f), Is.True);
            Assert.That(panel.Vertices.Count - panel.EdgeCount + panel.Faces.Count, Is.EqualTo(2));
            Assert.That(Vector3.Distance(panel.GetBounds().size, new Vector3(1.4f, .8f, .6f)), Is.LessThan(.00001f));
        }

        [Test]
        public void MaximumRoundingClosesAtThePolesWithoutDuplicateVertices()
        {
            var panel = VariousSolids.Panel(1f, 1f, 1f, 10f, 10f, widthSegments: 3, heightSegments: 2);
            Assert.That(panel.Halfedges.All(edge => edge.Pair != null), Is.True);
            Assert.That(panel.Vertices.Count - panel.EdgeCount + panel.Faces.Count, Is.EqualTo(2));
            Assert.That(panel.Vertices.Count(vertex => vertex.Position == Vector3.forward * .5f), Is.EqualTo(1));
            Assert.That(panel.Vertices.Count(vertex => vertex.Position == Vector3.back * .5f), Is.EqualTo(1));
        }

        [Test]
        public void RectangleRoundsOnlySelectedCornersAndRemainsOneFace()
        {
            var rectangle = Shapes.Rectangle(2f, 1f, .2f, 4,
                RectangleCorners.TopLeft | RectangleCorners.BottomRight);
            Assert.That(rectangle.Faces.Count, Is.EqualTo(1));
            Assert.That(rectangle.Vertices.Any(v => v.Position == new Vector3(1f, 0f, .5f)), Is.True);
            Assert.That(rectangle.Vertices.Any(v => v.Position == new Vector3(-1f, 0f, -.5f)), Is.True);
            Assert.That(rectangle.Vertices.Any(v => v.Position == new Vector3(-1f, 0f, .5f)), Is.False);
            Assert.That(rectangle.Vertices.Any(v => v.Position == new Vector3(1f, 0f, -.5f)), Is.False);
            Assert.That(Shapes.Rectangle(2f, 1f).Vertices.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExistingFaceRemoveProducesAnOpenRearIncludingBevelFaces()
        {
            var panel = VariousSolids.Panel(1f, .8f, .3f, .2f, .08f);
            var open = panel.FaceRemove(Filter.FacingDirection(Vector3.back, 89.9f));
            Assert.That(open.Halfedges.Any(edge => edge.Pair == null), Is.True);
            Assert.That(open.Faces.All(face => Vector3.Dot(face.Normal, Vector3.back) <= .001f), Is.True);
            Assert.That(open.Faces.Any(face => Vector3.Dot(face.Normal, Vector3.forward) > .99f), Is.True);
        }

        [Test]
        public void SuccessiveBendsUseTheExplicitOriginalBounds()
        {
            var vertices = new[] { new Vector3(-2f, -1f, 0f), new Vector3(2f, -1f, 0f),
                new Vector3(2f, 1f, 0f), new Vector3(-2f, 1f, 0f) };
            var input = new PolyMesh(vertices, new List<List<int>> { new List<int> { 0, 1, 2, 3 } });
            var reference = new Bounds(Vector3.zero, new Vector3(4f, 2f, 1f));
            var result = input.Bend(180f, Axis.X, reference).Bend(180f, Axis.Y, reference);
            var expected = new Vector3(2f / Mathf.PI, 2f / Mathf.PI, 4f / Mathf.PI);
            Assert.That(result.Vertices.Any(vertex => Vector3.Distance(vertex.Position, expected) < .00001f), Is.True);
            Assert.That(input.Vertices[2].Position, Is.EqualTo(vertices[2]));
        }

        [TestCase(13f, 20f)]
        [TestCase(-7f, -5f)]
        [TestCase(0f, 360f)]
        [TestCase(360f, -360f)]
        public void SampledPanelsCanBendWithoutLosingConnectivityOrLeavingWarpedQuads(float x, float y)
        {
            var input = VariousSolids.Panel(2f, 1f, .05f, .1f, .01f,
                widthSegments: 16, heightSegments: 8);
            var reference = new Bounds(Vector3.zero, new Vector3(2f, 1f, .05f));
            var bent = input.Bend(x, Axis.X, reference).Bend(y, Axis.Y, reference);
            Assert.That(bent.Halfedges.All(edge => edge.Pair != null), Is.True);
            Assert.That(bent.Vertices.Count - bent.EdgeCount + bent.Faces.Count, Is.EqualTo(2));
            foreach (var face in bent.Faces.Where(face => face.Sides == 4))
            {
                var points = face.GetVertices().Select(vertex => vertex.Position).ToArray();
                var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
                var span = points.Max(point => Vector3.Distance(points[0], point));
                Assert.That(Mathf.Abs(Vector3.Dot(normal, points[3] - points[0])),
                    Is.LessThanOrEqualTo(normal.magnitude * span * .00002f));
            }
        }

        [Test]
        public void TinyBendIsStableAndZeroBendPreservesSmoothing()
        {
            var input = VariousSolids.Panel(1f, .8f, .2f, .1f, .05f);
            var reference = new Bounds(Vector3.zero, new Vector3(1f, .8f, .2f));
            var zero = input.Bend(0f, Axis.X, reference);
            Assert.That(zero.Halfedges.Select(edge => edge.IsSmooth), Is.EqualTo(input.Halfedges.Select(edge => edge.IsSmooth)));
            var tiny = input.Bend(.000001f, Axis.X, reference);
            for (var i = 0; i < input.Vertices.Count; i++)
                Assert.That(Vector3.Distance(tiny.Vertices[i].Position, input.Vertices[i].Position), Is.LessThan(.00001f));
        }
    }
}
