using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "RadialSolidsSettings", menuName = "Polyhydra/RadialSolidsSettings", order = 1)]
public class RadialSolidsSettings : BaseSettings
{
    [Header("Radial Solid Parameters")]
    public RadialSolids.RadialPolyType type;
    [Range(3, 64)] public int Sides = 5;
    public bool SetHeight;
    public float Height = 1f;
    public float CapHeight = 1f;

    public override PolyMesh BuildBaseShape()
    {
        PolyMesh poly;
        if (SetHeight)
        {
            poly = RadialSolids.Build(type, Sides, Height, CapHeight);
        }
        else
        {
            poly = RadialSolids.Build(type, Sides);
        }
        return poly;
    }
}