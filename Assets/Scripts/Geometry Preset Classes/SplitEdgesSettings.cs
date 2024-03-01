using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "SplitEdgesSettings", menuName = "Polyhydra/SplitEdgesSettings", order = 1)]
public class SplitEdgesSettings : GridSettings
{
    [Serializable] public class SplitEdgeSetting
    {
        [Range(0,24)] public int face = 0;
        [Range(1,24)] public int edge = 2;
    }

    [Header("Base Shape Parameters")]
    public List<SplitEdgeSetting> splitEdgeSettings;

    public override PolyMesh ModifyPostOp(PolyMesh poly)
    {
        // int faceCount = poly.Faces.Count;
        // var edges = splitEdgeSettings.Select(x =>
        // {
        //     var face = poly.Faces[x.face % faceCount];
        //     return face.GetHalfedges()[x.edge % face.Sides];
        // });
        // edges = poly.Halfedges.Where(e => e.Pair == null);
        poly.SplitEdge(poly.Halfedges.First());
        poly.DebugVerts = poly.Vertices.Select(v => v.Position).ToList();
        return poly;
    }
}