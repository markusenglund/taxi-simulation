using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Fare
{
    public float baseFare { get; set; }
    public float surgeMultiplier { get; set; }
    public float total { get; set; }
    public float driverCut { get; set; }
    public float uberCut { get; set; }
}

public class RideOffer
{
    public float expectedWaitingTime { get; set; }
    public Fare fare { get; set; }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform intersectionPrefab;
    [SerializeField] public Transform streetPrefab;
    [SerializeField] public Transform passengerPrefab;

    private List<Driver> drivers = new List<Driver>();
    private Queue<Trip> queuedTrips = new Queue<Trip>();

    private List<Trip> trips = new List<Trip>();

    private List<Passenger> passengers = new List<Passenger>();

    private int currentHour = 0;

    public float surgeMultiplier = 1.0f;


    void Awake()
    {
        Instance = this;

        GridUtils.GenerateStreetGrid(intersectionPrefab, streetPrefab);
        // Create taxis in random places
        DriverPool.CreateDriverPool();
        DriverPerson[] midnightDrivers = DriverPool.GetDriversActiveDuringMidnight();
        for (int i = 0; i < midnightDrivers.Length; i++)
        {
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            DriverPerson driverPerson = midnightDrivers[i];
            drivers.Add(Driver.Create(driverPerson, taxiPrefab, randomPosition.x, randomPosition.z));
        }
        // createInitialPassengers();
        StartCoroutine(createPassengers());
    }

    void Update()
    {
        SpawnAndRemoveDrivers();
        UpdateSurgeMultiplier();
    }

    private void SpawnAndRemoveDrivers()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        if (simulationTime > currentHour + 1)
        {
            currentHour = Mathf.FloorToInt(simulationTime);
            List<Driver> nextDrivers = new List<Driver>();
            DriverPerson[] driverToCreate = DriverPool.GetDriversStartingAtHour(currentHour);
            for (int i = 0; i < driverToCreate.Length; i++)
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition();
                DriverPerson driverPerson = driverToCreate[i];

                nextDrivers.Add(Driver.Create(driverPerson, taxiPrefab, randomPosition.x, randomPosition.z));
            }

            for (int i = 0; i < drivers.Count; i++)
            {
                Driver driver = drivers[i];
                if ((driver.driverPerson.interval.endTime % 24) == (currentHour % 24))
                {
                    driver.HandleEndOfSession();
                }
                else
                {
                    nextDrivers.Add(driver);
                }
            }
            drivers = nextDrivers;
        }
    }

    void UpdateSurgeMultiplier()
    {
        // Recalculate only 4 times per hour
        if (Time.frameCount % 900 != 0)
        {
            return;
        }
        float maxSurgeMultiplier = 5f;
        float expectedNumPassengersPerHour = GetNumExpectedPassengersPerHour();
        SessionInterval[] intervals = drivers.Select(driver => driver.driverPerson.interval).ToArray();
        float[] tripCapacityByHour = DriverPool.GetTripCapacityByHour(intervals);

        int numWaitingPassengers = queuedTrips.Count;

        float newSurgeMultiplier = Math.Max(Mathf.Sqrt((expectedNumPassengersPerHour + numWaitingPassengers) / tripCapacityByHour[currentHour]), maxSurgeMultiplier);

        Debug.Log("New surge multiplier: " + newSurgeMultiplier);

        surgeMultiplier = newSurgeMultiplier;
    }


    private void createInitialPassengers()
    {
        // Create 8 passengers to start
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            Passenger passenger = Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
            passengers.Add(passenger);
        }
    }

    IEnumerator createPassengers()
    {

        while (true)
        {

            float expectedPassengersPerHour = GetNumExpectedPassengersPerHour();

            float interval = 1f / 30f;
            yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(interval));

            float expectedPassengersInInterval = expectedPassengersPerHour * interval;
            float numPassengersToCreate = 0;


            // TODO: Use an actual poisson distribution calculation instead of this inefficient approximation
            int iterations = 20;
            for (int i = 0; i < iterations; i++)
            {
                float chanceOfCreatingPassenger = expectedPassengersInInterval / iterations;
                if (Random.value < chanceOfCreatingPassenger)
                {
                    numPassengersToCreate += 1;
                }
            }

            for (int i = 0; i < numPassengersToCreate; i++)
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition();
                Passenger passenger = Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
                passengers.Add(passenger);
            }
        }
    }

    private float GetNumExpectedPassengersPerHour()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        // Get the demand index for the two hours surrounding the current time and get the weighted average of them
        int currentHour = Mathf.FloorToInt(simulationTime);
        float percentOfHour = simulationTime - currentHour;
        float[] expectedPassengersByHour = SimulationSettings.expectedPassengersByHour;
        float expectedPassengersPerHour = expectedPassengersByHour[currentHour] * percentOfHour + expectedPassengersByHour[(currentHour + 1) % 24] * (1 - percentOfHour);
        return expectedPassengersPerHour;
    }

    public Trip GetNextTrip()
    {
        // TODO: This creates an inefficiency, since the passenger at the front of the queue might not be the closest one to the taxi
        if (queuedTrips.Count > 0)
        {
            return queuedTrips.Dequeue();
        }
        return null;
    }

    public int CalculateNumStartedTripsInLastInterval(float intervalHours)
    {
        float intervalStartTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) - intervalHours;
        int numStartedTrips = 0;
        foreach (Trip trip in trips)
        {
            if (trip.pickedUpData != null && trip.pickedUpData.pickedUpTime > intervalStartTime)
            {
                numStartedTrips += 1;
            }
        }

        return numStartedTrips;
    }

    public int CalculateNumPassengersSpawnedInLastInterval(float intervalHours)
    {
        float intervalStartTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) - intervalHours;
        int numPassengersSpawned = 0;
        foreach (Passenger passenger in passengers)
        {
            if (passenger.timeCreated > intervalStartTime)
            {
                numPassengersSpawned += 1;
            }
        }

        return numPassengersSpawned;
    }

    private void DispatchDriver(Driver driver, Trip trip)
    {
        Passenger passenger = trip.tripCreatedData.passenger;
        trip.DispatchDriver(driver.transform.position);
        driver.HandleDriverDispatched(trip);
        // Debug.Log("Dispatching taxi " + driver.id + " to passenger " + passenger.id + " at " + passenger.positionActual);
    }

    public Trip AcceptRideOffer(TripCreatedData tripCreatedData, TripCreatedPassengerData tripCreatedPassengerData)
    {
        Passenger passenger = tripCreatedData.passenger;
        // TODO: Driver will be assigned in the RequestTripOffer method and set in as an argument to this function
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableDriver(passenger.positionActual);

        Trip trip = new Trip(tripCreatedData, tripCreatedPassengerData);

        trips.Add(trip);

        if (closestTaxi != null)
        {
            trip.AssignDriver(closestTaxi, closestTaxiDistance);
            closestTaxi.HandleDriverAssigned(trip);
            DispatchDriver(closestTaxi, trip);
        }
        else
        {
            queuedTrips.Enqueue(trip);
            // Debug.Log("No taxis available for passenger " + passenger.id + ", queued in waiting list at number " + queuedTrips.Count);
        }

        return trip;
    }

    public void HandleTripCompleted(Driver driver)
    {
        // Assign driver to next trip if there is one
        Trip trip = GetNextTrip();
        if (trip != null)
        {
            float enRouteDistance = GridUtils.GetDistance(driver.transform.position, trip.tripCreatedData.passenger.positionActual);
            trip.AssignDriver(driver, enRouteDistance);
            driver.HandleDriverAssigned(trip);
            DispatchDriver(driver, trip);
        }
        LogAverageTripTime();


    }

    private void LogAverageTripTime()
    {
        // Calculate late average enroute time and ontrip time based on all trips
        float totalEnrouteTime = 0;
        float totalOnTripTime = 0;
        int numTrips = 0;
        foreach (Trip _trip in trips)
        {
            if (_trip.droppedOffData != null)
            {
                totalEnrouteTime += _trip.pickedUpData.timeSpentEnRoute;
                totalOnTripTime += _trip.droppedOffData.timeSpentOnTrip;
                numTrips += 1;
            }
        }

        float averageEnrouteTime = totalEnrouteTime / numTrips;
        float averageOnTripTime = totalOnTripTime / numTrips;

        // Debug.Log($"Average enroute time: {averageEnrouteTime} average on trip time: {averageOnTripTime}, based on {numTrips} trips");
    }

    private (Driver, float) GetClosestAvailableDriver(Vector3 position)
    {
        float closestTaxiDistance = Mathf.Infinity;
        Driver closestTaxi = null;

        foreach (Driver driver in drivers)
        {
            if (driver.state != TaxiState.Idling)
            {
                continue;
            }
            float distance = GridUtils.GetDistance(driver.transform.position, position);
            if (distance < closestTaxiDistance)
            {
                closestTaxiDistance = distance;
                closestTaxi = driver;
            }
        }
        return (closestTaxi, closestTaxiDistance);
    }

    private float GetExpectedWaitingTime(Vector3 position)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableDriver(position);
        if (closestTaxi != null)
        {
            float extraPickUpTime = SimulationSettings.timeSpentWaitingForPassenger + 0.6f / 60f;
            float expectedWaitingTime = (closestTaxiDistance / SimulationSettings.driverSpeed) + extraPickUpTime;
            return expectedWaitingTime;
        }

        // ! These approximations that will change based on how efficient the queueing algorithm is
        float avgTimeEnRoute = 11f / 60f;
        float avgTimeOnTrip = 10f / 60f;
        float numTaxis = drivers.Count;
        float queueSize = queuedTrips.Count;

        float expectedWaitingTimeForQueue = ((avgTimeEnRoute + avgTimeOnTrip) * queueSize / numTaxis) + avgTimeEnRoute;
        return expectedWaitingTimeForQueue;

    }

    private Fare GetFare(float distance)
    {
        float baseFare = SimulationSettings.baseStartingFare + (distance * SimulationSettings.baseFarePerKm);
        float total = baseFare * SimulationSettings.surgeMultiplier;
        float driverCut = total * SimulationSettings.driverFareCutPercentage;
        float uberCut = total * SimulationSettings.uberFareCutPercentage;
        Fare fare = new Fare()
        {
            baseFare = baseFare,
            surgeMultiplier = SimulationSettings.surgeMultiplier,
            total = total,
            driverCut = driverCut,
            uberCut = uberCut
        };

        return fare;
    }

    public RideOffer RequestRideOffer(Vector3 position, Vector3 destination)
    {
        float tripDistance = GridUtils.GetDistance(position, destination);
        Fare fare = GetFare(tripDistance);
        float expectedWaitingTime = GetExpectedWaitingTime(position);

        RideOffer tripOffer = new RideOffer
        {
            expectedWaitingTime = expectedWaitingTime,
            fare = fare,
        };

        return tripOffer;

    }

}
