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

public enum RideOfferStatus
{
    NotYetRequested,
    NoneReceived,
    Accepted,
    Rejected
}

[Serializable]
public class TripOption
{
    [field: SerializeField] public TripType type { get; set; }

    [field: SerializeField] public float timeHours { get; set; }
    [field: SerializeField] public float timeCost { get; set; }
    [field: SerializeField] public float moneyCost { get; set; }
    [field: SerializeField] public float totalCost { get; set; }
}

[Serializable]
public class PassengerEconomicParameters
{
    // Base values
    [field: SerializeField] public float hourlyIncome { get; set; }
    [field: SerializeField] public float timePreference { get; set; }

    [field: SerializeField] public float waitingCostPerHour { get; set; }

    public List<TripOption> substitutes { get; set; }

    public TripOption GetBestSubstitute()
    {
        return substitutes.OrderByDescending(substitute => substitute.totalCost).Last();
    }
}


[Serializable]
public class PassengerPerson
{
    static int incrementalId = 1;
    public int id { get; set; }
    public float timeSpawned { get; set; }
    public PassengerEconomicParameters economicParameters { get; set; }
    public Trip trip { get; set; }

    public TripType tripTypeChosen { get; set; }

    public TripOption uberTripOption { get; set; }
    public PassengerState state { get; set; }

    public RideOfferStatus rideOfferStatus { get; set; }

    private Random random;

    private SimulationSettings simSettings;

    public Vector3 destination { get; set; }
    public Vector3 startPosition { get; set; }

    public float distanceToDestination { get; set; }

    public PassengerPerson() { }
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
        distanceToDestination = GridUtils.GetDistance(startPosition, destination);
        economicParameters = GenerateEconomicParameters();
        rideOfferStatus = RideOfferStatus.NotYetRequested;
    }

    public void SetState(PassengerState state)
    {
        this.state = state;
    }

    PassengerEconomicParameters GenerateEconomicParameters()
    {
        float hourlyIncome = simSettings.GetRandomHourlyIncome(random);
        float timePreference = StatisticsUtils.GetRandomFromNormalDistribution(random, 1.5f, 0.5f, 0, 3f);
        float waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        // Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);

        List<TripOption> substitutes = GenerateSubstitutes(waitingCostPerHour, hourlyIncome);
        PassengerEconomicParameters passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            substitutes = substitutes
        };

        return passengerEconomicParameters;
    }

    List<TripOption> GenerateSubstitutes(float waitingCostPerHour, float hourlyIncome)
    {
        // Public transport
        float publicTransportTime = distanceToDestination / simSettings.publicTransportSpeed + Mathf.Lerp(20f / 60f, 2, (float)random.NextDouble());
        // Public transport adds a random time between 20 minutes and 2 hours to the arrival time due to going to the bus stop, waiting for the bus, and walking to the destination
        float publicTransportTimeCost = publicTransportTime * waitingCostPerHour;
        float publicTransportUtilityCost = publicTransportTimeCost + simSettings.publicTransportCost;
        TripOption publicTransportSubstitute = new TripOption()
        {
            type = TripType.PublicTransport,
            timeHours = publicTransportTime,
            timeCost = publicTransportTimeCost,
            moneyCost = simSettings.publicTransportCost,
            totalCost = publicTransportUtilityCost,
        };

        // Walking
        float walkingTime = distanceToDestination / simSettings.walkingSpeed;
        float timeCostOfWalking = walkingTime * waitingCostPerHour;
        float moneyCostOfWalking = 0;
        float totalCostOfWalking = timeCostOfWalking + moneyCostOfWalking;
        TripOption walkingSubstitute = new TripOption()
        {
            type = TripType.Walking,
            timeHours = walkingTime,
            timeCost = timeCostOfWalking,
            moneyCost = moneyCostOfWalking,
            totalCost = totalCostOfWalking,
        };

        // // Private vehicle - the idea here is that if a taxi ride going to cost you more than 100$, you're gonna find a way to have your own vehicle
        // float rentalCarWaitingTime = 5 / 60f;
        // float rentalCarTime = distanceToDestination / simSettings.driverSpeed + rentalCarWaitingTime;
        // // Add a 5 minute waiting cost for getting into the car
        // float rentalCarTimeCost = rentalCarTime * waitingCostPerHour;
        // float marginalCostEnRoute = distanceToDestination * simSettings.driverMarginalCostPerKm;
        // float rentalCarCost = simSettings.rentalCarCost + marginalCostEnRoute;
        // float rentalCarUtilityCost = rentalCarTimeCost + rentalCarCost;
        // float netValueOfRentalCar = tripUtilityValue - rentalCarUtilityCost;
        // TripOption rentalCarSubstitute = new TripOption()
        // {
        //     type = TripType.RentalCar,
        //     timeHours = rentalCarTime,
        //     timeCost = rentalCarTimeCost,
        //     moneyCost = rentalCarCost,
        //     totalCost = rentalCarUtilityCost,
        //     netValue = netValueOfRentalCar,
        //     netUtility = netValueOfRentalCar / hourlyIncome
        // };

        // Skip trip
        TripOption skipTripSubstitute = new TripOption()
        {
            type = TripType.SkipTrip,
            timeCost = 0,
            moneyCost = 0,
            totalCost = 0,
        };

        List<TripOption> substitutes = new List<TripOption> { publicTransportSubstitute, walkingSubstitute, skipTripSubstitute };

        return substitutes;
    }


}
