using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class WaitingTimeData
{
    public int sampleSize;
    public float averageWaitingTime;
}

public class DataInspection : MonoBehaviour
{

    public static Dictionary<int, WaitingTimeData> GetAverageWaitingTimeByNumAssignedTrips(City[] cities)
    {
        List<Trip> trips = cities.SelectMany(city => city.GetTrips()).ToList();
        Dictionary<int, float[]> waitingTimesByNumAssignedTrips = new Dictionary<int, float[]>();
        foreach (Trip trip in trips)
        {
            int numAssignedTrips = trip.tripCreatedData.numTripsAssigned;
            if (!waitingTimesByNumAssignedTrips.ContainsKey(numAssignedTrips))
            {
                waitingTimesByNumAssignedTrips[numAssignedTrips] = new float[] { trip.tripCreatedData.expectedWaitingTime };
            }
            else
            {
                waitingTimesByNumAssignedTrips[numAssignedTrips] = waitingTimesByNumAssignedTrips[numAssignedTrips].Append(trip.tripCreatedData.expectedWaitingTime).ToArray();
            }
        }

        Dictionary<int, WaitingTimeData> averageWaitingTimeByNumAssignedTrips = new Dictionary<int, WaitingTimeData>();
        foreach (KeyValuePair<int, float[]> entry in waitingTimesByNumAssignedTrips)
        {
            int numAssignedTrips = entry.Key;
            float[] waitingTimes = entry.Value;
            float averageWaitingTime = waitingTimes.Average();
            averageWaitingTimeByNumAssignedTrips[numAssignedTrips] = new WaitingTimeData
            {
                sampleSize = waitingTimes.Length,
                averageWaitingTime = averageWaitingTime
            };

            Debug.Log($"Num assigned trips: {numAssignedTrips}, Average waiting time: {(averageWaitingTime * 60).ToString()}, sample size: {waitingTimes.Length}");
        }

        return averageWaitingTimeByNumAssignedTrips;

    }
}
