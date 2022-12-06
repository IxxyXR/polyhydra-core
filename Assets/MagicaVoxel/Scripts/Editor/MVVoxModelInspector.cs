﻿using UnityEngine;
using UnityEditor;
using Polyhydra.Core;

[CustomEditor(typeof(MVVoxModel))]
public class MVVoxModelInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MVVoxModel voxModel = this.target as MVVoxModel;

        if (voxModel.vox != null)
            AU.AUEditorUtility.ColoredLabel(
                string.Format("Controls ({3}: {0}x{1}x{2})", voxModel.vox.sizeX, voxModel.vox.sizeY, voxModel.vox.sizeZ,
                    System.IO.Path.GetFileNameWithoutExtension(voxModel.ed_filePath)), Color.green);
        else
            AU.AUEditorUtility.ColoredLabel("Controls", Color.green);

        AU.AUEditorUtility.ColoredHelpBox(Color.yellow,
            "Enabling this may create lots of GameObjects, careful when the vox model is big");
        voxModel.ed_importAsIndividualVoxels =
            EditorGUILayout.Toggle("Import as Individual Voxels", voxModel.ed_importAsIndividualVoxels);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load"))
        {
            string path = EditorUtility.OpenFilePanel(
                "Open VOX model",
                "Assets/MagicaVoxel/Vox",
                "vox"
            );

            voxModel.ed_filePath = path;
            voxModel.LoadVOXFile(path, voxModel.ed_importAsIndividualVoxels);
        }

        if (GUILayout.Button("Reimport"))
        {
            voxModel.LoadVOXFile(voxModel.ed_filePath, voxModel.ed_importAsIndividualVoxels);
        }

        if (GUILayout.Button("Clear"))
        {
            voxModel.ClearVoxMeshes();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Create Poly"))
        {
            voxModel.LoadVOXFile(voxModel.ed_filePath, voxModel.ed_importAsIndividualVoxels);

            var mf = voxModel.GetComponentInChildren<MeshFilter>();
            var poly = Vox2Poly.ConvertMesh(mf.sharedMesh);
            // poly = poly.Kis(new OpParams(.25f));
            var meshData = poly.BuildMeshData(colorMethod: ColorMethods.ByTags, largeMeshFormat: true);
            mf.sharedMesh = poly.BuildUnityMesh(meshData);
            var mr = voxModel.GetComponentInChildren<MeshRenderer>();
            mr.sharedMaterial.mainTexture = null;
        }
    }
}