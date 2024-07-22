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

    private static (float, float) GetAggregateSurplus(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
        PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();
        float aggregateSurplus = passengersWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus) + passengersWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedValueSurplus);
        float surplusPerPotentialPassenger = aggregateSurplus / passengers.Length;
        return (aggregateSurplus, surplusPerPotentialPassenger);
    }
    public static void ShowSurplusBreakdown(City[] staticCities, City[] surgeCities)
    {
        (float staticAggregateSurplus, float staticSurplusPerPotentialPassenger) = GetAggregateSurplus(staticCities);
        (float surgeAggregateSurplus, float surgeSurplusPerPotentialPassenger) = GetAggregateSurplus(surgeCities);

        Debug.Log($"Static aggregate surplus: {staticAggregateSurplus}, static surplus per potential passenger: {staticSurplusPerPotentialPassenger}");
        Debug.Log($"Surge aggregate surplus: {surgeAggregateSurplus}, surge surplus per potential passenger: {surgeSurplusPerPotentialPassenger}");

        float surgePerCapitaSurplusIncrease = (surgeSurplusPerPotentialPassenger - staticSurplusPerPotentialPassenger) / staticSurplusPerPotentialPassenger;
        Debug.Log("Surge per capita surplus increase: " + FormatUtils.formatPercentage(surgePerCapitaSurplusIncrease));
    }

}
