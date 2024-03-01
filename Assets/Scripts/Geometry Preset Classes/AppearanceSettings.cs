using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "AppearanceSettings", menuName = "Polyhydra/AppearanceSettings", order = 1)]
public class AppearanceSettings : ScriptableObject
{
    public ColorMethods ColorMethod = ColorMethods.ByRole;
    public List<Color> ColorList;

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

    public Color[] CalculateColorList()
    {
        if (ColorList != null && ColorList.Count == 12)
        {
            return ColorList?.ToArray();
        }
        return null;
    }

}