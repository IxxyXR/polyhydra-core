using Polyhydra.Core;
using UnityEngine;

public class MaquetteCylinderExample : MaquetteProceduralExample
{
    [Min(3)] public int sides = 24;
    [Min(.001f)] public float height = 1.7f;
    [Min(0)] public float radiusBottom = .7f;
    [Min(0)] public float radiusTop = .7f;
    [Range(-360f, 360f)] public float sweepAngle = 360f;
    public Vector2 topOffset;
    public Vector3 topEulerAngles;
    public Vector2 bottomScale = Vector2.one;
    public Vector2 topScale = Vector2.one;
    public bool smoothSides = true;
    public bool triangulateSides;

    protected override PolyMesh Build() => VariousSolids.Cylinder(sides, height,
        radiusBottom, radiusTop, sweepAngle, topOffset, Quaternion.Euler(topEulerAngles),
        bottomScale, topScale, smoothSides, triangulateSides);
}
