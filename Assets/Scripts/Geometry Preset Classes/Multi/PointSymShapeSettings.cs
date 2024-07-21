using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "PointSymShapeSettings", menuName = "PolyhydraMulti/PointSymShapeSettings", order = 1)]
public class PointSymShapeSettings : BaseSettings
{
    [Header("Point Symmetry Parameters")]
    public BaseSettings ShapeSettings;
    public PointSymmetry.Family Family;
    public PolyTransform Transform;
    public int Repeats = 3;
    public float Radius = 1f;

    public override PolyMesh BuildBaseShape()
    {
        PolyMesh finalPoly = new PolyMesh();
        var poly = ShapeSettings.BuildBaseShape();
        poly = ShapeSettings.ApplyModifiers(poly);
        poly.Transform(Transform.Matrix);
        var pointSym = new PointSymmetry(Family, Repeats, Radius);

        foreach (var mat in pointSym.matrices)
        {
            finalPoly.Append(poly.Duplicate(mat));
        }
        finalPoly = ApplyModifiers(finalPoly);

        return finalPoly;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        var finalPoly = BuildBaseShape();
        var meshData = finalPoly.BuildMeshData(
            colorMethod: GetColorMethod(appearanceSettings),
            colors: CalculateColorList(appearanceSettings)
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged, PolyhydraGenerator generator)
    {
        OnSettingsChanged += settingsChanged;
        ShapeSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        ShapeSettings.DetachAction(settingsChanged);
    }
}