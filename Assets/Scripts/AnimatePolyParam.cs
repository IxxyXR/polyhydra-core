using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatePolyParam : MonoBehaviour
{
    public int Operator;
    public int Parameter;
    public float min = 0f;
    public float max = 1f;
    public float frequency = 1f;
    public float phase = 0f;

    private PolyhydraGenerator controller;

    void Start()
    {
        controller = GetComponent<PolyhydraGenerator>();
    }

    void Update()
    {
        var value = Mathf.Lerp(min, max, (1 + Mathf.Sin(Time.time * frequency + phase)) / 2f);
        switch (Parameter)
        {
            case 0:
                controller.settings.Operators[Operator].Parameter1 = value;
                break;
            case 1:
                controller.settings.Operators[Operator].Parameter2 = value;
                break;
        }
        controller.NeedsRebuild = true;
    }
}
