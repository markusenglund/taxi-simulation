using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DriverPersonality
{
    public float[] baseOpportunityCostIndexByHour { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public float preferredSessionLength { get; set; }
}

public class DriverSession
{
    public int startTime { get; set; }
    public int endTime { get; set; }
    public float expectedSurplusValue { get; set; }
}

public class DriverPool : MonoBehaviour
{
    public static DriverPool Instance { get; private set; }

    public List<DriverPersonality> drivers = new List<DriverPersonality>();

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    const float averageOpportunityCostPerHour = 9f;
    const float opportunityCostStd = 2f;

    Dictionary<int, float> supplyIndexByHour = new Dictionary<int, float>()
    {
        { 0, 5f },
        { 1, 3f },
        { 2, 2f },
        { 3, 1f },
        { 4, 1f },
        { 5, 1.5f },
        { 6, 2f },
        { 7, 3.5f },
        { 8, 5f },
        { 9, 6f },
        { 10, 6f },
        { 11, 6f },
        { 12, 6f},
        { 13, 6f},
        { 14, 6.5f},
        { 15, 7f},
        { 16, 9f},
        { 17, 11f},
        { 18, 13f},
        { 19, 12f},
        { 20, 12f},
        { 21, 12f},
        { 22, 13f},
        { 23, 14f},
        { 24, 12f}

    };

    void Awake()
    {
        Instance = this;
    }


    private void CreateDriverPool()
    {
        float[] officeHoursOpportunityCostProfile = new float[24] { 5f, 3f, 2f, 1f, 1f, 1.5f, 2f, 3.5f, 5f, 6f, 6f, 6f, 6f, 6f, 6.5f, 7f, 9f, 11f, 13f, 12f, 12f, 12f, 13f, 14f };
        for (int i = 0; i < 10; i++)
        {
            // Capped normally distributed between 5 and 13
            float baseOpportunityCostPerHour = StatisticsUtils.GetRandomFromNormalDistribution(averageOpportunityCostPerHour, opportunityCostStd, averageOpportunityCostPerHour - 2 * opportunityCostStd, averageOpportunityCostPerHour + 2 * opportunityCostStd);

            float preferredSessionLength = UnityEngine.Random.Range(3f, 12f);

            // TODO: Figure out opportunity cost index by hour - we probably want a few archetypes of drivers, some that work only during rush hour, some that work only during the day, some that work only at night, etc.
        }
    }

    DriverSession CalculateMostProfitableSession(float[] opportunityCostProfile, float baseOpportunityCostPerHour, int preferredSessionLength)
    {
        float[] expectedGrossProfitByHour = CalculateExpectedGrossProfitByHour();
        float[] expectedSurplusValueByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float opportunityCostIndex = opportunityCostProfile[i];
            expectedSurplusValueByHour[i] = expectedGrossProfitByHour[i] - (opportunityCostIndex * baseOpportunityCostPerHour);
        }

        DriverSession mostProfitableSession = new DriverSession()
        {
            startTime = 0,
            endTime = 0,
            expectedSurplusValue = Mathf.NegativeInfinity
        };
        // TODO: START HERE - Simplify this into one single function that just checks every single possible session length, it's basically what we're doing anyway.
        for (int i = -3; i <= 3; i++)
        {
            DriverSession currentSession = CalculateMostProfitableSessionOfLength(preferredSessionLength + i, expectedSurplusValueByHour);
            int deviation = Math.Abs(i);
            float deviationCost = baseOpportunityCostPerHour * deviation * (deviation + 1) / 4;
            float currentSessionSurplusValue = currentSession.expectedSurplusValue - deviationCost;
            if (currentSessionSurplusValue > mostProfitableSession.expectedSurplusValue)
            {
                mostProfitableSession = currentSession;
                // Ugly, fix!
                mostProfitableSession.expectedSurplusValue = currentSessionSurplusValue;
            }
        }
        return mostProfitableSession;
    }

    DriverSession CalculateMostProfitableSessionOfLength(int hours, float[] expectedSurplusValueByHour)
    {
        DriverSession mostProfitableSession = new DriverSession()
        {
            startTime = 0,
            endTime = 0,
            expectedSurplusValue = Mathf.NegativeInfinity
        };
        float currentSessionSurplusSum = 0;
        for (int i = 0; i < hours; i++)
        {
            currentSessionSurplusSum += expectedSurplusValueByHour[i];
        }
        for (int i = 0; i < 24; i++)
        {
            int leadingIndex = (i + hours) % 24;
            currentSessionSurplusSum += expectedSurplusValueByHour[leadingIndex];
            currentSessionSurplusSum -= expectedSurplusValueByHour[i];
            if (currentSessionSurplusSum > mostProfitableSession.expectedSurplusValue)
            {
                mostProfitableSession = new DriverSession()
                {
                    startTime = i,
                    endTime = i + hours,
                    expectedSurplusValue = currentSessionSurplusSum
                };
            }
        }
        return mostProfitableSession;
    }


    private float[] CalculateExpectedGrossProfitByHour()
    {
        float[] expectedGrossProfitByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            expectedGrossProfitByHour[i] = CalculateExpectedGrossProfitForOneHourOfWork(i);
        }
        return expectedGrossProfitByHour;
    }

    private float CalculateExpectedGrossProfitForOneHourOfWork(int hourOfTheDay)
    {

        float driverSpeed = SimulationSettings.driverSpeed;
        float perKmFare = SimulationSettings.baseFarePerKm * SimulationSettings.surgeMultiplier;
        float driverFareCutPercentage = SimulationSettings.driverFareCutPercentage;
        float marginalCostPerKm = SimulationSettings.driverMarginalCostPerKm;

        // Theoretical earnings ceiling per hour, assuming that the driver is always driving a passenger or on the way to a passenger who is on average startingBaseFare/baseFarePerKm kms away
        float maxGrossProfitPerHour = driverSpeed * (perKmFare * driverFareCutPercentage - marginalCostPerKm);

        // TODO: Supply index will be a dynamic variable based on a slot allocation algorithm, that uses this method. So this will have to change. Also, we should include the driver's added capacity in the demand index. Or should we?
        float supplyIndex = (supplyIndexByHour[hourOfTheDay] + supplyIndexByHour[hourOfTheDay + 1]) / 2;
        float demandIndex = (SimulationSettings.demandIndexByHour[hourOfTheDay] + SimulationSettings.demandIndexByHour[hourOfTheDay + 1]) / 2;

        // TODO: Create a method to get the estimated percentage of passengers who are willing to pay the fare

        // For now we assume that the driver can drive passengers at full capacity if there are 1.3x more passengers than driver trip capacity
        float expectedGrossProfit = maxGrossProfitPerHour * Mathf.Min(demandIndex / (supplyIndex * 1.3f), 1);
        return expectedGrossProfit;
    }
}
