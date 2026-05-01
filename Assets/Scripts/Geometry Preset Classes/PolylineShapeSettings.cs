using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

public enum PolylineSweepMode { Tube, Strip }

[CreateAssetMenu(fileName = "PolylineShapeSettings", menuName = "Polyhydra/PolylineShapeSettings", order = 1)]
public class PolylineShapeSettings : BaseSettings
{
    [Header("Polyline")]
    public List<Vector3> Points = new List<Vector3>
    {
        new Vector3(-1, 0, 0),
        new Vector3( 0, 1, 0),
        new Vector3( 1, 0, 0),
    };
    public bool Closed = false;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 6;
    [Min(0.01f)] public float Radius = 0.25f;
    public Vector3 UpHint = Vector3.up;

    [Header("Caps")]
    public bool CapEnds = true;

    public override PolyMesh BuildBaseShape() =>
        PolylineSweep.Build(Points, Closed, Mode, Sides, Radius, UpHint, CapEnds);
}
