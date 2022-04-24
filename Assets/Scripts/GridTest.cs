using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridTest : TestBase
{
    [Header("Base Shape Parameters")]
    public GridEnums.GridTypes type;
    public GridEnums.GridShapes shape;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;

    public override void Go()
    {
        poly = Grids.Build(type, shape, X, Y);
        Build();
    }
}
