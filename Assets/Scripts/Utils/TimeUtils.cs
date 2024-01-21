using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeUtils : MonoBehaviour
{
    private static float simulationSecondsPerRealSecond = 5 * 60;

    public static float ConvertRealSpeedToSimulationSpeedPerHour(float realSpeed)
    {
        return 3600 * realSpeed / simulationSecondsPerRealSecond;
    }

    public static float ConvertSimulationSpeedPerHourToRealSpeed(float simulationSpeedPerHour)
    {
        return simulationSpeedPerHour * simulationSecondsPerRealSecond / 3600;
    }

    public static float ConvertRealSecondsToSimulationHours(float realSeconds)
    {
        return realSeconds * simulationSecondsPerRealSecond / 3600;
    }

    public static float ConvertSimulationHoursToRealSeconds(float simulationHours)
    {
        return simulationHours * 3600 / simulationSecondsPerRealSecond;
    }

    public static string ConvertSimulationHoursToTimeString(float simulationHours)
    {
        int hours = (int)simulationHours;
        int minutes = (int)((simulationHours - hours) * 60);
        int hoursSinceMidnight = hours % 24;
        return hoursSinceMidnight.ToString("00") + ":" + minutes.ToString("00");
    }
}
