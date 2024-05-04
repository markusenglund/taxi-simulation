using System;
using UnityEngine;
using Random = System.Random;
using System.Linq;
using System.Collections.Generic;

public enum PassengerState
{
    BeforeSpawn,
    Idling,
    AssignedToTrip,
    DroppedOff,
    RejectedRideOffer,
    NoRideOffer,
}

public enum TripType
{
    None,
    Walking,
    PublicTransport,
    RentalCar,
    SkipTrip,
    Uber
}

[Serializable]
public class Substitute
{
    [field: SerializeField] public TripType type { get; set; }
    [field: SerializeField] public float timeCost { get; set; }
    [field: SerializeField] public float moneyCost { get; set; }
    [field: SerializeField] public float totalCost { get; set; }
    [field: SerializeField] public float netValue { get; set; }
    [field: SerializeField] public float netUtility { get; set; }
}

[Serializable]
public class PassengerEconomicParameters
{
    // Base values
    [field: SerializeField] public float hourlyIncome { get; set; }
    [field: SerializeField] public float tripUtilityScore { get; set; }
    [field: SerializeField] public float timePreference { get; set; }

    // Derived values
    [field: SerializeField] public float waitingCostPerHour { get; set; }
    [field: SerializeField] public float tripUtilityValue { get; set; }

    [field: SerializeField] public Substitute bestSubstitute { get; set; }
}


public class PassengerPerson
{
    static int incrementalId = 1;
    public int id { get; set; }
    public float timeSpawned { get; set; }
    public PassengerEconomicParameters economicParameters { get; set; }
    public Trip trip { get; set; }

    public TripType tripTypeChosen { get; set; }
    public PassengerState state { get; set; }

    private Random random;

    private SimulationSettings simSettings;

    public Vector3 destination { get; set; }
    public Vector3 startPosition { get; set; }

    public PassengerPerson(Vector3 startPosition, SimulationSettings simSettings, Random random)
    {
        id = incrementalId;
        incrementalId++;
        this.random = random;
        this.simSettings = simSettings;
        this.startPosition = startPosition;
        timeSpawned = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        state = PassengerState.BeforeSpawn;
        destination = GridUtils.GetRandomPosition(random);

        economicParameters = GenerateEconomicParameters();
    }

    public void SetState(PassengerState state)
    {
        this.state = state;
    }

    PassengerEconomicParameters GenerateEconomicParameters()
    {
        float hourlyIncome = simSettings.GetRandomHourlyIncome(random);
        float tripUtilityScore = GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        float timePreference = Mathf.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(random, 1.5f, 0.5f, 0, 3f);
        float waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        float tripUtilityValue = tripUtilityScore * hourlyIncome;
        // Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);

        Substitute bestSubstitute = GetBestSubstituteForRideOffer(waitingCostPerHour, tripUtilityValue, hourlyIncome);
        PassengerEconomicParameters passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            tripUtilityScore = tripUtilityScore,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            tripUtilityValue = tripUtilityValue,
            bestSubstitute = bestSubstitute
        };

        return passengerEconomicParameters;
    }

    Substitute GetBestSubstituteForRideOffer(float waitingCostPerHour, float tripUtilityValue, float hourlyIncome)
    {
        float tripDistance = GridUtils.GetDistance(startPosition, destination);

        // Public transport
        float publicTransportTime = tripDistance / simSettings.publicTransportSpeed;
        // Public transport adds a random time between 20 minutes and 2 hours to the arrival time due to going to the bus stop, waiting for the bus, and walking to the destination
        float publicTransportAdditionalTime = Mathf.Lerp(20f / 60f, 2, (float)random.NextDouble());
        float publicTransportTimeCost = (publicTransportTime + publicTransportAdditionalTime) * waitingCostPerHour;
        float publicTransportMoneyCost = 3;
        float publicTransportUtilityCost = publicTransportTimeCost + publicTransportMoneyCost;
        float netValueOfPublicTransport = tripUtilityValue - publicTransportUtilityCost;
        Substitute publicTransportSubstitute = new Substitute()
        {
            type = TripType.PublicTransport,
            timeCost = publicTransportTimeCost,
            moneyCost = publicTransportMoneyCost,
            totalCost = publicTransportUtilityCost,
            netValue = netValueOfPublicTransport,
            netUtility = netValueOfPublicTransport / hourlyIncome
        };

        // Walking
        float walkingTime = tripDistance / simSettings.walkingSpeed;
        float timeCostOfWalking = walkingTime * waitingCostPerHour;
        float moneyCostOfWalking = 0;
        float utilityCostOfWalking = timeCostOfWalking + moneyCostOfWalking;
        float netValueOfWalking = tripUtilityValue - utilityCostOfWalking;
        Substitute walkingSubstitute = new Substitute()
        {
            type = TripType.Walking,
            timeCost = timeCostOfWalking,
            moneyCost = moneyCostOfWalking,
            totalCost = utilityCostOfWalking,
            netValue = netValueOfWalking,
            netUtility = netValueOfWalking / hourlyIncome
        };

        // Private vehicle - the idea here is that if a taxi ride going to cost you more than 100$, you're gonna find a way to have your own vehicle
        float privateVehicleTime = tripDistance / simSettings.driverSpeed;
        // Add a 5 minute waiting cost for getting into the car
        float privateVehicleWaitingTime = 5 / 60f;
        float privateVehicleTimeCost = (privateVehicleTime + privateVehicleWaitingTime) * waitingCostPerHour;
        float marginalCostEnRoute = tripDistance * simSettings.driverMarginalCostPerKm;
        float privateVehicleMoneyCost = simSettings.privateVehicleCost + marginalCostEnRoute;
        float privateVehicleUtilityCost = privateVehicleTimeCost + privateVehicleMoneyCost;
        float netValueOfPrivateVehicle = tripUtilityValue - privateVehicleUtilityCost;
        Substitute privateVehicleSubstitute = new Substitute()
        {
            type = TripType.SkipTrip,
            timeCost = privateVehicleTimeCost,
            moneyCost = privateVehicleMoneyCost,
            totalCost = privateVehicleUtilityCost,
            netValue = netValueOfPrivateVehicle,
            netUtility = netValueOfPrivateVehicle / hourlyIncome
        };

        // Skip trip
        Substitute skipTripSubstitute = new Substitute()
        {
            type = TripType.SkipTrip,
            timeCost = 0,
            moneyCost = 0,
            totalCost = 0,
            netValue = 0
        };

        List<Substitute> substitutes = new List<Substitute> { publicTransportSubstitute, walkingSubstitute, privateVehicleSubstitute, skipTripSubstitute };

        Substitute bestSubstitute = substitutes.OrderByDescending(substitute => substitute.netValue).First();


        return bestSubstitute;
    }




    float GenerateTripUtilityScore()
    {
        float tripDistance = GridUtils.GetDistance(startPosition, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        float mu = 0;
        float sigma = 0.4f;
        float tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(random, mu, sigma);
        // Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
        return tripUtilityScore;
    }
}
