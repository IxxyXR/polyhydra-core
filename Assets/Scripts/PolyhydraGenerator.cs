using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolyhydraGenerator : MonoBehaviour
{
    public BaseSettings settings;
    public AppearanceSettings appearanceSettings;
    private BaseSettings previousSettings;
    private AppearanceSettings previousAppearanceSettings;

    [Header("Debug Visualizations")]
    public bool debugFaces;
    public bool debugEdges;
    public bool debugVerts;

    [NonSerialized] public PolyMesh poly;
    [NonSerialized] public bool NeedsRebuild;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        AttachActions();
    }

    private void Start()
    {
        Camera.main.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        AttachActions();
    }

    private void OnDisable()
    {
        if (settings != null)
        {
            settings.OnSettingsChanged -= SettingsChanged;
        }
        if (appearanceSettings != null)
        {
            appearanceSettings.OnSettingsChanged -= SettingsChanged;
        }
    }

    private void AttachActions()
    {
        if (settings != null)
        {
            settings.AttachAction(SettingsChanged);
            appearanceSettings.OnSettingsChanged += SettingsChanged;
            Build();
        }
    }

    private void Build()
    {
        var mf = gameObject.GetComponent<MeshFilter>();
        mf.mesh = settings.BuildAll(appearanceSettings);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        bool reattachSettings = false;
        if (previousSettings != settings)
        {
            if (previousSettings != null)
            {
                previousSettings.DetachAction(SettingsChanged);
            }
            previousSettings = settings;
            reattachSettings = true;
        }
        if (previousAppearanceSettings != appearanceSettings)
        {
            if (previousAppearanceSettings != null)
            {
                previousAppearanceSettings.OnSettingsChanged -= SettingsChanged;
            }
            previousAppearanceSettings = appearanceSettings;
            reattachSettings = true;
        }
        if (reattachSettings) AttachActions();
    }

    private void SettingsChanged()
    {
        Build();
    }

    public void Update()
    {
        if (NeedsRebuild)
        {
            NeedsRebuild = false;
            Build();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (poly==null) return;

        if (debugFaces)
        {
            for (var f = 0; f < poly.Faces.Count; f++)
            {
                var face = poly.Faces[f];
                var pos = transform.TransformPoint(face.Centroid);
                Handles.Label(pos + new Vector3(0, 0.03f, 0), $"F{f}");
                if (debugEdges)
                {
                    var list = face.GetHalfedges();
                    for (var e = 0; e < list.Count; e++)
                    {
                        var edge = list[e];
                        pos = transform.TransformPoint(edge.Midpoint);
                        Handles.Label(pos + new Vector3(0, 0.03f, 0), $"{e}");
                    }
                }
            }
        }

        if (debugVerts && poly.DebugVerts != null)
        {
            for (int i = 0; i < poly.DebugVerts.Count; i++)
            {
                Vector3 vert = poly.DebugVerts[i];
                Vector3 pos = transform.TransformPoint(vert);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, .025f);
                Handles.Label(pos + new Vector3(0, 0.1f, 0), i.ToString());
            }
        }
    }
#endif
}
