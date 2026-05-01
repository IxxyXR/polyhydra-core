using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "InterpolatedShapeSettings", menuName = "PolyhydraMulti/InterpolatedShapeSettings", order = 1)]
public class InterpolatedShapeSettings : BaseSettings
{
    [Header("Interpolation Parameters")]
    public BaseSettings ShapeA;
    public BaseSettings ShapeB;
    [Min(2)] public int Count = 5;

    // Both shapes must have identical vertex counts and compatible topology.
    // Works reliably when ShapeA and ShapeB share the same base shape and operator list
    // but differ only in parameter values.

    public override PolyMesh BuildBaseShape()
    {
        var polyA = ShapeA.BuildBaseShape();
        polyA = ShapeA.ApplyModifiers(polyA);
        var polyB = ShapeB.BuildBaseShape();
        polyB = ShapeB.ApplyModifiers(polyB);

        var finalPoly = new PolyMesh();

        if (polyA.Vertices.Count != polyB.Vertices.Count)
        {
            Debug.LogWarning($"InterpolatedShape: vertex count mismatch ({polyA.Vertices.Count} vs {polyB.Vertices.Count}). Showing ShapeA only.");
            finalPoly.Append(polyA.Duplicate());
            finalPoly = ApplyModifiers(finalPoly);
            return finalPoly;
        }

        // Build face index lists from polyA's topology
        var vertexIndex = new Dictionary<Vertex, int>();
        for (int i = 0; i < polyA.Vertices.Count; i++)
            vertexIndex[polyA.Vertices[i]] = i;

        var faceIndices = polyA.Faces
            .Select(f => (IEnumerable<int>)f.GetVertices().Select(v => vertexIndex[v]).ToList())
            .ToList();

        var positionsA = polyA.Vertices.Select(v => v.Position).ToList();
        var positionsB = polyB.Vertices.Select(v => v.Position).ToList();

        for (int step = 0; step < Count; step++)
        {
            float t = Count > 1 ? step / (float)(Count - 1) : 0f;
            var lerpedVerts = positionsA.Select((p, i) => Vector3.Lerp(p, positionsB[i], t));
            var lerped = new PolyMesh(lerpedVerts, faceIndices);

            // Offset each copy along X so they don't overlap
            float spacing = polyA.GetBounds().size.x * 1.5f;
            lerped.Transform(Matrix4x4.Translate(Vector3.right * spacing * step));

            finalPoly.Append(lerped);
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
        ShapeA.AttachAction(settingsChanged, generator);
        ShapeB.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        ShapeA.DetachAction(settingsChanged);
        ShapeB.DetachAction(settingsChanged);
    }
}
