using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
public enum VariousSolidTypes
{
    Polygon,
    UvSphere,
    UvHemisphere,
    GriddedCube,
    C_Shape,
    L_Shape,
    L_Alt_Shape,
    H_Shape
}

public static class VariousSolids
{
    
    public static PolyMesh BuildOther(VariousSolidTypes type, int p, int q)
    {
    PolyMesh poly = null;

    switch (type)
    {
	    case VariousSolidTypes.Polygon:
		    poly = Polygon(p);
		    break;
	    case VariousSolidTypes.UvSphere:
		    poly = UvSphere(p, q);
		    break;
	    case VariousSolidTypes.UvHemisphere:
		    poly = UvHemisphere(p, q);
		    break;
	    case VariousSolidTypes.GriddedCube:
		    poly = GriddedCube(p);
		    break;
	    case VariousSolidTypes.C_Shape:
		    poly = C_Shape();
		    break;
	    case VariousSolidTypes.L_Shape:
		    poly = L_Shape();
		    break;
	    case VariousSolidTypes.L_Alt_Shape:
		    poly = L_Alt_Shape();
		    break;
	    case VariousSolidTypes.H_Shape:
		    poly = H_Shape();
		    break;
    }
    return poly;
    }

    
    public static PolyMesh L_Shape()
    {
        var verts = new List<Vector3>();
        for (var i = -0.25f; i <= 0.25f; i+=0.5f)
        {
            verts.Add(new Vector3(0, i, 0));
            verts.Add(new Vector3(0.5f, i, 0));
            verts.Add(new Vector3(0.5f, i, -0.5f));
            verts.Add(new Vector3(-0.5f, i, -0.5f));
            verts.Add(new Vector3(-0.5f, i, 0.5f));
            verts.Add(new Vector3(0, i, 0.5f));
        }

        var faces = new List<List<int>>
        {
            new List<int>{0, 5, 4, 3},
            new List<int>{0, 3, 2, 1},
            new List<int>{6, 7, 8, 9},
            new List<int>{6, 9, 10, 11},
            new List<int>{3, 9, 8, 2},
            new List<int>{2, 8, 7, 1},
            new List<int>{1, 7, 6, 0},
            new List<int>{0, 6, 11, 5},
            new List<int>{5, 11, 10, 4},
            new List<int>{4, 10, 9, 3}
        };

        var faceRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 10);
        var vertexRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 12);

        return new PolyMesh(verts, faces, faceRoles, vertexRoles);
    }

    public static PolyMesh L_Alt_Shape()
    {
        var verts = new List<Vector3>();
        for (var i = -0.25f; i <= 0.25f; i+=0.5f)
        {
            verts.Add(new Vector3(0, i, 0));
            verts.Add(new Vector3(0.5f, i, 0));
            verts.Add(new Vector3(0.5f, i, -0.5f));
            verts.Add(new Vector3(0, i, -0.5f));
            verts.Add(new Vector3(-0.5f, i, -0.5f));
            verts.Add(new Vector3(-0.5f, i, 0));
            verts.Add(new Vector3(-0.5f, i, 0.5f));
            verts.Add(new Vector3(0, i, 0.5f));
        }

        var faces = new List<List<int>>
        {
            new List<int>{0, 7, 6, 5},
            new List<int>{0, 5, 4, 3},
            new List<int>{0, 3, 2, 1},
            new List<int>{8, 9, 10, 11},
            new List<int>{8, 11, 12, 13},
            new List<int>{8, 13, 14, 15},
            new List<int>{4, 12, 11, 10, 2, 3},
            new List<int>{2, 10, 9, 1},
            new List<int>{1, 9, 8, 0},
            new List<int>{0, 8, 15, 7},
            new List<int>{7, 15, 14, 6},
            new List<int>{6, 14, 13, 12, 4, 5}
        };

        var faceRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 12);
        var vertexRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 16);

        return new PolyMesh(verts, faces, faceRoles, vertexRoles);
    }

    public static PolyMesh C_Shape()
    {
        var conway = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, 2, 3);
        conway = conway._FaceRemove(new OpParams(Filter.Index(4)));
        return conway;
    }

    public static PolyMesh H_Shape()
    {
        var conway = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, 3, 3);
        conway = conway.FaceRemove(new OpParams(Filter.Index(3)));
        conway = conway.FaceRemove(new OpParams(Filter.Index(4)));
        return conway;
    }

    public static PolyMesh GriddedCube(int PrismP)
    {
        var cap = Grids.Build(GridEnums.GridTypes.K_4_4_4_4, GridEnums.GridShapes.Plane, PrismP, PrismP);
        var box = cap.Duplicate();
        for (var i = 0; i < PrismP; i++)
        {
            box = box.ExtendBoundaries(new OpParams(i==0 ? -1 : 1, i==0 ? 90f : 0f));
        }
        cap.Transform(new Vector3(0, -PrismP, 0));
        box.Append(cap);
        box = box.Weld(0.001f);
        box.Recenter();
        return box;
    }

    public static PolyMesh Polygon(int sides)
    {
        return Shapes.MakePolygon(sides);
    }

    public static PolyMesh Test3Triangle()
    {
        var verts = new List<Vector3>();
        verts.Add(new Vector3(0.5f, 0, 0));
        verts.Add(new Vector3(-0.5f, 0, 0));
        verts.Add(new Vector3(0, 0, -0.5f));
        verts.Add(new Vector3(0, 0.5f, -0.5f));

        var faces = new List<List<int>>
        {
            new List<int>{0,1,2},
            new List<int>{3,1,0},
            new List<int>{0,2,3}
        };

        var faceRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 3);
        var vertexRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 4);
        return new PolyMesh(verts, faces, faceRoles, vertexRoles);
    }

    public static PolyMesh Test2Square()
    {
        var verts = new List<Vector3>();
        verts.Add(new Vector3(0.5f, 0, 0));
        verts.Add(new Vector3(-0.5f, 0, 0));
        verts.Add(new Vector3(0.5f, 0, -0.5f));
        verts.Add(new Vector3(-0.5f, 0, -0.5f));
        verts.Add(new Vector3(0.5f, 0.5f, -0.5f));
        verts.Add(new Vector3(-0.5f, 0.5f, -0.5f));

        var faces = new List<List<int>>
        {
            new List<int>{0,2,3,1},
            new List<int>{0,1,5,4}
        };

        var faceRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 2);
        var vertexRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, 6);
        return new PolyMesh(verts, faces, faceRoles, vertexRoles);
    }

    public static PolyMesh UvSphere(int verticalLines = 24, int horizontalLines = 24, float hemi = 1)
    {

        var faceRoles = new List<PolyMesh.Roles>();

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
                faces.Add(new List<int> {
                    (v * verticalLines) + u,
                    (v * verticalLines) + ((u + 1) % verticalLines),
                    ((v + 1) * verticalLines) + ((u + 1) % verticalLines),
                    ((v + 1) * verticalLines) + u
                });
                faceRoles.Add((u + v) % 2 == 0 ? PolyMesh.Roles.New : PolyMesh.Roles.NewAlt);
            }
        }

        var vertexRoles = Enumerable.Repeat(PolyMesh.Roles.Existing, verts.Count);
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