using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "GridSettings", menuName = "Polyhydra/GridSettings", order = 1)]
public class GridSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    public GridEnums.GridTypes type;
    public GridEnums.GridShapes shape;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    public List<int> Faces;

    public override PolyMesh BuildBaseShape()
    {
        var poly = Grids.Build(type, shape, X, Y);
        if (Faces.Count > 0) poly = poly.FaceRemove(true, Faces);
        return poly;
    }
}