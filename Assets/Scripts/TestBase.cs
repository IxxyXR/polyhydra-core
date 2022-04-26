using Polyhydra.Core;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TestBase : MonoBehaviour
{
    [Header("Modifications")]
    public PolyMesh.Operation op1;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = 0;
    public float MergeThreshold;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = 0;
    public int CanonicalizeIterations = 0;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    
    protected PolyMesh poly;

    public void Build(ColorMethods colorMethod = ColorMethods.ByRole)
    {
        poly = poly.AppyOperation(op1, new OpParams(Op1Parameter1, Op1Parameter2));
        if (MergeThreshold > 0) poly.MergeCoplanarFaces(MergeThreshold);
        poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2));
        if (CanonicalizeIterations > 0) poly = poly.Canonicalize(CanonicalizeIterations, CanonicalizeIterations);
        if (FaceScale<1f) poly = poly.FaceScale(new OpParams(FaceScale));
        var mesh = poly.BuildUnityMesh(colorMethod: colorMethod);
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
