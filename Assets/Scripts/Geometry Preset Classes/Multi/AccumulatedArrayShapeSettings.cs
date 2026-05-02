using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "AccumulatedArrayShapeSettings", menuName = "PolyhydraMulti/AccumulatedArrayShapeSettings", order = 1)]
public class AccumulatedArrayShapeSettings : BaseSettings
{
    [Header("Source")]
    public BaseSettings ShapeSettings;

    [Header("Primary Axis")]
    public int Count = 3;
    public PolyTransform Transform;

    [Header("Secondary Axis")]
    public bool UseSecondAxis = false;
    public int Count2 = 3;
    public PolyTransform Transform2;

    [Header("Tertiary Axis")]
    public bool UseThirdAxis = false;
    public int Count3 = 3;
    public PolyTransform Transform3;

    public override PolyMesh BuildBaseShape()
    {
        var finalPoly = new PolyMesh();
        var sourcePoly = ShapeSettings.BuildBaseShape();
        sourcePoly = ShapeSettings.ApplyModifiers(sourcePoly);

        var mat1 = Transform.Matrix;
        var mat2 = Transform2.Matrix;
        var mat3 = Transform3.Matrix;
        int count2 = UseSecondAxis ? Mathf.Max(1, Count2) : 1;
        int count3 = UseThirdAxis ? Mathf.Max(1, Count3) : 1;

        var accum1 = Matrix4x4.identity;
        for (int i = 0; i < Mathf.Max(1, Count); i++)
        {
            var accum2 = Matrix4x4.identity;
            for (int j = 0; j < count2; j++)
            {
                var accum3 = Matrix4x4.identity;
                for (int k = 0; k < count3; k++)
                {
                    finalPoly.Append(sourcePoly.Duplicate(accum1 * accum2 * accum3));
                    accum3 = mat3 * accum3;
                }
                accum2 = mat2 * accum2;
            }
            accum1 = mat1 * accum1;
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
