using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Polyhydra.Core.Tests
{
    public class PolyMeshSmoothingTests
    {
        private static PolyMesh CreateTwoFaceMesh()
        {
            var vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 0f, 1f)
            };
            var faces = new List<IEnumerable<int>>
            {
                new[] { 0, 1, 2 },
                new[] { 1, 0, 3 }
            };
            return new PolyMesh(vertices, faces);
        }

        private static Halfedge GetPairedEdge(PolyMesh poly)
        {
            return poly.Halfedges.First(edge => edge.Pair != null);
        }

        [Test]
        public void EitherHalfedgeMakesPairEffectivelySmooth()
        {
            var edge = GetPairedEdge(CreateTwoFaceMesh());

            Assert.That(edge.IsEdgeSmooth, Is.False);
            edge.IsSmooth = true;
            Assert.That(edge.IsEdgeSmooth, Is.True);
            Assert.That(edge.Pair.IsEdgeSmooth, Is.True);
            Assert.That(edge.Pair.IsSmooth, Is.False);

            edge.IsSmooth = false;
            edge.Pair.IsSmooth = true;
            Assert.That(edge.IsEdgeSmooth, Is.True);
            Assert.That(edge.Pair.IsEdgeSmooth, Is.True);
        }

        [Test]
        public void BoundaryMarkerDoesNotMakeBoundaryEffectivelySmooth()
        {
            var boundary = CreateTwoFaceMesh().Halfedges.First(edge => edge.Pair == null);

            boundary.IsSmooth = true;

            Assert.That(boundary.IsEdgeSmooth, Is.False);
        }

        [Test]
        public void AutoSmoothClassifiesUsingFinalDihedralAngle()
        {
            var poly = CreateTwoFaceMesh();
            var edge = GetPairedEdge(poly);
            var angle = edge.DihedralAngle;

            poly.AutoSmooth(angle - 0.1f);
            Assert.That(edge.IsEdgeSmooth, Is.False);

            poly.AutoSmooth(angle + 0.1f);
            Assert.That(edge.IsEdgeSmooth, Is.True);
            Assert.That(poly.Halfedges.Where(item => item.Pair == null).All(item => !item.IsSmooth), Is.True);
        }

        [Test]
        public void SmoothPairBlendsOnlySharedVertexNormals()
        {
            var poly = CreateTwoFaceMesh();
            var edge = GetPairedEdge(poly);
            var faceNormal = poly.Faces[0].Normal;

            var hardMeshData = poly.BuildMeshData();
            Assert.That(Vector3.Angle(hardMeshData.meshNormals[0], faceNormal), Is.LessThan(0.001f));
            Assert.That(Vector3.Angle(hardMeshData.meshNormals[1], faceNormal), Is.LessThan(0.001f));
            Assert.That(Vector3.Angle(hardMeshData.meshNormals[2], faceNormal), Is.LessThan(0.001f));

            edge.IsSmooth = true;
            var smoothMeshData = poly.BuildMeshData();
            var faceEdges = poly.Faces[0].GetHalfedges();
            for (var cornerIndex = 0; cornerIndex < faceEdges.Count; cornerIndex++)
            {
                var isSharedVertex = faceEdges[cornerIndex].Vertex == edge.Vertex ||
                                     faceEdges[cornerIndex].Vertex == edge.Prev.Vertex;
                var angleFromFace = Vector3.Angle(smoothMeshData.meshNormals[cornerIndex], faceNormal);
                if (isSharedVertex)
                {
                    Assert.That(angleFromFace, Is.GreaterThan(0.001f));
                }
                else
                {
                    Assert.That(angleFromFace, Is.LessThan(0.001f));
                }
            }

            Assert.That(smoothMeshData.meshColors, Is.EqualTo(hardMeshData.meshColors));
        }

        [Test]
        public void BuildMeshDataCanIgnoreStoredSmoothing()
        {
            var poly = CreateTwoFaceMesh();
            var edge = GetPairedEdge(poly);
            edge.IsSmooth = true;

            var meshData = poly.BuildMeshData(useSmoothing: false);
            for (var cornerIndex = 0; cornerIndex < poly.Faces[0].Sides; cornerIndex++)
            {
                Assert.That(
                    Vector3.Angle(meshData.meshNormals[cornerIndex], poly.Faces[0].Normal),
                    Is.LessThan(0.001f)
                );
            }
        }

        [Test]
        public void DuplicatePreservesLocalSmoothingMarkers()
        {
            var poly = CreateTwoFaceMesh();
            var sourceEdge = GetPairedEdge(poly);
            sourceEdge.IsSmooth = true;

            var duplicate = poly.Duplicate();
            var duplicateMarkers = duplicate.Halfedges.Count(edge => edge.IsSmooth);

            Assert.That(duplicateMarkers, Is.EqualTo(1));
            Assert.That(GetPairedEdge(duplicate).IsEdgeSmooth, Is.True);
        }
        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        public void ConcaveFacesRenderWithCorrectAreaAndWindingInDifferentPlanes(float angle)
        {
            var vertices = new[]
            {
                new Vector3(-2f, -2f, 0f), new Vector3(2f, -2f, 0f),
                new Vector3(2f, 2f, 0f), new Vector3(1f, 2f, 0f),
                new Vector3(1f, -1f, 0f), new Vector3(-1f, -1f, 0f),
                new Vector3(-1f, 2f, 0f), new Vector3(-2f, 2f, 0f)
            };
            var rotation = Quaternion.Euler(0f, angle, 0f);
            var poly = new PolyMesh(vertices.Select(vertex => rotation * vertex),
                new List<IEnumerable<int>> { Enumerable.Range(0, 8) });
            Assert.That(poly.Faces[0].AreAllVertsVisibleFromCentroid(), Is.False);
            var mesh = poly.BuildMeshData();
            var area = 0f;
            for (var index = 0; index < mesh.meshTriangles.Count; index += 3)
            {
                var a = mesh.meshVertices[mesh.meshTriangles[index]];
                var b = mesh.meshVertices[mesh.meshTriangles[index + 1]];
                var c = mesh.meshVertices[mesh.meshTriangles[index + 2]];
                var cross = Vector3.Cross(b - a, c - a);
                area += cross.magnitude * .5f;
                Assert.That(Vector3.Dot(cross, poly.Faces[0].Normal), Is.GreaterThan(0f));
            }
            Assert.That(area, Is.EqualTo(10f).Within(.0001f));
        }

        [TestCase(true, false, 1f, false)]
        [TestCase(true, false, -1f, false)]
        [TestCase(false, false, 1f, false)]
        [TestCase(true, true, 1f, false)]
        [TestCase(true, false, 1f, true)]
        [TestCase(true, false, -1f, true)]
        [TestCase(false, false, 1f, true)]
        [TestCase(true, true, 1f, true)]
        public void TransformLoftSmoothsSidesButKeepsCapsSharp(bool capEnds, bool apex, float height,
            bool triangulateSides)
        {
            var profile = Shapes.Polygon(4);
            var scale = apex ? Vector3.zero : Vector3.one;
            var faceted = profile.Loft(Vector3.up * height, Quaternion.identity, scale, capEnds,
                triangulateSides: triangulateSides);
            var smooth = profile.Loft(Vector3.up * height, Quaternion.identity, scale, capEnds,
                smoothSides: true, triangulateSides: triangulateSides);

            Assert.That(faceted.Halfedges.All(edge => !edge.IsEdgeSmooth), Is.True);
            Assert.That(profile.Halfedges.All(edge => !edge.IsSmooth), Is.True);
            var split = triangulateSides && !apex;
            Assert.That(smooth.Halfedges.Count(edge => edge.IsEdgeSmooth), Is.EqualTo(split ? 16 : 8));
            var caps = new HashSet<Face>(smooth.Faces.Skip(split ? 8 : 4));
            Assert.That(caps.SelectMany(face => face.GetHalfedges())
                .All(edge => !edge.IsEdgeSmooth), Is.True);

            // Equal-angle side and cap edges cannot be separated by AutoSmooth.
            if (capEnds && !apex && !triangulateSides)
                Assert.That(smooth.Halfedges.All(edge =>
                    Mathf.Abs(edge.DihedralAngle - 90f) < .001f), Is.True);
            var data = smooth.BuildMeshData();
            for (var index = 0; index < data.meshNormals.Count; index++)
            {
                var face = smooth.Faces[Mathf.RoundToInt(data.miscUVs2[index].w)];
                // Fan triangulation also emits face centroids, which retain the face normal.
                if (!face.GetVertices().Any(vertex => vertex.Position == data.meshVertices[index])) continue;
                var angle = Vector3.Angle(data.meshNormals[index], face.Normal);
                if (caps.Contains(face)) Assert.That(angle, Is.LessThan(.001f));
                else Assert.That(angle, Is.GreaterThan(1f));
            }
        }

        [TestCase(360f, 2f)]
        [TestCase(-360f, 2f)]
        [TestCase(90f, 2f)]
        [TestCase(-90f, 2f)]
        [TestCase(360f, 0f)]
        public void CapsuleSmoothsBodyIncludingCapJoinsButKeepsSweepCutsSharp(float sweep, float height)
        {
            var faceted = VariousSolids.Capsule(4, 3, height, sweepAngle: sweep);
            var smooth = VariousSolids.Capsule(4, 3, height, sweepAngle: sweep, smooth: true);
            Assert.That(faceted.Halfedges.All(edge => !edge.IsEdgeSmooth), Is.True);
            Assert.That(smooth.Halfedges.Any(edge => edge.IsEdgeSmooth), Is.True);
            var cuts = new HashSet<Face>(smooth.Faces.Where((face, index) =>
                smooth.FaceRoles[index] == Roles.Existing));
            Assert.That(cuts.Count, Is.EqualTo(Mathf.Abs(sweep) < 360f ? 2 : 0));
            foreach (var edge in smooth.Halfedges)
            {
                Assert.That(edge.Pair, Is.Not.Null);
                var touchesCut = cuts.Contains(edge.Face) || cuts.Contains(edge.Pair.Face);
                Assert.That(edge.IsEdgeSmooth, Is.EqualTo(!touchesCut));
            }
        }

        [TestCase(PolyMesh.ZeroSweepBehavior.Empty)]
        [TestCase(PolyMesh.ZeroSweepBehavior.DoubleSidedProfile)]
        public void CapsuleZeroSweepDoesNotSmoothOppositeProfileFaces(PolyMesh.ZeroSweepBehavior behavior)
        {
            var poly = VariousSolids.Capsule(sweepAngle: 0f, zeroSweepBehavior: behavior, smooth: true);
            Assert.That(poly.Halfedges.All(edge => !edge.IsEdgeSmooth), Is.True);
        }
    }
}
