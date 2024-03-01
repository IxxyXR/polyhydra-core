using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core.IsohedralGrids;
using UnityEngine;


namespace Polyhydra.Core
{
    public class MultiGrid
    {

        private int divisions, dimensions;
        private float offset;

        private float MinDistance;
        private float MaxDistance;

        private float colorRatio = 1.0f;
        private float colorIndex = 0.0f;
        private float colorIntersect = 0.0f;



        public MultiGrid(int divisions, int dimensions, float offset, float MinDistance, float MaxDistance, float colorRatio = 1.0f, float colorIndex = 0.0f, float colorIntersect = 0.0f)
        {
            this.divisions = divisions;
            this.dimensions = dimensions;
            this.offset = offset;

            this.MinDistance = MinDistance;
            this.MaxDistance = MaxDistance;

            this.colorRatio = colorRatio;
            this.colorIndex = colorIndex;
            this.colorIntersect = colorIntersect;
        }


        public (PolyMesh poly, List<List<Vector2>> shapes, List<float> colors) Build(bool sharedVertices=true, bool random=false)
        {
            var (shapes, colors) = GenerateShapes(Vector2.one, random);

            var faceRoles = new List<Roles>();
            var vertexDictionary = new Dictionary<Vector2, int>();
            var faceIndices = new List<List<int>>();
            var vertices = new List<Vector2>();

            if (shapes.Count == 0) return (new PolyMesh(), shapes, colors);

            float colorMin = colors.Min();
            float colorMax = colors.Max();

            for (var shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                var shape = shapes[shapeIndex];

                if (shape.Count == 5)
                {
                    var face = new List<int>();

                    for (var vertIndex = 0; vertIndex < shape.Count; vertIndex++)
                    {
                        Vector2 vert = shape[vertIndex];

                        if (sharedVertices)
                        {
                            if (!vertexDictionary.ContainsKey(vert))
                            {
                                vertexDictionary[vert] = vertexDictionary.Count;
                            }

                            face.Add(vertexDictionary[vert]);
                        }
                        else
                        {
                            vertices.Add(vert);
                            face.Add(vertices.Count - 1);
                        }
                    }

                    var normal = Vector3.Cross(shape[1] - shape[0], shape[2] - shape[0]);

                    if (normal.z < 0)
                    {
                        faceIndices.Add(new List<int>{face[0], face[1], face[2], face[3]});
                    }
                    else
                    {
                        faceIndices.Add(new List<int>{face[3], face[2], face[1], face[0]});
                    }
                    float colorValue = Mathf.InverseLerp(colorMin, colorMax, colors[shapeIndex]);
                    int roleIndex = Mathf.FloorToInt(colorValue * 4f);
                    var role = (Roles) roleIndex;
                    faceRoles.Add(role);
                }

            }

            if (sharedVertices)
            {
                vertices = vertexDictionary.Keys.ToList();
            }

            var poly = new PolyMesh(
                vertices.Select(v=>new Vector3(v.x, 0, v.y)),
                faceIndices,
                faceRoles,
                Enumerable.Repeat(Roles.New, vertices.Count)
            );

            return (poly, shapes, colors);
        }

        public (List<List<Vector2>> shapes, List<float> colors) GenerateShapes(Vector2 size, bool random=false)
        {

            (Vector2 topLeft, Vector2 bottomRight) bounds = (Vector2.zero, -size);

            float diameter = size.magnitude;
            float scale = diameter / 2f / divisions;

            var rhombs = generateRhombs(divisions, dimensions , offset, random);

            var tf = new Vector2(size.x / 2f, size.y / 2f);
            tf *= scale;
            if (dimensions % 2 > 0)
            {
                // Rotate around origin?
                tf = Quaternion.Euler(0, (Mathf.PI / dimensions) * 0.5f, 0) * tf;
            }

            var shapes = new List<List<Vector2>>();
            var colors = new List<float>();

            float sqrMaxDistance = (MaxDistance * MaxDistance);
            float sqrMinDistance = (MinDistance * MinDistance);

            for (int ii = 0; ii < rhombs.Count; ii++)
            {
                var rhomb = rhombs[ii];
                List<Vector2> shape = rhomb.shape.Select(x=>x*tf).ToList();

                if (shape.Any(o=>
                {
                    float distanceSqr = o.sqrMagnitude;
                    return distanceSqr > sqrMaxDistance || distanceSqr < sqrMinDistance;
                })) continue;

                var lineWidthTransform = Vector2.one;

                if (true)
                {
                    var p = new List<Vector2>();
                    p.AddRange(shape.Select(x=>x * lineWidthTransform));
                    float gradientPos = 1;

                    float w1 = (shape[2] - shape[0]).magnitude;
                    float w2 = (shape[3] - shape[1]).magnitude;
                    float shapeRatio = Mathf.Min(w1, w2) / Mathf.Max(w1, w2);

                    float intersectRatio = rhomb.line1/dimensions;
                    intersectRatio += rhomb.line2/dimensions;
                    intersectRatio *= 0.5f;

                    float indexRatio = 1f - Mathf.Abs(rhomb.parallel1/divisions/2.0f);
                    indexRatio *= 1f - Mathf.Abs(rhomb.parallel2/divisions/2.0f);

                    if (colorRatio >= 0)
                    {
                        gradientPos *= 1f - (shapeRatio * colorRatio);
                    }
                    else
                    {
                        gradientPos *= 1f - ((1f - shapeRatio) * Mathf.Abs(colorRatio));
                    }

                    if (colorIntersect >= 0)
                    {
                        gradientPos *= 1f - (intersectRatio * colorIntersect);
                    }
                    else
                    {
                        gradientPos *= 1f - ((1f - intersectRatio) * Mathf.Abs(colorIntersect));
                    }

                    if (colorIndex >= 0)
                    {
                        gradientPos *= 1 - (indexRatio * colorIndex);
                    }
                    else
                    {
                        gradientPos *= 1 - ((1 - indexRatio) * Mathf.Abs(colorIndex));
                    }
                    colors.Add(float.IsNaN(gradientPos) ? 0 : gradientPos);
                }
                shapes.Add(shape);
            }

            return (shapes, colors);
        }

        public List<Rhomb> generateRhombs(int div, int dim, float offset, bool random=false)
        {
            var rhombs = new List<Rhomb>();
            var angles = new List<float>();

            int halfLines = div;
            int totalLines = (halfLines * 2) + 1;

            float randomAngle = 0;

            // Setup our imaginary lines...
            int dimensions = dim;
            if (random)
            {
                float angle = 0;
                while (angle < Mathf.PI)
                {
                    angle += Random.Range(.00001f, Mathf.PI/(div/2f));
                    angles.Add(angle);
                }
            }
            else
            {
                for (int i = 0; i < dimensions; i++)
                {
                    var angle = 2 * (Mathf.PI / dim) * i;
                    angles.Add(angle);
                }
            }


            for (int i = 0; i < angles.Count; i++)
            {
                float angle1 = angles[i];
                Vector2 p1 = new Vector2(totalLines * Mathf.Cos(angle1), -totalLines * Mathf.Sin(angle1));
                Vector2 p2 = -p1;

                for (int parallel1 = 0; parallel1 < totalLines; parallel1++)
                {
                    int index1 = halfLines - parallel1;

                    Vector2 offset1 = new Vector2((index1 + offset) * Mathf.Sin(angle1), (index1 + offset) * Mathf.Cos(angle1));
                    var l1 = (p1 + offset1, p2 + offset1);

                    for (int k = i + 1; k < angles.Count; k++)
                    {
                        float angle2 = angles[k];
                        var p3 = new Vector2(totalLines * Mathf.Cos(angle2), -totalLines * Mathf.Sin(angle2));
                        Vector2 p4 = -p3;

                        for (int parallel2 = 0; parallel2 < totalLines; parallel2++)
                        {
                            int index2 = halfLines - parallel2;

                            var offset2 = new Vector2((index2 + offset) * Mathf.Sin(angle2), (index2 + offset) * Mathf.Cos(angle2));
                            var l2 = (p3 + offset2, p4 + offset2);
                            var intersect = new Vector2();

                            bool intersection = Intersects(l1, l2, ref intersect);
                            if (intersection)
                            {
                                List<int> indices = getIndicesFromPoint(intersect, angles, offset);
                                var shape = new List<Vector2>();
                                indices[i] = index1 + 1;
                                indices[k] = index2 + 1;
                                shape.Add(getVertex(indices, angles));
                                indices[i] = index1;
                                indices[k] = index2 + 1;
                                shape.Add(getVertex(indices, angles));
                                indices[i] = index1;
                                indices[k] = index2;
                                shape.Add(getVertex(indices, angles));
                                indices[i] = index1 + 1;
                                indices[k] = index2;
                                shape.Add(getVertex(indices, angles));
                                indices[i] = index1 + 1;
                                indices[k] = index2 + 1;
                                shape.Add(getVertex(indices, angles));

                                var rhomb = new Rhomb
                                {
                                    shape = shape,
                                    parallel1 = index1,
                                    parallel2 = index2,
                                    line1 = i,
                                    line2 = k,
                                };
                                rhombs.Add(rhomb);
                            }
                        }
                    }
                }
            }

            return rhombs;
        }

        private bool Intersects((Vector2 start , Vector2 end) A, (Vector2 start, Vector2 end) B, ref Vector2 intersect)
        {
            float tmp = (B.end.x - B.start.x) * (A.end.y - A.start.y) - (B.end.y - B.start.y) * (A.end.x - A.start.x);

            if (tmp == 0)
            {
                // No solution!
                return false;
            }

            float mu = ((A.start.x - B.start.x) * (A.end.y - A.start.y) - (A.start.y - B.start.y) * (A.end.x - A.start.x)) / tmp;

            intersect = new Vector2(
                B.start.x + (B.end.x - B.start.x) * mu,
                B.start.y + (B.end.y - B.start.y) * mu
            );
            return true;
        }

        public List<int> getIndicesFromPoint(Vector2 point, List<float> angles, float offset)
        {
            var indices = new List<int>();
            for (int a = 0; a < angles.Count; a++)
            {
                Vector2 p = point;
                float index = p.x * Mathf.Sin(angles[a]) + p.y * Mathf.Cos(angles[a]);
                indices.Add(Mathf.FloorToInt(index - offset + 1));
            }

            return indices;
        }

        public Vector2 getVertex(List<int> indices, List<float> angles)
        {
            if (indices==null || !indices.Any() || angles==null || !angles.Any())
            {
                Debug.LogError("error");
                return Vector2.zero;
            }

            float x = 0;
            float y = 0;

            for (int i = 0; i < indices.Count; i++)
            {
                x += indices[i] * Mathf.Cos(angles[i]);
                y += indices[i] * Mathf.Sin(angles[i]);
            }

            return new Vector2(x, y);
        }
    }
}