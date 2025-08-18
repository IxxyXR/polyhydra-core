using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;

[CreateAssetMenu(fileName = "WythoffSettings", menuName = "Polyhydra/WythoffSettings", order = 1)]
public class WythoffSettings : BaseSettings
{
    [Header("Wythoff Polyhedron Parameters")]
    public Uniform.WythoffNames WythoffType;

    public int Sides = 5;

    public override PolyMesh BuildBaseShape()
    {
        var wythoff = new WythoffPoly((int)WythoffType, Sides);
        var poly = wythoff.Build();
        return poly;
    }
}