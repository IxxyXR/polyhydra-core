using Polyhydra.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "MultiGridSettings", menuName = "Polyhydra/MultiGridSettings", order = 1)]
public class MultiGridSettings : BaseSettings
{
    public enum ColorFunctions
    {
        Unchanged,
        Mod,
        ActualMod,
        Normalized,
        Abs
    }

    [Header("Base Shape Parameters")]
    [Range(1, 30)] public int Divisions = 5;
    [Range(3, 30)] public int Dimensions = 5;
    public float Offset = .2f;
    public bool randomize;

    public float MinDistance = 0f;
    public float MaxDistance = 1f;

    public float colorRatio = 1.0f;
    public float colorIndex;
    public float colorIntersect;
    public Gradient ColorGradient;
    public bool SharedVertices;

    private List<List<Vector2>> shapes;
    private List<float> colors;

    public ColorFunctions ColorFunction;

    public override PolyMesh BuildBaseShape()
    {
        var multigrid = new MultiGrid(Divisions, Dimensions, Offset, MinDistance, MaxDistance, colorRatio, colorIndex, colorIntersect);
        PolyMesh poly = null;
        (poly, shapes, colors) = multigrid.Build(SharedVertices, randomize);

        if (shapes.Count == 0) return poly;
        if (true) // TODO ColorMethod == ColorMethods.ByTags
        {
            float colorMin = colors.Min();
            float colorMax = colors.Max();
            poly.InitTags();

            for (var faceIndex = 0; faceIndex < poly.Faces.Count; faceIndex++)
            {
                var colorIndex = colors[faceIndex];
                float colorValue;
                switch (ColorFunction)
                {
                    case ColorFunctions.Mod:
                        colorValue = colorIndex % 1;
                        break;
                    case ColorFunctions.ActualMod:
                        colorValue = Mathf.Repeat(colorIndex, 1);
                        break;
                    case ColorFunctions.Normalized:
                        colorValue = Mathf.InverseLerp(colorMin, colorMax, colorIndex);
                        break;
                    case ColorFunctions.Abs:
                        colorValue = Mathf.Abs(colorIndex);
                        break;
                    default:
                        colorValue = colorIndex;
                        break;
                }
                string colorString = ColorUtility.ToHtmlStringRGB(ColorGradient.Evaluate(colorValue));
                poly.FaceTags[faceIndex].Add($"#{colorString}");
            }
        }
        return poly;
    }
}
