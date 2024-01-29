using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SessionInterval
{
    public int startTime { get; set; }
    public int endTime { get; set; }
}

public static class DriverPool
{
    public static DriverPerson[] drivers = new DriverPerson[SimulationSettings.numDrivers];

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    const float averageOpportunityCostPerHour = 9f;
    const float opportunityCostStd = 2f;

    public static DriverPerson[] GetDriversActiveDuringMidnight()
    {
        DriverPerson[] midnightDrivers = drivers.Where(x => x.interval != null && (x.interval.startTime == 0 || x.interval.endTime > 24)).ToArray();

        Debug.Log($"Drivers active during midnight: {midnightDrivers.Length} out of {SimulationSettings.numDrivers} drivers");

        return midnightDrivers;
    }

    public static DriverPerson[] GetDriversStartingAtHour(int hour)
    {
        DriverPerson[] driversStartingAtHour = drivers.Where(x => x.interval != null && x.interval.startTime == hour).ToArray();

        Debug.Log($"Drivers starting at hour {hour}: {driversStartingAtHour.Length} out of {SimulationSettings.numDrivers} drivers");

        return driversStartingAtHour;
    }

    public static float CalculateAverageHourlyGrossProfitLastInterval(float intervalHours)
    {
        float currentTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float intervalStartTime = currentTime - intervalHours;

        float totalGrossProfit = 0;
        float totalDriverTime = 0;
        DriverPerson[] driversActiveDuringInterval = drivers.Where(driver =>
        {
            if (driver.interval == null)
            {
                return false;
            }
            bool driverIntervalCrossesMidnight = driver.interval.startTime > (driver.interval.endTime % 24);
            if (driverIntervalCrossesMidnight)
            {
                if (driver.actualSessionEndTime == null || driver.actualSessionEndTime >= intervalStartTime)
                {
                    return true;
                }
                return false;
            }
            if (driver.interval.startTime >= currentTime)
            {
                return false;
            }
            return true;
        }).ToArray();
        for (int i = 0; i < driversActiveDuringInterval.Length; i++)
        {
            DriverPerson driver = driversActiveDuringInterval[i];

            bool driverSessionEnded = driver.actualSessionEndTime != null;
            foreach (Trip trip in driver.completedTrips)
            {
                if (trip.droppedOffData.droppedOffTime > intervalStartTime)
                {
                    totalGrossProfit += trip.droppedOffDriverData.grossProfit;
                }
            }
            // TODO: START HERE - totalDriverTime is completely whack
            if (!driverSessionEnded)
            {
                totalDriverTime += Math.Min(intervalHours, currentTime);
            }
            else
            {
                totalDriverTime += driver.actualSessionEndTime.Value - Math.Max(intervalStartTime, 0);
            }

        }
        Debug.Log($"Total active drivers: {driversActiveDuringInterval.Length}, Total gross profit: {totalGrossProfit}, total driver time: {totalDriverTime}");
        return totalGrossProfit / totalDriverTime;
    }

    public static void CreateDriverPool()
    {
        // Profile of a person who strongly prefers working 8-5
        float[] workLifeBalanceProfile = new float[24] { 4, 5, 5, 4, 3, 1.8f, 1.3f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.2f, 1.5f, 2, 3, 4, 4, 4 };
        // Profile of a person who will work at any time
        float[] profitMaximizerProfile = new float[24] { 1.2f, 1.3f, 1.3f, 1.3f, 1.3f, 1.3f, 1.1f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Profile of a person who slightly flexible but prefers working early mornings
        float[] earlyBirdProfile = new float[24] { 3, 3, 3, 1.5f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.3f, 1.5f, 1.5f, 1.5f, 1.5f, 2, 3 };
        // Profile of a person who has built his life around driving at peak late night earning hours
        float[] lateSleeperProfile = new float[24] { 1.2f, 1.2f, 1.3f, 1.5f, 2, 2, 2, 3, 3, 3, 2, 2, 1.5f, 1.2f, 1.1f, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Profile of a person who is busy during 9-5, and will work only in the evenings
        // float[] worksTwoJobsProfile = new float[24] { 1.3f, 1.5f, 2, 3, 4, 5, 5, 5, 5, 10, 10, 10, 10, 10, 10, 10, 10, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Typical driver profile
        float[] normalDriverProfile = new float[24] { 1.4f, 1.5f, 1.8f, 2, 2, 1.5f, 1.2f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.2f, 1.3f, 1.4f, 1.4f, 1.4f };

        // float[] medianProfile = new float[24];
        // for (int i = 0; i < 24; i++)
        // {
        //     // Sort the opportunity cost profiles by hour and take the median
        //     float[] opportunityCostsByHour = new float[6] { workLifeBalanceProfile[i], profitMaximizerProfile[i], earlyBirdProfile[i], lateSleeperProfile[i], worksTwoJobsProfile[i], normalDriverProfile[i] };
        //     Array.Sort(opportunityCostsByHour);
        //     medianProfile[i] = opportunityCostsByHour[2];
        // }

        float[][] opportunityCostProfiles = new float[5][] { workLifeBalanceProfile, profitMaximizerProfile, earlyBirdProfile, lateSleeperProfile, normalDriverProfile };
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            float baseOpportunityCostPerHour = SimulationSettings.GetRandomHourlyIncome();
            while (baseOpportunityCostPerHour > 20)
            {
                baseOpportunityCostPerHour = SimulationSettings.GetRandomHourlyIncome();
            }

            int preferredSessionLength = UnityEngine.Random.Range(4, 11);
            float[] opportunityCostProfile = opportunityCostProfiles[i % opportunityCostProfiles.Length];
            DriverPerson driver = new DriverPerson()
            {
                opportunityCostProfile = opportunityCostProfile,
                baseOpportunityCostPerHour = baseOpportunityCostPerHour,
                preferredSessionLength = preferredSessionLength,
            };
            drivers[i] = driver;

        }

        SessionInterval[] intervals = new SessionInterval[SimulationSettings.numDrivers];
        float[] surplusValues = new float[SimulationSettings.numDrivers];
        // First pass at creating driver intervals
        float[] firstGuessTripCapacityByHour = SimulationSettings.GetFirstGuessTripCapacityByHour();
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            DriverPerson driver = drivers[i];
            (SessionInterval interval, float surplusValue) = CalculateMostProfitableSession(driver, firstGuessTripCapacityByHour);
            intervals[i] = interval;
            surplusValues[i] = surplusValue;
        }

        // Second pass at creating driver intervals, now based upon actual supply from the first pass. Give the drivers a chance  to adjust their interval slow in 4 iterations
        for (int i = 0; i < SimulationSettings.numDrivers * 4; i++)
        {
            int driverIndex = i % SimulationSettings.numDrivers;
            DriverPerson driver = drivers[driverIndex];
            float[] tripCapacityByHour = GetTripCapacityByHour(intervals);
            (SessionInterval interval, float surplusValue) = CalculateMostProfitableSession(driver, tripCapacityByHour);
            intervals[driverIndex] = interval;
            surplusValues[driverIndex] = surplusValue;
        }

        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            DriverPerson driver = drivers[i];
            driver.interval = intervals[i];
            driver.expectedSurplusValue = surplusValues[i];
        }

        int numDriversWithSessions = intervals.Where(x => x != null).Count();
        Debug.Log($"Number of drivers with intervals: {numDriversWithSessions} out of {SimulationSettings.numDrivers} drivers");

        Debug.Log("Surplus values:");
        Debug.Log(string.Join(", ", surplusValues.Select(x => x.ToString()).ToArray()));

        float[] secondPassTripCapacityByHour = GetTripCapacityByHour(intervals);

        Debug.Log("Second pass Trip capacity:");
        Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select(x => x.ToString()).ToArray()));

        Debug.Log("Expected passengers by hour:");
        Debug.Log(string.Join(", ", SimulationSettings.expectedPassengersByHour.Select(x => x.ToString()).ToArray()));

        Debug.Log("Expected supply demand imbalance per hour:");
        Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select((x, i) => (x - (SimulationSettings.expectedPassengersByHour[i] + SimulationSettings.expectedPassengersByHour[(i + 1) % 24]) / 2).ToString()).ToArray()));
        Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select((x, i) =>
            {
                float numPassengers = (SimulationSettings.expectedPassengersByHour[i] + SimulationSettings.expectedPassengersByHour[(i + 1) % 24]) / 2;
                return String.Format("{0:P2}", (x - numPassengers) / numPassengers);
            }
        ).ToArray()));
    }

    private static float[] GetTripCapacityByHour(SessionInterval[] intervals)
    {
        float[] tripCapacityByHour = new float[24];
        for (int i = 0; i < intervals.Length; i++)
        {
            SessionInterval interval = intervals[i];
            if (interval == null)
            {
                continue;
            }
            for (int j = interval.startTime; j < interval.endTime; j++)
            {
                tripCapacityByHour[j % 24] += SimulationSettings.driverAverageTripsPerHour;
            }
        }
        return tripCapacityByHour;
    }

    private static (SessionInterval interval, float surplusValue) CalculateMostProfitableSession(DriverPerson driver, float[] tripCapacityByHour)
    {

        float[] expectedGrossProfitByHour = CalculateExpectedGrossProfitByHour(tripCapacityByHour);
        float[] expectedSurplusValueByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float opportunityCostIndex = driver.opportunityCostProfile[i];
            expectedSurplusValueByHour[i] = expectedGrossProfitByHour[i] - (opportunityCostIndex * driver.baseOpportunityCostPerHour);
        }

        SessionInterval mostProfitableSession = new SessionInterval()
        {
            startTime = 0,
            endTime = 0,
        };
        float maxSurplusValue = Mathf.NegativeInfinity;
        for (int i = 1; i < 24; i++)
        {
            (SessionInterval interval, float expectedUtilityValue) = CalculateMostProfitableSessionOfLength(i, driver.preferredSessionLength, expectedSurplusValueByHour, driver.baseOpportunityCostPerHour);
            if (expectedUtilityValue > maxSurplusValue)
            {
                mostProfitableSession = interval;
                maxSurplusValue = expectedUtilityValue;
            }
        }
        if (maxSurplusValue <= 0)
        {
            return (null, 0);
        }
        return (mostProfitableSession, maxSurplusValue);
    }

    private static (SessionInterval interval, float expectedUtilityValue) CalculateMostProfitableSessionOfLength(int sessionLength, int preferredSessionLength, float[] expectedSurplusValueByHour, float baseOpportunityCostPerHour)
    {
        SessionInterval mostProfitableSession = new SessionInterval()
        {
            startTime = 0,
            endTime = 0,
        };
        float maxSurplusSum = Mathf.NegativeInfinity;
        float currentSessionSurplusSum = 0;
        for (int i = 0; i < sessionLength; i++)
        {
            currentSessionSurplusSum += expectedSurplusValueByHour[i];
        }
        for (int i = 0; i < 24; i++)
        {
            int leadingIndex = (i + sessionLength) % 24;
            currentSessionSurplusSum += expectedSurplusValueByHour[leadingIndex];
            currentSessionSurplusSum -= expectedSurplusValueByHour[i];
            if (currentSessionSurplusSum > maxSurplusSum)
            {
                mostProfitableSession = new SessionInterval()
                {
                    startTime = i,
                    endTime = i + sessionLength,
                };
                maxSurplusSum = currentSessionSurplusSum;
            }
        }

        int deviationFromPreferredLength = Math.Abs(sessionLength - preferredSessionLength);
        // Cost of deviatiating one additional hour, is the size of the deviation * baseOpportunityCostPerHour / 4
        float deviationCost = baseOpportunityCostPerHour * deviationFromPreferredLength * (deviationFromPreferredLength + 1) / 8;
        float currentSessionSurplusValue = maxSurplusSum - deviationCost;
        return (mostProfitableSession, currentSessionSurplusValue);
    }


    private static float[] CalculateExpectedGrossProfitByHour(float[] tripCapacityByHour)
    {
        float[] expectedGrossProfitByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            expectedGrossProfitByHour[i] = CalculateExpectedGrossProfitForOneHourOfWork(i, tripCapacityByHour[i]);
        }
        return expectedGrossProfitByHour;
    }

    private static float CalculateExpectedGrossProfitForOneHourOfWork(int hourOfTheDay, float expectedTripCapacity)
    {
        float driverSpeed = SimulationSettings.driverSpeed;
        float perKmFare = SimulationSettings.baseFarePerKm * SimulationSettings.surgeMultiplier;
        float driverFareCutPercentage = SimulationSettings.driverFareCutPercentage;
        float marginalCostPerKm = SimulationSettings.driverMarginalCostPerKm;

        // Theoretical earnings ceiling per hour, assuming that the driver is always driving a passenger or on the way to a passenger who is on average startingBaseFare/baseFarePerKm kms away
        float maxGrossProfitPerHour = driverSpeed * (perKmFare * driverFareCutPercentage - marginalCostPerKm);

        float expectedNumPassengers = (SimulationSettings.expectedPassengersByHour[hourOfTheDay % 24] + SimulationSettings.expectedPassengersByHour[(hourOfTheDay + 1) % 24]) / 2;

        float expectedTripCapacityIncludingDriver = expectedTripCapacity + 1 * SimulationSettings.driverAverageTripsPerHour;

        // TODO: Create a method to get the estimated percentage of passengers who are willing to pay the fare

        // For now we assume that the driver can drive passengers at full capacity if there are 1.3x more passengers than driver trip capacity
        float expectedGrossProfit = maxGrossProfitPerHour * Mathf.Min(expectedNumPassengers / (expectedTripCapacityIncludingDriver * 1.3f), 1);
        return expectedGrossProfit;
    }
}
