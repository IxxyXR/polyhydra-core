using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public static class PolylineSweep
{
    public static PolyMesh Build(
        List<Vector3> points, bool closed,
        PolylineSweepMode mode, int sides, float radius,
        Vector3 upHint, bool capEnds)
    {
        if (points == null || points.Count < 2) return new PolyMesh();
        int n = points.Count;
        var tangents = ComputeTangents(points, n, closed);
        var rights   = ComputeFrame(points, n, tangents, upHint);

        return mode == PolylineSweepMode.Tube
            ? BuildTube(points, n, tangents, rights, closed, sides, radius, capEnds)
            : BuildStrip(points, n, rights, closed, radius);
    }

    private static PolyMesh BuildTube(
        List<Vector3> points, int n, Vector3[] tangents, Vector3[] rights,
        bool closed, int sides, float radius, bool capEnds)
    {
        var verts = new List<Vector3>(n * sides);
        var faces = new List<IEnumerable<int>>();

        for (int i = 0; i < n; i++)
        {
            var up = Vector3.Cross(rights[i], tangents[i]).normalized;
            for (int j = 0; j < sides; j++)
            {
                float a = j * Mathf.PI * 2f / sides;
                verts.Add(points[i] + (rights[i] * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius);
            }
        }

        int segs = closed ? n : n - 1;
        for (int i = 0; i < segs; i++)
        {
            int ni = (i + 1) % n;
            for (int j = 0; j < sides; j++)
            {
                int j1 = (j + 1) % sides;
                faces.Add(new[] { i * sides + j, i * sides + j1, ni * sides + j1, ni * sides + j });
            }
        }

        if (!closed && capEnds)
        {
            var startCap = new int[sides];
            for (int j = 0; j < sides; j++) startCap[j] = sides - 1 - j;
            faces.Add(startCap);

            var endCap = new int[sides];
            int base_ = (n - 1) * sides;
            for (int j = 0; j < sides; j++) endCap[j] = base_ + j;
            faces.Add(endCap);
        }

        return new PolyMesh(verts, faces);
    }

    private static PolyMesh BuildStrip(
        List<Vector3> points, int n, Vector3[] rights, bool closed, float radius)
    {
        var verts = new List<Vector3>(n * 2);
        var faces = new List<IEnumerable<int>>();

        for (int i = 0; i < n; i++) verts.Add(points[i] - rights[i] * radius);
        for (int i = 0; i < n; i++) verts.Add(points[i] + rights[i] * radius);

        int segs = closed ? n : n - 1;
        for (int i = 0; i < segs; i++)
        {
            int ni = (i + 1) % n;
            faces.Add(new[] { i, ni, ni + n, i + n });
        }

        return new PolyMesh(verts, faces);
    }

    private static Vector3[] ComputeTangents(List<Vector3> pts, int n, bool closed)
    {
        var t = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            if (!closed && i == 0)
                t[i] = (pts[1] - pts[0]).normalized;
            else if (!closed && i == n - 1)
                t[i] = (pts[n - 1] - pts[n - 2]).normalized;
            else
                t[i] = (pts[(i + 1) % n] - pts[(i - 1 + n) % n]).normalized;
        }
        return t;
    }

    private static Vector3[] ComputeFrame(List<Vector3> pts, int n, Vector3[] tangents, Vector3 upHint)
    {
        var rights = new Vector3[n];
        var up = upHint.sqrMagnitude > 0.0001f ? upHint.normalized : Vector3.up;

        rights[0] = Vector3.Cross(tangents[0], up);
        if (rights[0].sqrMagnitude < 0.001f)
            rights[0] = Vector3.Cross(tangents[0], Vector3.forward);
        if (rights[0].sqrMagnitude < 0.001f)
            rights[0] = Vector3.Cross(tangents[0], Vector3.right);
        rights[0] = rights[0].normalized;

        for (int i = 1; i < n; i++)
            rights[i] = Quaternion.FromToRotation(tangents[i - 1], tangents[i]) * rights[i - 1];

        return rights;
    }
}
