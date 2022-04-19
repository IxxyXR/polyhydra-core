using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class JohnsonTest : MonoBehaviour
{
    public string name = "";
    [Range(1,92)] public int type = 1;
    public PolyMesh.ConwayOperator op;
    public float OpParameter = .3f;
    public float FaceScale = .99f;
    public bool Canonicalize;
    [ContextMenu("Go")]
    public void Go()
    {
        TextInfo textInfo = new CultureInfo("en-GB",false).TextInfo;
        name = textInfo.ToTitleCase(JohnsonSolids.Names.Keys.ToArray()[type - 1]);
        var poly = JohnsonSolids.Build(type);
        poly = poly.ApplyConwayOp(op, new OpParams(OpParameter));
        if (Canonicalize) poly = poly.Canonicalize(0.001f, 0.001f);
        poly = poly.FaceScale(new OpParams(FaceScale));
        var mesh = poly.BuildUnityMesh(colorMethod: PolyMesh.ColorMethods.BySides);
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
    }

    private void OnValidate()
    {
        Go();
    }
}
