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
    [field: SerializeField] public float maxTimeSavedByUber { get; set; }
}

[Serializable]
public class PassengerEconomicParameters
{
    // Base values
    [field: SerializeField] public float hourlyIncome { get; set; }
    [field: SerializeField] public float timePreference { get; set; }

    [field: SerializeField] public float valueOfTime { get; set; }

    public List<TripOption> substitutes { get; set; }

    public TripOption GetBestSubstitute()
    {
        return substitutes.OrderByDescending(substitute => substitute.totalCost).Last();
    }
}


[Serializable]
public class PassengerPerson
{
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

    public float hypotheticalTripDuration { get; set; }

    public PassengerPerson() { }
    public PassengerPerson(Vector3 startPosition, SimulationSettings simSettings, Random random, int id)
    {
        this.id = id;
        this.random = random;
        this.simSettings = simSettings;
        // Put the focus passenger in a specific position for the video
        destination = GridUtils.GetRandomPosition(random);
        if (simSettings.randomSeed == 4 && id == 55)
        {
            startPosition = new Vector3(0, startPosition.y, 6.33f);
            destination = new Vector3(6.33f, destination.y, 6);
        }
        this.startPosition = startPosition;
        timeSpawned = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        state = PassengerState.BeforeSpawn;
        distanceToDestination = GridUtils.GetDistance(startPosition, destination);
        hypotheticalTripDuration = Driver.CalculateWaypointSegment(startPosition, destination, simSettings.driverMaxSpeed, simSettings.driverAcceleration).duration + simSettings.timeSpentWaitingForPassenger;
        economicParameters = GenerateEconomicParameters();
        // Cheat by setting the focus passenger's time preference to a value that fits the narrative
        if (simSettings.randomSeed == 4 && id == 55)
        {
            economicParameters = GenerateFakeEconomicParameters();
        }
        rideOfferStatus = RideOfferStatus.NotYetRequested;
    }

    public void SetState(PassengerState state)
    {
        this.state = state;
    }

    PassengerEconomicParameters GenerateEconomicParameters()
    {
        float hourlyIncome = simSettings.GetRandomHourlyIncome(random);

        float timeSensitivity = simSettings.GetRandomTimeSensitivity(random);

        float valueOfTime = 10 * Mathf.Sqrt(hourlyIncome) * timeSensitivity;

        List<TripOption> substitutes = GenerateSubstitutes(valueOfTime);
        PassengerEconomicParameters passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            timePreference = timeSensitivity,
            valueOfTime = valueOfTime,
            substitutes = substitutes
        };

        return passengerEconomicParameters;
    }

    PassengerEconomicParameters GenerateFakeEconomicParameters()
    {
        float hourlyIncome = 16.40f;

        float timeSensitivity = 2.90f;

        float valueOfTime = 10 * Mathf.Sqrt(hourlyIncome) * timeSensitivity;

        TripOption walkingSubstitute = new TripOption
        {
            type = TripType.Walking,
            timeHours = 1.9f,
            timeCost = 1.9f * valueOfTime,
            moneyCost = 0,
            totalCost = 1.9f * valueOfTime
        };
        TripOption publicTransportSubstitute = new TripOption
        {
            type = TripType.PublicTransport,
            timeHours = 1.2f,
            timeCost = 1.2f * valueOfTime,
            moneyCost = 2.5f,
            totalCost = 1.2f * valueOfTime + 2.5f
        };
        List<TripOption> substitutes = new List<TripOption> { walkingSubstitute, publicTransportSubstitute };
        PassengerEconomicParameters passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            timePreference = timeSensitivity,
            valueOfTime = valueOfTime,
            substitutes = substitutes
        };

        return passengerEconomicParameters;
    }

    public List<TripOption> GenerateSubstitutes(float valueOfTime)
    {
        // Public transport adds a random duration between 10 minutes and 80 minutes to the arrival time due to going to the bus stop, waiting for the bus, switching buses, and walking to the destination
        float minPublicTransportExtraDuration = 10f / 60f;
        float maxPublicTransportExtraDuration = 80 / 60f;
        float publicTransportDuration = distanceToDestination / simSettings.publicTransportAverageSpeed + Mathf.Lerp(minPublicTransportExtraDuration, maxPublicTransportExtraDuration, (float)random.NextDouble());
        float publicTransportTimeCost = publicTransportDuration * valueOfTime;
        float publicTransportUtilityCost = publicTransportTimeCost + simSettings.publicTransportCost;
        float maxTimeSavedByUberOverPublicTransport = publicTransportDuration - hypotheticalTripDuration;
        TripOption publicTransportSubstitute = new TripOption()
        {
            type = TripType.PublicTransport,
            timeHours = publicTransportDuration,
            timeCost = publicTransportTimeCost,
            moneyCost = simSettings.publicTransportCost,
            totalCost = publicTransportUtilityCost,
            maxTimeSavedByUber = maxTimeSavedByUberOverPublicTransport
        };

        // Walking
        float walkingTime = distanceToDestination / simSettings.walkingSpeed;
        float timeCostOfWalking = walkingTime * valueOfTime;
        float moneyCostOfWalking = 0;
        float totalCostOfWalking = timeCostOfWalking + moneyCostOfWalking;
        float maxTimeSavedByUberOverWalking = walkingTime - hypotheticalTripDuration;
        TripOption walkingSubstitute = new TripOption()
        {
            type = TripType.Walking,
            timeHours = walkingTime,
            timeCost = timeCostOfWalking,
            moneyCost = moneyCostOfWalking,
            totalCost = totalCostOfWalking,
            maxTimeSavedByUber = maxTimeSavedByUberOverWalking
        };

        List<TripOption> substitutes = new List<TripOption> { publicTransportSubstitute, walkingSubstitute };

        return substitutes;
    }

    public bool StartedTrip()
    {
        bool passengerDidNotGetAnUber = this.tripTypeChosen != TripType.Uber;
        if (passengerDidNotGetAnUber)
        {
            return false;
        }
        bool passengerHasNotRequestedTripYet = this.state == PassengerState.Idling || this.state == PassengerState.BeforeSpawn;
        if (passengerHasNotRequestedTripYet)
        {
            return false;
        }

        // If the passenger's driver is assigned but not yet driving to the passenger, we don't want to count them to prevent bias in favor of having a large queue of waiting passengers
        bool passengerIsQueued = this.trip != null && (this.trip.state == TripState.Queued || this.trip.state == TripState.DriverAssigned);
        if (passengerIsQueued)
        {
            return false;
        }
        return true;
    }


}
