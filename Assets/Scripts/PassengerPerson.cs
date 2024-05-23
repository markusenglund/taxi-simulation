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
    public RideOffer rideOffer { get; set; }

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
        float timePreferenceSigma = 0.5f;
        float timePreferenceMedian = 2f;
        float timePreference = timePreferenceMedian * StatisticsUtils.getRandomFromLogNormalDistribution(random, 0, timePreferenceSigma);
        float waitingCostPerHour = hourlyIncome * timePreference;

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

    public List<TripOption> GenerateSubstitutes(float waitingCostPerHour, float hourlyIncome)
    {
        // Public transport
        float minPublicTransportExtraTime = 10f / 60f;
        float maxPublicTransportExtraTime = 90 / 60f;
        float publicTransportTime = distanceToDestination / simSettings.publicTransportSpeed + Mathf.Lerp(minPublicTransportExtraTime, maxPublicTransportExtraTime, (float)random.NextDouble());
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

        List<TripOption> substitutes = new List<TripOption> { publicTransportSubstitute, walkingSubstitute };

        return substitutes;
    }


}
