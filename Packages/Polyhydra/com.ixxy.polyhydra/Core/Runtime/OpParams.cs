using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    
    public struct FilterParams
    {
        public FilterParams(PolyMesh p, int i)
        {
            poly = p;
            index = i;
        }

        public PolyMesh poly;
        public int index;

    }

    public class Filter
    {
        public static Filter All = new Filter(p => true);
        public static Filter Outer = new Filter(p => p.poly.Faces[p.index].HasNakedEdge());
        public static Filter Inner = new Filter(p => !p.poly.Faces[p.index].HasNakedEdge());
        
        public Func<FilterParams, bool> eval;

        public enum PositionType
        {
            VertexAny,
            VertexAll,
            Center
        }

        public static Filter Position(PositionType type, Axis axis, float min=-1f, float max=1f, bool inside=true)
        {
            return new Filter(p =>
            {
                bool result = false;
                Func<Vector3, float> getComponent = GetVectorComponent(axis);
                switch (type)
                {
                    case PositionType.Center:
                        var position = getComponent(p.poly.Faces[p.index].Centroid);
                        result = position > min && position < max;
                        break;
                    case PositionType.VertexAll:
                        result = p.poly.Faces[p.index].GetVertices()
                            .Select(v=>getComponent(v.Position))
                            .All(c=> c > min && c < max);
                        break;
                    case PositionType.VertexAny:
                        result = p.poly.Faces[p.index].GetVertices()
                            .Select(v=>getComponent(v.Position))
                            .Any(c=> c > min && c < max);
                        break;
                }
                return inside ? result : !result;
            });

        }

        public static Filter Facing(Vector3 direction, float range=0.1f)
        {
            return new Filter(p =>
            {
                float angle = Vector3.Angle(direction, p.poly.Faces[p.index].Normal);
                return angle < range;
            });
        }
        
        public static Filter Index(int index)
        {
            return new Filter(p =>
            {
                if (index < 0) index = p.poly.Faces.Count - Mathf.Abs(index);
                return p.index == index;
            });
        }
        
        public static Filter Sides(int sides)
        {
            return new Filter(p =>
            {
                return p.poly.Faces[p.index].Sides == sides;
            });
        }
        
        public static Filter VertexEdges(int vertexOrder)
        {
            return new Filter(p =>
            {
                return p.poly.Vertices[p.index].Halfedges.Count == vertexOrder;
            });
        }
        
        public static Filter Role(Roles role)
        {
            return new Filter(p =>
            {
                return p.poly.FaceRoles[p.index] == role;
            });
        }
        
        private static Func<Vector3, float> GetVectorComponent(Axis axis)
        {
            Func<Vector3, float> getComponent = null;
            switch (axis)
            {
                case Axis.X:
                    getComponent = vec => vec.x;
                    break;
                case Axis.Y:
                    getComponent = vec => vec.y;
                    break;
                case Axis.Z:
                    getComponent = vec => vec.z;
                    break;
            }

            return getComponent;
        }

        public Filter(Func<FilterParams, bool> func)
        {
            eval = func;
        }
    }

    public class OpFunc
    {
        public Func<FilterParams, float> eval;

        public OpFunc(float a)
        {
            eval = p => a;
        }

        public OpFunc(Func<FilterParams, float> func)
        {
            eval = func;
        }
    }

    public class OpParams
    {
        public OpFunc funcA;
        public OpFunc funcB;
        public Filter filter;

        public float OriginalParamA => _OriginalParamA;
        public float OriginalParamB => _OriginalParamB;
        private float _OriginalParamA;
        private float _OriginalParamB;


        public float GetValueA(PolyMesh poly, int index) => funcA?.eval.Invoke(new FilterParams(poly, index)) ?? 0;
        public float GetValueB(PolyMesh poly, int index) => funcB?.eval.Invoke(new FilterParams(poly, index)) ?? 0;
        
        public OpParams(Filter filter=null)
        {
            this.filter = filter;
        }
        
        public OpParams(float a, Filter filter=null)
        {
            _OriginalParamA = a;
            funcA = new OpFunc(a);
            this.filter = filter;
        }

        public OpParams(float a, float b, Filter filter=null)
        {
            _OriginalParamA = a;
            _OriginalParamB = b;
            funcA = new OpFunc(a);
            funcB = new OpFunc(b);
            this.filter = filter;
        }
        
        public OpParams(OpFunc a, Filter filter=null)
        {
            funcA = a;
            this.filter = filter;
        }

        public OpParams(float a, OpFunc b, Filter filter=null)
        {
            _OriginalParamA = a;
            funcA = new OpFunc(a);
            funcB = b;
            this.filter = filter;
        }
    }
}