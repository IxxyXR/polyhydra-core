using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FaceSubstitutionShapeSettings", menuName = "PolyhydraMulti/FaceSubstitutionShapeSettings", order = 1)]
public class FaceSubstitutionShapeSettings : BaseSettings
{
    [Header("Face Substitution Parameters")]
    public BaseSettings HostSettings;
    public BaseSettings SourceSettings;
    public bool ScaleToFace = true;
    public bool AlignToNormal = true;
    public bool IncludeHost = false;

    public override PolyMesh BuildBaseShape()
    {
        var hostPoly = HostSettings.BuildBaseShape();
        hostPoly = HostSettings.ApplyModifiers(hostPoly);

        var sourcePoly = SourceSettings.BuildBaseShape();
        sourcePoly = SourceSettings.ApplyModifiers(sourcePoly);

        var finalPoly = IncludeHost ? hostPoly.Duplicate() : new PolyMesh();

        foreach (var face in hostPoly.Faces)
        {
            var centroid = face.Centroid;

            float scale = 1f;
            if (ScaleToFace)
            {
                foreach (var v in face.GetVertices())
                    scale = Mathf.Max(scale, Vector3.Distance(v.Position, centroid));
            }

            Matrix4x4 mat;
            if (AlignToNormal)
            {
                var normal  = face.Normal;
                var edgeDir = face.Halfedge.Vector.sqrMagnitude > 0.0001f
                    ? face.Halfedge.Vector.normalized
                    : Vector3.forward;
                var rot = Quaternion.LookRotation(edgeDir, normal);
                mat = Matrix4x4.TRS(centroid, rot, Vector3.one * scale);
            }
            else
            {
                mat = Matrix4x4.TRS(centroid, Quaternion.identity, Vector3.one * scale);
            }

            finalPoly.Append(sourcePoly.Duplicate(mat));
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
        HostSettings.AttachAction(settingsChanged, generator);
        SourceSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        HostSettings.DetachAction(settingsChanged);
        SourceSettings.DetachAction(settingsChanged);
    }
}
