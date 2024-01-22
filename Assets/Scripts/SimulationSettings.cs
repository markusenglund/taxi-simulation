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
    // $3.30 + $0.87 ⇥ (predicted miles) + $0.11 ⇥ (predicted minutes) was the formula used in the "Who benefits?" paper, which is a bit less than the formula below
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



}
