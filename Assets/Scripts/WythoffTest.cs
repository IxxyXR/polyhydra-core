using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WythoffTest : TestBase
{
    [Header("Base Shape Parameters")]
    public string PolyhedraName = "";
    [Range(6, 80)] public int type = 6;
    
    [ContextMenu("Go")]
    public override void Go()
    {
        var wythoff = new WythoffPoly(type);
        poly = wythoff.Build();
        PolyhedraName = Uniform.Uniforms[type].Name;
        Build();
    }
}
