using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VariousTest : TestBase
{
    [Header("Base Shape Parameters")]
    public VariousSolidTypes type;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    [Range(1, 64)] public int Z = 3;
    
    public override void Go()
    {
        poly = VariousSolids.Build(type, X, Y, Z);
        Build();
    }
}
