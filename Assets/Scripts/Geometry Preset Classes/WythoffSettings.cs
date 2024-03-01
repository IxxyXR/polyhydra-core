using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;

[CreateAssetMenu(fileName = "WythoffSettings", menuName = "Polyhydra/WythoffSettings", order = 1)]
public class WythoffSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    public string PolyhedraName = "";
    [Range(6, 80)] public int type = 6;

    public override PolyMesh BuildBaseShape()
    {
        var wythoff = new WythoffPoly(type);
        var poly = wythoff.Build();
        PolyhedraName = Uniform.Uniforms[type].Name;
        return poly;
    }
}