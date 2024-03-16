using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "TileGenSettings", menuName = "Polyhydra/TileGenSettings", order = 1)]
public class TileGenSettings : BaseSettings
{
    [Header("Tile Gen Parameters")]
    [Multiline(20)]
    public string format;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    public GridEnums.GridShapes shape;

    public override PolyMesh BuildBaseShape()
    {
        var tileDef = Grids.BuildTileDefFromFormat(format);
        tileDef.gridShape = shape;
        tileDef.xRepeats = X;
        tileDef.yRepeats = Y;
        var poly = Grids.BuildGridFromTileDef(tileDef);
        return poly;
    }
}