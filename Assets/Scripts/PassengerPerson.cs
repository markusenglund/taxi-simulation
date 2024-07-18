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

    public float hypotheticalTripDuration { get; set; }

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
        hypotheticalTripDuration = Driver.CalculateWaypointSegment(startPosition, destination, simSettings.driverMaxSpeed, simSettings.driverAcceleration).duration + simSettings.timeSpentWaitingForPassenger;
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
        float timePreferenceMedian = 1f;
        float timePreference = timePreferenceMedian * StatisticsUtils.getRandomFromLogNormalDistribution(random, 0, timePreferenceSigma);
        float waitingCostPerHour = 10 * Mathf.Sqrt(hourlyIncome) * timePreference;

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
        // Public transport adds a random duration between 10 minutes and 80 minutes to the arrival time due to going to the bus stop, waiting for the bus, switching buses, and walking to the destination
        float minPublicTransportExtraDuration = 10f / 60f;
        float maxPublicTransportExtraDuration = 80 / 60f;
        float publicTransportDuration = distanceToDestination / simSettings.publicTransportAverageSpeed + Mathf.Lerp(minPublicTransportExtraDuration, maxPublicTransportExtraDuration, (float)random.NextDouble());
        float publicTransportTimeCost = publicTransportDuration * waitingCostPerHour;
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
        float timeCostOfWalking = walkingTime * waitingCostPerHour;
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
