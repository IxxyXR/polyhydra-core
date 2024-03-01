using UnityEngine;
using UnityEngine.VFX;


public class PolyVFX : MonoBehaviour
{
    public VisualEffect Vfx;
    private Texture2D texture;

    public GameObject polyhydra;
    public bool ContinuousUpdate;
    public int RenderEvery = 2;
    public bool MatchScale;
    public float ScaleOffset;

    void Start()
    {
        UpdatePolyVFX();
    }

    void Update()
    {
        Vfx.SetVector3("Rotation Angle", transform.localEulerAngles);
        if (ContinuousUpdate && Time.frameCount % RenderEvery == 0)
        {
            UpdatePolyVFX();
        }
    }

    [ContextMenu("Update Conway VFX")]
    public void UpdatePolyVFX()
    {
        if (MatchScale)
        {
            Vfx.SetFloat("Scale", polyhydra.transform.localScale.x + ScaleOffset);
        }

        var edges = polyhydra.GetComponent<PolyhydraGenerator>().poly.Halfedges.GetUnique().ToArray();

        texture = new Texture2D(edges.Length, 2, TextureFormat.RGBAFloat, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        var pixelData = new Color[edges.Length * 2];

        int numEdges = edges.Length;

        for (var i = 0; i < numEdges; i++)
        {
            var start = edges[i].Vertex.Position;
            var edgeColor = i;
            pixelData[i] = new Color(start.x, start.y, start.z, edgeColor);
        }

        for (var i = 0; i < numEdges; i++)
        {
            Vector3 end;
            if (edges[i].Pair != null)
            {
                end = edges[i].Pair.Vertex.Position;
            }
            else
            {
                end = edges[i].Next.Vertex.Position;
            }

            var edgeColor = i; //CalcEdgeColor(edges[i]);
            pixelData[i + edges.Length] = new Color(end.x, end.y, end.z, edgeColor);
        }

        texture.SetPixels(pixelData);
        texture.Apply();
        Vfx.SetTexture("Positions", texture);
        Vfx.SetInt("Count", texture.width);
    }
}
