using System;
using System.Collections.Generic;
using UnityEngine;


public static class Easing
{

	private static Func<float, float> _bounceOut = (x =>
	{
		float n1 = 7.5625f;
		float d1 = 2.75f;

		if (x < 1 / d1)
		{
			return n1 * x * x;
		}
		else if (x < 2 / d1)
		{
			return n1 * (x -= 1.5f / d1) * x + 0.75f;
		}
		else if (x < 2.5 / d1)
		{
			return n1 * (x -= 2.25f / d1) * x + 0.9375f;
		}
		else
		{
			return n1 * (x -= 2.625f / d1) * x + 0.984375f;
		}
	});
	
	private static float _c1 = 1.70158f;
	private static float _c2 = _c1 * 1.525f;
	private static float _c3 = _c1 + 1f;
	private static float _c4 = (2f * Mathf.PI) / 3f;
	private static float _c5 = (2f * Mathf.PI) / 4.5f;

	public enum EasingType
	{
		Linear,
		EaseInQuad,
		EaseOutQuad,
		EaseInOutQuad,
		EaseInCubic,
		EaseOutCubic,
		EaseInOutCubic,
		EaseInQuart,
		EaseOutQuart,
		EaseInOutQuart,
		EaseInQuint,
		EaseOutQuint,
		EaseInOutQuint,
		EaseInSine,
		EaseOutSine,
		EaseInOutSine,
		EaseInExpo,
		EaseOutExpo,
		EaseInOutExpo,
		EaseInCirc,
		EaseOutCirc,
		EaseInOutCirc,
		EaseInBack,
		EaseOutBack,
		EaseInOutBack,
		EaseInElastic,
		EaseOutElastic,
		EaseInOutElastic,
		EaseInBounce,
		EaseOutBounce,
		EaseInOutBounce,
	}
	
	public static Dictionary<EasingType, Func<float, float>> Funcs = new Dictionary<EasingType, Func<float, float>>
    {
	    {EasingType.Linear, (x => x)},
        {EasingType.EaseInQuad, (x => x * x)},
        {EasingType.EaseOutQuad, (x => 1 - (1 - x) * (1 - x))},
        {EasingType.EaseInOutQuad, (x => x < 0.5 ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2)},
        {EasingType.EaseInCubic, (x => x * x * x)},
        {EasingType.EaseOutCubic, (x => 1 - Mathf.Pow(1 - x, 3))},
        {EasingType.EaseInOutCubic, (x => x < 0.5 ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2)},
        {EasingType.EaseInQuart, (x => x * x * x * x)},
        {EasingType.EaseOutQuart, (x => 1 - Mathf.Pow(1 - x, 4))},
        {EasingType.EaseInOutQuart, (x => x < 0.5f ? 8 * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 4) / 2)},
        {EasingType.EaseInQuint, (x => x * x * x * x * x)},
        {EasingType.EaseOutQuint, (x => 1 - Mathf.Pow(1 - x, 5))},
        {EasingType.EaseInOutQuint, (x => x < 0.5f ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2)},
        {EasingType.EaseInSine, (x => 1 - Mathf.Cos((x * Mathf.PI) / 2))},
        {EasingType.EaseOutSine, (x => Mathf.Sin((x * Mathf.PI) / 2))},
        {EasingType.EaseInOutSine, (x => -(Mathf.Cos(Mathf.PI * x) - 1) / 2)},
        {EasingType.EaseInExpo, (x => x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10))},
        {EasingType.EaseOutExpo, (x => x == 1 ? 1 : 1 - Mathf.Pow(2, -10 * x))},
        {EasingType.EaseInOutExpo, (x => x == 0
                ? 0
                : x == 1
                    ? 1
                    : x < 0.5f
                        ? Mathf.Pow(2, 20 * x - 10) / 2
                        : (2 - Mathf.Pow(2, -20 * x + 10)) / 2)
        },
        {EasingType.EaseInCirc, (x => 1 - Mathf.Sqrt(1 - Mathf.Pow(x, 2)))},
        {EasingType.EaseOutCirc, (x => Mathf.Sqrt(1 - Mathf.Pow(x - 1, 2)))},
        {EasingType.EaseInOutCirc, (x => x < 0.5f
                ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
                : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2)
        },
        {EasingType.EaseInBack, (x => _c3 * x * x * x - _c1 * x * x)},
        {EasingType.EaseOutBack, (x => 1 + _c3 * Mathf.Pow(x - 1, 3) + _c1 * Mathf.Pow(x - 1, 2))},
        {EasingType.EaseInOutBack, (x => x < 0.5f
                ? (Mathf.Pow(2 * x, 2) * ((_c2 + 1) * 2 * x - _c2)) / 2
                : (Mathf.Pow(2 * x - 2, 2) * ((_c2 + 1) * (x * 2 - 2) + _c2) + 2) / 2)
        },
        {EasingType.EaseInElastic, (x => x == 0
                ? 0
                : x == 1
                    ? 1
                    : -Mathf.Pow(2, 10 * x - 10) * Mathf.Sin((x * 10 - 10.75f) * _c4))
        },
        {EasingType.EaseOutElastic, (x => x == 0
                ? 0
                : x == 1
                    ? 1
                    : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * _c4) + 1)
        },
        {EasingType.EaseInOutElastic, (x => x == 0
                ? 0
                : x == 1
                    ? 1
                    : x < 0.5f
                        ? -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * _c5)) / 2
                        : (Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * _c5)) / 2 + 1)
        },
        {EasingType.EaseInBounce, (x => 1 - _bounceOut(1 - x))},
        {EasingType.EaseOutBounce, _bounceOut},
        {EasingType.EaseInOutBounce, (x => x < 0.5
                ? (1 - _bounceOut(1 - 2 * x)) / 2
                : (1 + _bounceOut(2 * x - 1)) / 2)
        },
    };

}