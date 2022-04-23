using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WythoffTest : MonoBehaviour
{
    public string name = "";
    [Range(6, 80)] public int type = 6;
    public PolyMesh.Operation op;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = .3f;
    public bool Merge;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = .3f;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    public int Canonicalize = 10;
    
    private PolyMesh poly;
    
    [ContextMenu("Go")]
    public void Go()
    {
        var wythoff = new WythoffPoly(type);
        poly = wythoff.Build();
        name = Uniform.Uniforms[type].Name;
        poly = poly.AppyOperation(op, new OpParams(Op1Parameter1, Op1Parameter2));
        if (Merge) poly.MergeCoplanarFaces(0.01f);
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
