using System.Collections.Generic;
using UnityEngine;

namespace Polyhydra.Core
{
    public enum ShapeTypes
    {
        Polygon,
        Star,
        C_Shape,
        L_Shape,
        H_Shape,
    }

    public class Shapes
    {
        public static PolyMesh Build(ShapeTypes type, float a=0.5f, float b=0.5f, float c=0.5f)
        {
            PolyMesh poly = null;

            switch (type)
            {
                case ShapeTypes.Polygon:
                    poly = Polygon(Mathf.FloorToInt(a));
                    break;
                case ShapeTypes.Star:
                    poly = Polygon(Mathf.FloorToInt(a * 2));
                    poly.VertexStellate(new OpParams(b));
                    break;
                case ShapeTypes.C_Shape:
                    poly = C_Shape(a, b, c);
                    break;
                case ShapeTypes.L_Shape:
                    poly = L_Shape(a, b, c);
                    break;
                case ShapeTypes.H_Shape:
                    poly = H_Shape(a, b, c);
                    break;
            }

            return poly;
        }

        public static PolyMesh Polygon(int sides, bool flip = false, float angleOffset = 0,
            float heightOffset = 0, float radius = 1)
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


        public static PolyMesh L_Shape(float a, float b, float c)
        {
            var verts = new List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(b, 0, 0),
                new Vector3(b, 0, -c),
                new Vector3(-c, 0, -c),
                new Vector3(-c, 0, a),
                new Vector3(0, 0, a),
            };

            var faces = new List<List<int>>
            {
                new List<int> { 0, 1, 2, 3, 4, 5 }
            };

            return new PolyMesh(verts, faces);
        }

        public static PolyMesh C_Shape(float a, float b, float c=0.25f)
        {
            var verts = new List<Vector3>
            {
                new Vector3(a, 0, -b),
                new Vector3(a, 0, -(b+c)),
                new Vector3(-c, 0, -(b+c)),
                new Vector3(-c, 0, b+c),
                new Vector3(a, 0, b+c),
                new Vector3(a, 0, b),
                new Vector3(0, 0, b),
                new Vector3(0, 0, -b),
            };

            var faces = new List<List<int>>
            {
                new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 }
            };

            return new PolyMesh(verts, faces);
        }

        public static PolyMesh H_Shape(float a, float b, float c)
        {
            var verts = new List<Vector3>
            {
                new Vector3(a+b, 0, b+c),
                new Vector3(a, 0, b+c),
                new Vector3(a, 0, -(b+c)),
                new Vector3(a+b, 0, -(b+c)),
                new Vector3(a+b, 0, -b/2f),
                new Vector3(-(a+b), 0, -b/2f),
                new Vector3(-(a+b), 0, -(b+c)),
                new Vector3(-a, 0, -(b+c)),
                new Vector3(-a, 0, b+c),
                new Vector3(-(a+b), 0, b+c),
                new Vector3(-(a+b), 0, b/2f),
                new Vector3(a+b, 0, b/2f),
                
            };

            var faces = new List<List<int>>
            {
                new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }
            };

            return new PolyMesh(verts, faces);
        }
    }
}