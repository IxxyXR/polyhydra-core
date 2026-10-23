using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public class MaquetteRectangleExample : MaquetteProceduralExample
{
    [Min(.001f)] public float width = 2.3f;
    [Min(.001f)] public float height = 1.5f;
    [Min(0)] public float radius = .3f;
    [Tooltip("Segments per corner; used when radius is greater than zero.")]
    [Min(1)] public int cornerSegments = 4;
    [Min(.001f)] public float thickness = .22f;
    public bool smooth = true;

    protected override GeometryRecipe CreateRecipe()
    {
        var operations = new List<GeometryStep>
        {
            new GeometryStep("shell", new Dictionary<string, object> { { "thickness", thickness } }),
            new GeometryStep("transform", new Dictionary<string, object>
            {
                { "rotation", Quaternion.Euler(90f, 0f, 0f) }
            })
        };
        if (smooth)
            operations.Add(new GeometryStep("autoSmooth", new Dictionary<string, object> { { "angle", 89f } }));
        return new GeometryRecipe(new GeometryStep("rectangle", new Dictionary<string, object>
        {
            { "width", width },
            { "height", height },
            { "radius", radius },
            { "cornerSegments", cornerSegments },
        }), operations);
    }
}
