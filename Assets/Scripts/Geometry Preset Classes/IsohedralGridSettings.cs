using System.Collections.Generic;
using Polyhydra.Core;
using Polyhydra.Core.IsohedralGrids;
using UnityEngine;

[CreateAssetMenu(fileName = "IsohedralGridSettings", menuName = "Polyhydra/IsohedralGridSettings", order = 1)]
public class IsohedralGridSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    [Range(1,93)]
    public int TilingType = 1;
    public List<Vector2> NewVerts;
    [Range(0f, 1f)]
    public List<double> tilingParameters;
    public bool Weld = true;
    public Vector2 Size = Vector2.one * 4;

    private IsohedralPoly isohedralPoly;
    private int previousTilingType = -1;

    [Header("Info (Read Only)")]
    public string TilingName;
    public string Symmetry;
    public string Grid;

    public override PolyMesh BuildBaseShape()
    {
        if (TilingType!=previousTilingType || isohedralPoly==null)
        {
            isohedralPoly = new IsohedralPoly(TilingType);
            TilingName = isohedralPoly.TilingName;
            Symmetry = IsohedralTilingHelpers.tiling_types[isohedralPoly.TilingType].symmetry_group;
            Grid = IsohedralTilingHelpers.tiling_types[isohedralPoly.TilingType].grid;
            previousTilingType = TilingType;
            tilingParameters = isohedralPoly.GetDefaultTilingParameters();
        }
        var poly = isohedralPoly.MakePoly(tilingParameters, NewVerts, Size);
        if (Weld)
        {
            poly = poly.Weld(0.01f);
        }
        return poly;
    }
}