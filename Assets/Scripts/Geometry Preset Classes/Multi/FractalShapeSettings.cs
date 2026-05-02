using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalShapeSettings", menuName = "PolyhydraMulti/FractalShapeSettings", order = 1)]
public class FractalShapeSettings : BaseSettings
{
    [Header("Fractal Parameters")]
    public BaseSettings ShapeSettings;
    [Range(1, 5)] public int Depth = 2;
    public float InitialRotation = 0f;

    // At each level every face of the current mesh is replaced by a copy of the
    // original source shape, scaled and oriented to fit that face.
    // Face count grows as O(sourceFaces ^ Depth) — keep Depth small.

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = ShapeSettings.BuildBaseShape();
        sourcePoly = ShapeSettings.ApplyModifiers(sourcePoly);
        if (sourcePoly.Faces.Count == 0)
        {
            var empty = ApplyModifiers(new PolyMesh());
            return empty;
        }

        var sourceAnchorFace = sourcePoly.Faces[0];
        var sourceAnchorCentroid = sourceAnchorFace.Centroid;
        var sourceAnchorRotation = GetFaceRotation(sourceAnchorFace, flipNormal: true);
        var sourceToAnchorSpace = Matrix4x4.Rotate(Quaternion.Inverse(sourceAnchorRotation)) *
                                  Matrix4x4.Translate(-sourceAnchorCentroid);
        var sourceAnchorPolygon = GetFacePolygonInFrame(sourceAnchorFace, sourceToAnchorSpace);

        var result = sourcePoly.Duplicate();
        var frontier = sourcePoly.Duplicate();

        for (int d = 0; d < Depth - 1; d++)
        {
            var nextFrontier = new PolyMesh();
            foreach (var face in frontier.Faces)
            {
                nextFrontier.Append(sourcePoly.Duplicate(
                    FaceMatrix(face, sourceToAnchorSpace, sourceAnchorPolygon, InitialRotation)
                ));
            }
            result.Append(nextFrontier);
            frontier = nextFrontier;
        }

        result = ApplyModifiers(result);
        return result;
    }

    private static Matrix4x4 FaceMatrix(
        Face face,
        Matrix4x4 sourceToAnchorSpace,
        List<Vector2> sourceAnchorPolygon,
        float initialRotation
    )
    {
        var hostFaceRotation = GetFaceRotation(face);
        var hostFaceSpace = Matrix4x4.Rotate(Quaternion.Inverse(hostFaceRotation)) *
                            Matrix4x4.Translate(-face.Centroid);
        var hostFacePolygon = GetFacePolygonInFrame(face, hostFaceSpace);
        var rotatedSourcePolygon = RotatePolygon(sourceAnchorPolygon, initialRotation);
        var scale = GetFitScale(hostFacePolygon, rotatedSourcePolygon);

        return Matrix4x4.TRS(
            face.Centroid,
            hostFaceRotation * Quaternion.AngleAxis(initialRotation, Vector3.up),
            Vector3.one * scale
        ) * sourceToAnchorSpace;
    }

    private static List<Vector2> GetFacePolygonInFrame(Face face, Matrix4x4 frame)
    {
        var polygon = new List<Vector2>();
        foreach (var v in face.GetVertices())
        {
            var p = frame.MultiplyPoint3x4(v.Position);
            polygon.Add(new Vector2(p.x, p.z));
        }
        return polygon;
    }

    private static Quaternion GetFaceRotation(Face face, bool flipNormal = false)
    {
        var centroid = face.Centroid;
        var normal = flipNormal ? -face.Normal : face.Normal;
        var bestEdge = face.GetBestEdge() ?? face.Halfedge;
        var primary = (bestEdge.Midpoint - centroid).normalized;

        if (primary.sqrMagnitude < 0.0001f)
        {
            primary = bestEdge.Vector.sqrMagnitude > 0.0001f
                ? Vector3.ProjectOnPlane(bestEdge.Vector, normal).normalized
                : Vector3.forward;
        }

        if (primary.sqrMagnitude < 0.0001f)
        {
            primary = Vector3.Cross(normal, Vector3.right);
            if (primary.sqrMagnitude < 0.0001f)
            {
                primary = Vector3.Cross(normal, Vector3.forward);
            }
            primary.Normalize();
        }

        return Quaternion.LookRotation(primary, normal);
    }

    private static List<Vector2> RotatePolygon(List<Vector2> polygon, float degrees)
    {
        if (Mathf.Abs(degrees) < 0.0001f)
        {
            return polygon;
        }

        var radians = degrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        var rotated = new List<Vector2>(polygon.Count);
        foreach (var p in polygon)
        {
            rotated.Add(new Vector2(
                p.x * cos - p.y * sin,
                p.x * sin + p.y * cos
            ));
        }

        return rotated;
    }

    private static float GetFitScale(List<Vector2> targetPolygon, List<Vector2> sourcePolygon)
    {
        var scale = float.PositiveInfinity;
        foreach (var p in sourcePolygon)
        {
            var radius = p.magnitude;
            if (radius < 0.0001f)
            {
                continue;
            }

            var limit = GetRayPolygonIntersectionDistance(targetPolygon, p / radius);
            if (!float.IsFinite(limit))
            {
                return 1f;
            }

            scale = Mathf.Min(scale, limit / radius);
        }

        if (!float.IsFinite(scale))
        {
            return 1f;
        }

        return Mathf.Max(scale * 0.999f, 0f);
    }

    private static float GetRayPolygonIntersectionDistance(List<Vector2> polygon, Vector2 direction)
    {
        var best = float.PositiveInfinity;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            var edge = b - a;
            var denominator = Cross(direction, edge);
            if (Mathf.Abs(denominator) < 0.0001f)
            {
                continue;
            }

            var t = Cross(a, edge) / denominator;
            var u = Cross(a, direction) / denominator;
            if (t >= 0f && u >= 0f && u <= 1f)
            {
                best = Mathf.Min(best, t);
            }
        }

        return best;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
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
