using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WythoffTest : TestBase
{
    [Header("Base Shape Parameters")]
    public string name = "";
    [Range(6, 80)] public int type = 6;
    
    private PolyMesh poly;
    
    [ContextMenu("Go")]
    public override void Go()
    {
        var wythoff = new WythoffPoly(type);
        poly = wythoff.Build();
        name = Uniform.Uniforms[type].Name;
        Build();
    }
}
