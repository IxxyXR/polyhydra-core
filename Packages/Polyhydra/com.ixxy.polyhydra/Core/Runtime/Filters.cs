using System;
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
        MinimumAngle,
        AverageAngle,
        MaximumAngle,

        // Distance or position
        PositionX,
        PositionY,
        PositionZ,
        DistanceFromCenter,
    }

    public class Filter
    {
        public static Filter All = new (
            p => true, 
            p => true
        );
        public static Filter None = new (            
            p => false, 
            p => false
        );
        public static Filter Outer = new (
            p => p.poly.Faces[p.index].HasNakedEdge(),
            p => p.poly.Vertices[p.index].Halfedges.Any(e=>e.Pair==null)
        );
        public static Filter Inner = new (
            p => !p.poly.Faces[p.index].HasNakedEdge(),
            p => !p.poly.Vertices[p.index].Halfedges.Any(e=>e.Pair==null)
        );
        public static Filter EvenSided = new (
            p => p.poly.Faces[p.index].Sides % 2 != 0,
            p => p.poly.Vertices[p.index].Halfedges.Count % 2 != 0
        );
        public static Filter OddSided = new (
            p => p.poly.Faces[p.index].Sides % 2 == 0,
            p => p.poly.Vertices[p.index].Halfedges.Count % 2 == 0
        );

        public Func<FilterParams, bool> evalFace;
        public Func<FilterParams, bool> evalVertex;

        public enum PositionType
        {
            VertexAny,
            VertexAll,
            Center
        }

        public static Filter GetFilter(FilterTypes filterType, float filterParamFloat, int filterParamInt,
            bool filterNot)
        {
            return filterType switch
            {
                FilterTypes.OnlyNth => OnlyNth(filterParamInt, filterNot),
                FilterTypes.All => filterNot ? None : All,
                FilterTypes.Inner => filterNot ? Outer : Inner,
                FilterTypes.Random => Random(filterNot ? 1f - filterParamFloat : filterParamFloat),
                FilterTypes.Role => Role((Roles)filterParamInt, filterNot),
                FilterTypes.FacingVertical => FacingDirection(Vector3.up, filterParamFloat, includeOpposite: true,
                    filterNot),
                FilterTypes.FacingUp => FacingDirection(Vector3.up, filterParamFloat, false, filterNot),
                FilterTypes.FacingForward => FacingDirection(Vector3.forward, filterParamFloat, false, filterNot),
                FilterTypes.FacingRight => FacingDirection(Vector3.right, filterParamFloat, false, filterNot),
                FilterTypes.NSided => NumberOfSides(filterParamInt, filterNot),
                FilterTypes.MinimumAngle => MinimumAngle(filterParamFloat, filterNot),
                FilterTypes.AverageAngle => AverageAngle(filterParamFloat, filterNot),
                FilterTypes.MaximumAngle => MaximumAngle(filterParamFloat, filterNot),
                FilterTypes.EvenSided => filterNot ? EvenSided : OddSided,
                FilterTypes.EveryNth => EveryNth(filterParamInt, filterNot),
                FilterTypes.FirstN => Range(filterParamInt, filterNot),
                FilterTypes.LastN => Range(-filterParamInt, filterNot),
                FilterTypes.PositionX => Position(PositionType.Center, Axis.X, filterParamFloat, 10f, not: filterNot),
                FilterTypes.PositionY => Position(PositionType.Center, Axis.Y, filterParamFloat, 10f, not: filterNot),
                FilterTypes.PositionZ => Position(PositionType.Center, Axis.Z, filterParamFloat, 10f, not: filterNot),
                FilterTypes.DistanceFromCenter => RadialDistance(0, filterParamFloat, not: filterNot),
                _ => throw new ArgumentOutOfRangeException(nameof(filterType), filterType, null)
            };
        }

        public static Filter Position(PositionType type, Axis axis, float min = -1f, float max = 1f,
            bool not = false)
        {
            return new Filter(
                p =>
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
                },
                p =>
                {
                    Func<Vector3, float> getComponent = GetVectorComponent(axis);
                    var position = getComponent(p.poly.Vertices[p.index].Position);
                    bool result = position > min && position < max;
                    return not ? !result : result;
                }
            );
        }

        public static Filter RadialDistance(float min = 0f, float max = 1f, bool not = false)
        {
            return new Filter(
                p =>
                {
                    float distance = p.poly.Faces[p.index].Centroid.magnitude;
                    var result = distance > min && distance < max;
                    return not ? !result : result;
                },
                p =>
                {
                    float distance = p.poly.Vertices[p.index].Position.magnitude;
                    var result = distance > min && distance < max;
                    return not ? !result : result;
                }
            );
        }

        public static Filter FacingDirection(Vector3 direction, float range = 0.1f, bool includeOpposite = false,
            bool not = false)
        {
            return new Filter(
                p =>
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
                },
                p =>
                {
                    float angle = Vector3.Angle(direction, p.poly.Vertices[p.index].Normal);
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
                }
            );
        }

        public static Filter AverageAngle(float inputAngle, bool not = false)
        {
            return _CompareAngle(inputAngle, not, 0);
        }

        public static Filter MinimumAngle(float inputAngle, bool not = false)
        {
            return _CompareAngle(inputAngle, not, -1);
        }

        public static Filter MaximumAngle(float inputAngle, bool not = false)
        {
            return _CompareAngle(inputAngle, not, 1);
        }

        private static Filter _CompareAngle(float inputAngle, bool not = false, int minMaxAvg = 0)
        {
            return new Filter(
                p =>
                {
                    var face = p.poly.Faces[p.index];
                    var edges = face.GetHalfedges();
                    float angle = minMaxAvg switch
                    {
                        -1 => edges.Select(e => e.DihedralAngle).Min(),
                        0 => edges.Select(e => e.DihedralAngle).Sum() / edges.Count,
                        1 => edges.Select(e => e.DihedralAngle).Max(),
                        _ => throw new ArgumentOutOfRangeException(nameof(minMaxAvg), minMaxAvg, null)
                    };
                    bool result = angle < inputAngle;
                    return not ? !result : result;
                },
                p =>
                {
                    var vert = p.poly.Vertices[p.index];
                    var edges = vert.Halfedges;
                    float angle = edges.Select(e => e.DihedralAngle).Sum() / edges.Count;
                    bool result = angle < inputAngle;
                    return not ? !result : result;
                }
            );
        }

        public static Filter OnlyNth(int index, bool not = false)
        {
            return new Filter(
                p =>
                {
                    if (index < 0) index = p.poly.Faces.Count - Mathf.Abs(index);
                    var result = not ? p.index != index : p.index == index;
                    return not ? !result : result;
                },
                p =>
                {
                    if (index < 0) index = p.poly.Vertices.Count - Mathf.Abs(index);
                    var result = not ? p.index != index : p.index == index;
                    return not ? !result : result;
                }
            );
        }

        public static Filter EveryNth(int index, bool not = false)
        {
            return new Filter(
                p =>
                {
                    if (index == 0) return not;
                    if (index < 0) index = p.poly.Faces.Count - Mathf.Abs(index);
                    return not ? p.index % index != 0 : p.index % index == 0;
                },
                p =>
                {
                    if (index == 0) return not;
                    if (index < 0) index = p.poly.Vertices.Count - Mathf.Abs(index);
                    return not ? p.index % index != 0 : p.index % index == 0;
                });
        }

        public static Filter Range(int index, bool not = false)
        {
            return new Filter(
            p =>
                {
                    if (index < 0) // Python-style - negative indexes count from the end
                    {
                        index = p.poly.Faces.Count - Mathf.Abs(index);
                        not = !not;
                    }

                    bool result = index > p.index;
                    return not ? !result : result;
                },
                p =>
                {
                    if (index < 0) // Python-style - negative indexes count from the end
                    {
                        index = p.poly.Vertices.Count - Mathf.Abs(index);
                        not = !not;
                    }

                    bool result = index > p.index;
                    return not ? !result : result;
                }
            );
        }

        public static Filter NumberOfSides(int sides, bool not = false)
        {
            return new Filter(
                p =>
                {
                    var result = p.poly.Faces[p.index].Sides == sides;
                    return not ? !result : result;
                },
                p =>
                {
                    var result = p.poly.Vertices[p.index].Halfedges.Count == sides;
                    return not ? !result : result;
                }
            );
        }


        public static Filter Random(float cutoff = 0.5f, bool not = false)
        {
            // nb - Can't use Unity random off the main thread. Use system random instead.
            // Use p.index as a seed as we don't want the result to change every time we make a new poly
            return new Filter(
                p =>
                {
                    Random _random = new Random(p.index);
                    var result = _random.NextDouble() < cutoff;
                    return not ? !result : result;
                },
                p =>
                {
                    Random _random = new Random(p.index);
                    var result = _random.NextDouble() < cutoff;
                    return not ? !result : result;
                }
            );
        }

        public static Filter Role(Roles role, bool not = false)
        {
            return new Filter(
                p =>
                {
                    var result = p.poly.FaceRoles[p.index] == role;
                    return not ? !result : result;
                },
                p =>
                {
                    int faceIndex = p.poly.Faces.IndexOf(p.poly.Vertices[p.index].Halfedge.Face);
                    var result = p.poly.FaceRoles[faceIndex] == role;
                    return not ? !result : result;
                }
            );
        }

        // Utils
        /////////////////////////////////////////////////
        private static Func<Vector3, float> GetVectorComponent(Axis axis)
        {
            return vec => vec[(int)axis];
        }

        public Filter(Func<FilterParams, bool> funcFace, Func<FilterParams, bool> funcVertex)
        {
            evalFace = funcFace;
            evalVertex = funcVertex;
        }
    }
}