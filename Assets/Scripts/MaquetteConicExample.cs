using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public class MaquetteConicExample : MaquetteProceduralExample
{
    public Shapes.ConicType type = Shapes.ConicType.Circle;
    [Min(3)] public int sides = 24;
    [Min(2)] public int verticalSegments = 12;
    [Min(.001f)] public float radius = .9f;
    [Min(.001f)] public float majorAxis = 1f;
    [Min(.001f)] public float minorAxis = 1f;
    public float centerOffset = .2f;
    [Range(0f, 1f)] public float bottomClip;
    [Range(0f, 1f)] public float topClip = 1f;
    [Range(-360f, 360f)] public float sweepAngle = 360f;
    public bool smooth = true;
    public PolyMesh.ZeroSweepBehavior zeroSweepBehavior = PolyMesh.ZeroSweepBehavior.ThinWedge;

    protected override GeometryRecipe CreateRecipe()
    {
        return new GeometryRecipe(new GeometryStep("conic", new Dictionary<string, object>
        {
            { "curve", CurveName() },
            { "sides", sides },
            { "verticalSegments", verticalSegments },
            { "radius", radius },
            { "majorAxis", majorAxis },
            { "minorAxis", minorAxis },
            { "centerOffset", centerOffset },
            { "bottomClip", bottomClip },
            { "topClip", topClip },
            { "sweepAngle", sweepAngle },
            { "smooth", smooth },
            { "zeroSweep", ZeroSweepName(zeroSweepBehavior) },
        }));
    }

    private string CurveName()
    {
        switch (type)
        {
            case Shapes.ConicType.Circle: return "circle";
            case Shapes.ConicType.Ellipse: return "ellipse";
            case Shapes.ConicType.Hyperbola: return "hyperbola";
            case Shapes.ConicType.Parabola: return "parabola";
            default: throw new System.ArgumentOutOfRangeException(nameof(type));
        }
    }
}
