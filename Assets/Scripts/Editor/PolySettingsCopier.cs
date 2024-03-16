using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class PolySettingsCopier
{
    private static BaseSettings copiedSettings;

    [MenuItem("Assets/Copy Poly Settings", true)]
    private static bool CopySettingsValidation()
    {
        var selected = Selection.activeObject;
        return selected && selected is BaseSettings;
    }

    [MenuItem("Assets/Copy Poly Settings")]
    private static void CopySettings()
    {
        copiedSettings = Selection.activeObject as BaseSettings;
        Debug.Log("Settings copied.");
    }

    [MenuItem("Assets/Paste Poly Settings", true)]
    private static bool PasteSettingsValidation()
    {
        var selected = Selection.activeObject;
        return copiedSettings != null && selected && selected is BaseSettings;
    }

    [MenuItem("Assets/Paste Poly Settings")]
    private static void PasteSettings()
    {
        var targetSettings = Selection.activeObject as BaseSettings;
        if (targetSettings != null && copiedSettings != null)
        {
            Undo.RecordObject(targetSettings, "Paste Settings");

            targetSettings.Operators = new List<BaseSettings.Operator>(copiedSettings.Operators);
            targetSettings.FastConicalize = copiedSettings.FastConicalize;
            targetSettings.CanonicalizeIterations = copiedSettings.CanonicalizeIterations;
            targetSettings.PlanarizeIterations = copiedSettings.PlanarizeIterations;
            targetSettings.FaceInset = copiedSettings.FaceInset;

            EditorUtility.SetDirty(targetSettings);
            Debug.Log("Settings pasted to " + targetSettings.name);
        }
    }
}