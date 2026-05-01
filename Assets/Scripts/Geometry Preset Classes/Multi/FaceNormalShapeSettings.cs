using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FaceNormalShapeSettings", menuName = "PolyhydraMulti/FaceNormalShapeSettings", order = 1)]
public class FaceNormalShapeSettings : BaseSettings
{
    [Header("Source")]
    public BaseSettings SourceSettings;
    public bool IncludeSource = false;

    [Header("Normal Tube")]
    [Min(0.001f)] public float Length = 0.5f;
    public bool Bidirectional = false;

    [Header("Filter")]
    [Tooltip("Only extrude faces with this many sides. 0 = all faces.")]
    public int OnlySides = 0;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 4;
    [Min(0.001f)] public float Radius = 0.05f;
    public bool CapEnds = true;

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = SourceSettings.BuildBaseShape();
        sourcePoly = SourceSettings.ApplyModifiers(sourcePoly);

        var finalPoly = IncludeSource ? sourcePoly.Duplicate() : new PolyMesh();

        foreach (var face in sourcePoly.Faces)
        {
            if (OnlySides > 0 && face.Sides != OnlySides) continue;

            var centroid = face.Centroid;
            var normal   = face.Normal;

            var points = Bidirectional
                ? new List<Vector3> { centroid - normal * Length, centroid + normal * Length }
                : new List<Vector3> { centroid, centroid + normal * Length };

            // Use face normal as up hint so the tube's cross-section is perpendicular to it
            var upHint = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f ? Vector3.up : Vector3.forward;
            finalPoly.Append(PolylineSweep.Build(points, closed: false, Mode, Sides, Radius, upHint, CapEnds));
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
