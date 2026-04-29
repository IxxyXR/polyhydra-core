using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEditor;
using UnityEngine;

public class OmniAtomGridPopup : PopupWindowContent
{
    // Point classes form both axes of the grid
    private static readonly string[] PointClasses =
        { "V", "E", "F", "F!", "ve", "ve0", "ve1", "vf", "vf!", "fe", "fe!" };

    private const float CellSize    = 28f;
    private const float LabelWidth  = 34f;
    private const float LabelHeight = 34f;
    private const float Gap         = 2f;

    private static readonly Color ColAvailable   = new Color(0.25f, 0.42f, 0.25f);
    private static readonly Color ColIncompat    = new Color(0.38f, 0.22f, 0.22f);
    private static readonly Color ColCurrent     = new Color(0.20f, 0.50f, 0.85f);
    private static readonly Color ColHover       = new Color(0.30f, 0.55f, 0.30f);

    private readonly HashSet<string> _otherSelected;
    private readonly string          _currentAtom;
    private readonly Action<string>  _onSelect;
    private readonly HashSet<string> _compatible;

    private string _hoveredAtom;

    public OmniAtomGridPopup(IEnumerable<string> otherSelected, string currentAtom, Action<string> onSelect)
    {
        _otherSelected = new HashSet<string>(otherSelected);
        _currentAtom   = currentAtom;
        _onSelect      = onSelect;

        _compatible = new HashSet<string>();
        foreach (var atom in PolyMesh.OmniAtoms)
        {
            if (_otherSelected.Contains(atom)) continue;
            if (PolyMesh.IsCompatibleSubset(_otherSelected, atom))
                _compatible.Add(atom);
        }
    }

    public override Vector2 GetWindowSize()
    {
        int n = PointClasses.Length;
        float w = Gap + LabelWidth + Gap + n * (CellSize + Gap) + Gap;
        float h = Gap + LabelHeight + Gap + n * (CellSize + Gap) + Gap + EditorGUIUtility.singleLineHeight + Gap;
        return new Vector2(w, h);
    }

    public override void OnGUI(Rect rect)
    {
        int n = PointClasses.Length;
        _hoveredAtom = null;

        float gridX = rect.x + Gap + LabelWidth + Gap;
        float gridY = rect.y + Gap + LabelHeight + Gap;

        // Column headers
        var headerStyle = GetHeaderStyle();
        for (int col = 0; col < n; col++)
        {
            var r = new Rect(gridX + col * (CellSize + Gap), rect.y + Gap, CellSize, LabelHeight);
            GUI.Label(r, PointClasses[col], headerStyle);
        }

        // Rows
        for (int row = 0; row < n; row++)
        {
            float y = gridY + row * (CellSize + Gap);

            // Row label
            var labelRect = new Rect(rect.x + Gap, y, LabelWidth, CellSize);
            GUI.Label(labelRect, PointClasses[row], headerStyle);

            for (int col = 0; col < n; col++)
            {
                float x = gridX + col * (CellSize + Gap);
                var cellRect = new Rect(x, y, CellSize, CellSize);

                string atom = FindAtom(PointClasses[row], PointClasses[col]);

                if (atom == null)
                {
                    EditorGUI.DrawRect(cellRect, ColIncompat);
                    continue;
                }

                bool isCurrent    = atom == _currentAtom;
                bool isCompatible = _compatible.Contains(atom);
                bool isHovered    = cellRect.Contains(Event.current.mousePosition);

                if (isHovered) _hoveredAtom = atom;

                Color bg = isCurrent     ? ColCurrent  :
                           !isCompatible ? ColIncompat :
                           isHovered     ? ColHover    :
                                           ColAvailable;

                EditorGUI.DrawRect(cellRect, bg);

                if (isCompatible && Event.current.type == EventType.MouseDown && isHovered)
                {
                    _onSelect(atom);
                    editorWindow.Close();
                    Event.current.Use();
                }
            }
        }

        // Status bar — show hovered atom name
        float statusY = gridY + n * (CellSize + Gap) + Gap;
        var statusRect = new Rect(rect.x + Gap, statusY, rect.width - Gap * 2, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(statusRect, _hoveredAtom ?? string.Empty, EditorStyles.centeredGreyMiniLabel);

        editorWindow.Repaint();
    }

    private static string FindAtom(string rowClass, string colClass)
    {
        string ab = $"{rowClass}-{colClass}";
        string ba = $"{colClass}-{rowClass}";
        foreach (var atom in PolyMesh.OmniAtoms)
            if (atom == ab || atom == ba) return atom;
        return null;
    }

    private static GUIStyle _headerStyle;
    private static GUIStyle GetHeaderStyle() => _headerStyle ??= new GUIStyle(EditorStyles.miniLabel)
    {
        alignment = TextAnchor.MiddleCenter,
        clipping  = TextClipping.Clip,
        fontStyle = FontStyle.Bold,
    };
}
