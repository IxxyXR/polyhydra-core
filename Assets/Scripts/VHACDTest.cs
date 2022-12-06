using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MeshProcess;
using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VHACDTest : MonoBehaviour
{
    [Header("Modifications")]
    public PolyMesh.Operation op1;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = 0;
    // public int Op1Iterations = 1;
    public FilterTypes Op1FilterType;
    public float Op1FilterParam;
    public bool Op1FilterFlip;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = 0;
    // public int Op2Iterations = 1;
    public FilterTypes Op2FilterType;
    public float Op2FilterParam;
    public bool Op2FilterFlip;
    // public int CanonicalizeIterations = 0;
    // public int PlanarizeIterations = 0;
    // [Range(.1f, 1f)] public float FaceScale = .99f;

    [NonSerialized] public PolyMesh poly;

    [Header("Base Shape Parameters")]
    public RadialSolids.RadialPolyType type;
    [Range(3, 64)] public int Sides = 5;
    public bool SetHeight;
    public float Height = 1f;
    public float CapHeight = 1f;

    public Material VhacdMaterial;

    [ContextMenu("Go")]
    public void Go()
    {

        if (SetHeight)
        {
            poly = RadialSolids.Build(type, Sides, Height, CapHeight);
        }
        else
        {
            poly = RadialSolids.Build(type, Sides);
        }
        Build();
    }

    private void OnValidate()
    {
        Go();
    }

    public void Build(ColorMethods colorMethod = ColorMethods.ByRole)
    {
        int Op1Iterations = 1;
        int Op2Iterations = 1;

        for (int i1 = 0; i1 < Op1Iterations; i1++)
        {
            var op1Filter = Filter.GetFilter(Op1FilterType, Op1FilterParam, Mathf.FloorToInt(Op1FilterParam), Op1FilterFlip);

            OpParams op1Params = new OpParams(
                    Op1Parameter1,
                    Op1Parameter2,
                    filter: op1Filter
            );
            poly = poly.AppyOperation(op1, op1Params);
        }
        for (int i2 = 0; i2 < Op2Iterations; i2++)
        {
            var op2Filter = Filter.GetFilter(Op2FilterType, Op2FilterParam, Mathf.FloorToInt(Op2FilterParam), Op2FilterFlip);
            poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2, filter: op2Filter));
        }
        // if (CanonicalizeIterations > 0 || PlanarizeIterations > 0) poly = poly.Canonicalize(CanonicalizeIterations, PlanarizeIterations);
        // if (FaceScale<1f) poly = poly.FaceScale(new OpParams(FaceScale));
        var meshData = poly.BuildMeshData(colorMethod: colorMethod, largeMeshFormat: false);
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

    [ContextMenu("VHACD")]
    public void DoVHACD()
    {
        Debug.Log($"Children before: {transform.childCount}");
        for (int i = transform.childCount; i > 0; --i)
            DestroyImmediate(transform.GetChild(0).gameObject);
        Debug.Log($"Children after: {transform.childCount}");
        // return;

        var vhacd = GetComponent<VHACD>();
        var initialMf = GetComponent<MeshFilter>();
        var convexMeshes = vhacd.GenerateConvexMeshes(initialMf.sharedMesh);
        foreach (var mesh in convexMeshes)
        {
            mesh.RecalculateNormals();
            var go = new GameObject();
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Instantiate(VhacdMaterial);
            mr.sharedMaterial.color = Random.ColorHSV();
            go.transform.parent = transform;
        }
    }

}
