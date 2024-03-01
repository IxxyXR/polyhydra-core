using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "ShapesSettings", menuName = "Polyhydra/ShapesSettings", order = 1)]
public class ShapesSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    public ShapeTypes type;
    public Shapes.Method method;

    [Range(0, 24)] public float A;
    [Range(0, 4)] public float B;
    [Range(0, 4)] public float C;
    [Range(0, 16)] public int Layers;
    [Range(0, 1f)] public float LayerHeight = .25f;

    public override PolyMesh BuildBaseShape()
    {
        var poly = Shapes.Build(type, A, B, C, method);
        Axis axis = type switch
        {
            ShapeTypes.Polygon => Axis.Y,
            ShapeTypes.Star => Axis.Y,
            ShapeTypes.C_Shape => Axis.Y,
            ShapeTypes.L_Shape => Axis.Y,
            ShapeTypes.H_Shape => Axis.Y,
            ShapeTypes.Arc => Axis.Z,
            ShapeTypes.Arch => Axis.Z,
            ShapeTypes.GothicArch => Axis.Z,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (Layers>0) poly = poly.LayeredExtrude(Layers, LayerHeight, axis);
        return poly;
    }
}