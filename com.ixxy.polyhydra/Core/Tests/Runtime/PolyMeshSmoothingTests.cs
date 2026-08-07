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
    }
}
