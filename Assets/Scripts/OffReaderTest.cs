using System.IO;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OffReaderTest : MonoBehaviour
{
    public string filename = "test.off";
    public PolyMesh.Operation op;
    public float OpParameter = .3f;
    [Range(.1f, 1f)] public float FaceScale = .99f;
    public bool Canonicalize;
    
    [ContextMenu("Go")]
    public void Go()
    {
        using (StreamReader reader = new StreamReader($"Assets/{filename}"))
        {
            var poly = new PolyMesh(reader);
            poly = poly.AppyOperation(op, new OpParams(OpParameter));
            if (Canonicalize) poly = poly.Canonicalize(0.001f, 0.001f);
            if (FaceScale<1f) poly = poly.FaceScale(new OpParams(FaceScale));
            var mesh = poly.BuildUnityMesh(colorMethod: PolyMesh.ColorMethods.ByTags);
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void OnValidate()
    {
        Go();
    }
}
