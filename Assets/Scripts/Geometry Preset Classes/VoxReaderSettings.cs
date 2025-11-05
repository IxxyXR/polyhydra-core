using System.IO;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "VoxReaderSettings", menuName = "Polyhydra/VoxReaderSettings", order = 1)]
public class VoxReaderSettings : BaseSettings
{
    [Header(".vox Reader Parameters")]
    public string filename = "test.vox";

    [Tooltip("When enabled, removes faces between adjacent voxels for optimized mesh. When disabled, generates separate cube geometry for each voxel.")]
    public bool cullInternalFaces = true;

    public override PolyMesh BuildBaseShape()
    {
        byte[] fileData = File.ReadAllBytes($"Assets/{filename}");
        var poly = new PolyMesh(fileData, Path.GetExtension(filename), cullInternalFaces);
        return poly;
    }
}
