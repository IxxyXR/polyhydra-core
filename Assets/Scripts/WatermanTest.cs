using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WatermanTest : MonoBehaviour
{
    public int root = 2;
    [Range(0,6)] public int c = 0;
    public PolyMesh.ConwayOperator op;
    public float OpParameter = .3f;
    public float FaceScale = .99f;
    public bool Canonicalize;
    public bool mergeFaces = false;
    
    [ContextMenu("Go")]
    public void Go()
    {
        PolyMesh poly = WatermanPoly.Build(1f, root, c, mergeFaces);
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
