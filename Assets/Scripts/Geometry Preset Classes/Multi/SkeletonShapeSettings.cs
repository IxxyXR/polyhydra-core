using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonShapeSettings", menuName = "PolyhydraMulti/SkeletonShapeSettings", order = 1)]
public class SkeletonShapeSettings : BaseSettings
{
    [Header("Skeleton Parameters")]
    public BaseSettings ShapeSettings;
    public List<PolyTransform> Spine = new List<PolyTransform>();

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = ShapeSettings.BuildBaseShape();
        sourcePoly = ShapeSettings.ApplyModifiers(sourcePoly);

        var finalPoly = new PolyMesh();
        foreach (var transform in Spine)
            finalPoly.Append(sourcePoly.Duplicate(transform.Matrix));

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
