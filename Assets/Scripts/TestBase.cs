using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;
using UnityEditor;
using Random = System.Random;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TestBase : MonoBehaviour
{
    [Header("Modifications")]
    public PolyMesh.Operation op1;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = 0;
    public int Op1Iterations = 1;
    public bool Op1Parameter1Randomize = false;
    public bool Op1Parameter2Randomize = false;
    public FilterTypes Op1FilterType;
    public float Op1FilterParam;
    public bool Op1FilterFlip;
    public float MergeThreshold;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = 0;
    public int Op2Iterations = 1;
    public FilterTypes Op2FilterType;
    public float Op2FilterParam;
    public bool Op2FilterFlip;
    public float SpherizeAmount = 0;
    public bool FastConicalize = true;
    public int CanonicalizeIterations = 0;
    public int PlanarizeIterations = 0;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    [Range(-1f, 1f)] public float FaceExtrude = 0;
    [Range(-1f, 1f)] public float FaceInset = 0;

    public bool debugFaces;
    public bool debugEdges;
    public bool debugVerts;

    [NonSerialized] public PolyMesh poly;

    public void Build(ColorMethods colorMethod = ColorMethods.ByRole)
    {
        var _random = new Random();
        for (int i1 = 0; i1 < Op1Iterations; i1++)
        {
            var op1Filter = Filter.GetFilter(Op1FilterType, Op1FilterParam, Mathf.FloorToInt(Op1FilterParam), Op1FilterFlip);

            var op1Func1 = new OpFunc(_ => Mathf.Lerp(0, Op1Parameter1, (float)_random.NextDouble()));
            var op1Func2 = new OpFunc(_ => Mathf.Lerp(0, Op1Parameter2, (float)_random.NextDouble()));

            OpParams op1Params = (Op1Parameter1Randomize, Op1Parameter2Randomize) switch
            {
                (false, false) => new OpParams(
                    Op1Parameter1,
                    Op1Parameter2,
                    filter: op1Filter
                ),
                (true, false) => new OpParams(
                    op1Func1,
                    Op1Parameter2,
                    filter: op1Filter
                ),
                (false, true) => new OpParams(
                    Op1Parameter1,
                    op1Func2,
                    filter: op1Filter
                ),
                (true, true) => new OpParams(
                    op1Func1,
                    op1Func2,
                    filter: op1Filter
                ),
            };
            poly = poly.AppyOperation(op1, op1Params);
        }
        if (MergeThreshold > 0) poly.MergeCoplanarFaces(MergeThreshold);
        for (int i2 = 0; i2 < Op2Iterations; i2++)
        {
            var op2Filter = Filter.GetFilter(Op2FilterType, Op2FilterParam, Mathf.FloorToInt(Op2FilterParam), Op2FilterFlip);
            poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2, filter: op2Filter));
        }

        ModifyPostOp();

        if (SpherizeAmount > 0) poly.Spherize(new OpParams(SpherizeAmount));
        if (FastConicalize)
        {
            if (CanonicalizeIterations > 0)
            {
                poly = poly.Kanonicalize(CanonicalizeIterations);
            }
        }
        else
        {
            if (CanonicalizeIterations > 0 || PlanarizeIterations > 0)
            {
                poly = poly.Canonicalize(CanonicalizeIterations, PlanarizeIterations);
            }
        }

        if (FaceInset != 0) poly = poly.FaceInset(new OpParams(FaceInset));
        if (FaceExtrude != 0) poly = poly.Extrude(new OpParams(FaceExtrude));
        if (FaceScale < 1f) poly = poly.FaceScale(new OpParams(FaceScale));

        var meshData = poly.BuildMeshData(colorMethod: colorMethod);
        var mesh = poly.BuildUnityMesh(meshData);

        if (Application.isPlaying)
        {
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

    protected virtual void ModifyPostOp()
    {}

    public virtual void Go(){}

    private void OnValidate()
    {
        Go();
    }

    private void OnDrawGizmos()
    {
        if (poly==null) return;

        if (debugFaces)
        {
            for (var f = 0; f < poly.Faces.Count; f++)
            {
                var face = poly.Faces[f];
                var pos = transform.TransformPoint(face.Centroid);
                Handles.Label(pos + new Vector3(0, 0.03f, 0), $"F{f}");
                if (debugEdges)
                {
                    var list = face.GetHalfedges();
                    for (var e = 0; e < list.Count; e++)
                    {
                        var edge = list[e];
                        pos = transform.TransformPoint(edge.Midpoint);
                        Handles.Label(pos + new Vector3(0, 0.03f, 0), $"{e}");
                    }
                }
            }
        }

        if (debugVerts && poly.DebugVerts != null)
        {
            for (int i = 0; i < poly.DebugVerts.Count; i++)
            {
                Vector3 vert = poly.DebugVerts[i];
                Vector3 pos = transform.TransformPoint(vert);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, .025f);
                Handles.Label(pos + new Vector3(0, 0.1f, 0), i.ToString());
            }
        }
    }
}
