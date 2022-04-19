using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RotationalSolidsTest : MonoBehaviour
{
    public RotationalSolids.RotationalPolyType type;
    [Range(3, 64)] public int Sides = 5;
    public bool SetHeight;
    public float Height = 1f;
    public float CapHeight = 1f;
    public PolyMesh.ConwayOperator op;
    public float OpParameter = .3f;
    public float FaceScale = .99f;
    public bool Canonicalize;
    
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
        poly = poly.ApplyConwayOp(op, new OpParams(OpParameter));
        if (Canonicalize) poly = poly.Canonicalize(0.001f, 0.001f);
        poly = poly.FaceScale(new OpParams(FaceScale));
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
