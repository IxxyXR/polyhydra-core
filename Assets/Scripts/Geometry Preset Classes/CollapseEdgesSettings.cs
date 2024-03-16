using System.Linq;
using Polyhydra.Core;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CollapseEdgesSettings", menuName = "Polyhydra/CollapseEdgesSettings", order = 1)]
public class CollapseEdgesSettings : GridSettings
{

    [Header("Collapse Edge Parameters")]
    [Range(0,24)] public int Face1 = 3;
    [Range(0,24)] public int Face2 = 4;
    public bool Either = false;

    public override PolyMesh ModifyPostOp(PolyMesh poly)
    {
        poly = poly.CollapseEdges(Face1, Face2, Either);
        poly.DebugVerts = poly.Vertices.Select(v => v.Position).ToList();
        return poly;
    }
}