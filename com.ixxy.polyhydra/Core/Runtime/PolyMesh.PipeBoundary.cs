using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        /// <summary>
        /// Sweeps a regular polygonal tube around the closed boundary of one selected planar face.
        /// Parameter A is tube radius and parameter B is tube side count.
        /// </summary>
        public PolyMesh PipeBoundary(OpParams o)
        {
            var selectedFaces = Faces.Where(face => IncludeFace(face, o.filter)).ToList();
            if (selectedFaces.Count != 1)
            {
                Debug.LogWarning($"[PolyhydraPipeBoundary] PipeBoundary requires exactly one selected face; found {selectedFaces.Count}.");
                return Duplicate();
            }

            var radius = Mathf.Max(.0001f, o.OriginalParamA);
            var tubeSides = Mathf.Clamp(Mathf.RoundToInt(o.OriginalParamB), 3, 64);
            var pathFace = selectedFaces[0];
            var path = pathFace.GetVertices().Select(vertex => vertex.Position).ToList();
            if (path.Count < 3) return Duplicate();

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<List<int>>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();
            var planeNormal = pathFace.Normal.normalized;

            for (var pathIndex = 0; pathIndex < path.Count; pathIndex++)
            {
                var previous = path[ActualMod(pathIndex - 1, path.Count)];
                var next = path[(pathIndex + 1) % path.Count];
                var tangent = (next - previous).normalized;
                var inPlaneNormal = Vector3.Cross(tangent, planeNormal).normalized;
                for (var side = 0; side < tubeSides; side++)
                {
                    var angle = Mathf.PI * 2f * side / tubeSides;
                    var offset = planeNormal * (Mathf.Cos(angle) * radius) +
                                 inPlaneNormal * (Mathf.Sin(angle) * radius);
                    vertexPoints.Add(path[pathIndex] + offset);
                    vertexRoles.Add(Roles.New);
                }
            }

            for (var pathIndex = 0; pathIndex < path.Count; pathIndex++)
            {
                var nextPath = (pathIndex + 1) % path.Count;
                for (var side = 0; side < tubeSides; side++)
                {
                    var nextSide = (side + 1) % tubeSides;
                    faceIndices.Add(new List<int>
                    {
                        pathIndex * tubeSides + side,
                        pathIndex * tubeSides + nextSide,
                        nextPath * tubeSides + nextSide,
                        nextPath * tubeSides + side
                    });
                    faceRoles.Add((pathIndex + side) % 2 == 0 ? Roles.New : Roles.NewAlt);
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles);
        }
    }
}
