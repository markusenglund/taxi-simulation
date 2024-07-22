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
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        float aggregateSurplus = passengersWhoGotRide.Sum(passenger => passenger.trip.droppedOffData != null ? passenger.trip.droppedOffPassengerData.valueSurplus : passenger.trip.tripCreatedPassengerData.expectedValueSurplus);
        float surplusPerPotentialPassenger = aggregateSurplus / passengers.Length;
        return (aggregateSurplus, surplusPerPotentialPassenger);
    }

    private static (float, float) GetFaresPaid(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        float aggregateFaresPaid = passengersWhoGotRide.Sum(passenger => passenger.trip.tripCreatedData.fare.total);
        float averageFarePaid = aggregateFaresPaid / passengersWhoGotRide.Length;
        return (aggregateFaresPaid, averageFarePaid);
    }

    private static (float, float, float) GetWaitingTime(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        float aggregateWaitingTime = passengersWhoGotRide.Sum(passenger => passenger.trip.pickedUpData != null ? passenger.trip.pickedUpData.waitingTime : passenger.trip.tripCreatedData.expectedWaitingTime);
        float averageWaitingTime = aggregateWaitingTime / passengersWhoGotRide.Length;
        float summedValueOfTime = passengersWhoGotRide.Sum(passenger => passenger.economicParameters.valueOfTime);
        float averageValueOfTime = summedValueOfTime / passengersWhoGotRide.Length;
        return (aggregateWaitingTime, averageWaitingTime, averageValueOfTime);
    }

    private static float GetNumberOfRides(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        return passengersWhoGotRide.Length;
    }

    // TODO: Calculate average surplus generated per trip
    public static void ShowSurplusBreakdown(City[] staticCities, City[] surgeCities)
    {
        float numStaticPassengersWhoGotRides = staticCities.Sum(city => city.GetPassengerPeople().Where(passenger => passenger.StartedTrip()).ToArray().Length);
        (float staticAggregateSurplus, float staticSurplusPerPotentialPassenger) = GetAggregateSurplus(staticCities);
        (float surgeAggregateSurplus, float surgeSurplusPerPotentialPassenger) = GetAggregateSurplus(surgeCities);

        Debug.Log($"Static aggregate surplus: {staticAggregateSurplus}, static surplus per potential passenger: {staticSurplusPerPotentialPassenger}");
        Debug.Log($"Surge aggregate surplus: {surgeAggregateSurplus}, surge surplus per potential passenger: {surgeSurplusPerPotentialPassenger}");

        float surgePerCapitaSurplusIncrease = (surgeSurplusPerPotentialPassenger - staticSurplusPerPotentialPassenger) / staticSurplusPerPotentialPassenger;
        Debug.Log("Surge per capita surplus increase: " + FormatUtils.formatPercentage(surgePerCapitaSurplusIncrease));

        (float staticAggregateFaresPaid, float staticAverageFarePaid) = GetFaresPaid(staticCities);
        (float surgeAggregateFaresPaid, float surgeAverageFarePaid) = GetFaresPaid(surgeCities);

        // To answer the question of how much utility passengers lost due higher fares, we need to correct for the fact that surge had a lower amount of passengers. So we calculate the fare per passenger and assume that both static and surge pricing had the same number of passengers (based on the static number).
        float surgeAggregateFaresPaidCorrected = surgeAverageFarePaid * numStaticPassengersWhoGotRides;
        float utilityDifferenceDueToFares = staticAggregateFaresPaid - surgeAggregateFaresPaidCorrected;
        Debug.Log("Average static fare paid: " + staticAverageFarePaid);
        Debug.Log("Average surge fare paid: " + surgeAverageFarePaid);
        Debug.Log("Utility difference due to fares: " + utilityDifferenceDueToFares);

        (float staticAggregateWaitingTime, float staticAverageWaitingTime, float staticAverageValueOfTime) = GetWaitingTime(staticCities);
        (float surgeAggregateWaitingTime, float surgeAverageWaitingTime, float surgeAverageValueOfTime) = GetWaitingTime(surgeCities);

        Debug.Log("Average static value of time: " + staticAverageValueOfTime);
        Debug.Log("Average surge value of time: " + surgeAverageValueOfTime);

        // This calculation is not entirely correct, since waiting time and value of time are not independent.
        float staticAverageWaitingCost = staticAverageWaitingTime * staticAverageValueOfTime;
        // Here, we correct for surge pricing having a higher average value of time by assuming that the static pricing had the same average value of time as the surge pricing.
        float surgeAverageWaitingCostCorrected = surgeAverageWaitingTime * staticAverageValueOfTime;
        float utilityDifferenceDueToTimeCost = staticAverageWaitingCost - surgeAverageWaitingCostCorrected;
        Debug.Log("Utility difference due to time cost: " + utilityDifferenceDueToTimeCost);
    }

}
