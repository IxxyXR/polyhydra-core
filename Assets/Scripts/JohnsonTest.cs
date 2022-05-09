using System.Globalization;
using System.IO;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class JohnsonTest : TestBase
{
    [Header("Base Shape Parameters")]
    public string SolidName = "";
    [Range(1,92)] public int type = 1;
    
    [ContextMenu("Go")]
    public override void Go()
    {
        TextInfo textInfo = new CultureInfo("en-GB",false).TextInfo;
        SolidName = textInfo.ToTitleCase(JohnsonSolids.Names.Keys.ToArray()[type - 1]);
        poly = JohnsonSolids.Build(type);
        Build();
    }
}
