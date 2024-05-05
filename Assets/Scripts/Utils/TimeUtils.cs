using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeUtils : MonoBehaviour
{
    private static float simulationSecondsPerRealSecond = 5 * 60;

    public static float simulationStartTime = 0;

    public static void SetSimulationStartTime(float startTime)
    {
        simulationStartTime = startTime;
    }

    public static float ConvertRealSpeedToSimulationSpeedPerHour(float realSpeed)
    {
        return 3600 * realSpeed / simulationSecondsPerRealSecond;
    }

    public static float ConvertSimulationSpeedPerHourToRealSpeed(float simulationSpeedPerHour)
    {
        return simulationSpeedPerHour * simulationSecondsPerRealSecond / 3600;
    }

    public static float ConvertRealSecondsTimeToSimulationHours(float timeSeconds)
    {
        return Mathf.Max(0, (timeSeconds - simulationStartTime) * simulationSecondsPerRealSecond / 3600);
    }

    public static float ConvertRealSecondsDurationToSimulationHours(float durationSeconds)
    {
        return Mathf.Max(durationSeconds * simulationSecondsPerRealSecond / 3600);
    }

    public static float ConvertSimulationHoursTimeToRealSeconds(float timeHours)
    {
        return simulationStartTime + timeHours * 3600 / simulationSecondsPerRealSecond;
    }

    public static float ConvertSimulationHoursDurationToRealSeconds(float durationHours)
    {
        return durationHours * 3600 / simulationSecondsPerRealSecond;
    }

    public static string ConvertSimulationHoursToTimeString(float simulationHours)
    {
        int hours = (int)simulationHours;
        int minutes = (int)((simulationHours - hours) * 60);
        int hoursSinceMidnight = hours % 24;
        return hoursSinceMidnight.ToString("00") + ":" + minutes.ToString("00");
    }

    public static string ConvertSimulationHoursToMinuteString(float simulationHours)
    {
        int minutes = (int)(simulationHours * 60);
        return minutes.ToString();
    }
}
