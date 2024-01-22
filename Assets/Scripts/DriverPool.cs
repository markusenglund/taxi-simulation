using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DriverPersonality
{
    public float[] baseOpportunityCostIndexByHour { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public float preferredSessionLength { get; set; }
}

public class DriverSession
{
    public float startTime { get; set; }
    public float endTime { get; set; }
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

            float preferredSessionLength = Random.Range(3f, 12f);

            // TODO: Figure out opportunity cost index by hour - we probably want a few archetypes of drivers, some that work only during rush hour, some that work only during the day, some that work only at night, etc.
        }
    }

    DriverSession CalculateMostProfitableSession(float baseOpportunityCostIndexByHour, float baseOpportunityCostPerHour, float preferredSessionLength)
    {
        float sessionValue = 0;
        return sessionValue;
    }

    float CalculateExpectedGrossProfitForOneHourOfWork(float startTime)
    {
        // We need the following information:
        // The average fare in the hour
        // Total ride capacity in the hour (supply)
        // Personal ride capacity
        // Number of passengers in the hour who are willing to pay the fare (demand)
        // Max gross profit = 30km/hr * 1hr * 2f(per km fare) * surgeMultiplier * 0.67 (drivers cut) - 30km * 0.13f
        // Then the expected gross profit is = maxGrossProfit * Min(demand / supply (including new supply), 1)
    }
}
