using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BaseSettings.Operator))]
public class OperatorDrawer : PropertyDrawer
{
    private static readonly string[] AllFields =
    {
        "Active", "OpType",
        "StringParameter",
        "Parameter1", "Parameter1Randomize",
        "Parameter2", "Parameter2Randomize",
        "Iterations",
        "FilterType", "FilterParam", "FilterFlip"
    };

    private static readonly float LineH = EditorGUIUtility.singleLineHeight;
    private static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;

    private bool IsOmni(SerializedProperty property) =>
        property.FindPropertyRelative("OpType").intValue == (int)PolyMesh.Operation.Omni;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var iterations = property.FindPropertyRelative("Iterations");
        if (iterations.intValue == 0)
            iterations.intValue = 1;

        EditorGUI.BeginProperty(position, label, property);

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, LineH),
            property.isExpanded, label, true);

        if (property.isExpanded)
        {
            bool omni = IsOmni(property);
            float y = position.y + LineH + Spacing;
            EditorGUI.indentLevel++;

            foreach (var fieldName in AllFields)
            {
                var prop = property.FindPropertyRelative(fieldName);
                float h = EditorGUI.GetPropertyHeight(prop, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), prop, true);
                y += h + Spacing;
            }

            if (omni)
                y = DrawOmniAtoms(position, property, y);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private float DrawOmniAtoms(Rect position, SerializedProperty property, float y)
    {
        var stringProp = property.FindPropertyRelative("StringParameter");
        var atoms = ParseAtomString(stringProp.stringValue);

        // Capture stable references for callbacks (SerializedProperty can go stale)
        var serializedObject = property.serializedObject;
        var stringPropPath   = stringProp.propertyPath;

        EditorGUI.LabelField(new Rect(position.x, y, position.width, LineH), "Atoms");
        y += LineH + Spacing;

        int removeIndex = -1;

        float indentedX = position.x + EditorGUI.indentLevel * 15f;
        float indentedW = position.width - EditorGUI.indentLevel * 15f;
        float removeW   = 22f;
        float popupW    = indentedW - removeW - 2f;

        for (int i = 0; i < atoms.Count; i++)
        {
            string atom      = atoms[i];
            bool   isInvalid = !IsAtomCompatibleWithOthers(atom, atoms, i);

            var buttonStyle = new GUIStyle(EditorStyles.popup);
            if (isInvalid) buttonStyle.normal.textColor = Color.red;

            int capturedIndex = i;
            var buttonRect = new Rect(indentedX, y, popupW, LineH);
            if (GUI.Button(buttonRect, atom, buttonStyle))
                OpenGridPopup(buttonRect, atoms, capturedIndex, serializedObject, stringPropPath);

            if (GUI.Button(new Rect(indentedX + popupW + 2f, y, removeW, LineH), "×"))
                removeIndex = i;

            y += LineH + Spacing;
        }

        if (removeIndex >= 0)
        {
            atoms.RemoveAt(removeIndex);
            WriteAtoms(serializedObject, stringPropPath, atoms);
        }

        var addRect = new Rect(indentedX, y, indentedW, LineH);
        if (GUI.Button(addRect, "Add Atom"))
            OpenGridPopup(addRect, atoms, -1, serializedObject, stringPropPath);
        y += LineH + Spacing;

        // Validity status
        string statusMsg;
        Color  statusColor;
        if (atoms.Count == 0)
        {
            statusMsg   = "No atoms";
            statusColor = Color.gray;
        }
        else if (PolyMesh.IsCompleteOperator(atoms))
        {
            statusMsg   = "Valid";
            statusColor = Color.green;
        }
        else if (PolyMesh.IsValidSubset(atoms))
        {
            statusMsg   = "Incomplete — add more atoms";
            statusColor = new Color(1f, 0.65f, 0f);
        }
        else
        {
            statusMsg   = "Invalid combination";
            statusColor = Color.red;
        }

        var prevColor = GUI.contentColor;
        GUI.contentColor = statusColor;
        EditorGUI.LabelField(new Rect(indentedX, y, indentedW, LineH), statusMsg, EditorStyles.boldLabel);
        GUI.contentColor = prevColor;
        y += LineH + Spacing;

        return y;
    }

    private static void OpenGridPopup(
        Rect buttonRect, List<string> atoms, int editIndex,
        SerializedObject serializedObject, string stringPropPath)
    {
        var otherAtoms = new List<string>(atoms);
        if (editIndex >= 0) otherAtoms.RemoveAt(editIndex);

        string currentAtom = editIndex >= 0 ? atoms[editIndex] : null;

        PopupWindow.Show(buttonRect, new OmniAtomGridPopup(otherAtoms, currentAtom, selected =>
        {
            serializedObject.Update();
            var prop    = serializedObject.FindProperty(stringPropPath);
            var current = ParseAtomString(prop.stringValue);
            if (editIndex >= 0 && editIndex < current.Count)
                current[editIndex] = selected;
            else
                current.Add(selected);
            WriteAtoms(serializedObject, stringPropPath, current);
        }));
    }

    private static void WriteAtoms(SerializedObject so, string path, List<string> atoms)
    {
        so.Update();
        so.FindProperty(path).stringValue = string.Join(",", atoms);
        so.ApplyModifiedProperties();
    }

    private static bool IsAtomCompatibleWithOthers(string atom, List<string> atoms, int excludeIndex)
    {
        if (!PolyMesh.OmniAtomCompatibility.TryGetValue(atom, out var compat)) return true;
        for (int i = 0; i < atoms.Count; i++)
        {
            if (i == excludeIndex) continue;
            if (!compat.Contains(atoms[i])) return false;
        }
        return true;
    }

    private static List<string> ParseAtomString(string s)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(s)) return result;
        foreach (var atom in s.Split(','))
        {
            var trimmed = atom.Trim();
            if (trimmed.Length > 0) result.Add(trimmed);
        }
        return result;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return LineH;

        bool omni = IsOmni(property);
        float total = LineH + Spacing;

        foreach (var fieldName in AllFields)
        {
            var prop = property.FindPropertyRelative(fieldName);
            total += EditorGUI.GetPropertyHeight(prop, true) + Spacing;
        }

        if (omni)
        {
            var stringProp = property.FindPropertyRelative("StringParameter");
            int atomCount = ParseAtomString(stringProp.stringValue).Count;
            total += LineH + Spacing;                      // "Atoms" label
            total += (atomCount + 1) * (LineH + Spacing); // one row per atom + Add button
            total += LineH + Spacing;                      // validity status
        }

        return total;
    }
}
