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

    private static (float, float, float, float, float, float) GetAggregateSurplus(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        float aggregatePassengerSurplus = passengersWhoGotRide.Sum(passenger => passenger.trip.droppedOffData != null ? passenger.trip.droppedOffPassengerData.valueSurplus : passenger.trip.tripCreatedPassengerData.expectedValueSurplus);
        float surplusPerPotentialPassenger = aggregatePassengerSurplus / passengers.Length;
        float surplusPerPassenger = aggregatePassengerSurplus / passengersWhoGotRide.Length;

        float driverIncome = passengersWhoGotRide.Sum(passenger =>
        {
            // Marginal cost should be the same in all cities, so pick from the first one
            float driverMarginalCostPerKm = cities[0].simulationSettings.driverMarginalCostPerKm;
            float marginalDriverCost = (passenger.trip.tripCreatedData.tripDistance + passenger.trip.driverDispatchedData.enRouteDistance) * driverMarginalCostPerKm;
            return passenger.trip.tripCreatedData.fare.driverCut - marginalDriverCost;
        });

        float uberIncome = passengersWhoGotRide.Sum(passenger => passenger.trip.tripCreatedData.fare.uberCut);
        float totalSurplus = aggregatePassengerSurplus + driverIncome + uberIncome;

        return (aggregatePassengerSurplus, surplusPerPotentialPassenger, surplusPerPassenger, driverIncome, uberIncome, totalSurplus);
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

        float aggregateEnRouteDistance = passengersWhoGotRide.Sum(passenger => passenger.trip.driverDispatchedData.enRouteDistance);
        float averageEnRouteDistance = aggregateEnRouteDistance / passengersWhoGotRide.Length;
        Debug.Log("Average en route distance: " + averageEnRouteDistance);

        return (aggregateWaitingTime, averageWaitingTime, averageValueOfTime);
    }

    private static float GetNumberOfRides(City[] cities)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();
        PassengerPerson[] passengersWhoGotRide = passengers.Where(passenger => passenger.StartedTrip()).ToArray();
        return passengersWhoGotRide.Length;
    }

    private static float GetAverageTimeSensitivity(PassengerPerson[] passengers)
    {
        if (passengers.Length == 0)
        {
            return 0;
        }
        return passengers.Average(passenger => passenger.economicParameters.timePreference);
    }

    private static float GetAverageTimeSavedByUber(PassengerPerson[] passengers)
    {
        if (passengers.Length == 0)
        {
            return 0;
        }
        return passengers.Average(passenger => passenger.trip.droppedOffPassengerData != null ? passenger.trip.droppedOffData.totalTime : passenger.uberTripOption.timeHours);
    }

    private static void ShowSurplusDifferenceCausedByAllocation(City[] staticCities, City[] surgeCities, float staticAverageValueOfTime)
    {
        PassengerPerson[] staticPassengers = staticCities.SelectMany(city => city.GetPassengerPeople()).ToArray().Where(passenger => passenger.StartedTrip()).ToArray();
        PassengerPerson[] surgePassengers = surgeCities.SelectMany(city => city.GetPassengerPeople()).ToArray().Where(passenger => passenger.StartedTrip()).ToArray();

        if (staticPassengers.Length == 0 || surgePassengers.Length == 0)
        {
            return;
        }

        float staticAverageTimeSensitivity = GetAverageTimeSensitivity(staticPassengers);
        float surgeAverageTimeSensitivity = GetAverageTimeSensitivity(surgePassengers);

        Debug.Log("Static average time sensitivity: " + staticAverageTimeSensitivity);
        Debug.Log("Surge average time sensitivity: " + surgeAverageTimeSensitivity);

        float staticAverageTimeSavedByUber = GetAverageTimeSavedByUber(staticPassengers);

        float staticTotalTimeSavedByUber = staticAverageTimeSavedByUber * staticPassengers.Length;
        float staticAverageValueOfTimeNetOfTimeSensitivity = staticAverageValueOfTime / staticAverageTimeSensitivity;
        float staticTotalTimeCostSavedByUberTimeSensitivity = staticTotalTimeSavedByUber * staticAverageValueOfTimeNetOfTimeSensitivity * staticAverageTimeSensitivity;

        float surgeTotalTimeCostSavedByUberTimeSensitivity = staticTotalTimeSavedByUber * staticAverageValueOfTimeNetOfTimeSensitivity * surgeAverageTimeSensitivity;

        float surplusDifferenceDueToTimeSensitivity = surgeTotalTimeCostSavedByUberTimeSensitivity - staticTotalTimeCostSavedByUberTimeSensitivity;
        Debug.Log("Surplus difference due to time sensitivity: " + surplusDifferenceDueToTimeSensitivity);
        Debug.Log("Surplus difference due to time sensitivity per passenger: " + surplusDifferenceDueToTimeSensitivity / staticPassengers.Length);


        float staticAverageHourlyIncome = staticPassengers.Average(passenger => passenger.economicParameters.hourlyIncome);
        float surgeAverageHourlyIncome = surgePassengers.Average(passenger => passenger.economicParameters.hourlyIncome);
        Debug.Log("Static average hourly income: " + staticAverageHourlyIncome);
        Debug.Log("Surge average hourly income: " + surgeAverageHourlyIncome);
        float staticAverageValueOfTimeNetOfIncome = staticAverageValueOfTime / Mathf.Sqrt(staticAverageHourlyIncome);
        float staticTotalTimeCostSavedByUberIncome = staticTotalTimeSavedByUber * staticAverageValueOfTimeNetOfIncome * Mathf.Sqrt(staticAverageHourlyIncome);
        float surgeTotalTimeCostSavedByUberIncome = staticTotalTimeSavedByUber * staticAverageValueOfTimeNetOfIncome * Mathf.Sqrt(surgeAverageHourlyIncome);
        float surplusDifferenceDueToIncome = surgeTotalTimeCostSavedByUberIncome - staticTotalTimeCostSavedByUberIncome;
        Debug.Log("Surplus difference due to income: " + surplusDifferenceDueToIncome);
        Debug.Log("Surplus difference due to income per passenger: " + surplusDifferenceDueToIncome / staticPassengers.Length);

        float staticAverageMaxTimeSavedByUber = staticPassengers.Average(passenger => passenger.economicParameters.GetBestSubstitute().maxTimeSavedByUber);
        float surgeAverageMaxTimeSavedByUber = surgePassengers.Average(passenger => passenger.economicParameters.GetBestSubstitute().maxTimeSavedByUber);
        Debug.Log("Static average max time saved by Uber: " + FormatUtils.formatTime(staticAverageMaxTimeSavedByUber));
        Debug.Log("Surge average max time saved by Uber: " + FormatUtils.formatTime(surgeAverageMaxTimeSavedByUber));
        Debug.Log("Difference in average time saved: " + FormatUtils.formatTime(surgeAverageMaxTimeSavedByUber - staticAverageMaxTimeSavedByUber));

        float staticAverageMaxTimeCostSavedByUber = staticAverageMaxTimeSavedByUber * staticAverageValueOfTime;
        float surgeAverageMaxTimeCostSavedByUber = surgeAverageMaxTimeSavedByUber * staticAverageValueOfTime;
        float staticTotalMaxTimeCostSavedByUber = staticAverageMaxTimeCostSavedByUber * staticPassengers.Length;
        float surgeTotalMaxTimeCostSavedByUber = surgeAverageMaxTimeCostSavedByUber * staticPassengers.Length;
        float surplusDifferenceDueToMaxTimeSavedByUber = surgeTotalMaxTimeCostSavedByUber - staticTotalMaxTimeCostSavedByUber;
        Debug.Log("Surplus difference due to max time saved by Uber: " + surplusDifferenceDueToMaxTimeSavedByUber);
        Debug.Log("Surplus difference due to max time saved by Uber per passenger: " + surplusDifferenceDueToMaxTimeSavedByUber / staticPassengers.Length);
    }

    public static void ShowSurplusBreakdown(City[] staticCities, City[] surgeCities)
    {
        float numStaticPassengersWhoGotRides = GetNumberOfRides(staticCities);
        float numSurgePassengersWhoGotRides = GetNumberOfRides(surgeCities);
        (float staticAggregatePassengerSurplus, float staticSurplusPerPotentialPassenger, float staticSurplusPerPassenger, float staticDriverIncome, float staticUberIncome, float staticTotalSurplus) = GetAggregateSurplus(staticCities);
        (float surgeAggregatePassengerSurplus, float surgeSurplusPerPotentialPassenger, float surgeSurplusPerPassenger, float surgeDriverIncome, float surgeUberIncome, float surgeTotalSurplus) = GetAggregateSurplus(surgeCities);

        Debug.Log($"Static aggregate passenger surplus: {staticAggregatePassengerSurplus}, static surplus per potential passenger: {staticSurplusPerPotentialPassenger}");
        Debug.Log($"Surge aggregate passengersurplus: {surgeAggregatePassengerSurplus}, surge surplus per potential passenger: {surgeSurplusPerPotentialPassenger}");
        Debug.Log("Difference in aggregate passenger surplus: " + (surgeAggregatePassengerSurplus - staticAggregatePassengerSurplus));
        float surgeSurplusPerPassengerCorrected = surgeAggregatePassengerSurplus / numStaticPassengersWhoGotRides;
        float surplusDifferencePerPassenger = surgeSurplusPerPassengerCorrected - staticSurplusPerPassenger;
        Debug.Log("Surplus difference per passenger: " + surplusDifferencePerPassenger);

        // Log driver, uber and total surplus
        Debug.Log("Static driver income: " + staticDriverIncome);
        Debug.Log("Surge driver income: " + surgeDriverIncome);
        Debug.Log("Difference in driver income: " + (surgeDriverIncome - staticDriverIncome));
        Debug.Log("Static uber income: " + staticUberIncome);
        Debug.Log("Surge uber income: " + surgeUberIncome);
        Debug.Log("Difference in uber income: " + (surgeUberIncome - staticUberIncome));
        Debug.Log("Static total surplus: " + staticTotalSurplus);
        Debug.Log("Surge total surplus: " + surgeTotalSurplus);
        Debug.Log("Difference in total surplus: " + (surgeTotalSurplus - staticTotalSurplus));

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
        Debug.Log("Average static waiting time: " + FormatUtils.formatTime(staticAverageWaitingTime));
        Debug.Log("Average surge waiting time: " + FormatUtils.formatTime(surgeAverageWaitingTime));
        Debug.Log("Average static value of time: " + staticAverageValueOfTime);
        Debug.Log("Average surge value of time: " + surgeAverageValueOfTime);

        // This calculation is not entirely correct, since waiting time and value of time are not independent.
        float staticAverageWaitingCost = staticAverageWaitingTime * staticAverageValueOfTime;
        // Here, we correct for surge pricing having a higher average value of time by assuming that the static pricing had the same average value of time as the surge pricing.
        // We also assume that surge and static simulations had the same amount of rides.
        float surgeAverageWaitingCostCorrected = surgeAverageWaitingTime * staticAverageValueOfTime;
        float aggregateStaticWaitingCost = staticAverageWaitingCost * numStaticPassengersWhoGotRides;
        float aggregateSurgeWaitingCostCorrected = surgeAverageWaitingCostCorrected * numStaticPassengersWhoGotRides;
        float utilityDifferenceDueToTimeCost = aggregateStaticWaitingCost - aggregateSurgeWaitingCostCorrected;
        Debug.Log("Average static waiting time: " + FormatUtils.formatTime(staticAverageWaitingTime));
        Debug.Log("Average surge waiting time: " + FormatUtils.formatTime(surgeAverageWaitingTime));
        Debug.Log("Average static waiting cost: " + staticAverageWaitingCost);
        Debug.Log("Average surge waiting cost: " + surgeAverageWaitingCostCorrected);
        Debug.Log("Utility difference due to time cost: " + utilityDifferenceDueToTimeCost);
        Debug.Log("Utility difference due to time cost per passenger: " + utilityDifferenceDueToTimeCost / numStaticPassengersWhoGotRides);

        float surgePassengerDifference = numSurgePassengersWhoGotRides - numStaticPassengersWhoGotRides;
        // Assume that surge passengers have the same average surplus per trip as static passengers
        float utilityDifferenceDueToPassengerDifference = surgePassengerDifference * staticSurplusPerPassenger;
        Debug.Log("Number of static passengers: " + numStaticPassengersWhoGotRides);
        Debug.Log("Number of surge passengers: " + numSurgePassengersWhoGotRides);
        Debug.Log("Number of passengers difference: " + surgePassengerDifference);
        Debug.Log("Utility difference due to passenger difference: " + utilityDifferenceDueToPassengerDifference);
        Debug.Log("Utility difference due to passenger difference per passenger: " + utilityDifferenceDueToPassengerDifference / numStaticPassengersWhoGotRides);

        // ShowSurplusDifferenceCausedByAllocation(staticCities, surgeCities, staticAverageValueOfTime);
        ShowSurplusBucketInfo(staticCities, surgeCities);
    }

    private static void ShowSurplusBucketInfo(City[] staticCities, City[] surgeCities)
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };

        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;

        SimStatistic[] surgeBuckets = new SimStatistic[4];
        SimStatistic[] staticBuckets = new SimStatistic[4];
        for (int i = 0; i < 4; i++)
        {
            surgeBuckets[i] = DataInspection.GetSurplusBucketInfo(surgeCities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
            staticBuckets[i] = DataInspection.GetSurplusBucketInfo(staticCities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
        }
        Debug.Log($"Poorest quartile - Static avg: {staticBuckets[0].value / staticBuckets[0].totalNumPassengers}, Surge avg: {surgeBuckets[0].value / surgeBuckets[0].totalNumPassengers}");
        // Debug.Log($"Poorest quartile static - number of Uber passengers: {staticBuckets[0].sampleSize}, number of total passengers: {staticBuckets[0].totalNumPassengers} total surplus: {staticBuckets[0].value}");
        // Debug.Log($"Poorest quartile surge - number of passengers: {surgeBuckets[0].totalNumPassengers}, total surplus: {surgeBuckets[0].value}");
        Debug.Log($"Second quartile - Static avg: {staticBuckets[1].value / staticBuckets[1].totalNumPassengers}, Surge avg: {surgeBuckets[1].value / surgeBuckets[1].totalNumPassengers}");
        Debug.Log($"Third quartile - Static avg: {staticBuckets[2].value / staticBuckets[2].totalNumPassengers}, Surge avg: {surgeBuckets[2].value / surgeBuckets[2].totalNumPassengers}");
        Debug.Log($"Richest quartile - Static avg: {staticBuckets[3].value / staticBuckets[3].totalNumPassengers}, Surge avg: {surgeBuckets[3].value / surgeBuckets[3].totalNumPassengers}");
    }

    public static SimStatistic GetSurplusBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

        PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
        // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
        PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();


        List<PassengerPerson> passengersInQuartileWhoCompletedJourney = new List<PassengerPerson>();
        List<PassengerPerson> passengersInQuartileWhoAreWaitingOrInTransit = new List<PassengerPerson>();
        List<PassengerPerson> passengersInQuartile = new List<PassengerPerson>();
        if (quartile == 0)
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartile = passengers.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
        }
        else
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartile = passengers.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
        }

        float aggregateSurplus = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedValueSurplus);
        // float aggregateExpectedSurplus = passengersWhoAreWaitingOrInTransit
        int sampleSize = passengersInQuartileWhoCompletedJourney.Count + passengersInQuartileWhoAreWaitingOrInTransit.Count;
        int totalNumPassengers = passengersInQuartile.Count;

        return new SimStatistic
        {
            value = aggregateSurplus,
            sampleSize = sampleSize,
            totalNumPassengers = totalNumPassengers
        };
    }

}
