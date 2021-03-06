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
        public enum Method
        {
            Concave,
            Convex,
            Grid,
        }
        public static PolyMesh Build(ShapeTypes type, float a=0.5f, float b=0.5f, float c=0.5f, Method method = Method.Concave)
        {
            return type switch
            {
                ShapeTypes.Polygon => Polygon(Mathf.FloorToInt(a)),
                ShapeTypes.Star => Polygon(Mathf.FloorToInt(a * 2), stellate: b),
                ShapeTypes.C_Shape => C_Shape(a, b, c, method),
                ShapeTypes.L_Shape => L_Shape(a, b, c, method),
                ShapeTypes.H_Shape => H_Shape(a, b, c, method),
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

        public static PolyMesh L_Shape(float a, float b, float c, Method method = Method.Convex)
        {

            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, 2, 3);
                poly = poly.FaceRemove(false, new List<int>{0, 2});
                return poly;
            }
            
            List<List<int>> faces;
            List<Vector3> verts = new()
            {
                // Base
                new(0, 0, 0),
                new(b, 0, 0),
                new(b, 0, -c),
                new(-c, 0, -c),
                new(-c, 0, a),
                new(0, 0, a),
            };

            if (method == Method.Concave)
            {
                faces = new(){ Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 3 },
                    new() { 0, 3, 4, 5 },
                };
            }
            return new PolyMesh(verts, faces);
        }
        
        public static PolyMesh C_Shape(float a, float b, float c = 0.25f, Method method = Method.Convex)
        {

            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, 2, 3);
                poly = poly.FaceRemove(false, new List<int>{2});
                return poly;
            }
            
            List<List<int>> faces;
            List<Vector3> verts = new()
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

            if (method == Method.Concave)
            {
                faces = new() { Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 7 },
                    new() { 2, 3, 6, 7 },
                    new() {3, 4, 5, 6}
                };
            }
            return new PolyMesh(verts, faces);
        }

        public static PolyMesh H_Shape(float a, float b, float c, Method method = Method.Convex)
        {
            
            if (method == Method.Grid)
            {
                var poly = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, 3, 3);
                poly = poly.FaceRemove(false, new List<int>{1, 7});
                return poly;
            }
            
            List<List<int>> faces;
            List<Vector3> verts = new()
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

            if (method == Method.Concave)
            {
                faces = new() { Enumerable.Range(0, verts.Count).ToList() };
            }
            else
            {
                faces = new()
                {
                    new() { 0, 1, 2, 3, 4, 11 },
                    new () { 11, 4, 5, 10 },
                    new() { 5, 6, 7, 8, 9, 10 },
                };
            }
            return new PolyMesh(verts, faces);
        }
    }
}