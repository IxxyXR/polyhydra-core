using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public enum ProceduralCurveType
{
    TorusKnot,    // trefoil = P:2 Q:3
    Helix,
    Lissajous,
    Epicycloid,
}

[CreateAssetMenu(fileName = "ProceduralPolylineShapeSettings", menuName = "Polyhydra/ProceduralPolylineShapeSettings", order = 1)]
public class ProceduralPolylineShapeSettings : BaseSettings
{
    [Header("Curve")]
    public ProceduralCurveType CurveType = ProceduralCurveType.TorusKnot;
    [Min(8)] public int Resolution = 120;

    [Header("Torus Knot  (Trefoil = P:2 Q:3)")]
    public int P = 2;
    public int Q = 3;
    public float MajorRadius = 2f;
    public float MinorRadius = 0.75f;

    [Header("Helix")]
    public float HelixRadius = 1f;
    [Min(0.001f)] public float HelixPitch = 0.3f;
    [Min(1)] public int Turns = 4;

    [Header("Lissajous")]
    public float FreqX = 3f;
    public float FreqY = 2f;
    public float FreqZ = 1f;
    public float PhaseX = 0f;
    public float PhaseY = 0f;
    public float PhaseZ = 0f;
    public float AmpX = 1f;
    public float AmpY = 1f;
    public float AmpZ = 1f;

    [Header("Epicycloid")]
    public float OuterRadius = 3f;
    public float InnerRadius = 1f;
    public float TraceDistance = 1f;
    [Min(1)] public int EpiLoops = 1;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 6;
    [Min(0.01f)] public float Radius = 0.15f;
    public Vector3 UpHint = Vector3.up;
    public bool CapEnds = false;

    public override PolyMesh BuildBaseShape()
    {
        bool closed;
        var points = GeneratePoints(out closed);
        return PolylineSweep.Build(points, closed, Mode, Sides, Radius, UpHint, CapEnds);
    }

    private List<Vector3> GeneratePoints(out bool closed)
    {
        switch (CurveType)
        {
            case ProceduralCurveType.TorusKnot:  return TorusKnot(out closed);
            case ProceduralCurveType.Helix:       return Helix(out closed);
            case ProceduralCurveType.Lissajous:   return Lissajous(out closed);
            case ProceduralCurveType.Epicycloid:  return Epicycloid(out closed);
            default: closed = false; return new List<Vector3>();
        }
    }

    // -------------------------------------------------------------------------
    // Torus Knot
    // Winds P times around the torus' axis and Q times through the hole.
    // Naturally closed; the curve period is 2π.
    // -------------------------------------------------------------------------
    private List<Vector3> TorusKnot(out bool closed)
    {
        closed = true;
        var pts = new List<Vector3>(Resolution);
        for (int i = 0; i < Resolution; i++)
        {
            float t = i * Mathf.PI * 2f / Resolution;
            float r = MajorRadius + MinorRadius * Mathf.Cos(Q * t);
            pts.Add(new Vector3(
                r * Mathf.Cos(P * t),
                MinorRadius * Mathf.Sin(Q * t),
                r * Mathf.Sin(P * t)
            ));
        }
        return pts;
    }

    // -------------------------------------------------------------------------
    // Helix — open curve, rises along Y
    // -------------------------------------------------------------------------
    private List<Vector3> Helix(out bool closed)
    {
        closed = false;
        var pts = new List<Vector3>(Resolution);
        float totalAngle = Turns * Mathf.PI * 2f;
        for (int i = 0; i < Resolution; i++)
        {
            float t = i * totalAngle / (Resolution - 1);
            pts.Add(new Vector3(
                HelixRadius * Mathf.Cos(t),
                HelixPitch * t,
                HelixRadius * Mathf.Sin(t)
            ));
        }
        return pts;
    }

    // -------------------------------------------------------------------------
    // 3D Lissajous — closed when frequencies are rational multiples
    // -------------------------------------------------------------------------
    private List<Vector3> Lissajous(out bool closed)
    {
        closed = true;
        var pts = new List<Vector3>(Resolution);
        for (int i = 0; i < Resolution; i++)
        {
            float t = i * Mathf.PI * 2f / Resolution;
            pts.Add(new Vector3(
                AmpX * Mathf.Sin(FreqX * t + PhaseX),
                AmpY * Mathf.Sin(FreqY * t + PhaseY),
                AmpZ * Mathf.Sin(FreqZ * t + PhaseZ)
            ));
        }
        return pts;
    }

    // -------------------------------------------------------------------------
    // Epicycloid (Spirograph) — lies in XZ plane, closed after EpiLoops turns
    // -------------------------------------------------------------------------
    private List<Vector3> Epicycloid(out bool closed)
    {
        closed = true;
        var pts = new List<Vector3>(Resolution);
        float totalAngle = EpiLoops * Mathf.PI * 2f;
        float ratio = (OuterRadius + InnerRadius) / Mathf.Max(InnerRadius, 0.0001f);
        for (int i = 0; i < Resolution; i++)
        {
            float t = i * totalAngle / Resolution;
            pts.Add(new Vector3(
                (OuterRadius + InnerRadius) * Mathf.Cos(t) - TraceDistance * Mathf.Cos(ratio * t),
                0f,
                (OuterRadius + InnerRadius) * Mathf.Sin(t) - TraceDistance * Mathf.Sin(ratio * t)
            ));
        }
        return pts;
    }
}
