using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;


public static class RadialSolids
{
    private static PolyMesh _MakeCupola(int sides, float capHeight, bool bi = false, bool capGyro = true)
    {
        sides = Mathf.Clamp(sides, 3, 64);

        PolyMesh poly = Shapes.Polygon(sides * 2, true);
        Face bottom = poly.Faces[0];
        PolyMesh cap1 = Shapes.Polygon(sides, false, 0.25f, capHeight, _CalcCupolaCapRadius(sides));
        poly.Append(cap1);

        int i = 0;
        var edge1 = poly.Halfedges[0];
        var edge2 = poly.Halfedges[sides * 2];
        while (true)
        {
            var side1 = new List<Vertex>
            {
                edge1.Next.Vertex,
                edge1.Vertex,
                edge2.Prev.Vertex
            };
            poly.Faces.Add(side1);
            poly.FaceRoles.Add(Roles.New);
            poly.FaceTags.Add(new HashSet<string>());

            var side2 = new List<Vertex>
            {
                edge1.Vertex,
                edge1.Prev.Vertex,
                edge2.Vertex,
                edge2.Prev.Vertex
            };
            poly.Faces.Add(side2);
            poly.FaceRoles.Add(Roles.NewAlt);
            poly.FaceTags.Add(new HashSet<string>());

            i++;
            edge1 = edge1.Next.Next;
            edge2 = edge2.Prev;
            if (i == sides) break;
        }

        if (bi)
        {
            float angleOffset = capGyro ? 0.75f : 0.25f;
            PolyMesh cap2 = Shapes.Polygon(sides, true, angleOffset, -capHeight, _CalcCupolaCapRadius(sides));
            poly.Append(cap2);

            i = 0;
            var middleVerts = bottom.GetVertices();

            poly.Faces.Remove(bottom);
            poly.FaceRoles.RemoveAt(0);
            poly.FaceTags.RemoveAt(0);

            edge2 = poly.Faces.Last().Halfedge.Prev;
            int indexOffset = capGyro ? 0 : -1;
            while (true)
            {
                var side1 = new List<Vertex>
                {
                    middleVerts[PolyMesh.ActualMod(i * 2 - 1 + indexOffset, sides * 2)],
                    middleVerts[PolyMesh.ActualMod(i * 2 + indexOffset, sides * 2)],
                    edge2.Vertex
                };
                poly.Faces.Add(side1);
                poly.FaceRoles.Add(Roles.New);
                poly.FaceTags.Add(new HashSet<string>());

                var side2 = new List<Vertex>
                {
                    middleVerts[PolyMesh.ActualMod(i * 2 + indexOffset, sides * 2)],
                    middleVerts[PolyMesh.ActualMod(i * 2 + 1 + indexOffset, sides * 2)],
                    edge2.Next.Vertex,
                    edge2.Vertex,
                };
                poly.Faces.Add(side2);
                poly.FaceRoles.Add(Roles.NewAlt);
                poly.FaceTags.Add(new HashSet<string>());

                i++;
                edge2 = edge2.Next;

                if (i == sides) break;

            }
        }

        poly.Halfedges.MatchPairs();
        return poly;
    }

    // Work in progress. Not working at the moment
    private static PolyMesh _MakeRotunda(int sides, float height, bool bi = false)
    {

        sides = Mathf.Clamp(sides, 3, 64);

        PolyMesh poly = Shapes.Polygon(sides);
        Face bottom = poly.Faces[0];
        PolyMesh cap1 = Shapes.Polygon(sides, true, 0.25f, height, _CalcCupolaCapRadius(sides));
        poly.Append(cap1);

        int i = 0;
//            var upperTriFaces = new List<Face>();
//            var LowerTriFaces = new List<Face>();
//            var SidePentFaces = new List<Face>();

        var edge1 = poly.Halfedges[0];
        var edge2 = poly.Halfedges[sides * 2];

        while (true)
        {
            poly.Vertices.Add(new Vertex(Vector3.Lerp(edge1.Vector, edge2.Vector, _CalcCupolaCapRadius(sides))));
            var newV1 = poly.Vertices.Last();
            poly.Vertices.Add(new Vertex(Vector3.Lerp(edge1.Prev.Vector, edge2.Next.Vector, 0.5f)));
            var newV2 = poly.Vertices.Last();

            var pentFace = new List<Vertex>
            {
                edge1.Next.Vertex,
                edge1.Vertex,
                newV1,
            };
            poly.Faces.Add(pentFace);
            poly.FaceRoles.Add(Roles.New);
            poly.FaceTags.Add(new HashSet<string>());

            i++;
            edge1 = edge1.Next.Next;
            edge2 = edge2.Prev;
            if (i == sides) break;
        }

        poly.Halfedges.MatchPairs();
        return poly;
    }

    public static PolyMesh Prism(int sides)
    {
        return Prism(sides, _CalcSideLength(sides));
    }
    public static PolyMesh Prism(int sides, float height)
    {
        return _MakePrism(sides, height);
    }

    private static float _CalcAntiprismHeight(int sides)
    {
        return _CalcSideLength(sides) * Mathf.Sqrt(0.75f);
    }

    public static PolyMesh Antiprism(int sides)
    {
        return Antiprism(sides, _CalcAntiprismHeight(sides));
    }

    public static PolyMesh Trapezohedron(int sides)
    {
        var antiprism = Antiprism(sides);
        var poly = antiprism.Dual();
        poly.Recenter();
        return poly.Kanonicalize(16); // TODO figure out the minimum iterations
    }

    public static PolyMesh Antiprism(int sides, float height)
    {

        return _MakePrism(sides, height, true);
    }

    private static PolyMesh _MakePrism(int sides, float height, bool anti = false)
    {
        PolyMesh poly = Shapes.Polygon(sides, true);
        PolyMesh cap = Shapes.Polygon(sides, false, anti ? 0.5f : 0, height);
        poly.Append(cap);

        int i = 0;
        var edge1 = poly.Halfedges[0];
        var edge2 = poly.Halfedges[sides];
        while (true)
        {
            if (anti)
            {
                var side1 = new List<Vertex>
                {
                    edge1.Vertex,
                    edge1.Prev.Vertex,
                    edge2.Vertex
                };
                poly.Faces.Add(side1);
                poly.FaceRoles.Add(Roles.New);
                poly.FaceTags.Add(new HashSet<string>());


                var side2 = new List<Vertex>
                {
                    edge1.Vertex,
                    edge2.Vertex,
                    edge2.Prev.Vertex
                };
                poly.Faces.Add(side2);
                poly.FaceRoles.Add(Roles.NewAlt);
                poly.FaceTags.Add(new HashSet<string>());

            }
            else
            {
                var side = new List<Vertex>
                {
                    edge1.Vertex,
                    edge1.Prev.Vertex,
                    edge2.Vertex,
                    edge2.Prev.Vertex
                };
                poly.Faces.Add(side);
                poly.FaceRoles.Add(Roles.New);
                poly.FaceTags.Add(new HashSet<string>());

            }

            i++;
            edge1 = edge1.Next;
            edge2 = edge2.Prev;

            if (i == sides) break;

        }

        poly.Halfedges.MatchPairs();
        return poly;
    }

    private static float _CalcPyramidHeight(float sides)
    {
        float height;

        // Try and make equilateral sides if we can
        // Otherwise just use the nearest valid side count
        sides = Mathf.Clamp(sides, 3, 5);

        float sideLength = _CalcSideLength(sides);
        height = Mathf.Sqrt(Mathf.Pow(sideLength, 2) - 1f);


        return height;
    }

    public static PolyMesh Pyramid(int sides)
    {
        var height = _CalcPyramidHeight(sides);
        return _MakePyramid(sides, height);
    }

    public static PolyMesh Pyramid(int sides, float height)
    {
        return _MakePyramid(sides, height);
    }

    private static PolyMesh _MakePyramid(int sides, float height)
    {
        PolyMesh polygon = Shapes.Polygon(sides);
        var poly = polygon.Kis(new OpParams(height));
        var baseVerts = poly.Vertices.GetRange(0, sides);
        baseVerts.Reverse();
        poly.Faces.Insert(0, baseVerts);
        poly.FaceRoles.Insert(0, Roles.Existing);
        poly.FaceTags.Insert(0, new HashSet<string>());
        poly.Halfedges.MatchPairs();
        return poly;
    }

    // Base forms are pyramids, cupolae and rotundae.
    // For each base we have elongate and bi. For each bi form we can also gyroelongate
    // Bi can come in ortho and gyro flavours in most cases.
    // You can combine bases i.e. cupolarotunda. These also come in ortho and gyro flavours.
    // Gyrobifastigium is just trying to be weird
    // Prisms can be augmented and diminished. Also bi, tri, para and meta
    // Truncation is a thing.and can be combined with augment/diminish.
    // Phew! Then stuff gets weirder.

    public static PolyMesh ElongatedPyramid(int sides)
    {
        return ElongatedPyramid(sides, _CalcSideLength(sides), _CalcPyramidHeight(sides));
    }

    public static PolyMesh ElongatedPyramid(int sides, float height, float capHeight)
    {
        PolyMesh poly = _MakePrism(sides, height);
        poly = poly.Kis(new OpParams(capHeight, Filter.FacingDirection(Vector3.up)));
        return poly;
    }

    public static PolyMesh ElongatedDipyramid(int sides)
    {
        return ElongatedDipyramid(sides, _CalcSideLength(sides), _CalcPyramidHeight(sides));
    }

    public static PolyMesh ElongatedDipyramid(int sides, float height, float capHeight)
    {
        PolyMesh poly = ElongatedPyramid(sides, height, capHeight);
        poly = poly.Kis(new OpParams(capHeight, Filter.FacingDirection(Vector3.down)));
        poly.Transform(Vector3.down * height/2f);
        return poly;
    }

    public static PolyMesh
    GyroelongatedPyramid(int sides)
    {
        return GyroelongatedPyramid(sides, _CalcAntiprismHeight(sides), _CalcPyramidHeight(sides));
    }

    public static PolyMesh GyroelongatedPyramid(int sides, float height, float capHeight)
    {
        PolyMesh poly = Antiprism(sides, height);
        poly = poly.Kis(new OpParams(capHeight, Filter.FacingDirection(Vector3.up)));
        return poly;
    }

    public static PolyMesh GyroelongatedDipyramid(int sides)
    {
        return GyroelongatedDipyramid(sides, _CalcAntiprismHeight(sides), _CalcPyramidHeight(sides));
    }

    public static PolyMesh GyroelongatedDipyramid(int sides, float height, float capHeight)
    {
        PolyMesh poly = GyroelongatedPyramid(sides, height, capHeight);
        poly = poly.Kis(new OpParams(capHeight , Filter.FacingDirection(Vector3.down)));
        poly.Transform(Vector3.down * height/2f);
        return poly;
    }

    public static PolyMesh ElongatedCupola(int sides)
    {
        return ElongatedCupola(sides, _CalcSideLength(sides*2f), _CalcCupolaHeight(sides));
    }

    public static PolyMesh ElongatedCupola(int sides, float height, float capHeight)
    {
        PolyMesh poly = Cupola(sides, capHeight);
        poly = poly.Loft(new OpParams(0, height, filter: Filter.FacingDirection(Vector3.down)));
        poly.Transform(Vector3.up * height);
        return poly;
    }

    public static PolyMesh ElongatedBicupola(int sides, bool sideGyro)
    {
        float height = _CalcSideLength(sides * 2);
        return ElongatedBicupola(sides, height, _CalcCupolaHeight(sides), sideGyro);
    }

    public static PolyMesh ElongatedBicupola(int sides, float height, float capHeight, bool sideGyro)
    {
        PolyMesh poly = ElongatedCupola(sides, height, capHeight);
        Face bottom = poly.Faces[sides * 2];
        int i = 0;
        var middleVerts = bottom.GetVertices();

        poly.Faces.Remove(bottom);
        poly.FaceRoles.RemoveAt(poly.FaceRoles.Count - 1);
        poly.FaceTags.RemoveAt(poly.FaceRoles.Count - 1);

        float angleOffset = sideGyro ? 0.75f : 0.25f;
        PolyMesh cap2 = Shapes.Polygon(sides, true, angleOffset, -capHeight, _CalcCupolaCapRadius(sides));
        poly.Append(cap2);
        var edge2 = poly.Faces.Last().Halfedge.Prev;

        int edgeOffset = sideGyro ? 0 : 1;

        while (true)
        {
            var side1 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2 - 1 - edgeOffset, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2 - edgeOffset, sides * 2)],
                edge2.Vertex
            };
            poly.Faces.Add(side1);
            poly.FaceRoles.Add(Roles.New);
            poly.FaceTags.Add(new HashSet<string>());

            var side2 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2 - edgeOffset, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2 + 1 - edgeOffset, sides * 2)],
                edge2.Next.Vertex,
                edge2.Vertex,
            };
            poly.Faces.Add(side2);
            poly.FaceRoles.Add(Roles.NewAlt);
            poly.FaceTags.Add(new HashSet<string>());

            i++;
            edge2 = edge2.Next;

            if (i == sides) break;
        }

        poly.Halfedges.MatchPairs();
        poly.Transform(Vector3.down * height/2f);
        return poly;
    }

    public static PolyMesh GyroelongatedCupola(int sides)
    {
        return GyroelongatedCupola(sides,  _CalcAntiprismHeight(sides * 2), _CalcCupolaHeight(sides));
    }

    public static PolyMesh GyroelongatedCupola(int sides, float height, float capHeight)
    {
        PolyMesh poly = Antiprism(sides * 2, height);
        Face topFace = poly.Faces[1];
        PolyMesh cap1 = Shapes.Polygon(
            sides,
            false,
            0f,
            height + capHeight,
            _CalcCupolaCapRadius(sides)
        );
        poly.Append(cap1);

        int i = 0;
        var middleVerts = topFace.GetVertices();
        poly.Faces.Remove(topFace);
        poly.FaceRoles.RemoveAt(1);
        poly.FaceTags.RemoveAt(1);

        var edge2 = poly.Faces.Last().Halfedge.Prev;
        while (true)
        {
            var side1 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2 - 1, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2, sides * 2)],
                edge2.Vertex
            };
            poly.Faces.Add(side1);
            poly.FaceRoles.Add(Roles.New);
            poly.FaceTags.Add(new HashSet<string>());

            var side2 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2 + 1, sides * 2)],
                edge2.Next.Vertex,
                edge2.Vertex,
            };
            poly.Faces.Add(side2);
            poly.FaceRoles.Add(Roles.NewAlt);
            poly.FaceTags.Add(new HashSet<string>());

            i++;
            edge2 = edge2.Next;
            if (i == sides) break;
        }

        poly.Halfedges.MatchPairs();
        return poly;
    }

    // Gyro bool determines if the caps are gyro - not the elongation
    public static PolyMesh GyroelongatedBicupola(int sides, bool capGyro)
    {
        return GyroelongatedBicupola(sides, _CalcAntiprismHeight(sides * 2), _CalcCupolaHeight(sides), capGyro);
    }

    public static PolyMesh GyroelongatedBicupola(int sides, float height, float capHeight, bool capGyro)
    {
        PolyMesh poly = GyroelongatedCupola(sides, height, capHeight);
        Face bottomFace = poly.Faces[0];
        float angleOffset = capGyro ? 0.75f : 0.25f;
        PolyMesh cap2 = Shapes.Polygon(sides, true, angleOffset, -capHeight,
            _CalcCupolaCapRadius(sides));
        poly.Append(cap2);

        int i = 0;
        var middleVerts = bottomFace.GetVertices();
        poly.Faces.Remove(bottomFace);
        poly.FaceRoles.RemoveAt(0);
        poly.FaceTags.RemoveAt(0);
        var edge2 = poly.Faces.Last().Halfedge.Prev;
        while (true)
        {
            int indexOffset = capGyro ? 0 : -1;
            var side1 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2 - 1 + indexOffset, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2 + indexOffset, sides * 2)],
                edge2.Vertex
            };
            poly.Faces.Add(side1);
            poly.FaceRoles.Add(Roles.New);
            poly.FaceTags.Add(new HashSet<string>());
            var side2 = new List<Vertex>
            {
                middleVerts[PolyMesh.ActualMod(i * 2 + indexOffset, sides * 2)],
                middleVerts[PolyMesh.ActualMod(i * 2 + 1 + indexOffset, sides * 2)],
                edge2.Next.Vertex,
                edge2.Vertex,
            };
            poly.Faces.Add(side2);
            poly.FaceRoles.Add(Roles.NewAlt);
            poly.FaceTags.Add(new HashSet<string>());

            i++;
            edge2 = edge2.Next;

            if (i == sides) break;
        }

        poly.Halfedges.MatchPairs();
        poly.Transform(Vector3.down * height/2f);
        return poly;
    }

    private static float _CalcSideLength(float sides)
    {
        return 2 * Mathf.Sin(Mathf.PI / sides);
    }

    public static PolyMesh Dipyramid(int sides)
    {
        return Dipyramid(sides, _CalcPyramidHeight(sides));
    }

    public static PolyMesh Dipyramid(int sides, float capHeight)
    {
        var poly =  _MakeDipyramid(sides, capHeight);
        return poly;
    }

    private static PolyMesh _MakeDipyramid(int sides, float height)
    {
        PolyMesh poly = _MakePyramid(sides, height);
        poly = poly.Kis(new OpParams(height, Filter.Role(Roles.Existing)));
        return poly;
    }

    private static float _CalcPolygonSide(int sides)
    {
        return Mathf.Sin(Mathf.PI / sides) * 2f;
    }

    private static float _CalcPolygonInradius(int sides, float sideLength)
    {
        return (sideLength / (2 * Mathf.Tan(Mathf.PI / sides)));
    }

    private static float _CalcCupolaHeight(int sides)
    {
        sides = Mathf.Clamp(sides, 3, 5);
        var baseSide = _CalcPolygonSide(sides * 2);
        float radDelta = _CalcPolygonInradius(sides * 2, baseSide) - _CalcPolygonInradius(sides, baseSide);
        return Mathf.Sqrt(Mathf.Pow(baseSide, 2) - Mathf.Pow(radDelta, 2));
    }

    private static float _CalcCupolaCapRadius(int sides)
    {
        var baseSide = _CalcPolygonSide(sides * 2);
        return baseSide / (Mathf.Sin(Mathf.PI / sides) * 2f);
    }

    public static PolyMesh Cupola(int sides)
    {
        return Cupola(sides, _CalcCupolaHeight(sides));
    }

    public static PolyMesh Cupola(int sides, float capHeight)
    {
        return _MakeCupola(sides, capHeight);
    }

    public static PolyMesh OrthoBicupola(int sides)
    {
        return OrthoBicupola(sides, _CalcCupolaHeight(sides));
    }

    public static PolyMesh OrthoBicupola(int sides, float capHeight)
    {
        return _MakeBicupola(sides, capHeight, false);
    }

    public static PolyMesh GyroBicupola(int sides)
    {
        return GyroBicupola(sides, _CalcCupolaHeight(sides));
    }

    public static PolyMesh GyroBicupola(int sides, float capHeight)
    {
        return _MakeBicupola(sides, capHeight, true);
    }

    private static PolyMesh _MakeBicupola(int sides, float height, bool capGyro)
    {
        if (sides < 3) sides = 3;
        PolyMesh poly = _MakeCupola(sides, height, true, capGyro);
        return poly;
    }

    public static PolyMesh Build(RadialPolyType type, int sides)
    {
          PolyMesh poly = type switch
          {
              RadialPolyType.Prism => Prism(sides),
              RadialPolyType.Antiprism => Antiprism(sides),
              RadialPolyType.Trapezohedron => Trapezohedron(sides),
              RadialPolyType.Pyramid => Pyramid(sides),
              RadialPolyType.ElongatedPyramid => ElongatedPyramid(sides),
              RadialPolyType.GyroelongatedPyramid => GyroelongatedPyramid(sides),
              RadialPolyType.Dipyramid => Dipyramid(sides),
              RadialPolyType.ElongatedDipyramid => ElongatedDipyramid(sides),
              RadialPolyType.GyroelongatedDipyramid => GyroelongatedDipyramid(sides),
              RadialPolyType.Cupola => Cupola(sides),
              RadialPolyType.ElongatedCupola => ElongatedCupola(sides),
              RadialPolyType.GyroelongatedCupola => GyroelongatedCupola(sides),
              RadialPolyType.OrthoBicupola => OrthoBicupola(sides),
              RadialPolyType.GyroBicupola => GyroBicupola(sides),
              RadialPolyType.ElongatedOrthoBicupola => ElongatedBicupola(sides, false),
              RadialPolyType.ElongatedGyroBicupola => ElongatedBicupola(sides, true),
              RadialPolyType.GyroelongatedBicupola => GyroelongatedBicupola(sides, false),
              _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
          };
        return poly;
    }

    public static PolyMesh Build(RadialPolyType type, int sides, float height, float capHeight)
    {
        PolyMesh poly = type switch
        {
            RadialPolyType.Prism => Prism(sides, height),
            RadialPolyType.Antiprism => Antiprism(sides, height),
            RadialPolyType.Pyramid => Pyramid(sides, capHeight),
            RadialPolyType.ElongatedPyramid => ElongatedPyramid(sides, height, capHeight),
            RadialPolyType.GyroelongatedPyramid => GyroelongatedPyramid(sides, height, capHeight),
            RadialPolyType.Dipyramid => Dipyramid(sides, capHeight),
            RadialPolyType.ElongatedDipyramid => ElongatedDipyramid(sides, height, capHeight),
            RadialPolyType.GyroelongatedDipyramid => GyroelongatedDipyramid(sides, height, capHeight),
            RadialPolyType.Cupola => Cupola(sides, capHeight),
            RadialPolyType.ElongatedCupola => ElongatedCupola(sides, height, capHeight),
            RadialPolyType.GyroelongatedCupola => GyroelongatedCupola(sides, height, capHeight),
            RadialPolyType.OrthoBicupola => OrthoBicupola(sides, capHeight),
            RadialPolyType.GyroBicupola => GyroBicupola(sides, capHeight),
            RadialPolyType.ElongatedOrthoBicupola => ElongatedBicupola(sides, height, capHeight, false),
            RadialPolyType.ElongatedGyroBicupola => ElongatedBicupola(sides, height, capHeight, true),
            RadialPolyType.GyroelongatedBicupola => GyroelongatedBicupola(sides, height, capHeight, true),
            RadialPolyType.Trapezohedron => Trapezohedron(sides),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        return poly;
    }

    public enum RadialPolyType
    {
        Prism,
        Antiprism,
        Pyramid,
        ElongatedPyramid,
        GyroelongatedPyramid,
        Dipyramid,
        ElongatedDipyramid,
        GyroelongatedDipyramid,
        Cupola,
        ElongatedCupola,
        GyroelongatedCupola,
        OrthoBicupola,
        GyroBicupola,
        ElongatedOrthoBicupola,
        ElongatedGyroBicupola,
        GyroelongatedBicupola,
        Trapezohedron
    }
}
