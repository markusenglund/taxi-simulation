using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormatUtils : MonoBehaviour
{
    public static string formatPercentage(float value)
    {
        return (value * 100).ToString("0.0") + "%";
    }

    public static string formatTime(float timeHours)
    {
        float timeMinutes = timeHours * 60;
        float timeSeconds = timeMinutes * 60;

        if (timeMinutes < 1)
        {
            return timeSeconds.ToString("0") + " seconds";
        }
        else if (timeHours < 1)
        {
            return timeMinutes.ToString("0") + " minutes";
        }
        else
        {
            return timeHours.ToString("0.0") + " hours";
        }
    }

    public static string formatMoney(float value)
    {
        if (value > 10000)
        {
            return "$" + (value / 1000).ToString("F0") + "k";
        }
        if (value > 1000)
        {
            return "$" + (value / 1000).ToString("F1") + "k";
        }
        return "$" + value.ToString("F0");
    }
}
