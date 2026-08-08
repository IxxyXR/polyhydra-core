using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        /// <summary>
        /// Revolves the boundary of one selected face around a world axis.
        /// Parameter A is a normalized turn and parameter B is the revolution segment count.
        /// Partial revolutions are capped; a full revolution is welded at its seam by construction.
        /// </summary>
        public PolyMesh Revolve(OpParams o, Axis axis)
        {
            var selectedFaces = Faces.Where(face => IncludeFace(face, o.filter)).ToList();
            if (selectedFaces.Count != 1)
            {
                Debug.LogWarning($"[PolyhydraRevolve] Revolve requires exactly one selected face; found {selectedFaces.Count}.");
                return Duplicate();
            }

            var turns = Mathf.Clamp(o.OriginalParamA, .0001f, 1f);
            var segments = Mathf.Clamp(Mathf.RoundToInt(o.OriginalParamB), 1, 128);
            var closed = Mathf.Approximately(turns, 1f);
            if (closed) segments = Mathf.Max(3, segments);
            var angle = turns * Mathf.PI * 2f;
            var ringCount = closed ? segments : segments + 1;
            var profileFace = selectedFaces[0];
            var profile = profileFace.GetVertices().Select(vertex => vertex.Position).ToList();
            var profileCount = profile.Count;
            var axisVector = axis switch
            {
                Axis.X => Vector3.right,
                Axis.Y => Vector3.up,
                Axis.Z => Vector3.forward,
                _ => Vector3.up
            };
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<List<int>>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();
            var ringIndices = new int[ringCount, profileCount];

            bool IsOnAxis(Vector3 point)
            {
                return axis switch
                {
                    Axis.X => Mathf.Abs(point.y) < .000001f && Mathf.Abs(point.z) < .000001f,
                    Axis.Y => Mathf.Abs(point.x) < .000001f && Mathf.Abs(point.z) < .000001f,
                    Axis.Z => Mathf.Abs(point.x) < .000001f && Mathf.Abs(point.y) < .000001f,
                    _ => false
                };
            }

            Vector3 Rotate(Vector3 point, float radians)
            {
                var sin = Mathf.Sin(radians);
                var cos = Mathf.Cos(radians);
                return axis switch
                {
                    Axis.X => new Vector3(point.x, point.y * cos - point.z * sin,
                        point.y * sin + point.z * cos),
                    Axis.Y => new Vector3(point.x * cos + point.z * sin, point.y,
                        -point.x * sin + point.z * cos),
                    Axis.Z => new Vector3(point.x * cos - point.y * sin,
                        point.x * sin + point.y * cos, point.z),
                    _ => point
                };
            }

            void AddFace(IEnumerable<int> source, Vector3 expectedNormal, Roles role)
            {
                var face = source.ToList();
                for (var index = face.Count - 1; index > 0; index--)
                    if (face[index] == face[index - 1]) face.RemoveAt(index);
                if (face.Count > 1 && face[0] == face[face.Count - 1]) face.RemoveAt(face.Count - 1);
                if (face.Distinct().Count() < 3) return;
                var a = vertexPoints[face[0]];
                var b = vertexPoints[face[1]];
                var c = vertexPoints[face[2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - b), expectedNormal) < 0f) face.Reverse();
                faceIndices.Add(face);
                faceRoles.Add(role);
            }

            for (var ring = 0; ring < ringCount; ring++)
            {
                var radians = angle * ring / segments;
                for (var profileIndex = 0; profileIndex < profileCount; profileIndex++)
                {
                    var point = profile[profileIndex];
                    if (ring > 0 && IsOnAxis(point))
                    {
                        ringIndices[ring, profileIndex] = ringIndices[0, profileIndex];
                        continue;
                    }
                    ringIndices[ring, profileIndex] = vertexPoints.Count;
                    vertexPoints.Add(Rotate(point, radians));
                    vertexRoles.Add(Roles.New);
                }
            }

            for (var ring = 0; ring < segments; ring++)
            {
                var nextRing = (ring + 1) % ringCount;
                var middleAngle = angle * (ring + .5f) / segments;
                for (var index = 0; index < profileCount; index++)
                {
                    var next = (index + 1) % profileCount;
                    var edgeVector = profile[next] - profile[index];
                    var profileOutward = Vector3.Cross(edgeVector, profileFace.Normal).normalized;
                    var expectedNormal = Rotate(profileOutward, middleAngle);
                    AddFace(new[]
                    {
                        ringIndices[ring, index],
                        ringIndices[ring, next],
                        ringIndices[nextRing, next],
                        ringIndices[nextRing, index]
                    }, expectedNormal, (ring + index) % 2 == 0 ? Roles.New : Roles.NewAlt);
                }
            }

            if (!closed)
            {
                var centroid = profile.Aggregate(Vector3.zero, (sum, point) => sum + point) / profileCount;
                var startTangent = Vector3.Cross(axisVector, centroid).normalized;
                var endTangent = Vector3.Cross(axisVector, Rotate(centroid, angle)).normalized;
                AddFace(Enumerable.Range(0, profileCount).Select(index => ringIndices[0, index]),
                    -startTangent, Roles.Existing);
                AddFace(Enumerable.Range(0, profileCount)
                        .Select(index => ringIndices[ringCount - 1, index]),
                    endTangent, Roles.Existing);
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles);
        }
    }
}
