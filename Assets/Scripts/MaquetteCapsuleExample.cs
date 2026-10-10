using Polyhydra.Core;
using UnityEngine;

public class MaquetteCapsuleExample : MaquetteProceduralExample
{
    [Min(3)] public int sides = 24;
    [Min(1)] public int capSegments = 12;
    [Min(0)] public float cylinderHeight = 1.2f;
    [Min(.001f)] public float radius = .6f;
    [Range(-360f, 360f)] public float sweepAngle = 360f;
    public bool smooth = true;
    public Vector3 scale = Vector3.one;
    public PolyMesh.ZeroSweepBehavior zeroSweepBehavior = PolyMesh.ZeroSweepBehavior.ThinWedge;

    protected override PolyMesh Build()
    {
        var poly = VariousSolids.Capsule(sides, capSegments, cylinderHeight, radius,
            sweepAngle, zeroSweepBehavior, smooth);
        foreach (var vertex in poly.Vertices)
            vertex.Position = Vector3.Scale(vertex.Position, scale);
        return poly;
    }
}
