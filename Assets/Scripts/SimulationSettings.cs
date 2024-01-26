using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SimulationSettings
{
    // 30km/hr is a reasonable average speed for a taxi in an urban area (including stopping at traffic lights etc)
    // Real data from Atlanta: https://www.researchgate.net/figure/Average-speed-in-miles-per-hour-for-rural-and-urban-roads_tbl3_238594974
    public const float driverSpeed = 30f;
    public const float timeSpentWaitingForPassenger = 1f / 60f;
    // Fare values were empirically chosen to approximate the fare for a getting a ride in Utrecht
    // In the "Who benefits?" paper $3.30 + $0.87 ⇥ (predicted miles) + $0.11 ⇥ (predicted minutes) was the formula used which is a bit less than the values below
    public const float baseStartingFare = 4f;
    public const float baseFarePerKm = 1.5f;
    public const float surgeMultiplier = 1f;
    // Cut percentages are not public for Uber but hover around 33% for Lyft according to both official statements and third-party analysis https://therideshareguy.com/how-much-is-lyft-really-taking-from-your-pay/
    public const float driverFareCutPercentage = 0.67f;
    public const float uberFareCutPercentage = 0.33f;

    // Driver economic parameters

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    public const float driverAverageOpportunityCostPerHour = 9f;
    // Marginal costs include fuel + the part of maintenance, repairs, and depreciation that is proportional to the distance driven, estimated at $0.21 per mile = $0.13 per km
    public const float driverMarginalCostPerKm = 0.13f;
    public const float driverFixedCostsPerDay = 5f;


    // Passenger economic parameters
    // A log-normal distribution with mu=0, sigma=0.7, medianIncome=20. This a distribution with mean=25.6, median=20 and 1.1% of the population with income > 100
    public const float passengerMedianIncome = 20;
    public const float passengerIncomeSigma = 0.7f;

    // Based on real friday data, demand is indexed by as 1 being the lowest measured number
    public static readonly Dictionary<int, float> demandIndexByHour = new Dictionary<int, float>()
    // TODO: We should add some data for start of Saturday to help drivers make an informed decision when they pick a session
    {
        { 0, 5f },
        { 1, 3f },
        { 2, 2f },
        { 3, 1f },
        { 4, 1f },
        { 5, 1.5f },
        { 6, 2f },
        { 7, 3f },
        { 8, 4f },
        { 9, 5f },
        { 10, 5f },
        { 11, 5f },
        { 12, 5f},
        { 13, 5.5f},
        { 14, 6f},
        { 15, 7f},
        { 16, 9f},
        { 17, 11f},
        { 18, 13f},
        { 19, 12f},
        { 20, 12f},
        { 21, 13f},
        { 22, 14f},
        { 23, 16f},
        { 24, 12f}
    };

    public const float demandIndexMultiplier = 5;

    public static List<float> GetExpectedPassengersByHour()
    {
        List<float> expectedPassengersByHour = new List<float>();
        for (int i = 0; i < 24; i++)
        {
            expectedPassengersByHour.Add(demandIndexByHour[i] * demandIndexMultiplier);
        }
        return expectedPassengersByHour;
    }


}
