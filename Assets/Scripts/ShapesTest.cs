using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShapesTest : TestBase
{
    [Header("Base Shape Parameters")]
    public ShapeTypes type;

    [Range(0, 24)] public float A;
    [Range(0, 4)] public float B;
    [Range(0, 4)] public float C;
    [Range(0, 16)] public int Layers;
    [Range(0, 1f)] public float LayerHeight = .25f;
    
    public override void Go()
    {
        poly = Shapes.Build(type, A, B, C);
        if (Layers>0) poly = poly.LayeredExtrude(Layers, LayerHeight);
        Build();
    }
}
