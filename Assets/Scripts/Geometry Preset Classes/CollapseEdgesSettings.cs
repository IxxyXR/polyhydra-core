using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CollapseEdgeParameters
{
    public bool enabled = true;
    [FormerlySerializedAs("Face1")] [Range(0,24)] public int Face1Sides = 3;
    public Roles Face1Role;
    [FormerlySerializedAs("Face2")] [Range(0,24)] public int Face2Sides = 4;
    public bool Either = false;
}

[CreateAssetMenu(fileName = "CollapseEdgesSettings", menuName = "Polyhydra/CollapseEdgesSettings", order = 1)]
public class CollapseEdgesSettings : GridSettings
{

    [Header("Collapse Edge Parameters")]
    public List<CollapseEdgeParameters> CollapseEdgeParameterList;

    public override PolyMesh ModifyPostOp(PolyMesh poly)
    {
        foreach (var item in CollapseEdgeParameterList)
        {
            if (!item.enabled) continue;
            var f = Filter.Role(item.Face1Role);
            poly = poly.CollapseEdges(item.Face1Sides, item.Face2Sides, item.Either, f);
            poly.DebugVerts = poly.Vertices.Select(v => v.Position).ToList();
        }
        return poly;
    }
}