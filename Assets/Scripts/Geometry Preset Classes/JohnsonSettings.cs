using System.Globalization;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "JohnsonSettings", menuName = "Polyhydra/JohnsonSettings", order = 1)]
public class JohnsonSettings : BaseSettings
{
    [Header("Johnson Parameters")]
    public string SolidName = "";
    [Range(1,92)] public int type = 1;

    public override PolyMesh BuildBaseShape()
    {
        TextInfo textInfo = new CultureInfo("en-GB",false).TextInfo;
        SolidName = textInfo.ToTitleCase(JohnsonSolids.Names.Keys.ToArray()[type - 1]);
        var poly = JohnsonSolids.Build(type);
        return poly;
    }
}