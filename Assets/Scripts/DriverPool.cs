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

        float[] medianProfile = new float[24];
        for (int i = 0; i < 24; i++)
        {
            // Sort the opportunity cost profiles by hour and take the median
            float[] opportunityCostsByHour = new float[6] { workLifeBalanceProfile[i], profitMaximizerProfile[i], earlyBirdProfile[i], lateSleeperProfile[i], worksTwoJobsProfile[i], normalDriverProfile[i] };
            Array.Sort(opportunityCostsByHour);
            medianProfile[i] = opportunityCostsByHour[2];
        }

        float[][] opportunityCostProfiles = new float[6][] { workLifeBalanceProfile, profitMaximizerProfile, earlyBirdProfile, lateSleeperProfile, worksTwoJobsProfile, normalDriverProfile };
        DriverSession[] sessions = new DriverSession[10];
        for (int i = 0; i < 10; i++)
        {
            // Capped normally distributed between 5 and 13
            float baseOpportunityCostPerHour = StatisticsUtils.GetRandomFromNormalDistribution(averageOpportunityCostPerHour, opportunityCostStd, averageOpportunityCostPerHour - 2 * opportunityCostStd, averageOpportunityCostPerHour + 2 * opportunityCostStd);

            float preferredSessionLength = UnityEngine.Random.Range(3f, 12f);
            float[] opportunityCostProfile = opportunityCostProfiles[i % 5];
            DriverSession session = CalculateMostProfitableSession(opportunityCostProfile, baseOpportunityCostPerHour, (int)preferredSessionLength);
            sessions[i] = session;
        }
        Debug.Log("Sessions:");
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
        };
        float maxSurplusValue = Mathf.NegativeInfinity;
        for (int i = 1; i < 24; i++)
        {
            (DriverSession session, float expectedUtilityValue) = CalculateMostProfitableSessionOfLength(i, preferredSessionLength, expectedSurplusValueByHour, baseOpportunityCostPerHour);
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
