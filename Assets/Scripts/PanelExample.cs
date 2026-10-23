using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public class PanelExample : MaquetteProceduralExample
{
    [Min(.001f)] public float width = 2.3f;
    [Min(.001f)] public float height = 1.5f;
    [Min(.001f)] public float depth = .3f;
    [Min(0f)] public float cornerRadius = .3f;
    public RectangleCorners roundedCorners = RectangleCorners.All;
    [Min(0f)] public float edgeRadius = .1f;
    [Min(1)] public int cornerSegments = 8;
    [Min(1)] public int edgeSegments = 8;
    [Min(1)] public int widthSegments = 8;
    [Min(1)] public int heightSegments = 8;
    public bool openBack;
    [Range(-360f, 360f)] public float bendX;
    [Range(-360f, 360f)] public float bendY;

    protected override GeometryRecipe CreateRecipe()
    {
        var corners = new List<string>();
        if ((roundedCorners & RectangleCorners.TopRight) != 0) corners.Add("topRight");
        if ((roundedCorners & RectangleCorners.TopLeft) != 0) corners.Add("topLeft");
        if ((roundedCorners & RectangleCorners.BottomLeft) != 0) corners.Add("bottomLeft");
        if ((roundedCorners & RectangleCorners.BottomRight) != 0) corners.Add("bottomRight");
        var operations = new List<GeometryStep>();
        if (openBack)
            operations.Add(new GeometryStep("faceRemove", new Dictionary<string, object>
            {
                { "direction", Vector3.back }, { "angle", 89.9f }
            }));
        void Bend(float angle, string axis)
        {
            if (angle == 0f) return;
            operations.Add(new GeometryStep("bend", new Dictionary<string, object>
            {
                { "angle", angle }, { "axis", axis },
                { "referenceCenter", Vector3.zero },
                { "referenceSize", new Vector3(width, height, depth) }
            }));
        }
        Bend(bendX, "x");
        Bend(bendY, "y");
        // Present the XY panel face-up on the same plinths as the other examples.
        operations.Add(new GeometryStep("transform", new Dictionary<string, object>
        {
            { "rotation", Quaternion.Euler(-90f, 0f, 0f) }
        }));
        return new GeometryRecipe(new GeometryStep("panel", new Dictionary<string, object>
        {
            { "width", width }, { "height", height }, { "depth", depth },
            { "cornerRadius", cornerRadius }, { "roundedCorners", corners.ToArray() },
            { "edgeRadius", edgeRadius }, { "cornerSegments", cornerSegments },
            { "edgeSegments", edgeSegments }, { "widthSegments", widthSegments },
            { "heightSegments", heightSegments }
        }), operations);
    }
}
