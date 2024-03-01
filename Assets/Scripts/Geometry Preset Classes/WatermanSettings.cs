using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "WatermanSettings", menuName = "Polyhydra/WatermanSettings", order = 1)]
public class WatermanSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    public int Root = 2;
    [Range(0,6)] public int C = 0;
    public bool MergeFaces;

    public override PolyMesh BuildBaseShape()
    {
        var poly = WatermanPoly.Build(1f, Root, C, MergeFaces);
        return poly;
    }
}