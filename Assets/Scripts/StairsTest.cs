using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StairsTest : TestBase
{
    [Header("Base Shape Parameters")]
    public int Segments = 4;
    public float Rotation;
    public float Scaling = 0;
    public float Distance = 1f;
    public Vector3 vector;

    [ContextMenu("Go")]
    public override void Go()
    {
        poly = Grids.Build(GridEnums.GridTypes.Square, GridEnums.GridShapes.Plane, 1, 1);
        for (var i = 0; i < Segments; i++)
        {
            poly = poly.Loft(new OpParams(0, Distance, filter: Filter.Role(Roles.Existing)));
            poly = poly.VertexRotate(new OpParams(Rotation, Scaling, filter: Filter.Role(Roles.Existing)), vector);
        }
        Build();
    }
}
