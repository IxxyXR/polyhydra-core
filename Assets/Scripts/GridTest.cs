using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridTest : MonoBehaviour
{
    public GridEnums.GridTypes type;
    public GridEnums.GridShapes shape;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    public PolyMesh.Operation op1;
    public float Op1Parameter1 = .3f;
    public float Op1Parameter2 = 0;
    public float MergeThreshold;
    public PolyMesh.Operation op2;
    public float Op2Parameter1 = .3f;
    public float Op2Parameter2 = .3f;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    [ContextMenu("Go")]
    public void Go()
    {
        PolyMesh poly = Grids.Build(type, shape, X, Y);
        poly = poly.AppyOperation(op1, new OpParams(Op1Parameter1, Op1Parameter2));
        if (MergeThreshold > 0) poly.MergeCoplanarFaces(MergeThreshold);
        poly = poly.AppyOperation(op2, new OpParams(Op2Parameter1, Op2Parameter2));
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
