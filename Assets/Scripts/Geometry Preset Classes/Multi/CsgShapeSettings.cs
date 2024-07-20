using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "CsgShapeSettings", menuName = "PolyhydraMulti/CsgShapeSettings", order = 1)]
public class CsgShapeSettings : BaseSettings
{
    [Header(" ")]
    public PolyMesh.CsgOp csgOp;
    public BaseSettings Operand1;
    public BaseSettings Operand2;
    public PolyTransform Operand2Transform;

    public override PolyMesh BuildBaseShape()
    {
        return null;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        PolyMesh finalPoly = new PolyMesh();
        var poly1 = Operand1.BuildBaseShape();
        poly1 = Operand1.ApplyModifiers(poly1);
        var poly2 = Operand2.BuildBaseShape();
        poly2 = Operand2.ApplyModifiers(poly2);
        poly2.Transform(
            Operand2Transform.Translation,
            Operand2Transform.Rotation,
            Operand2Transform.NonUniformScale * Operand2Transform.Scale
        );
        finalPoly = poly1.ApplyCsg(poly2, csgOp);
        finalPoly = ApplyModifiers(finalPoly);

        var meshData = finalPoly.BuildMeshData(
            colorMethod: GetColorMethod(appearanceSettings),
            colors: CalculateColorList(appearanceSettings)
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged, PolyhydraGenerator generator)
    {
        OnSettingsChanged += settingsChanged;
        Operand1.AttachAction(settingsChanged, generator);
        Operand2.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        Operand1.DetachAction(settingsChanged);
        Operand2.DetachAction(settingsChanged);
    }
}