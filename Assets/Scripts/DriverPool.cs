using System;
using System.Linq;
using UnityEngine;

using Random = System.Random;

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

    // Profile of a person who strongly prefers working 8-5
    static float[] workLifeBalanceProfile = new float[24] { 4, 5, 5, 4, 3, 1.8f, 1.3f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.2f, 1.5f, 2, 3, 4, 4, 4 };
    // Profile of a person who will work at any time
    static float[] profitMaximizerProfile = new float[24] { 1.2f, 1.3f, 1.3f, 1.3f, 1.3f, 1.3f, 1.1f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
    // Profile of a person who slightly flexible but prefers working early mornings
    static float[] earlyBirdProfile = new float[24] { 3, 3, 3, 1.5f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.3f, 1.5f, 1.5f, 1.5f, 1.5f, 2, 3 };
    // Profile of a person who has built his life around driving at peak late night earning hours
    static float[] lateSleeperProfile = new float[24] { 1.2f, 1.2f, 1.3f, 1.5f, 2, 2, 2, 3, 3, 3, 2, 2, 1.5f, 1.2f, 1.1f, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
    // Profile of a person who is busy during 9-5, and will work only in the evenings
    // static float[] worksTwoJobsProfile = new float[24] { 1.3f, 1.5f, 2, 3, 4, 5, 5, 5, 5, 10, 10, 10, 10, 10, 10, 10, 10, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
    // Typical driver profile
    static float[] normalDriverProfile = new float[24] { 1.4f, 1.5f, 1.8f, 2, 2, 1.5f, 1.2f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.2f, 1.3f, 1.4f, 1.4f, 1.4f };

    static float[][] opportunityCostProfiles = new float[5][] { workLifeBalanceProfile, profitMaximizerProfile, earlyBirdProfile, lateSleeperProfile, normalDriverProfile };

    static Random random = new Random(SimulationSettings.randomSeed);

    public static DriverPerson[] GetDriversActiveDuringMidnight()
    {
        DriverPerson[] drivers1 = drivers;
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

    public static (float grossProfitPerDriver, float surplusValuePerDriver) CalculateAverageHourlyGrossProfitLastHour()
    {
        float currentTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float intervalStartTime = Math.Max(currentTime - 1, 0);
        float totalGrossProfit = 0;
        float totalSurplusValue = 0;
        float totalDriverTime = 0;
        int numDriversCurrentlyDriving = 0;
        int numDriversCurrentlyDriving2 = 0;
        int numDriversWhoStoppedDrivingDuringInterval = 0;

        foreach (DriverPerson driver in drivers)
        {
            if (driver.interval == null)
            {
                continue;
            }

            bool driverIntervalCrossesMidnight = driver.interval.endTime > 24;
            float actualStartTime = driverIntervalCrossesMidnight && currentTime < driver.interval.startTime ? 0 : driver.interval.startTime;
            float actualIntendedEndTime = actualStartTime < driver.interval.startTime ? driver.interval.endTime - 24 : driver.interval.endTime;
            bool hasEndedSession = driver.actualSessionEndTimes.Count > 0 && driver.actualSessionEndTimes[0] < currentTime && driver.actualSessionEndTimes[0] > actualStartTime;
            bool isCurrentlyDriving = actualStartTime <= currentTime && !hasEndedSession;
            bool wasDrivingDuringInterval = driver.actualSessionEndTimes.Count > 0 && driver.actualSessionEndTimes[0] > intervalStartTime;

            if (driver.isCurrentlyDriving != isCurrentlyDriving)
            {
                Debug.Log($"Driver {driver.interval.startTime} isCurrentlyDriving: {driver.isCurrentlyDriving} isCurrentlyDriving2: {isCurrentlyDriving}");
            }
            if (driver.isCurrentlyDriving)
            {
                numDriversCurrentlyDriving2 += 1;
            }
            if (isCurrentlyDriving)
            {
                numDriversCurrentlyDriving += 1;
            }
            else if (wasDrivingDuringInterval)
            {
                numDriversWhoStoppedDrivingDuringInterval += 1;
            }
            else
            {
                continue;
            }

            float driverGrossProfit = 0;
            foreach (Trip trip in driver.completedTrips)
            {
                if (trip.droppedOffData.droppedOffTime > intervalStartTime)
                {
                    driverGrossProfit += trip.droppedOffDriverData.grossProfit;
                }
            }
            totalGrossProfit += driverGrossProfit;
            if (isCurrentlyDriving)
            {
                float timeDrivenThisInterval = currentTime - Math.Max(intervalStartTime, actualStartTime);
                // Debug.Log($"Driver has driven {timeDrivenThisInterval} hours this interval");
                totalDriverTime += timeDrivenThisInterval;

                float[] opportunityCostPerHour = driver.GetOpportunityCostPerHour();
                int hour = Mathf.FloorToInt(currentTime);
                int lastHour = hour - 1;
                float timeDrivenThisHour = Math.Min(timeDrivenThisInterval, currentTime - hour);
                float timeDrivenLastHour = timeDrivenThisInterval - timeDrivenThisHour;
                float opportunityCostLastHour = lastHour >= 0 ? opportunityCostPerHour[lastHour] * timeDrivenLastHour : 0;
                float opportunityCostThisHour = opportunityCostPerHour[hour] * timeDrivenThisHour;
                float opportunityCost = opportunityCostLastHour + opportunityCostThisHour;
                // totalOpportunityCost += opportunityCost;
                totalSurplusValue += driverGrossProfit - opportunityCost;
            }
            else if (wasDrivingDuringInterval)
            {
                float timeDrivenThisInterval = (float)driver.actualSessionEndTimes[0] - Math.Max(intervalStartTime, actualStartTime);
                totalDriverTime += timeDrivenThisInterval;
            }
            else
            {
                Debug.Log("This should never happen");
            }


        }

        Debug.Log($"Total active drivers: {numDriversCurrentlyDriving}, Drivers who stopped during interval: {numDriversWhoStoppedDrivingDuringInterval} Total gross profit: {totalGrossProfit}, total driver time: {totalDriverTime}");
        Debug.Log($"Total active drivers2: {numDriversCurrentlyDriving2}");
        float averageGrossProfitPerHour = totalGrossProfit / totalDriverTime;
        float averageSurplusValuePerHour = totalSurplusValue / totalDriverTime;
        return (averageGrossProfitPerHour, averageSurplusValuePerHour);
    }

    public static (float[] expectedAverageGrossProfitByHour, float[] expectedAverageSurplusValueByHour) CalculateExpectedAverageProfitabilityByHour()
    {
        float[] expectedAverageGrossProfitByHour = new float[24];
        float[] expectedAverageSurplusValueByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float totalExpectedGrossProfit = 0;
            float totalExpectedSurplusValue = 0;
            int numDrivers = 0;
            foreach (DriverPerson driver in drivers)
            {
                if (driver.interval == null)
                {
                    continue;
                }
                bool driverIntervalCrossesMidnight = driver.interval.endTime > 24;

                float actualStartTime = driverIntervalCrossesMidnight && i < driver.interval.startTime ? 0 : driver.interval.startTime;
                float actualIntendedEndTime = actualStartTime < driver.interval.startTime ? driver.interval.endTime - 24 : driver.interval.endTime;

                bool isCurrentHourInInterval = actualStartTime <= i && i < actualIntendedEndTime;
                if (!isCurrentHourInInterval)
                {
                    continue;
                }
                totalExpectedGrossProfit += driver.expectedGrossProfitByHour[i];
                totalExpectedSurplusValue += driver.expectedSurplusValueByHour[i];
                numDrivers += 1;
            }

            if (numDrivers > 0)
            {
                expectedAverageGrossProfitByHour[i] = totalExpectedGrossProfit / numDrivers;
                expectedAverageSurplusValueByHour[i] = totalExpectedSurplusValue / numDrivers;
            }
        }

        return (expectedAverageGrossProfitByHour, expectedAverageSurplusValueByHour);

    }

    public static void CreateDriverPool()
    {
        if (SimulationSettings.useConstantSupplyMode)
        {
            CreateConstantSupplyDriverPool();
        }
        else
        {
            CreateDynamicSupplyDriverPool();
        }
    }

    private static void CreateConstantSupplyDriverPool()
    {
        float[] tripCapacityByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            tripCapacityByHour[i] = SimulationSettings.numDrivers * SimulationSettings.driverAverageTripsPerHour;
        }
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            // Since constant supply mode doesn't realistically simulate driver preferences, we'll just use the same values for all drivers
            float baseOpportunityCostPerHour = SimulationSettings.passengerMedianIncome / 2;
            float[] opportunityCostProfile = (float[])normalDriverProfile.Clone();
            int preferredSessionLength = 24;
            SessionInterval interval = new SessionInterval()
            {
                startTime = 0,
                endTime = 24,
            };
            DriverPerson driver = new DriverPerson()
            {
                opportunityCostProfile = opportunityCostProfile,
                baseOpportunityCostPerHour = baseOpportunityCostPerHour,
                preferredSessionLength = preferredSessionLength,
                interval = interval,
            };

            (float[] expectedSurplusValueByHour, float[] expectedGrossProfitByHour) = GetExpectedSessionProfitabilityByHour(driver, tripCapacityByHour);
            driver.expectedSurplusValueByHour = expectedSurplusValueByHour;
            driver.expectedGrossProfitByHour = expectedGrossProfitByHour;
            (float expectedGrossProfit, float expectedSurplusValue) = GetExpectedSessionProfitability(interval, expectedGrossProfitByHour, expectedSurplusValueByHour);
            driver.expectedSurplusValue = expectedSurplusValue;
            driver.expectedGrossProfit = expectedGrossProfit;
            drivers[i] = driver;

        }
    }

    private static void CreateDynamicSupplyDriverPool()
    {

        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            float baseOpportunityCostPerHour = SimulationSettings.GetRandomHourlyIncome(GameManager.Instance.driverSpawnRandom);
            while (baseOpportunityCostPerHour > 20)
            {
                baseOpportunityCostPerHour = SimulationSettings.GetRandomHourlyIncome(GameManager.Instance.driverSpawnRandom);
            }

            int preferredSessionLength = random.Next(SimulationSettings.sessionLengthRange.start, SimulationSettings.sessionLengthRange.end);
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
            intervals[driverIndex] = null;
            float[] tripCapacityByHour = GetTripCapacityByHour(intervals);
            (SessionInterval interval, float surplusValue) = CalculateMostProfitableSession(driver, tripCapacityByHour);
            intervals[driverIndex] = interval;
            surplusValues[driverIndex] = surplusValue;
        }

        float[] tripCapacityByHourFinal = GetTripCapacityByHour(intervals);
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            DriverPerson driver = drivers[i];

            (float[] expectedSurplusValueByHour, float[] expectedGrossProfitByHour) = GetExpectedSessionProfitabilityByHour(driver, tripCapacityByHourFinal);
            SessionInterval interval = intervals[i];
            driver.interval = interval;
            driver.expectedSurplusValueByHour = expectedSurplusValueByHour;
            driver.expectedGrossProfitByHour = expectedGrossProfitByHour;
            if (interval != null)
            {
                (float expectedGrossProfit, float expectedSurplusValue) = GetExpectedSessionProfitability(interval, expectedGrossProfitByHour, expectedSurplusValueByHour);
                driver.expectedSurplusValue = expectedSurplusValue;
                driver.expectedGrossProfit = expectedGrossProfit;
            }
        }

        int numDriversWithSessions = intervals.Where(x => x != null).Count();
        Debug.Log($"Number of drivers with intervals: {numDriversWithSessions} out of {SimulationSettings.numDrivers} drivers");

        Debug.Log("Surplus values:");
        Debug.Log(string.Join(", ", surplusValues.Select(x => x.ToString()).ToArray()));

        float[] secondPassTripCapacityByHour = GetTripCapacityByHour(intervals);

        // Debug.Log("Second pass Trip capacity:");
        // Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select(x => x.ToString()).ToArray()));

        // Debug.Log("Expected passengers by hour:");
        // Debug.Log(string.Join(", ", SimulationSettings.expectedPassengersByHour.Select(x => x.ToString()).ToArray()));

        Debug.Log("Expected supply demand imbalance per hour:");
        Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select((x, i) => (x - (SimulationSettings.expectedPassengersByHour[i] + SimulationSettings.expectedPassengersByHour[(i + 1) % 24]) / 2).ToString()).ToArray()));
        Debug.Log(string.Join(", ", secondPassTripCapacityByHour.Select((x, i) =>
            {
                float numPassengers = (SimulationSettings.expectedPassengersByHour[i] + SimulationSettings.expectedPassengersByHour[(i + 1) % 24]) / 2;
                return String.Format("{0:P2}", (x - numPassengers) / numPassengers);
            }
        ).ToArray()));
    }

    private static (float expectedGrossProfit, float expectedSurplusValue) GetExpectedSessionProfitability(SessionInterval interval, float[] expectedGrossProfitByHour, float[] expectedSurplusValueByHour)
    {
        float expectedGrossProfit = 0;
        float expectedSurplusValue = 0;
        for (int i = interval.startTime; i < interval.endTime; i++)
        {
            int actualTime = i % 24;
            float expectedGrossProfitForHour = expectedGrossProfitByHour[actualTime];
            float expectedSurplusValueForHour = expectedSurplusValueByHour[actualTime];
            expectedGrossProfit += expectedGrossProfitForHour;
            expectedSurplusValue += expectedSurplusValueForHour;
        }
        return (expectedGrossProfit, expectedSurplusValue);
    }

    public static float[] GetTripCapacityByHour(SessionInterval[] intervals)
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

    private static (float[] surplusValueByHour, float[] grossProfitByHour) GetExpectedSessionProfitabilityByHour(DriverPerson driver, float[] tripCapacityByHour)
    {
        float[] expectedGrossProfitByHour = CalculateExpectedGrossProfitByHour(tripCapacityByHour, false);
        float[] expectedSurplusValueByHour = CalculateExpectedSurplusValueByHour(driver, expectedGrossProfitByHour);

        return (expectedSurplusValueByHour, expectedGrossProfitByHour);
    }


    private static (SessionInterval interval, float surplusValue) CalculateMostProfitableSession(DriverPerson driver, float[] tripCapacityByHour)
    {

        float[] expectedGrossProfitByHour = CalculateExpectedGrossProfitByHour(tripCapacityByHour, true);
        float[] expectedSurplusValueByHour = CalculateExpectedSurplusValueByHour(driver, expectedGrossProfitByHour);

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


    private static float[] CalculateExpectedGrossProfitByHour(float[] tripCapacityByHour, bool shouldAddDriverCapacity)
    {
        float[] expectedGrossProfitByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float tripCapacityIncludingDriver = shouldAddDriverCapacity ? tripCapacityByHour[i] + 1 * SimulationSettings.driverAverageTripsPerHour : tripCapacityByHour[i];
            expectedGrossProfitByHour[i] = CalculateExpectedGrossProfitForOneHourOfWork(i, tripCapacityIncludingDriver);
        }
        return expectedGrossProfitByHour;
    }

    private static float[] CalculateExpectedSurplusValueByHour(DriverPerson driver, float[] expectedGrossProfitByHour)
    {
        float[] expectedSurplusValueByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float opportunityCostIndex = driver.opportunityCostProfile[i];
            expectedSurplusValueByHour[i] = expectedGrossProfitByHour[i] - (opportunityCostIndex * driver.baseOpportunityCostPerHour);
        }

        return expectedSurplusValueByHour;
    }

    private static float CalculateExpectedGrossProfitForOneHourOfWork(int hourOfTheDay, float expectedTripCapacityIncludingDriver)
    {
        float driverSpeed = SimulationSettings.driverSpeed;
        // TODO: FIX ME - actually calculate the expected surge multiplier to get a realistic perKmFare - this is currenly incorrect!
        float surgeMultiplier = 1f;
        float perKmFare = SimulationSettings.baseFarePerKm * surgeMultiplier;
        float driverFareCutPercentage = SimulationSettings.driverFareCutPercentage;
        float marginalCostPerKm = SimulationSettings.driverMarginalCostPerKm;

        // Theoretical earnings ceiling per hour, assuming that the driver is always driving a passenger or on the way to a passenger who is on average startingBaseFare/baseFarePerKm kms away
        float maxGrossProfitPerHour = driverSpeed * (perKmFare * driverFareCutPercentage - marginalCostPerKm);

        float expectedNumPassengers = (SimulationSettings.expectedPassengersByHour[hourOfTheDay % 24] + SimulationSettings.expectedPassengersByHour[(hourOfTheDay + 1) % 24]) / 2;

        // TODO: Create a method to get the estimated percentage of passengers who are willing to pay the fare

        // For now we assume that the driver can drive passengers at full capacity if there are 1.3x more passengers than driver trip capacity
        float expectedGrossProfit = maxGrossProfitPerHour * Mathf.Min(expectedNumPassengers / (expectedTripCapacityIncludingDriver * 1.3f), 1);
        return expectedGrossProfit;
    }
}
