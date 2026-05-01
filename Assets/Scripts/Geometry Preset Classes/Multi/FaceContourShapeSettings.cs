using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FaceContourShapeSettings", menuName = "PolyhydraMulti/FaceContourShapeSettings", order = 1)]
public class FaceContourShapeSettings : BaseSettings
{
    [Header("Source")]
    public BaseSettings SourceSettings;
    public bool IncludeSource = false;

    [Header("Filter")]
    [Tooltip("Only sweep faces with this many sides. 0 = all faces.")]
    public int OnlySides = 0;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 4;
    [Min(0.001f)] public float Radius = 0.05f;
    public Vector3 UpHint = Vector3.up;

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = SourceSettings.BuildBaseShape();
        sourcePoly = SourceSettings.ApplyModifiers(sourcePoly);

        var finalPoly = IncludeSource ? sourcePoly.Duplicate() : new PolyMesh();

        foreach (var face in sourcePoly.Faces)
        {
            var faceVerts = face.GetVertices();
            if (OnlySides > 0 && faceVerts.Count != OnlySides) continue;

            var points = faceVerts.Select(v => v.Position).ToList();
            finalPoly.Append(PolylineSweep.Build(points, closed: true, Mode, Sides, Radius, UpHint, capEnds: false));
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
        SourceSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        SourceSettings.DetachAction(settingsChanged);
    }
}
