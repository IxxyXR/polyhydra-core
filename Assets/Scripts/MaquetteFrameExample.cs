using Polyhydra.Core;
using UnityEngine;

public class MaquetteFrameExample : MaquetteProceduralExample
{
    [Min(.001f)] public float width = 2.3f;
    [Min(.001f)] public float height = 1.5f;
    [Min(0)] public float borderWidth = .22f;
    [Min(0)] public float radius = .3f;
    [Tooltip("Segments per corner; used when radius is greater than zero.")]
    [Min(1)] public int cornerSegments = 4;
    [Min(.001f)] public float thickness = .22f;
    public bool smooth = true;

    protected override PolyMesh Build()
    {
        var solid = Shapes.Frame(width, height, borderWidth, radius, cornerSegments)
            .Shell(thickness).Rotate(Vector3.right, 90f);
        if (smooth) solid.AutoSmooth(89f);
        return solid;
    }
}
