using System.IO;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "OffReaderSettings", menuName = "Polyhydra/OffReaderSettings", order = 1)]
public class OffReaderSettings : BaseSettings
{
    [Header(".off Reader Parameters")]
    public string filename = "test.off";

    public override PolyMesh BuildBaseShape()
    {
        using (StreamReader reader = new StreamReader($"Assets/{filename}"))
        {
            var poly = new PolyMesh(reader);
            return poly;
        }
    }
}