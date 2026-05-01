using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalShapeSettings", menuName = "PolyhydraMulti/FractalShapeSettings", order = 1)]
public class FractalShapeSettings : BaseSettings
{
    [Header("Fractal Parameters")]
    public BaseSettings ShapeSettings;
    [Range(1, 4)] public int Depth = 2;

    // At each level every face of the current mesh is replaced by a copy of the
    // original source shape, scaled and oriented to fit that face.
    // Face count grows as O(sourceFaces ^ Depth) — keep Depth small.

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = ShapeSettings.BuildBaseShape();
        sourcePoly = ShapeSettings.ApplyModifiers(sourcePoly);

        var current = sourcePoly.Duplicate();

        for (int d = 0; d < Depth - 1; d++)
        {
            var next = new PolyMesh();
            foreach (var face in current.Faces)
                next.Append(sourcePoly.Duplicate(FaceMatrix(face)));
            current = next;
        }

        current = ApplyModifiers(current);
        return current;
    }

    private static Matrix4x4 FaceMatrix(Face face)
    {
        var centroid = face.Centroid;
        var normal   = face.Normal;
        var edgeDir  = face.Halfedge.Vector.sqrMagnitude > 0.0001f
            ? face.Halfedge.Vector.normalized
            : Vector3.forward;

        float radius = 0f;
        foreach (var v in face.GetVertices())
            radius = Mathf.Max(radius, Vector3.Distance(v.Position, centroid));

        var rot = Quaternion.LookRotation(edgeDir, normal);
        return Matrix4x4.TRS(centroid, rot, Vector3.one * radius);
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
