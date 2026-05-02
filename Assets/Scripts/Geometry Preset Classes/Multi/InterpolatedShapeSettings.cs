using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public enum InterpolationMode
{
    VertexTopology,
    DirectionalHull
}

public enum VertexMatchMode
{
    ByIndex,
    NearestPosition,
    ByDirection
}

public enum InterpolationTopologySource
{
    ShapeA,
    ShapeB
}

public enum InterpolationOutputMode
{
    Sequence,
    SingleBlend
}

[CreateAssetMenu(fileName = "InterpolatedShapeSettings", menuName = "PolyhydraMulti/InterpolatedShapeSettings", order = 1)]
public class InterpolatedShapeSettings : BaseSettings
{
    [Header("Shape Inputs")]
    public BaseSettings ShapeA;
    public BaseSettings ShapeB;

    [Header("Shared Output")]
    [Tooltip("Choose whether to output a sequence of blends or a single blended mesh.")]
    public InterpolationOutputMode OutputMode = InterpolationOutputMode.Sequence;
    [Tooltip("Number of shapes in the interpolation sequence.")]
    [Min(2)] public int Count = 5;
    [Tooltip("Blend amount used when OutputMode is SingleBlend.")]
    [Range(0f, 1f)] public float Blend = 0.5f;
    [Tooltip("Horizontal spacing multiplier for Sequence output.")]
    public float Spacing = 1.5f;

    [Header("Shared Preprocessing")]
    [Tooltip("Recenters both input shapes before interpolation.")]
    public bool AutoAlign = true;
    [Tooltip("Normalizes both input shapes to a comparable overall radius before interpolation.")]
    public bool NormalizeScale = true;

    [Header("Interpolation Mode")]
    [Tooltip("VertexTopology preserves one mesh topology. DirectionalHull blends sampled support points and rebuilds a hull.")]
    public InterpolationMode Mode = InterpolationMode.VertexTopology;

    [Header("VertexTopology Only")]
    [Tooltip("Which input mesh topology to preserve in VertexTopology mode.")]
    public InterpolationTopologySource TopologySource = InterpolationTopologySource.ShapeA;
    [Tooltip("How vertices are matched between the two inputs in VertexTopology mode.")]
    public VertexMatchMode MatchMode = VertexMatchMode.NearestPosition;

    [Header("DirectionalHull Only")]
    [Tooltip("Number of support directions used to sample each shape in DirectionalHull mode.")]
    [Min(8)] public int SampleCount = 64;

    // VertexTopology keeps one mesh topology and morphs matched vertices.
    // DirectionalHull ignores source topology and blends support samples,
    // then rebuilds each intermediate as a convex hull.

    public override PolyMesh BuildBaseShape()
    {
        var polyA = ShapeA.BuildBaseShape();
        polyA = ShapeA.ApplyModifiers(polyA);
        var polyB = ShapeB.BuildBaseShape();
        polyB = ShapeB.ApplyModifiers(polyB);

        var preparedA = PrepareForInterpolation(polyA);
        var preparedB = PrepareForInterpolation(polyB);

        var finalPoly = new PolyMesh();
        float spacing = GetSpacing(preparedA, preparedB);
        int outputCount = OutputMode == InterpolationOutputMode.SingleBlend ? 1 : Count;

        bool useDirectionalHull = Mode == InterpolationMode.DirectionalHull;
        if (!useDirectionalHull && preparedA.Vertices.Count != preparedB.Vertices.Count)
        {
            Debug.LogWarning(
                $"InterpolatedShape: vertex count mismatch ({preparedA.Vertices.Count} vs {preparedB.Vertices.Count}). " +
                "Falling back to DirectionalHull mode."
            );
            useDirectionalHull = true;
        }

        if (useDirectionalHull)
        {
            var supportDirections = GenerateFibonacciDirections(Mathf.Max(8, SampleCount));
            var supportA = GetSupportPoints(preparedA, supportDirections);
            var supportB = GetSupportPoints(preparedB, supportDirections);

            for (int step = 0; step < outputCount; step++)
            {
                float t = GetInterpolationAmount(step, outputCount);
                var blendedVerts = supportA.Select((p, i) => Vector3.Lerp(p, supportB[i], t)).ToList();
                var lerped = new PolyMesh(blendedVerts).ConvexHull();
                if (OutputMode == InterpolationOutputMode.Sequence)
                {
                    lerped.Transform(Matrix4x4.Translate(Vector3.right * spacing * step));
                }
                finalPoly.Append(lerped);
            }
        }
        else
        {
            var basePoly = TopologySource == InterpolationTopologySource.ShapeA ? preparedA : preparedB;
            var targetPoly = TopologySource == InterpolationTopologySource.ShapeA ? preparedB : preparedA;
            var positionsBase = basePoly.Vertices.Select(v => v.Position).ToList();
            var positionsTarget = GetMatchedTargetPositions(basePoly, targetPoly, MatchMode);
            var faceIndices = BuildFaceIndices(basePoly);

            for (int step = 0; step < outputCount; step++)
            {
                float t = GetInterpolationAmount(step, outputCount);
                var lerpedVerts = positionsBase.Select((p, i) => Vector3.Lerp(p, positionsTarget[i], t));
                var lerped = new PolyMesh(lerpedVerts, faceIndices);
                if (OutputMode == InterpolationOutputMode.Sequence)
                {
                    lerped.Transform(Matrix4x4.Translate(Vector3.right * spacing * step));
                }
                finalPoly.Append(lerped);
            }
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

    private PolyMesh PrepareForInterpolation(PolyMesh poly)
    {
        var prepared = poly.Duplicate();
        if (AutoAlign)
        {
            prepared.Recenter();
        }

        if (NormalizeScale)
        {
            float radius = GetMaxRadius(prepared);
            if (radius > 0.0001f)
            {
                prepared.Transform(Matrix4x4.Scale(Vector3.one / radius));
            }
        }

        return prepared;
    }

    private float GetSpacing(PolyMesh polyA, PolyMesh polyB)
    {
        float sizeA = GetMaxDimension(polyA);
        float sizeB = GetMaxDimension(polyB);
        return Mathf.Max(sizeA, sizeB) * Spacing;
    }

    private float GetInterpolationAmount(int step, int outputCount)
    {
        if (OutputMode == InterpolationOutputMode.SingleBlend)
        {
            return Blend;
        }

        return outputCount > 1 ? step / (float)(outputCount - 1) : 0f;
    }

    private static float GetMaxDimension(PolyMesh poly)
    {
        var size = poly.GetBounds().size;
        return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
    }

    private static float GetMaxRadius(PolyMesh poly)
    {
        float radius = 0f;
        foreach (var vertex in poly.Vertices)
        {
            radius = Mathf.Max(radius, vertex.Position.magnitude);
        }
        return radius;
    }

    private static List<IEnumerable<int>> BuildFaceIndices(PolyMesh poly)
    {
        var vertexIndex = new Dictionary<Vertex, int>();
        for (int i = 0; i < poly.Vertices.Count; i++)
        {
            vertexIndex[poly.Vertices[i]] = i;
        }

        return poly.Faces
            .Select(f => (IEnumerable<int>)f.GetVertices().Select(v => vertexIndex[v]).ToList())
            .ToList();
    }

    private static List<Vector3> GetMatchedTargetPositions(
        PolyMesh sourcePoly,
        PolyMesh targetPoly,
        VertexMatchMode matchMode
    )
    {
        if (matchMode == VertexMatchMode.ByIndex)
        {
            return targetPoly.Vertices.Select(v => v.Position).ToList();
        }

        var sourcePositions = sourcePoly.Vertices.Select(v => v.Position).ToList();
        var targetPositions = targetPoly.Vertices.Select(v => v.Position).ToList();
        var matched = new Vector3[sourcePositions.Count];
        var remaining = Enumerable.Range(0, targetPositions.Count).ToHashSet();
        var sourceOrder = Enumerable.Range(0, sourcePositions.Count)
            .OrderByDescending(i => sourcePositions[i].sqrMagnitude)
            .ToList();

        foreach (int sourceIndex in sourceOrder)
        {
            int bestTargetIndex = -1;
            float bestScore = matchMode == VertexMatchMode.NearestPosition
                ? float.PositiveInfinity
                : float.NegativeInfinity;

            foreach (int targetIndex in remaining)
            {
                float score = matchMode == VertexMatchMode.NearestPosition
                    ? (sourcePositions[sourceIndex] - targetPositions[targetIndex]).sqrMagnitude
                    : Vector3.Dot(
                        SafeNormalize(sourcePositions[sourceIndex]),
                        SafeNormalize(targetPositions[targetIndex])
                    );

                bool isBetter = matchMode == VertexMatchMode.NearestPosition
                    ? score < bestScore
                    : score > bestScore;

                if (!isBetter) continue;
                bestScore = score;
                bestTargetIndex = targetIndex;
            }

            if (bestTargetIndex < 0)
            {
                bestTargetIndex = remaining.First();
            }

            matched[sourceIndex] = targetPositions[bestTargetIndex];
            remaining.Remove(bestTargetIndex);
        }

        return matched.ToList();
    }

    private static Vector3 SafeNormalize(Vector3 value)
    {
        return value.sqrMagnitude > 0.0001f ? value.normalized : Vector3.up;
    }

    private static List<Vector3> GenerateFibonacciDirections(int count)
    {
        var directions = new List<Vector3>(count);
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float y = Mathf.Lerp(1f, -1f, t);
            float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = goldenAngle * i;
            directions.Add(new Vector3(
                Mathf.Cos(theta) * radius,
                y,
                Mathf.Sin(theta) * radius
            ));
        }

        return directions;
    }

    private static List<Vector3> GetSupportPoints(PolyMesh poly, List<Vector3> directions)
    {
        var positions = poly.Vertices.Select(v => v.Position).ToList();
        var supportPoints = new List<Vector3>(directions.Count);
        foreach (var direction in directions)
        {
            float bestDot = float.NegativeInfinity;
            Vector3 bestPoint = Vector3.zero;
            for (int i = 0; i < positions.Count; i++)
            {
                float dot = Vector3.Dot(positions[i], direction);
                if (dot <= bestDot) continue;
                bestDot = dot;
                bestPoint = positions[i];
            }
            supportPoints.Add(bestPoint);
        }

        return supportPoints;
    }
}
