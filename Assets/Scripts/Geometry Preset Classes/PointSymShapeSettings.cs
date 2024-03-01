using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "PointSymShapeSettings", menuName = "PolyhydraMulti/PointSymShapeSettings", order = 1)]
public class PointSymShapeSettings : BaseSettings
{
    public BaseSettings ShapeSettings;
    public PointSymmetry.Family Family;
    public PolyTransform Transform;
    public int Repeats = 3;
    public float Radius = 1f;

    public override PolyMesh BuildBaseShape()
    {
        return null;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
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

        var meshData = finalPoly.BuildMeshData(
            colorMethod: appearanceSettings.ColorMethod,
            colors: appearanceSettings.CalculateColorList()
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged)
    {
        OnSettingsChanged += settingsChanged;
        ShapeSettings.AttachAction(settingsChanged);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        ShapeSettings.DetachAction(settingsChanged);
    }
}