using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public enum ScatterPlacement
{
    Vertices,
    EdgeMidpoints,
    FaceCentroids
}

[CreateAssetMenu(fileName = "ScatterShapeSettings", menuName = "PolyhydraMulti/ScatterShapeSettings", order = 1)]
public class ScatterShapeSettings : BaseSettings
{
    [Header("Scatter Parameters")]
    public BaseSettings DestinationSettings;
    public BaseSettings SourceSettings;
    public ScatterPlacement Placement = ScatterPlacement.FaceCentroids;
    public bool IncludeHost = false;
    public bool AlignToNormal = false;
    public PolyTransform GuestTransform;

    public override PolyMesh BuildBaseShape()
    {
        var hostPoly = DestinationSettings.BuildBaseShape();
        hostPoly = DestinationSettings.ApplyModifiers(hostPoly);

        var guestPoly = SourceSettings.BuildBaseShape();
        guestPoly = SourceSettings.ApplyModifiers(guestPoly);

        var finalPoly = IncludeHost ? hostPoly.Duplicate() : new PolyMesh();

        switch (Placement)
        {
            case ScatterPlacement.Vertices:
                for (int i = 0; i < hostPoly.Vertices.Count; i++)
                {
                    var vert = hostPoly.Vertices[i];
                    Quaternion placementRot;
                    if (AlignToNormal)
                    {
                        var normal = vert.Normal;
                        var neighbours = vert.Neighbours;
                        var toNeighbor = neighbours.Count > 0
                            ? Vector3.ProjectOnPlane(neighbours[0].Position - vert.Position, normal)
                            : Vector3.zero;
                        var tangent = toNeighbor.sqrMagnitude > 0.0001f ? toNeighbor.normalized : Vector3.right;
                        placementRot = Quaternion.LookRotation(tangent, normal);
                    }
                    else
                    {
                        placementRot = Quaternion.identity;
                    }
                    finalPoly.Append(guestPoly.Duplicate(Matrix4x4.TRS(vert.Position, placementRot, Vector3.one) * GuestTransform.Matrix));
                }
                break;

            case ScatterPlacement.EdgeMidpoints:
                var visitedEdges = new HashSet<(Guid, Guid)>();
                foreach (var halfedge in hostPoly.Halfedges)
                {
                    var key = halfedge.PairedName;
                    if (key == null || !visitedEdges.Add(key.Value)) continue;
                    Quaternion placementRot;
                    if (AlignToNormal)
                    {
                        var edgeDir = halfedge.Vector.normalized;
                        var n1 = halfedge.Face?.Normal ?? Vector3.zero;
                        var n2 = halfedge.Pair?.Face?.Normal ?? Vector3.zero;
                        var faceNormal = (n1 + n2).normalized;
                        if (faceNormal.sqrMagnitude < 0.0001f) faceNormal = Vector3.up;
                        placementRot = Quaternion.LookRotation(edgeDir, faceNormal);
                    }
                    else
                    {
                        placementRot = Quaternion.identity;
                    }
                    finalPoly.Append(guestPoly.Duplicate(Matrix4x4.TRS(halfedge.Midpoint, placementRot, Vector3.one) * GuestTransform.Matrix));
                }
                break;

            case ScatterPlacement.FaceCentroids:
                foreach (var face in hostPoly.Faces)
                {
                    Quaternion placementRot;
                    if (AlignToNormal)
                    {
                        var normal = face.Normal;
                        var edgeDir = face.Halfedge.Vector.normalized;
                        placementRot = Quaternion.LookRotation(edgeDir, normal);
                    }
                    else
                    {
                        placementRot = Quaternion.identity;
                    }
                    finalPoly.Append(guestPoly.Duplicate(Matrix4x4.TRS(face.Centroid, placementRot, Vector3.one) * GuestTransform.Matrix));
                }
                break;
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
        DestinationSettings.AttachAction(settingsChanged, generator);
        SourceSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        DestinationSettings.DetachAction(settingsChanged);
        SourceSettings.DetachAction(settingsChanged);
    }
}
