using System;
using UnityEngine;

public class RuntimeSettingsGUI : MonoBehaviour
{
    public PolyhydraGenerator generator;
    public GUISkin guiSkin;
    public bool showGUI = true;
    private Vector2 scroll;
    class DropdownState
    {
        public bool expanded;
        public Vector2 scroll;
    }

    Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private readonly System.Collections.Generic.Dictionary<string, DropdownState> _enumStates = new();
    private Texture2D dividerTex;
    private GUIStyle dividerStyle;
    private GUIStyle headerStyle;

    private void Awake()
    {
        if (generator == null)
            generator = GetComponent<PolyhydraGenerator>();
    }

    private void SetupGUI()
    {
        if (dividerTex == null)
            dividerTex = MakeTex(1, 1, Color.gray);

        if (dividerStyle == null)
        {
            dividerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = dividerTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 4, 4),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (guiSkin != null)
        {
            var bg = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f));
            foreach (var style in guiSkin.customStyles)
            {
                style.normal.background = bg;
                style.hover.background = bg;
                style.active.background = bg;
                style.focused.background = bg;
            }
            guiSkin.box.normal.background = bg;
            guiSkin.button.normal.background = bg;
            guiSkin.label.normal.background = bg;
        }
    }

    void OnGUI()
    {
        if (!showGUI || generator == null || generator.settings == null)
            return;

        SetupGUI();

        GUI.skin = guiSkin;

        if (dividerStyle == null)
        {
            dividerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = dividerTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 4, 4),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);

        var settings = generator.settings;
        GUILayout.Label(settings.name, headerStyle);

        bool changed = false;

        if (settings is RadialSolidsSettings radial)
            changed |= DrawRadial(radial);
        else if (settings is GridSettings grid)
            changed |= DrawGrid(grid);
        else if (settings is ShapesSettings shapes)
            changed |= DrawShapes(shapes);
        else if (settings is VariousGeometrySettings various)
            changed |= DrawVarious(various);
        else
            GUILayout.Label($"Runtime GUI not implemented for {settings.GetType().Name}");

        changed |= FloatSlider("Auto Smooth Angle", ref settings.AutoSmoothAngle, 0f, 180f);
        changed |= DrawOperators(settings);

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (changed)
        {
            generator.NeedsRebuild = true;
        }
    }

    bool DrawRadial(RadialSolidsSettings s)
    {
        bool changed = false;
        changed |= EnumField("Type", ref s.type);
        changed |= IntSlider("Sides", ref s.Sides, 3, 64);
        changed |= ToggleField("Set Height", ref s.SetHeight);
        if (s.SetHeight)
        {
            changed |= FloatSlider("Height", ref s.Height);
            changed |= FloatSlider("Cap Height", ref s.CapHeight);
        }
        return changed;
    }

    bool DrawGrid(GridSettings s)
    {
        bool changed = false;
        changed |= EnumField("Type", ref s.type);
        changed |= EnumField("Shape", ref s.shape);
        changed |= IntSlider("X", ref s.X, 1, 64);
        changed |= IntSlider("Y", ref s.Y, 1, 64);
        return changed;
    }

    bool DrawShapes(ShapesSettings s)
    {
        bool changed = false;
        changed |= EnumField("Type", ref s.type);
        changed |= EnumField("Method", ref s.method);
        changed |= FloatSlider("A", ref s.A, 0f, 24f);
        changed |= FloatSlider("B", ref s.B, 0f, 4f);
        changed |= FloatSlider("C", ref s.C, 0f, 4f);
        changed |= IntSlider("Layers", ref s.Layers, 0, 16);
        changed |= FloatSlider("Layer Height", ref s.LayerHeight, 0f, 1f);
        return changed;
    }

    bool DrawVarious(VariousGeometrySettings s)
    {
        bool changed = false;
        changed |= EnumField("Type", ref s.type);
        changed |= IntSlider("X", ref s.X, 1, 64);
        changed |= FloatSlider("Y", ref s.Y, 0.01f, 64f);
        changed |= FloatSlider("Z", ref s.Z, 0.01f, 64f);
        return changed;
    }

    bool ToggleField(string label, ref bool value)
    {
        bool newValue = GUILayout.Toggle(value, label);
        if (newValue != value)
        {
            value = newValue;
            return true;
        }
        return false;
    }

    bool IntSlider(string label, ref int value, int min, int max)
    {
        GUILayout.Label($"{label}: {value}");
        float newValue = GUILayout.HorizontalSlider(value, min, max); // Use float for value, min, max
        int roundedValue = Mathf.RoundToInt(newValue);
        if (roundedValue != value)
        {
            value = roundedValue;
            return true;
        }
        return false;
    }

    bool FloatSlider(string label, ref float value, float min = 0f, float max = 1f)
    {
        GUILayout.Label($"{label}: {value:F2}");
        float clampedValue = Mathf.Clamp(value, min, max);
        float newValue = GUILayout.HorizontalSlider(clampedValue, min, max);
        if (Mathf.Abs(newValue - value) > Mathf.Epsilon)
        {
            value = newValue;
            return true;
        }
        return false;
    }

    bool IntField(string label, ref int value, int min = int.MinValue, int max = int.MaxValue)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        string input = GUILayout.TextField(value.ToString(), GUILayout.Width(50));
        int newValue = value;
        if (int.TryParse(input, out int parsed))
            newValue = Mathf.Clamp(parsed, min, max);
        GUILayout.EndHorizontal();
        if (newValue != value)
        {
            value = newValue;
            return true;
        }
        return false;
    }

    bool EnumField<T>(string label, ref T value, string uniqueId = null) where T : Enum
    {
        var names = Enum.GetNames(typeof(T));
        var values = Enum.GetValues(typeof(T));
        int index = Array.IndexOf(values, value);

        // Add uniqueId to the key to distinguish between different dropdowns
        string key = typeof(T).FullName + label + (uniqueId ?? "");
        if (!_enumStates.TryGetValue(key, out var state))
        {
            state = new DropdownState();
            _enumStates[key] = state;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(label);
        if (GUILayout.Button(names[index], GUILayout.Width(150)))
        {
            state.expanded = !state.expanded;
        }
        GUILayout.EndHorizontal();

        bool changed = false;
        if (state.expanded)
        {
            state.scroll = GUILayout.BeginScrollView(state.scroll, GUI.skin.box, GUILayout.Height(Mathf.Min(150, names.Length * 20)));
            for (int i = 0; i < names.Length; i++)
            {
                if (GUILayout.Button(names[i], GUILayout.ExpandWidth(true)))
                {
                    if (i != index)
                    {
                        value = (T)values.GetValue(i);
                        changed = true;
                    }
                    state.expanded = false;
                }
            }
            GUILayout.EndScrollView();
        }

        return changed;
    }

    bool DrawOperators(BaseSettings settings)
    {
        if (settings.Operators == null)
            return false;

        bool changed = false;
        GUILayout.Space(10);

        GUILayout.Label("Operators", headerStyle);
        GUILayout.Box("", dividerStyle, GUILayout.Height(2), GUILayout.ExpandWidth(true));

        for (int i = 0; i < settings.Operators.Count; i++)
        {
            var op = settings.Operators[i];
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Op {i + 1}");
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                settings.Operators.RemoveAt(i);
                changed = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                break;
            }
            GUILayout.EndHorizontal();

            string opId = $"_Op{i}";

            changed |= ToggleField("Active", ref op.Active);
            changed |= EnumField("Type", ref op.OpType, opId);
            changed |= FloatSlider("Parameter1", ref op.Parameter1);
            changed |= FloatSlider("Parameter2", ref op.Parameter2);
            changed |= IntField("Iterations", ref op.Iterations, 1, 10);
            changed |= ToggleField("Param1 Random", ref op.Parameter1Randomize);
            changed |= ToggleField("Param2 Random", ref op.Parameter2Randomize);
            changed |= EnumField("Filter Type", ref op.FilterType, opId);
            changed |= FloatSlider("Filter Param", ref op.FilterParam);
            changed |= ToggleField("Filter Flip", ref op.FilterFlip);

            GUILayout.EndVertical();

            GUILayout.Box("", dividerStyle, GUILayout.Height(2), GUILayout.ExpandWidth(true));
        }

        if (GUILayout.Button("Add Operator"))
        {
            settings.Operators.Add(new BaseSettings.Operator());
            changed = true;
        }

        return changed;
    }
}
