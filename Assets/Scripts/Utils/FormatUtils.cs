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
}
