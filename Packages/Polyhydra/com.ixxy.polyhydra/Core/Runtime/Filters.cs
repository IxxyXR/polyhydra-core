﻿using System;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Polyhydra.Core
{
    public enum FilterTypes
    {
        All,

        // Sides
        NSided,
        EvenSided,

        // Direction
        FacingUp,
        FacingForward,
        FacingRight,
        FacingVertical,

        // Role
        Role,

        // Index
        OnlyNth,
        EveryNth,
        FirstN,
        LastN,
        Random,

        // Edges
        Inner,

        // Distance or position
        PositionX,
        PositionY,
        PositionZ,
        DistanceFromCenter,
    }

    public class Filter
    {
        public static Filter All = new Filter(p => true);
        public static Filter None = new Filter(p => false);
        public static Filter Outer = new Filter(p => p.poly.Faces[p.index].HasNakedEdge());
        public static Filter Inner = new Filter(p => !p.poly.Faces[p.index].HasNakedEdge());
        public static Filter EvenSided = new Filter(p => p.poly.Faces[p.index].Sides % 2 != 0);
        public static Filter OddSided = new Filter(p => p.poly.Faces[p.index].Sides % 2 == 0);

        public Func<FilterParams, bool> eval;

        public enum PositionType
        {
            VertexAny,
            VertexAll,
            Center
        }

        public static Filter GetFilter(FilterTypes filterType, float filterParamFloat, int filterParamInt,
            bool filterNot)
        {
            switch (filterType)
            {
                case FilterTypes.OnlyNth:
                    return OnlyNth(filterParamInt, filterNot);
                case FilterTypes.All:
                    return filterNot ? None : All;
                case FilterTypes.Inner:
                    return filterNot ? Outer : Inner;
                case FilterTypes.Random:
                    return Random(filterNot ? 1f - filterParamFloat : filterParamFloat);
                case FilterTypes.Role:
                    return Role((Roles)filterParamInt, filterNot);
                case FilterTypes.FacingVertical:
                    return FacingDirection(Vector3.up, filterParamFloat, includeOpposite: true, filterNot);
                case FilterTypes.FacingUp:
                    return FacingDirection(Vector3.up, filterParamFloat, false, filterNot);
                case FilterTypes.FacingForward:
                    return FacingDirection(Vector3.forward, filterParamFloat, false, filterNot);
                case FilterTypes.FacingRight:
                    return FacingDirection(Vector3.right, filterParamFloat, false, filterNot);
                case FilterTypes.NSided:
                    return NumberOfSides(filterParamInt, filterNot);
                case FilterTypes.EvenSided:
                    return filterNot ? EvenSided : OddSided;
                case FilterTypes.EveryNth:
                    return EveryNth(filterParamInt, filterNot);
                case FilterTypes.FirstN:
                    return Range(filterParamInt, filterNot);
                case FilterTypes.LastN:
                    return Range(-filterParamInt, filterNot);
                case FilterTypes.PositionX:
                    return Position(PositionType.Center, Axis.X, filterParamFloat, 10f, not: filterNot);
                case FilterTypes.PositionY:
                    return Position(PositionType.Center, Axis.Y, filterParamFloat, 10f, not: filterNot);
                case FilterTypes.PositionZ:
                    return Position(PositionType.Center, Axis.Z, filterParamFloat, 10f, not: filterNot);
                case FilterTypes.DistanceFromCenter:
                    return RadialDistance(0, filterParamFloat, not: filterNot);
                default:
                    return All;
            }
        }


        public static Filter Position(PositionType type, Axis axis, float min = -1f, float max = 1f,
            bool not = false)
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
                            .Select(v => getComponent(v.Position))
                            .All(c => c > min && c < max);
                        break;
                    case PositionType.VertexAny:
                        result = p.poly.Faces[p.index].GetVertices()
                            .Select(v => getComponent(v.Position))
                            .Any(c => c > min && c < max);
                        break;
                }

                return not ? !result : result;
            });
        }

        public static Filter RadialDistance(float min = 0f, float max = 1f, bool not = false)
        {
            return new Filter(p =>
            {
                float distance = p.poly.Faces[p.index].Centroid.magnitude;
                var result = distance > min && distance < max;
                return not ? !result : result;
            });
        }

        public static Filter FacingDirection(Vector3 direction, float range = 0.1f, bool includeOpposite = false,
            bool not = false)
        {
            return new Filter(p =>
            {
                float angle = Vector3.Angle(direction, p.poly.Faces[p.index].Normal);
                float oppositeAngle = 180f - angle;
                bool result;
                if (includeOpposite)
                {
                    result = angle < range || oppositeAngle < range;
                }
                else
                {
                    result = angle < range;
                }

                return not ? !result : result;
            });
        }

        public static Filter OnlyNth(int index, bool not = false)
        {
            return new Filter(p =>
            {
                if (index < 0) index = p.poly.Faces.Count - Mathf.Abs(index);
                var result = not ? p.index != index : p.index == index;
                return not ? !result : result;
            });
        }

        public static Filter EveryNth(int index, bool not = false)
        {
            return new Filter(p =>
            {
                if (index < 0) index = p.poly.Faces.Count - Mathf.Abs(index);
                return not ? p.index % index == 0 : p.index % index != 0;
            });
        }

        public static Filter Range(int index, bool not = false)
        {
            return new Filter(p =>
            {
                if (index < 0) // Python-style - negative indexes count from the end
                {
                    index = p.poly.Faces.Count - Mathf.Abs(index);
                    not = !not;
                }

                bool result = index > p.index;
                return not ? !result : result;
            });
        }

        public static Filter NumberOfSides(int sides, bool not = false)
        {
            return new Filter(p =>
            {
                var result = p.poly.Faces[p.index].Sides == sides;
                return not ? !result : result;
            });
        }

        public static Filter EdgesPerVertex(int vertexOrder, bool not = false)
        {
            return new Filter(p =>
            {
                var result = p.poly.Vertices[p.index].Halfedges.Count == vertexOrder;
                return not ? !result : result;
            });
        }

        public static Filter Random(float cutoff = 0.5f, bool not = false)
        {
            return new Filter(p =>
            {
                // Can't use Unity random off the main thread
                Random _random = new Random(p.index);
                var result = _random.NextDouble() < cutoff;
                return not ? !result : result;
            });
        }

        public static Filter Role(Roles role, bool not = false)
        {
            return new Filter(p =>
            {
                var result = p.poly.FaceRoles[p.index] == role;
                return not ? !result : result;
            });
        }

        // Utils
        /////////////////////////////////////////////////
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
}