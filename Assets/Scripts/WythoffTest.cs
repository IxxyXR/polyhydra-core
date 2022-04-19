using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WythoffTest : MonoBehaviour
{
    public string name = "";
    [Range(6, 92)] public int type = 6;
    public int P = 5;
    public int Q = 2;
    public PolyMesh.ConwayOperator op;
    public float OpParameter = .3f;
    public float FaceScale = .99f;
    public bool Canonicalize;
    
    [ContextMenu("Go")]
    public void Go()
    {
        var wythoff = new WythoffPoly(type, 5, 2);
        var poly = wythoff.Build();
        name = Uniform.Uniforms[type].Name;
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
