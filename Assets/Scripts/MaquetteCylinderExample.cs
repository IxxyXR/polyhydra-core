using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public class MaquetteCylinderExample : MaquetteProceduralExample
{
    [Min(3)] public int sides = 24;
    [Min(.001f)] public float height = 1.7f;
    [Tooltip("Bottom X radius; zero collapses the bottom to a point.")]
    [Min(0)] public float radiusBottom = .7f;
    [Tooltip("Bottom Z radius divided by X radius; 1 makes a circle.")]
    [Min(.001f)] public float aspectBottom = 1f;
    [Tooltip("Top X radius; zero collapses the top to a point.")]
    [Min(0)] public float radiusTop = .7f;
    [Tooltip("Top Z radius divided by X radius; 1 makes a circle.")]
    [Min(.001f)] public float aspectTop = 1f;
    [Range(-360f, 360f)] public float sweepAngle = 360f;
    public Vector2 topOffset;
    public Vector3 topEulerAngles;
    public bool smoothSides = true;

    protected override GeometryRecipe CreateRecipe()
    {
        return new GeometryRecipe(new GeometryStep("cylinder", new Dictionary<string, object>
        {
            { "sides", sides },
            { "height", height },
            { "bottomRadius", radiusBottom },
            { "bottomAspect", aspectBottom },
            { "topRadius", radiusTop },
            { "topAspect", aspectTop },
            { "sweepAngle", sweepAngle },
            { "topOffset", topOffset },
            { "topRotation", Quaternion.Euler(topEulerAngles) },
            { "smoothSides", smoothSides },
        }));
    }
}
