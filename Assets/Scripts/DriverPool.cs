using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DriverPersonality
{
    public float[] opportunityCostProfile { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public int preferredSessionLength { get; set; }
}

public class DriverSession
{
    public int startTime { get; set; }
    public int endTime { get; set; }
}

public class DriverPool : MonoBehaviour
{
    public static DriverPool Instance { get; private set; }

    public List<DriverPersonality> drivers = new List<DriverPersonality>();

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    const float averageOpportunityCostPerHour = 9f;
    const float opportunityCostStd = 2f;



    void Awake()
    {
        Instance = this;
    }


    public void CreateDriverPool()
    {
        // Profile of a person who strongly prefers working 8-5
        float[] workLifeBalanceProfile = new float[24] { 4, 5, 5, 4, 3, 1.8f, 1.3f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.2f, 1.5f, 2, 3, 4, 4, 4 };
        // Profile of a person who will work at any time
        float[] profitMaximizerProfile = new float[24] { 1.3f, 1.3f, 1.3f, 1.3f, 1.3f, 1.3f, 1.1f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Profile of a person who slightly flexible but prefers working early mornings
        float[] earlyBirdProfile = new float[24] { 3, 4, 4, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.5f, 2, 2, 2, 2, 2, 3 };
        // Profile of a person who has built his life around driving at peak late night earning hours
        float[] lateSleeperProfile = new float[24] { 1.2f, 1.2f, 1.3f, 1.5f, 3, 4, 4, 4, 4, 4, 4, 2, 1.5f, 1.2f, 1.1f, 1, 1, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Profile of a person who is busy during 9-5, and will work only in the evenings
        float[] worksTwoJobsProfile = new float[24] { 1.3f, 1.5f, 2, 3, 4, 5, 5, 5, 5, 10, 10, 10, 10, 10, 10, 10, 10, 1, 1, 1, 1.1f, 1.1f, 1.2f, 1.2f };
        // Typical driver profile
        float[] normalDriverProfile = new float[24]
        { 1.3f, 1.5f, 1.8f, 2, 2, 1.5f, 1.2f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.1f, 1.2f, 1.2f, 1.2f, 1.2f};

        // float[] medianProfile = new float[24];
        // for (int i = 0; i < 24; i++)
        // {
        //     // Sort the opportunity cost profiles by hour and take the median
        //     float[] opportunityCostsByHour = new float[6] { workLifeBalanceProfile[i], profitMaximizerProfile[i], earlyBirdProfile[i], lateSleeperProfile[i], worksTwoJobsProfile[i], normalDriverProfile[i] };
        //     Array.Sort(opportunityCostsByHour);
        //     medianProfile[i] = opportunityCostsByHour[2];
        // }

        float[][] opportunityCostProfiles = new float[6][] { workLifeBalanceProfile, profitMaximizerProfile, earlyBirdProfile, lateSleeperProfile, worksTwoJobsProfile, normalDriverProfile };
        DriverPersonality[] drivers = new DriverPersonality[SimulationSettings.numDrivers];
        DriverSession[] sessions = new DriverSession[SimulationSettings.numDrivers];
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            // Capped normally distributed between 5 and 13
            float baseOpportunityCostPerHour = StatisticsUtils.GetRandomFromNormalDistribution(averageOpportunityCostPerHour, opportunityCostStd, averageOpportunityCostPerHour - 2 * opportunityCostStd, averageOpportunityCostPerHour + 2 * opportunityCostStd);

            int preferredSessionLength = UnityEngine.Random.Range(3, 12);
            float[] opportunityCostProfile = opportunityCostProfiles[i % opportunityCostProfiles.Length];
            DriverPersonality driver = new DriverPersonality()
            {
                opportunityCostProfile = opportunityCostProfile,
                baseOpportunityCostPerHour = baseOpportunityCostPerHour,
                preferredSessionLength = preferredSessionLength,
            };
            drivers[i] = driver;

        }

        // First pass at creating driver profiles
        List<float> firstGuessTripCapacityByHour = SimulationSettings.GetFirstGuessTripCapacityByHour();
        for (int i = 0; i < SimulationSettings.numDrivers; i++)
        {
            DriverPersonality driver = drivers[i];
            DriverSession session = CalculateMostProfitableSession(driver, firstGuessTripCapacityByHour);
            sessions[i] = session;
        }
        Debug.Log("Sessions:");
    }

    DriverSession CalculateMostProfitableSession(DriverPersonality driver, List<float> tripCapacityByHour)
    {

        float[] expectedGrossProfitByHour = CalculateExpectedGrossProfitByHour(tripCapacityByHour);
        float[] expectedSurplusValueByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float opportunityCostIndex = driver.opportunityCostProfile[i];
            expectedSurplusValueByHour[i] = expectedGrossProfitByHour[i] - (opportunityCostIndex * driver.baseOpportunityCostPerHour);
        }

        DriverSession mostProfitableSession = new DriverSession()
        {
            startTime = 0,
            endTime = 0,
        };
        float maxSurplusValue = Mathf.NegativeInfinity;
        for (int i = 1; i < 24; i++)
        {
            (DriverSession session, float expectedUtilityValue) = CalculateMostProfitableSessionOfLength(i, driver.preferredSessionLength, expectedSurplusValueByHour, driver.baseOpportunityCostPerHour);
            if (expectedUtilityValue > maxSurplusValue)
            {
                mostProfitableSession = session;
                maxSurplusValue = expectedUtilityValue;
            }
        }
        return mostProfitableSession;
    }

    (DriverSession session, float expectedUtilityValue) CalculateMostProfitableSessionOfLength(int sessionLength, int preferredSessionLength, float[] expectedSurplusValueByHour, float baseOpportunityCostPerHour)
    {
        DriverSession mostProfitableSession = new DriverSession()
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
                mostProfitableSession = new DriverSession()
                {
                    startTime = i,
                    endTime = i + sessionLength,
                };
                maxSurplusSum = currentSessionSurplusSum;
            }
        }

        int deviationFromPreferredLength = Math.Abs(sessionLength - preferredSessionLength);
        // Cost of deviatiating one additional hour, is the size of the deviation * baseOpportunityCostPerHour / 2
        float deviationCost = baseOpportunityCostPerHour * deviationFromPreferredLength * (deviationFromPreferredLength + 1) / 4;
        float currentSessionSurplusValue = maxSurplusSum - deviationCost;
        return (mostProfitableSession, currentSessionSurplusValue);
    }


    private float[] CalculateExpectedGrossProfitByHour(List<float> tripCapacityByHour)
    {
        float[] expectedGrossProfitByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            expectedGrossProfitByHour[i] = CalculateExpectedGrossProfitForOneHourOfWork(i, tripCapacityByHour[i]);
        }
        return expectedGrossProfitByHour;
    }

    private float CalculateExpectedGrossProfitForOneHourOfWork(int hourOfTheDay, float expectedTripCapacity)
    {
        float driverSpeed = SimulationSettings.driverSpeed;
        float perKmFare = SimulationSettings.baseFarePerKm * SimulationSettings.surgeMultiplier;
        float driverFareCutPercentage = SimulationSettings.driverFareCutPercentage;
        float marginalCostPerKm = SimulationSettings.driverMarginalCostPerKm;

        // Theoretical earnings ceiling per hour, assuming that the driver is always driving a passenger or on the way to a passenger who is on average startingBaseFare/baseFarePerKm kms away
        float maxGrossProfitPerHour = driverSpeed * (perKmFare * driverFareCutPercentage - marginalCostPerKm);

        float expectedNumPassengers = (SimulationSettings.expectedPassengersByHour[hourOfTheDay] + SimulationSettings.expectedPassengersByHour[hourOfTheDay + 1]) / 2;

        float expectedTripCapacityIncludingDriver = expectedTripCapacity + 1 * SimulationSettings.driverAverageTripsPerHour;

        // TODO: Create a method to get the estimated percentage of passengers who are willing to pay the fare

        // For now we assume that the driver can drive passengers at full capacity if there are 1.3x more passengers than driver trip capacity
        float expectedGrossProfit = maxGrossProfitPerHour * Mathf.Min(expectedNumPassengers / (expectedTripCapacityIncludingDriver * 1.3f), 1);
        return expectedGrossProfit;
    }
}
