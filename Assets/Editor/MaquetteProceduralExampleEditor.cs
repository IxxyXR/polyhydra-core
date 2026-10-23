using Polyhydra.Core;
using UnityEditor;

[CustomEditor(typeof(MaquetteProceduralExample), true)]
public class MaquetteProceduralExampleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Changes rebuild this example during Play mode.", MessageType.Info);
        var property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (!IsRelevant(property.name)) continue;
            var radius = serializedObject.FindProperty("cornerRadius") ?? serializedObject.FindProperty("radius");
            bool disabled = property.name == "m_Script" ||
                (property.name == "cornerSegments" && radius != null && radius.floatValue == 0f);
            using (new EditorGUI.DisabledScope(disabled))
            {
                if (property.name == "roundedCorners")
                    property.intValue = (int)(RectangleCorners)EditorGUILayout.EnumFlagsField(property.displayName,
                        (RectangleCorners)property.intValue);
                else EditorGUILayout.PropertyField(property, true);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private bool IsRelevant(string name)
    {
        if (name == "zeroSweepBehavior")
            return serializedObject.FindProperty("sweepAngle").floatValue == 0f;
        if (!(target is MaquetteConicExample)) return true;
        var type = (Shapes.ConicType)serializedObject.FindProperty("type").intValue;
        switch (name)
        {
            case "majorAxis": return type != Shapes.ConicType.Circle;
            case "minorAxis": return type == Shapes.ConicType.Ellipse || type == Shapes.ConicType.Hyperbola;
            case "centerOffset": return type == Shapes.ConicType.Parabola;
            default: return true;
        }
    }
}
