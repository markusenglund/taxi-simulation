using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeUtils : MonoBehaviour
{
    private static float simulationSecondsPerRealSecond = 60;

    public static float ConvertRealSpeedToSimulationSpeedPerHour(float realSpeed)
    {
        return 3600 * realSpeed / simulationSecondsPerRealSecond;
    }

    public static float ConvertRealSecondsToSimulationHours(float realSeconds)
    {
        return realSeconds * simulationSecondsPerRealSecond / 3600;
    }
}
