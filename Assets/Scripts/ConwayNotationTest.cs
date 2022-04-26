using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConwayNotationTest : TestBase
{
    [Header("Base Shape Parameters")]
    public string input = "kC";
    public string results;
    
    [ContextMenu("Go")]
    public override void Go()
    {
        poly = PolyMesh.FromConwayString(input);
        var vef = poly.vef;
        results = $"{vef.f} faces {vef.e} edges {vef.v} vertices";
        Build();
    }
}
