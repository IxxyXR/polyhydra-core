using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "StairsSettings", menuName = "Polyhydra/StairsSettings", order = 1)]
public class StairsSettings : BaseSettings
{
    [Header("Stair Parameters")]
    public int Segments = 4;
    public float Rotation;
    public float Scaling = 0;
    public float Distance = 1f;
    public Vector3 vector;

    public override PolyMesh BuildBaseShape()
    {
        var poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 1, 1);
        for (var i = 0; i < Segments; i++)
        {
            poly = poly.Loft(new OpParams(0, Distance, filter: Filter.Role(Roles.Existing)));
            poly = poly.VertexRotate(new OpParams(Rotation, Scaling, filter: Filter.Role(Roles.Existing)), vector);
        }
        return poly;
    }
}