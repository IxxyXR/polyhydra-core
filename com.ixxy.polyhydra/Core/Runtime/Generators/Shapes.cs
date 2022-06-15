using System;
using System.Collections.Generic;
using System.Linq;
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
            return type switch
            {
                ShapeTypes.Polygon => Polygon(Mathf.FloorToInt(a)),
                ShapeTypes.Star => Polygon(Mathf.FloorToInt(a * 2), stellate: b),
                ShapeTypes.C_Shape => C_Shape(a, b, c),
                ShapeTypes.L_Shape => L_Shape(a, b, c),
                ShapeTypes.H_Shape => H_Shape(a, b, c),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static PolyMesh Polygon(int sides, bool flip = false, float angleOffset = 0,
            float heightOffset = 0, float radius = 1, float stellate = 0)
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

            var poly = new PolyMesh(vertexPoints, faceIndices);
            if (stellate!=0) poly.VertexStellate(new OpParams(stellate));
            return poly;
        }


        public static PolyMesh L_Shape(float a, float b, float c)
        {
            var verts = new List<Vector3>
            {
                new (0, 0, 0),
                new (b, 0, 0),
                new (b, 0, -c),
                new (-c, 0, -c),
                new (-c, 0, a),
                new (0, 0, a),
            };

            var faces = new List<List<int>>();
            faces.Add(Enumerable.Range(0, verts.Count).ToList());
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh C_Shape(float a, float b, float c=0.25f)
        {
            var verts = new List<Vector3>
            {
                new (a, 0, -b),
                new (a, 0, -(b+c)),
                new (-c, 0, -(b+c)),
                new (-c, 0, b+c),
                new (a, 0, b+c),
                new (a, 0, b),
                new (0, 0, b),
                new (0, 0, -b),
            };

            var faces = new List<List<int>>();
            faces.Add(Enumerable.Range(0, verts.Count).ToList());
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh H_Shape(float a, float b, float c)
        {
            var verts = new List<Vector3>
            {
                // Top right
                new (a, 0, b+c),
                new (a+b, 0, b+c),
                
                // Base right
                new (a+b, 0, -(b+c)),
                new (a, 0, -(b+c)),
                
                // Crossbar bottom
                new (a, 0, -b/2f),
                new (-a, 0, -b/2f),
                
                // Base left
                new (-a, 0, -(b+c)),
                new (-(a+b), 0, -(b+c)),
                
                // Top left
                new (-(a+b), 0, b+c),
                new (-a, 0, b+c),
                
                // Crossbar top
                new (-a, 0, b/2f),
                new (a, 0, b/2f), 
            };

            var faces = new List<List<int>>();
            faces.Add(Enumerable.Range(0, verts.Count).ToList());
            return new PolyMesh(verts, faces);
        }
    }
}