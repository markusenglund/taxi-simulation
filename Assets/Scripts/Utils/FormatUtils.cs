using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormatUtils : MonoBehaviour
{
    public static string formatPercentage(float value)
    {
        return (value * 100).ToString("0.0") + "%";
    }
}
