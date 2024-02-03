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

    // This value is supposed to be directly comparable to the demand index. If the demand index is 1 and the supply index is 2, then the expected number of passengers is 2x the expected number of maximum trip capacity for that hour
    public static readonly Dictionary<int, float> firstEstimationOfSupplyIndexByHour = new Dictionary<int, float>()
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

    // The following variables are approximations of in-simulation values that will change over time - in the future they should be regularly updated
    public const float driverAverageTripsPerHour = 2.85f;

    public const int numDrivers = 50;


    // Passenger economic parameters
    public const float passengerMedianIncome = 20;
    public const float passengerIncomeSigma = 0.9f;

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

    public const float demandIndexMultiplier = 15;

    // Computed values
    public static float[] expectedPassengersByHour = GetExpectedPassengersByHour();



    private static float[] GetExpectedPassengersByHour()
    {
        float[] expectedPassengersByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            expectedPassengersByHour[i] = demandIndexByHour[i] * demandIndexMultiplier;
        }
        return expectedPassengersByHour;
    }

    public static float[] GetFirstGuessTripCapacityByHour()
    {
        float[] expectedTripCapacityByHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float expectedTripCapacity = firstEstimationOfSupplyIndexByHour[i] * demandIndexMultiplier;
            expectedTripCapacityByHour[i] = expectedTripCapacity;
        }
        return expectedTripCapacityByHour;
    }

    public static float GetRandomHourlyIncome()
    {
        // When agents income approach zero, their time becomes completely worthless which is not realistic, so we set a minimum income of 4$/hr
        // A log-normal distribution with mu=0, sigma=0.9, medianIncome=17. This a distribution with mean=27, median=20 and 2.3% of the population with income > 100
        float minIncome = 4f;
        float mu = 0;
        float hourlyIncome = minIncome + (passengerMedianIncome - minIncome) * StatisticsUtils.getRandomFromLogNormalDistribution(mu, passengerIncomeSigma);

        return hourlyIncome;
    }

}
