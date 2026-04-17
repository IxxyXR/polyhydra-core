using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Polyhydra.Core;
using UnityEngine;

public class RuntimeSettingsGUI : MonoBehaviour
{
    [Serializable]
    private class DropdownState
    {
        public bool expanded;
        public Vector2 scroll;
        public string search = string.Empty;
    }

    public PolyhydraGenerator generator;
    public GUISkin guiSkin;
    public bool showGUI = true;
    public bool useSafeOperatorRanges = true;
    public float panelWidth = 380f;

    private readonly Dictionary<string, DropdownState> _dropdownStates = new();
    private readonly Dictionary<string, bool> _foldoutStates = new();
    private Vector2 _scroll;
    private Texture2D _dividerTex;
    private GUIStyle _dividerStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _miniLabelStyle;
    private string _newOperatorSearch = string.Empty;
    private PolyMesh.Operation _newOperatorType = PolyMesh.Operation.Identity;

    private static readonly HashSet<string> HiddenBaseSettingFields = new(StringComparer.Ordinal)
    {
        nameof(BaseSettings.Operators)
    };

    private void Awake()
    {
        if (generator == null)
            generator = GetComponent<PolyhydraGenerator>();
    }

    private void SetupGui()
    {
        if (_dividerTex == null)
            _dividerTex = MakeTex(1, 1, Color.gray);

        if (guiSkin != null)
            GUI.skin = guiSkin;

        if (_dividerStyle == null)
        {
            _dividerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _dividerTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 6, 6),
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (_subHeaderStyle == null)
        {
            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (_miniLabelStyle == null)
        {
            _miniLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
        }
    }

    private void OnGUI()
    {
        if (!showGUI || generator == null || generator.settings == null)
            return;

        SetupGui();

        bool changed = false;

        GUILayout.BeginArea(new Rect(10f, 10f, panelWidth, Screen.height - 20f), GUI.skin.box);
        _scroll = GUILayout.BeginScrollView(_scroll);

        GUILayout.Label(generator.settings.name, _headerStyle);
        GUILayout.Label(generator.settings.GetType().Name, _miniLabelStyle);

        changed |= DrawToolbar();
        DrawDivider();

        changed |= DrawShapeSettings(generator.settings);
        changed |= DrawBaseSettings(generator.settings);
        changed |= DrawOperators(generator.settings);
        changed |= DrawAppearance(generator.appearanceSettings);
        changed |= DrawDebug(generator);

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (changed)
            generator.NeedsRebuild = true;
    }

    private bool DrawToolbar()
    {
        bool changed = false;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rebuild"))
            generator.NeedsRebuild = true;

        bool newSafeRanges = GUILayout.Toggle(useSafeOperatorRanges, "Safe Ranges", GUILayout.Width(110));
        if (newSafeRanges != useSafeOperatorRanges)
        {
            useSafeOperatorRanges = newSafeRanges;
            changed = true;
        }

        GUILayout.EndHorizontal();
        return changed;
    }

    private bool DrawShapeSettings(BaseSettings settings)
    {
        if (!BeginSection("Shape", true))
            return false;

        bool changed = DrawFieldsForType(settings, settings.GetType(), typeof(BaseSettings), null);
        EndSection();
        return changed;
    }

    private bool DrawBaseSettings(BaseSettings settings)
    {
        if (!BeginSection("Post", false))
            return false;

        bool changed = DrawFieldsForType(settings, typeof(BaseSettings), typeof(ScriptableObject), HiddenBaseSettingFields);
        EndSection();
        return changed;
    }

    private bool DrawAppearance(AppearanceSettings appearance)
    {
        if (appearance == null || !BeginSection("Appearance", false))
            return false;

        bool changed = false;
        changed |= DrawEnumField(
            "Color Mode",
            nameof(AppearanceSettings.ColorMode),
            appearance,
            typeof(AppearanceSettings).GetField(nameof(AppearanceSettings.ColorMode))
        );

        if (appearance.ColorMode == AppearanceSettings.Mode.List)
        {
            appearance.ColorList ??= new List<Color>();
            while (appearance.ColorList.Count < 12)
                appearance.ColorList.Add(Color.white);

            for (int i = 0; i < appearance.ColorList.Count; i++)
            {
                Color color = appearance.ColorList[i];
                if (DrawColorField($"Color {i + 1}", ref color))
                {
                    appearance.ColorList[i] = color;
                    changed = true;
                }
            }
        }
        else
        {
            GUILayout.Label("Gradient runtime editing is not implemented in this panel.", _miniLabelStyle);
            if (GUILayout.Button("Bake Gradient To List"))
            {
                appearance.ColorList = appearance.CalculateColors()?.ToList() ?? new List<Color>();
                appearance.ColorMode = AppearanceSettings.Mode.List;
                changed = true;
            }
        }

        EndSection();
        return changed;
    }

    private bool DrawDebug(PolyhydraGenerator target)
    {
        if (!BeginSection("Debug", false))
            return false;

        bool changed = false;
        changed |= DrawBool("Debug Faces", ref target.debugFaces);
        changed |= DrawBool("Debug Edges", ref target.debugEdges);
        changed |= DrawBool("Debug Verts", ref target.debugVerts);

        if (target.poly != null)
        {
            var vef = target.poly.vef;
            GUILayout.Label($"V {vef.v}  E {vef.e}  F {vef.f}", _miniLabelStyle);
        }

        EndSection();
        return changed;
    }

    private bool DrawOperators(BaseSettings settings)
    {
        settings.Operators ??= new List<BaseSettings.Operator>();
        if (!BeginSection("Operators", true))
            return false;

        bool changed = false;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50));
        _newOperatorSearch = GUILayout.TextField(_newOperatorSearch);
        GUILayout.EndHorizontal();

        _newOperatorType = DrawOperationPopup("New Op", _newOperatorType, "add-op", _newOperatorSearch);
        if (GUILayout.Button("Add Operator"))
        {
            settings.Operators.Add(new BaseSettings.Operator
            {
                OpType = _newOperatorType
            });
            changed = true;
        }

        DrawDivider();

        for (int i = 0; i < settings.Operators.Count; i++)
        {
            var op = settings.Operators[i];
            string sectionKey = $"operator-{i}";
            bool expanded = GetFoldout(sectionKey, true);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(expanded ? "▼" : "▶", GUILayout.Width(26)))
                SetFoldout(sectionKey, !expanded);

            GUILayout.Label($"{i + 1}. {Nicify(op.OpType.ToString())}", _subHeaderStyle);

            if (GUILayout.Button("▲", GUILayout.Width(28)) && i > 0)
            {
                (settings.Operators[i - 1], settings.Operators[i]) = (settings.Operators[i], settings.Operators[i - 1]);
                changed = true;
            }

            if (GUILayout.Button("▼", GUILayout.Width(28)) && i < settings.Operators.Count - 1)
            {
                (settings.Operators[i + 1], settings.Operators[i]) = (settings.Operators[i], settings.Operators[i + 1]);
                changed = true;
            }

            if (GUILayout.Button("Dup", GUILayout.Width(42)))
            {
                settings.Operators.Insert(i + 1, CloneOperator(op));
                changed = true;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                settings.Operators.RemoveAt(i);
                changed = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                break;
            }

            GUILayout.EndHorizontal();

            if (GetFoldout(sectionKey, true))
                changed |= DrawOperatorBody(op, i);

            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        EndSection();
        return changed;
    }

    private bool DrawOperatorBody(BaseSettings.Operator op, int index)
    {
        bool changed = false;
        string uniqueId = $"op-{index}";

        changed |= DrawBool("Active", ref op.Active);

        PolyMesh.Operation newOpType = DrawOperationPopup("Type", op.OpType, uniqueId, string.Empty);
        if (newOpType != op.OpType)
        {
            op.OpType = newOpType;
            changed = true;
        }

        OpConfigs.Configs.TryGetValue(op.OpType, out var config);
        config ??= new OpConfig();

        if (op.OpType == PolyMesh.Operation.Omni)
            changed |= DrawString("Notation", ref op.StringParameter);
        else if (!string.IsNullOrEmpty(op.StringParameter))
            changed |= DrawString("String", ref op.StringParameter);

        if (config.usesAmount)
        {
            float min = useSafeOperatorRanges ? config.amountSafeMin : config.amountMin;
            float max = useSafeOperatorRanges ? config.amountSafeMax : config.amountMax;
            changed |= DrawFloat("Amount", ref op.Parameter1, min, max);
            changed |= DrawBool("Randomize Amount", ref op.Parameter1Randomize);
        }

        if (config.usesAmount2)
        {
            float min = useSafeOperatorRanges ? config.amount2SafeMin : config.amount2Min;
            float max = useSafeOperatorRanges ? config.amount2SafeMax : config.amount2Max;
            changed |= DrawFloat("Amount 2", ref op.Parameter2, min, max);
            changed |= DrawBool("Randomize Amount 2", ref op.Parameter2Randomize);
        }

        changed |= DrawInt("Iterations", ref op.Iterations, 1, 32);

        if (config.usesFilter)
        {
            DrawDivider();
            changed |= DrawEnumField(
                "Filter",
                nameof(BaseSettings.Operator.FilterType),
                op,
                typeof(BaseSettings.Operator).GetField(nameof(BaseSettings.Operator.FilterType))
            );
            changed |= DrawFilterParam(op);
            changed |= DrawBool("Invert Filter", ref op.FilterFlip);
        }

        return changed;
    }

    private bool DrawFilterParam(BaseSettings.Operator op)
    {
        switch (op.FilterType)
        {
            case FilterTypes.All:
            case FilterTypes.Inner:
            case FilterTypes.EvenSided:
                return false;

            case FilterTypes.Role:
            {
                int role = Mathf.RoundToInt(op.FilterParam);
                if (!DrawInt("Filter Role", ref role, 0, Enum.GetValues(typeof(Roles)).Length - 1))
                    return false;
                op.FilterParam = role;
                return true;
            }

            case FilterTypes.OnlyNth:
            case FilterTypes.EveryNth:
            case FilterTypes.FirstN:
            case FilterTypes.LastN:
            case FilterTypes.NSided:
            {
                int intValue = Mathf.RoundToInt(op.FilterParam);
                if (!DrawInt("Filter Param", ref intValue, 0, 128))
                    return false;
                op.FilterParam = intValue;
                return true;
            }

            case FilterTypes.Random:
                return DrawFloat("Filter Param", ref op.FilterParam, 0f, 1f);

            case FilterTypes.MinimumAngle:
            case FilterTypes.AverageAngle:
            case FilterTypes.MaximumAngle:
            case FilterTypes.MinimumEdgeAngle:
            case FilterTypes.AverageEdgeAngle:
            case FilterTypes.MaximumEdgeAngle:
                return DrawFloat("Filter Param", ref op.FilterParam, 0f, 180f);

            case FilterTypes.PositionX:
            case FilterTypes.PositionY:
            case FilterTypes.PositionZ:
                return DrawFloat("Filter Param", ref op.FilterParam, -10f, 10f);

            case FilterTypes.DistanceFromCenter:
                return DrawFloat("Filter Param", ref op.FilterParam, 0f, 10f);

            case FilterTypes.FacingUp:
            case FilterTypes.FacingForward:
            case FilterTypes.FacingRight:
            case FilterTypes.FacingVertical:
                return DrawFloat("Filter Param", ref op.FilterParam, -1f, 1f);

            default:
                return DrawFloat("Filter Param", ref op.FilterParam, -10f, 10f);
        }
    }

    private bool DrawFieldsForType(object target, Type currentType, Type stopAtExclusive, HashSet<string> hiddenFields)
    {
        bool changed = false;
        var typeChain = new Stack<Type>();
        for (var type = currentType; type != null && type != stopAtExclusive; type = type.BaseType)
            typeChain.Push(type);

        while (typeChain.Count > 0)
        {
            var type = typeChain.Pop();
            foreach (var field in GetEditableFields(type))
            {
                if (hiddenFields != null && hiddenFields.Contains(field.Name))
                    continue;

                var header = field.GetCustomAttribute<HeaderAttribute>();
                if (header != null)
                {
                    GUILayout.Space(4f);
                    GUILayout.Label(header.header, _subHeaderStyle);
                }

                changed |= DrawField(target, field);
            }
        }

        return changed;
    }

    private static IEnumerable<FieldInfo> GetEditableFields(Type type)
    {
        return type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(field => !field.IsStatic && !field.IsInitOnly && !field.IsLiteral && !field.IsNotSerialized)
            .OrderBy(field => field.MetadataToken);
    }

    private bool DrawField(object target, FieldInfo field)
    {
        object value = field.GetValue(target);
        string label = Nicify(field.Name);
        var range = field.GetCustomAttribute<RangeAttribute>();
        Type type = field.FieldType;

        if (type == typeof(bool))
        {
            bool boolValue = (bool)value;
            if (!DrawBool(label, ref boolValue))
                return false;
            field.SetValue(target, boolValue);
            return true;
        }

        if (type == typeof(int))
        {
            int intValue = (int)value;
            bool changed = range != null
                ? DrawInt(label, ref intValue, Mathf.RoundToInt(range.min), Mathf.RoundToInt(range.max))
                : DrawInt(label, ref intValue, int.MinValue, int.MaxValue);
            if (!changed)
                return false;
            field.SetValue(target, intValue);
            return true;
        }

        if (type == typeof(float))
        {
            float floatValue = (float)value;
            bool changed = range != null
                ? DrawFloat(label, ref floatValue, range.min, range.max)
                : DrawFloat(label, ref floatValue, -100f, 100f);
            if (!changed)
                return false;
            field.SetValue(target, floatValue);
            return true;
        }

        if (type == typeof(string))
        {
            string stringValue = (string)value;
            if (!DrawString(label, ref stringValue))
                return false;
            field.SetValue(target, stringValue);
            return true;
        }

        if (type.IsEnum)
            return DrawEnumField(label, field.Name, target, field);

        if (type == typeof(Color))
        {
            Color colorValue = (Color)value;
            if (!DrawColorField(label, ref colorValue))
                return false;
            field.SetValue(target, colorValue);
            return true;
        }

        if (type == typeof(Vector2))
        {
            Vector2 vectorValue = (Vector2)value;
            if (!DrawVector2(label, ref vectorValue))
                return false;
            field.SetValue(target, vectorValue);
            return true;
        }

        if (type == typeof(Vector3))
        {
            Vector3 vectorValue = (Vector3)value;
            if (!DrawVector3(label, ref vectorValue))
                return false;
            field.SetValue(target, vectorValue);
            return true;
        }

        if (type == typeof(List<Color>))
        {
            var colors = (List<Color>)value ?? new List<Color>();
            bool changed = DrawColorList(label, colors);
            field.SetValue(target, colors);
            return changed;
        }

        GUILayout.Label($"{label}: {type.Name} is not editable here", _miniLabelStyle);
        return false;
    }

    private bool DrawEnumField(string label, string keySuffix, object target, FieldInfo field)
    {
        Type enumType = field.FieldType;
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);
        int currentIndex = Array.IndexOf(values, field.GetValue(target));
        string key = $"{enumType.FullName}:{keySuffix}";

        if (!_dropdownStates.TryGetValue(key, out var state))
        {
            state = new DropdownState();
            _dropdownStates[key] = state;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        string buttonLabel = currentIndex >= 0 ? Nicify(names[currentIndex]) : "Select";
        if (GUILayout.Button(buttonLabel))
            state.expanded = !state.expanded;
        GUILayout.EndHorizontal();

        if (!state.expanded)
            return false;

        state.search = DrawInlineSearch(state.search);
        state.scroll = GUILayout.BeginScrollView(state.scroll, GUI.skin.box, GUILayout.Height(140f));
        bool changed = false;

        for (int i = 0; i < names.Length; i++)
        {
            string displayName = Nicify(names[i]);
            if (!string.IsNullOrWhiteSpace(state.search) &&
                displayName.IndexOf(state.search, StringComparison.OrdinalIgnoreCase) < 0 &&
                names[i].IndexOf(state.search, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (!GUILayout.Button(displayName, GUILayout.ExpandWidth(true)))
                continue;

            if (i != currentIndex)
            {
                field.SetValue(target, values.GetValue(i));
                changed = true;
            }
            state.expanded = false;
        }

        GUILayout.EndScrollView();
        return changed;
    }

    private PolyMesh.Operation DrawOperationPopup(string label, PolyMesh.Operation value, string keySuffix, string search)
    {
        string key = $"{typeof(PolyMesh.Operation).FullName}:{keySuffix}";
        if (!_dropdownStates.TryGetValue(key, out var state))
        {
            state = new DropdownState();
            _dropdownStates[key] = state;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        if (GUILayout.Button(Nicify(value.ToString())))
            state.expanded = !state.expanded;
        GUILayout.EndHorizontal();

        if (!state.expanded)
            return value;

        string effectiveSearch = string.IsNullOrWhiteSpace(search) ? state.search : search;
        if (string.IsNullOrWhiteSpace(search))
            state.search = DrawInlineSearch(state.search);
        else
            GUILayout.Label($"Filtered by \"{search}\"", _miniLabelStyle);

        state.scroll = GUILayout.BeginScrollView(state.scroll, GUI.skin.box, GUILayout.Height(160f));
        foreach (var op in GetFilteredOperations(effectiveSearch))
        {
            if (!GUILayout.Button(Nicify(op.ToString()), GUILayout.ExpandWidth(true)))
                continue;

            value = op;
            state.expanded = false;
            break;
        }
        GUILayout.EndScrollView();

        return value;
    }

    private static string Nicify(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var chars = new List<char>(input.Length + 8) { input[0] };
        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && !char.IsUpper(input[i - 1]))
                chars.Add(' ');
            chars.Add(input[i]);
        }

        return new string(chars.ToArray());
    }

    private bool DrawBool(string label, ref bool value)
    {
        bool newValue = GUILayout.Toggle(value, label);
        if (newValue == value)
            return false;
        value = newValue;
        return true;
    }

    private bool DrawInt(string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value}", GUILayout.Width(180));
        string input = GUILayout.TextField(value.ToString(), GUILayout.Width(64));
        GUILayout.EndHorizontal();

        int parsed = value;
        if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int raw))
            parsed = Mathf.Clamp(raw, min, max);

        if (min > int.MinValue / 2 && max < int.MaxValue / 2)
        {
            float sliderValue = GUILayout.HorizontalSlider(parsed, min, max);
            parsed = Mathf.Clamp(Mathf.RoundToInt(sliderValue), min, max);
        }

        if (parsed == value)
            return false;

        value = parsed;
        return true;
    }

    private bool DrawFloat(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:F3}", GUILayout.Width(180));
        string input = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(64));
        GUILayout.EndHorizontal();

        float parsed = value;
        if (!string.IsNullOrWhiteSpace(input) && float.TryParse(input, out float raw))
            parsed = Mathf.Clamp(raw, min, max);

        float sliderValue = GUILayout.HorizontalSlider(parsed, min, max);
        parsed = Mathf.Clamp(sliderValue, min, max);

        if (Mathf.Abs(parsed - value) <= 0.0001f)
            return false;

        value = parsed;
        return true;
    }

    private bool DrawString(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        string newValue = GUILayout.TextField(value ?? string.Empty);
        GUILayout.EndHorizontal();
        if (newValue == value)
            return false;
        value = newValue;
        return true;
    }

    private bool DrawColorField(string label, ref Color value)
    {
        bool changed = false;
        GUILayout.Label(label);
        GUILayout.BeginHorizontal();
        float r = value.r;
        float g = value.g;
        float b = value.b;
        float a = value.a;
        changed |= DrawCompactFloat("R", ref r);
        changed |= DrawCompactFloat("G", ref g);
        changed |= DrawCompactFloat("B", ref b);
        changed |= DrawCompactFloat("A", ref a);
        GUILayout.EndHorizontal();

        if (!changed)
            return false;

        value = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        return true;
    }

    private bool DrawCompactFloat(string label, ref float value)
    {
        GUILayout.BeginVertical(GUILayout.Width(80));
        GUILayout.Label($"{label}: {value:F2}", _miniLabelStyle);
        float newValue = GUILayout.HorizontalSlider(value, 0f, 1f);
        GUILayout.EndVertical();
        if (Mathf.Abs(newValue - value) <= 0.0001f)
            return false;
        value = newValue;
        return true;
    }

    private bool DrawVector2(string label, ref Vector2 value)
    {
        bool changed = false;
        GUILayout.Label(label);
        GUILayout.BeginHorizontal();
        float x = value.x;
        float y = value.y;
        changed |= DrawAxisField("X", ref x);
        changed |= DrawAxisField("Y", ref y);
        GUILayout.EndHorizontal();
        if (!changed)
            return false;
        value = new Vector2(x, y);
        return true;
    }

    private bool DrawVector3(string label, ref Vector3 value)
    {
        bool changed = false;
        GUILayout.Label(label);
        GUILayout.BeginHorizontal();
        float x = value.x;
        float y = value.y;
        float z = value.z;
        changed |= DrawAxisField("X", ref x);
        changed |= DrawAxisField("Y", ref y);
        changed |= DrawAxisField("Z", ref z);
        GUILayout.EndHorizontal();
        if (!changed)
            return false;
        value = new Vector3(x, y, z);
        return true;
    }

    private bool DrawAxisField(string label, ref float value)
    {
        GUILayout.BeginVertical();
        GUILayout.Label(label, _miniLabelStyle);
        string input = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(80));
        GUILayout.EndVertical();
        if (!float.TryParse(input, out float parsed) || Mathf.Abs(parsed - value) <= 0.0001f)
            return false;
        value = parsed;
        return true;
    }

    private bool DrawColorList(string label, List<Color> colors)
    {
        bool changed = false;
        GUILayout.Label(label, _subHeaderStyle);
        for (int i = 0; i < colors.Count; i++)
        {
            Color color = colors[i];
            if (DrawColorField($"{label} {i + 1}", ref color))
            {
                colors[i] = color;
                changed = true;
            }
        }
        return changed;
    }

    private bool BeginSection(string key, bool defaultValue)
    {
        bool expanded = GetFoldout(key, defaultValue);
        GUILayout.BeginVertical(GUI.skin.box);
        if (GUILayout.Button($"{(expanded ? "▼" : "▶")} {key}", _subHeaderStyle))
            SetFoldout(key, !expanded);

        if (!GetFoldout(key, defaultValue))
        {
            GUILayout.EndVertical();
            return false;
        }

        return true;
    }

    private static void EndSection()
    {
        GUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    private void DrawDivider()
    {
        GUILayout.Box(string.Empty, _dividerStyle, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
    }

    private bool GetFoldout(string key, bool defaultValue)
    {
        if (_foldoutStates.TryGetValue(key, out bool expanded))
            return expanded;

        _foldoutStates[key] = defaultValue;
        return defaultValue;
    }

    private void SetFoldout(string key, bool value)
    {
        _foldoutStates[key] = value;
    }

    private static string DrawInlineSearch(string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50));
        string newValue = GUILayout.TextField(value ?? string.Empty);
        GUILayout.EndHorizontal();
        return newValue;
    }

    private static BaseSettings.Operator CloneOperator(BaseSettings.Operator source)
    {
        return new BaseSettings.Operator
        {
            Active = source.Active,
            OpType = source.OpType,
            StringParameter = source.StringParameter,
            Parameter1 = source.Parameter1,
            Parameter2 = source.Parameter2,
            Iterations = source.Iterations,
            Parameter1Randomize = source.Parameter1Randomize,
            Parameter2Randomize = source.Parameter2Randomize,
            FilterType = source.FilterType,
            FilterParam = source.FilterParam,
            FilterFlip = source.FilterFlip
        };
    }

    private static IEnumerable<PolyMesh.Operation> GetFilteredOperations(string search)
    {
        var values = Enum.GetValues(typeof(PolyMesh.Operation)).Cast<PolyMesh.Operation>();
        if (string.IsNullOrWhiteSpace(search))
            return values.OrderBy(value => value.ToString(), StringComparer.Ordinal);

        return values
            .Where(value => value.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(value => value.ToString(), StringComparer.Ordinal);
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
            pix[i] = col;

        var result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
