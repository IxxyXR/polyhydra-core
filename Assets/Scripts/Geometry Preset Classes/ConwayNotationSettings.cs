using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "ConwayNotationSettings", menuName = "Polyhydra/ConwayNotationSettings", order = 1)]
public class ConwayNotationSettings : BaseSettings
{
    [Header("Conway Notation Parameters")]
    public string input = "kC";

    public override PolyMesh BuildBaseShape()
    {
        var poly = PolyMesh.FromConwayString(input);
        var vef = poly.vef;
        Debug.Log($"{vef.f} faces {vef.e} edges {vef.v} vertices");
        return poly;
    }
}