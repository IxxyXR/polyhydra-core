using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RotationalSolidsTest : MonoBehaviour
{
    public RotationalSolids.RotationalPolyType type;
    [Range(3, 64)] public int Sides = 5;
    public bool SetHeight;
    public float Height = 1f;
    public float CapHeight = 1f;
    public PolyMesh.Operation op1;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = .3f;
    public float MergeThreshold;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = .3f;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    public int Canonicalize;
    
    [ContextMenu("Go")]
    public void Go()
    {
        PolyMesh poly;
        if (SetHeight)
        {
            poly = RotationalSolids.Build(type, Sides, Height, CapHeight);
        }
        else
        {
            poly = RotationalSolids.Build(type, Sides);
        }
        poly = poly.AppyOperation(op1, new OpParams(Op1Parameter1, Op1Parameter2));
        if (MergeThreshold > 0) poly.MergeCoplanarFaces(MergeThreshold);
        poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2));
        if (Canonicalize > 0) poly = poly.Canonicalize(Canonicalize, Canonicalize);
        if (FaceScale<1f) poly = poly.FaceScale(new OpParams(FaceScale));
        var mesh = poly.BuildUnityMesh(colorMethod: PolyMesh.ColorMethods.ByRole);
        if (Application.isPlaying)
        {
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

    private void OnValidate()
    {
        Go();
    }
}
