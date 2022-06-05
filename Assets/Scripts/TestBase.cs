using System;
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
    public int CanonicalizeIterations = 0;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    
    protected PolyMesh poly;

    public void Build(ColorMethods colorMethod = ColorMethods.ByRole)
    {
        var _random = new Random();
        for (int i1 = 0; i1 < Op1Iterations; i1++)
        {
            var op1Filter = Filter.GetFilter(Op1FilterType, Op1FilterParam, Mathf.FloorToInt(Op1FilterParam), Op1FilterFlip);

            var op1Func1 = Op1Parameter1Randomize ? 
                new OpFunc(_ => Mathf.Lerp(0, Op1Parameter1, (float)_random.NextDouble())) : 
                new OpFunc(Op1Parameter1);
            
            var op1Func2 = Op1Parameter2Randomize ? 
                new OpFunc(_ => Mathf.Lerp(0, Op1Parameter1, (float)_random.NextDouble())) : 
                new OpFunc(Op1Parameter2);
            
            poly = poly.AppyOperation(op1, new OpParams(op1Func1, op1Func2, filter: op1Filter));
        }
        if (MergeThreshold > 0) poly.MergeCoplanarFaces(MergeThreshold);
        for (int i2 = 0; i2 < Op2Iterations; i2++)
        {   
            var op2Filter = Filter.GetFilter(Op2FilterType, Op2FilterParam, Mathf.FloorToInt(Op2FilterParam), Op2FilterFlip);
            poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2, filter: op2Filter));
        }
        if (CanonicalizeIterations > 0) poly = poly.Canonicalize(CanonicalizeIterations, CanonicalizeIterations);
        if (FaceScale<1f) poly = poly.FaceScale(new OpParams(FaceScale));
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

    public virtual void Go(){}
    
    private void OnValidate()
    {
        Go();
    }
    
    private void OnDrawGizmos()
    {
        if (poly==null || poly.DebugVerts == null) return;
        for (int i = 0; i < poly.DebugVerts.Count; i++)
        {
            Vector3 vert = poly.DebugVerts[i];
            Vector3 pos = transform.TransformPoint(vert);
            Gizmos.DrawWireSphere(pos, .1f);
            Handles.Label(pos + new Vector3(0, 0, 0), i.ToString());
        }
    }
}
