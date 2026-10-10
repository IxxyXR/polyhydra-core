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
    public bool triangulateSides;
    public PolyMesh.ZeroSweepBehavior zeroSweepBehavior = PolyMesh.ZeroSweepBehavior.ThinWedge;

    protected override PolyMesh Build() => VariousSolids.Conic(type, sides, verticalSegments,
        radius, majorAxis, minorAxis, centerOffset, bottomClip, topClip, sweepAngle,
        smooth, triangulateSides, zeroSweepBehavior);
}
