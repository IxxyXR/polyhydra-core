using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using Polyhydra.Wythoff;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RadialSolidsTest : TestBase
{
    [Header("Base Shape Parameters")]
    public RadialSolids.RadialPolyType type;
    [Range(3, 64)] public int Sides = 5;
    public bool SetHeight;
    public float Height = 1f;
    public float CapHeight = 1f;

    [ContextMenu("Go")]
    public override void Go()
    {
        if (SetHeight)
        {
            poly = RadialSolids.Build(type, Sides, Height, CapHeight);
        }
        else
        {
            poly = RadialSolids.Build(type, Sides);
        }
        Build();
    }
}
