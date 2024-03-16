using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EaseUtils : MonoBehaviour
{
    public static float EaseInOutCubic(float t)
    {
        float t2;
        if (t <= 0.5f)
        {
            t2 = Mathf.Pow(t * 2, 3) / 2;
        }
        else
        {
            t2 = (2 - Mathf.Pow((1 - t) * 2, 3)) / 2;
        }
        return t2;
    }

    public static float EaseInQuadratic(float t)
    {
        return t * t;
    }

    public static float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }

    public static float EaseInCubic(float t)
    {
        return Mathf.Pow(t, 3);
    }
}
