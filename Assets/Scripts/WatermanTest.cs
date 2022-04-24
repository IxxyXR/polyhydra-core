using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WatermanTest : TestBase
{
    [Header("Base Shape Parameters")]
    public int Root = 2;
    [Range(0,6)] public int C = 0;

    public bool MergeFaces;

    [ContextMenu("Go")]
    public override void Go()
    {
        poly = WatermanPoly.Build(1f, Root, C, MergeFaces);
        Build();
    }

}
