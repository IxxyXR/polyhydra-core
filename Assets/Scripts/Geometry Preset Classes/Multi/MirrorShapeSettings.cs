using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "MirrorShapeSettings", menuName = "PolyhydraMulti/MirrorShapeSettings", order = 1)]
public class MirrorShapeSettings : BaseSettings
{
    [Header("Mirror Parameters")]
    public BaseSettings ShapeSettings;
    public bool MirrorX = true;
    public bool MirrorY = false;
    public bool MirrorZ = false;

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = ShapeSettings.BuildBaseShape();
        sourcePoly = ShapeSettings.ApplyModifiers(sourcePoly);

        // Accumulate mirror matrices — each enabled axis doubles the copy count,
        // producing all combinations (e.g. X+Z gives original, X-mirror, Z-mirror, XZ-mirror).
        var matrices = new List<Matrix4x4> { Matrix4x4.identity };

        if (MirrorX) AddMirrors(matrices, new Vector3(-1, 1, 1));
        if (MirrorY) AddMirrors(matrices, new Vector3(1, -1, 1));
        if (MirrorZ) AddMirrors(matrices, new Vector3(1, 1, -1));

        var finalPoly = new PolyMesh();
        foreach (var mat in matrices)
            finalPoly.Append(sourcePoly.Duplicate(mat));

        finalPoly = ApplyModifiers(finalPoly);
        return finalPoly;
    }

    private static void AddMirrors(List<Matrix4x4> matrices, Vector3 flipScale)
    {
        int count = matrices.Count;
        for (int i = 0; i < count; i++)
            matrices.Add(Matrix4x4.Scale(flipScale) * matrices[i]);
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
