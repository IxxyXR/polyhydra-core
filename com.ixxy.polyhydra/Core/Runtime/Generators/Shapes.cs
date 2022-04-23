using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

namespace Polyhydra.Core
{
    public class Shapes
    {
        public static PolyMesh MakePolygon(int sides, bool flip = false, float angleOffset = 0, float heightOffset = 0, float radius = 1)
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
            return new PolyMesh(vertexPoints, faceIndices);
        }
    }
}
