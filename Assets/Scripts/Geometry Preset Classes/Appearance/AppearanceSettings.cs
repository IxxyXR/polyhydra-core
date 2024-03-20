using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AppearanceSettings", menuName = "Polyhydra/AppearanceSettings", order = 1)]
public class AppearanceSettings : ScriptableObject
{
    [Serializable]
    public enum Mode
    {
        List,
        Gradient
    }

    public Mode ColorMode;
    public List<Color> ColorList;
    public Gradient ColorGradient;

    public event Action OnSettingsChanged;

    void OnEnable()
    {
        OnSettingsChanged = null;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
            OnSettingsChanged?.Invoke();
    }

    public Color[] CalculateColors()
    {
        switch (ColorMode)
        {
            case Mode.List:
                return ColorList is { Count: 12 } ? ColorList?.ToArray() : null;
            case Mode.Gradient:
                return Enumerable.Range(0, 12).Select(t => ColorGradient.Evaluate(t / 12f)).ToArray();
            default:
                return null;
        }
    }

}