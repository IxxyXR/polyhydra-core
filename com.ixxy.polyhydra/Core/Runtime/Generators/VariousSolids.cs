using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public enum VariousSolidTypes
    {
        UvSphere,
        UvHemisphere,
        Box,
    }

    public static class VariousSolids
    {
        public static PolyMesh Build(VariousSolidTypes type, int x, int y, int z = 0)
        {
            PolyMesh poly = null;

            switch (type)
            {
                case VariousSolidTypes.UvSphere:
                    poly = UvSphere(x, y);
                    break;
                case VariousSolidTypes.UvHemisphere:
                    poly = UvHemisphere(x, y);
                    break;
                case VariousSolidTypes.Box:
                    poly = Box(x, y, z);
                    break;
            }

            return poly;
        }

        public static PolyMesh Box(int x, int y, int z)
        {
            var shape = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, x, z);

            shape = shape.LayeredExtrude(y, 1.4142f);

            // Nice patterns
            var capRoles = new List<Roles>();
            var newFaceRoles = new List<Roles>();

            // Top and bottom roles
            int facesOnCap = x * z;
            for (var i = 0; i < facesOnCap; i++)
            {
                int alt = 0;
                if (x % 2 == 0)
                {
                    alt = Mathf.FloorToInt((i / x) % 2);
                }

                capRoles.Add((i + alt) % 2 == 0 ? Roles.New : Roles.NewAlt);
            }

            newFaceRoles.AddRange(capRoles);

            // Roles per vertical layer
            int facesPerLayer = x * 2 + z * 2;
            for (var i = 0; i < y * facesPerLayer; i++)
            {
                int alt = (i / facesPerLayer) % 2 == 0 ? 0 : 1;
                newFaceRoles.Add((i + alt) % 2 == 0 ? Roles.New : Roles.NewAlt);
            }

            newFaceRoles.AddRange(capRoles);

            shape.FaceRoles = newFaceRoles;
            return shape;
        }

        public static PolyMesh UvSphere(int verticalLines = 24, int horizontalLines = 24, float hemi = 1)
        {
            var faceRoles = new List<Roles>();

            horizontalLines = Mathf.Clamp(horizontalLines, 3, 24);
            verticalLines = Mathf.Clamp(verticalLines, 3, 24);

            var verts = new List<Vector3>();
            for (float v = 0; v <= horizontalLines; v++)
            {
                for (float u = 0; u < verticalLines; u++)
                {
                    var vv = v / horizontalLines;
                    var uu = u / verticalLines;
                    // Avoid coincident vertices at the tip
                    // as this caused weird glitches on Lace
                    if (vv == 0) vv = 0.0001f;

                    float x = Mathf.Sin(Mathf.PI * vv) * Mathf.Cos(2f * Mathf.PI * uu);
                    float y = Mathf.Sin(Mathf.PI * vv) * Mathf.Sin(2f * Mathf.PI * uu);
                    float z = Mathf.Cos(Mathf.PI * vv);
                    verts.Add(new Vector3(x, z, y));
                }
            }

            var faces = new List<List<int>>();
            for (int v = 0; v < horizontalLines * hemi; v += 1)
            {
                for (int u = 0; u < verticalLines; u += 1)
                {
                    faces.Add(new List<int>
                    {
                        (v * verticalLines) + u,
                        (v * verticalLines) + ((u + 1) % verticalLines),
                        ((v + 1) * verticalLines) + ((u + 1) % verticalLines),
                        ((v + 1) * verticalLines) + u
                    });
                    faceRoles.Add((u + v) % 2 == 0 ? Roles.New : Roles.NewAlt);
                }
            }

            var vertexRoles = Enumerable.Repeat(Roles.Existing, verts.Count);
            var poly = new PolyMesh(verts, faces, faceRoles, vertexRoles);
            return poly;
        }

        public static PolyMesh UvHemisphere(int verticalLines = 24, int horizontalLines = 24)
        {
            var poly = UvSphere(verticalLines, horizontalLines, 0.5f);
            poly = poly.FillHoles();
            return poly;
        }
    }
}