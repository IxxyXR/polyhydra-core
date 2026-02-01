using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Antiprism;

[CustomEditor(typeof(AntiprismPluginSettings))]
public class AntiprismPluginSettingsEditor : Editor
{
    private ReorderableList modifierList;
    private SerializedProperty antiprismModifiersProp;

    private void OnEnable()
    {
        antiprismModifiersProp = serializedObject.FindProperty("antiprismModifiers");

        modifierList = new ReorderableList(serializedObject, antiprismModifiersProp, true, true, true, true)
        {
            drawHeaderCallback = DrawHeader,
            drawElementCallback = DrawModifierElement,
            elementHeightCallback = GetElementHeight
        };
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Antiprism Modifiers (executed in order)");
    }

    private float GetElementHeight(int index)
    {
        if (index < 0 || index >= antiprismModifiersProp.arraySize)
            return EditorGUIUtility.singleLineHeight;

        var element = antiprismModifiersProp.GetArrayElementAtIndex(index);
        var typeProp = element.FindPropertyRelative("Type");
        var modifierType = (ModifierType)typeProp.enumValueIndex;

        // Base height: Active toggle + Type dropdown + spacing
        float height = EditorGUIUtility.singleLineHeight * 2 + 8;

        // Add height for parameters based on modifier type
        switch (modifierType)
        {
            case ModifierType.Canonicalize:
                height += EditorGUIUtility.singleLineHeight + 2; // CanonicalizeIterations
                break;
            case ModifierType.Dual:
                height += EditorGUIUtility.singleLineHeight + 2; // DualRadius
                break;
            case ModifierType.Truncate:
                height += (EditorGUIUtility.singleLineHeight + 2) * 2; // TruncateRatio, TruncateOrder
                break;
            case ModifierType.Kis:
                height += EditorGUIUtility.singleLineHeight + 2; // KisFaceSides
                break;
            case ModifierType.Needle:
                height += EditorGUIUtility.singleLineHeight + 2; // NeedleHeight
                break;
            case ModifierType.Gyro:
            case ModifierType.Meta:
            case ModifierType.Snub:
                height += EditorGUIUtility.singleLineHeight + 2; // ConwayN
                break;
            case ModifierType.Bevel:
                height += (EditorGUIUtility.singleLineHeight + 2) * 2; // ConwayN, TruncateRatio
                break;
            case ModifierType.Subdivide:
            case ModifierType.Expand:
            case ModifierType.Ortho:
                height += (EditorGUIUtility.singleLineHeight + 2) * 2; // ConwayN, ConwayM
                break;
            case ModifierType.None:
            case ModifierType.Ambo:
            case ModifierType.Join:
            case ModifierType.Zip:
            case ModifierType.ConvexHull:
            case ModifierType.Zonohedron:
                // No parameters
                break;
        }

        return height + 4; // Extra padding
    }

    private void DrawModifierElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= antiprismModifiersProp.arraySize)
            return;

        var element = antiprismModifiersProp.GetArrayElementAtIndex(index);
        var activeProp = element.FindPropertyRelative("Active");
        var typeProp = element.FindPropertyRelative("Type");

        rect.y += 2;
        float lineHeight = EditorGUIUtility.singleLineHeight;

        // Draw active toggle and modifier type on first line
        Rect activeRect = new Rect(rect.x, rect.y, 20, lineHeight);
        Rect labelRect = new Rect(rect.x + 20, rect.y, 60, lineHeight);
        Rect typeRect = new Rect(rect.x + 85, rect.y, rect.width - 85, lineHeight);

        EditorGUI.PropertyField(activeRect, activeProp, GUIContent.none);
        EditorGUI.LabelField(labelRect, $"#{index + 1}");
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        // Only draw parameters if active
        if (!activeProp.boolValue)
        {
            GUI.enabled = false;
        }

        var modifierType = (ModifierType)typeProp.enumValueIndex;
        rect.y += lineHeight + 2;

        // Draw parameters based on modifier type
        switch (modifierType)
        {
            case ModifierType.Canonicalize:
                DrawProperty(rect, element, "CanonicalizeIterations", "Iterations");
                break;

            case ModifierType.Dual:
                DrawProperty(rect, element, "DualRadius", "Radius");
                break;

            case ModifierType.Truncate:
                DrawProperty(rect, element, "TruncateRatio", "Ratio");
                rect.y += lineHeight + 2;
                DrawProperty(rect, element, "TruncateOrder", "Order");
                break;

            case ModifierType.Kis:
                DrawProperty(rect, element, "KisFaceSides", "Face Sides");
                break;

            case ModifierType.Needle:
                DrawProperty(rect, element, "NeedleHeight", "Height");
                break;

            case ModifierType.Gyro:
            case ModifierType.Meta:
            case ModifierType.Snub:
                DrawProperty(rect, element, "ConwayN", "N");
                break;

            case ModifierType.Bevel:
                DrawProperty(rect, element, "ConwayN", "N");
                rect.y += lineHeight + 2;
                DrawProperty(rect, element, "TruncateRatio", "Ratio");
                break;

            case ModifierType.Subdivide:
            case ModifierType.Expand:
            case ModifierType.Ortho:
                DrawProperty(rect, element, "ConwayN", "N");
                rect.y += lineHeight + 2;
                DrawProperty(rect, element, "ConwayM", "M");
                break;

            case ModifierType.Zonohedron:
                DrawHelpBox(rect, "Creates zonohedron from current vertices");
                break;

            case ModifierType.None:
            case ModifierType.Ambo:
            case ModifierType.Join:
            case ModifierType.Zip:
            case ModifierType.ConvexHull:
                // No parameters needed
                break;
        }

        GUI.enabled = true;
    }

    private void DrawProperty(Rect rect, SerializedProperty element, string propertyName, string label)
    {
        var prop = element.FindPropertyRelative(propertyName);
        Rect propRect = new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propRect, prop, new GUIContent(label));
    }

    private void DrawHelpBox(Rect rect, string message)
    {
        Rect helpRect = new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight);
        EditorGUI.HelpBox(helpRect, message, MessageType.Info);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all default fields except antiprismModifiers
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip script reference and antiprismModifiers (we'll draw it custom)
            if (prop.name == "m_Script" || prop.name == "antiprismModifiers")
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        // Draw custom modifier list
        EditorGUILayout.Space(10);
        modifierList.DoLayoutList();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Antiprism modifiers execute BEFORE Polyhydra operators. " +
            "For Dual, Kis, Ambo, etc., consider using the Operators list instead for more flexibility.",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}
