using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SplitEdgesTest : GridTest
{
    [Serializable] public class SplitEdgeSetting
    {
        [Range(0,24)] public int face = 0;
        [Range(1,24)] public int edge = 2;
    }

    public List<SplitEdgeSetting> splitEdgeSettings;

    protected override void ModifyPostOp()
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
    }
}