using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TileGenTest : TestBase
{
    [Header("Base Shape Parameters")]
    [Multiline(20)]
    public string format;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    public GridEnums.GridShapes shape;
    
    public override void Go()
    {
        var tileDef = Grids.BuildTileDefFromFormat(format);
        tileDef.gridShape = shape;
        tileDef.xRepeats = X;
        tileDef.yRepeats = Y;
        poly = Grids.BuildGridFromTileDef(tileDef);
        Build();
    }
}
