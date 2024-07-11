#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

using Random = System.Random;

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

    public float expectedTripTime { get; set; }
    public Fare fare { get; set; }

}

public class City : MonoBehaviour
{
    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform passengerPrefab;
    [SerializeField] public Transform ResultsInfoBoxPrefab;
    [SerializeField] public Transform SurgeMultiplierGraphicPrefab;

    private List<Driver> drivers = new List<Driver>();

    private List<Trip> trips = new List<Trip>();

    private List<PassengerPerson> passengerAgents = new List<PassengerPerson>();
    private List<Passenger> passengers = new List<Passenger>();

    private int currentHour = 0;

    public float surgeMultiplier = 1.0f;

    public Random passengerSpawnRandom;
    public Random driverSpawnRandom;

    private ResultsInfoBox resultsInfoBox;
    private SurgeMultiplierGraphic surgeMultiplierGraphic;

    public bool simulationEnded = false;

    private bool spawnInitialDrivers;
    public DriverPool driverPool;

    public SimulationSettings simulationSettings;
    public GraphSettings graphSettings;
    public static City Create(Transform cityPrefab, float x, float z, SimulationSettings simulationSettings, GraphSettings graphSettings, bool spawnInitialDrivers = true)

    {
        Transform cityTransform = Instantiate(cityPrefab, new Vector3(x, 0, z), Quaternion.identity);
        City city = cityTransform.GetComponent<City>();
        city.simulationSettings = simulationSettings;
        city.graphSettings = graphSettings;
        city.spawnInitialDrivers = spawnInitialDrivers;
        return city;
    }



    void Start()
    {
        passengerSpawnRandom = new Random(simulationSettings.randomSeed);
        driverSpawnRandom = new Random(simulationSettings.randomSeed);
        // GridUtils.GenerateStreetGrid(this.transform);
        driverPool = new DriverPool(this);

        if (graphSettings.showGraphs)
        {
            InstantiateGraphs();
        }
        if (simulationSettings.showSurgeMultiplier)
        {
            StartCoroutine(StartSurgeMultiplierGraphic());
        }
        if (spawnInitialDrivers)
        {
            SpawnInitialDrivers();
        }
    }

    void Update()
    {
        SpawnAndRemoveDrivers();
        EndSimulation();
    }

    public IEnumerator StartSimulation()
    {
        StartCoroutine(createPassengers());
        yield return null;
    }

    void InstantiateGraphs()
    {
        resultsInfoBox = ResultsInfoBox.Create(ResultsInfoBoxPrefab, graphSettings.resultsInfoPos, this);
    }

    IEnumerator StartSurgeMultiplierGraphic()
    {
        surgeMultiplierGraphic = SurgeMultiplierGraphic.Create(SurgeMultiplierGraphicPrefab, graphSettings.surgeMultiplierGraphicPos);
        float intervalSimulationTime = 5f / 60f;
        float intervalRealTime = TimeUtils.ConvertSimulationHoursDurationToRealSeconds(intervalSimulationTime);
        yield return null; // Wait one tick so that drivers exist before starting the surge multiplier calculation
        while (!simulationEnded)
        {
            UpdateSurgeMultiplier();
            surgeMultiplierGraphic.SetNewValue(surgeMultiplier);
            yield return new WaitForSeconds(intervalRealTime);
        }
    }

    private void EndSimulation()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        if (simulationTime > simulationSettings.simulationLengthHours + 0.1 / 60f && !simulationEnded) // Add a small buffer to make sure all data collection at the top of the hour finishes
        {
            Time.timeScale = 0;
            simulationEnded = true;
        }

    }

    public void PauseSimulation()
    {
        simulationEnded = true;
    }

    public Driver CreateDriver(DriverPerson driverPerson, Vector3 position)
    {
        Driver driver = Driver.Create(driverPerson, taxiPrefab, transform, position.x, position.z, simulationSettings, this);
        drivers.Add(driver);
        return driver;
    }

    public void SpawnInitialDrivers()
    {
        DriverPerson[] midnightDrivers = driverPool.GetDriversActiveDuringMidnight();
        for (int i = 0; i < midnightDrivers.Length; i++)
        {
            Vector3 randomPosition = GridUtils.GetRandomPosition(driverSpawnRandom);
            DriverPerson driverPerson = midnightDrivers[i];
            CreateDriver(driverPerson, randomPosition);
        }
    }

    private void SpawnAndRemoveDrivers()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        if (simulationTime > currentHour + 1)
        {
            currentHour = Mathf.FloorToInt(simulationTime);
            List<Driver> nextDrivers = new List<Driver>();
            DriverPerson[] driverToCreate = driverPool.GetDriversStartingAtHour(currentHour);
            for (int i = 0; i < driverToCreate.Length; i++)
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition(driverSpawnRandom);
                DriverPerson driverPerson = driverToCreate[i];

                nextDrivers.Add(Driver.Create(driverPerson, taxiPrefab, transform, randomPosition.x, randomPosition.z, simulationSettings, this));
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
        if (!simulationSettings.useConstantSurgeMultiplier)
        {
            float expectedNumPassengersPerHour = GetNumExpectedPassengersPerHour(TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time));

            int numWaitingPassengers = trips.Count(trip => trip.state == TripState.Queued || trip.state == TripState.DriverAssigned);
            int numOccupiedDrivers = drivers.Count(driver => driver.currentTrip != null);
            // Bias the calculation in favor of more aggressively increasing prices when many cabs are occupied
            float numCurrentPassengersBias = 1.2f;
            float tripCapacityNextHour = drivers.Count * simulationSettings.driverAverageTripsPerHour - numCurrentPassengersBias * (numWaitingPassengers + numOccupiedDrivers / 2) / simulationSettings.driverAverageTripsPerHour;

            // Account for some passenger agents not finding Uber to be the best option for them
            float totalExpectedPassengers = expectedNumPassengersPerHour / 1.5f;


            float demandPerSupply = totalExpectedPassengers / tripCapacityNextHour;

            float minMultiplier = 0.7f;
            float newSurgeMultiplier = Mathf.Max(1f + (demandPerSupply - 1), minMultiplier);

            surgeMultiplier = newSurgeMultiplier;

        }
    }


    IEnumerator createPassengers()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        while (simulationTime < simulationSettings.simulationLengthHours && !simulationEnded)
        {

            float expectedPassengersPerHour = GetNumExpectedPassengersPerHour(TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time));

            float interval = 1f / 30f;
            yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursDurationToRealSeconds(interval));

            float expectedPassengersInInterval = expectedPassengersPerHour * interval;
            float numPassengersToCreate = 0;


            // TODO: Use an actual poisson distribution calculation instead of this inefficient approximation
            int iterations = 20;
            for (int i = 0; i < iterations; i++)
            {
                float chanceOfCreatingPassenger = expectedPassengersInInterval / iterations;
                if (passengerSpawnRandom.NextDouble() < chanceOfCreatingPassenger)
                {
                    numPassengersToCreate += 1;
                }
            }

            for (int i = 0; i < numPassengersToCreate; i++)
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition(passengerSpawnRandom);
                CreatePassenger(randomPosition);
            }
        }
    }

    public Passenger[] SpawnSavedPassengers()
    {
        PassengerPerson[] savedPersons = SaveData.LoadObject<PassengerPerson[]>(simulationSettings.randomSeed + "_016");

        foreach (PassengerPerson person in savedPersons)
        {
            Passenger passenger = Passenger.Create(person, passengerPrefab, transform, simulationSettings, city: this, PassengerMode.Inactive, 1);
            passengers.Add(passenger);
        }

        return passengers.ToArray();
    }

    public Passenger CreatePassenger(Vector3 position)
    {
        PassengerPerson person = new PassengerPerson(position, simulationSettings, passengerSpawnRandom);
        passengerAgents.Add(person);
        Passenger passenger = Passenger.Create(person, passengerPrefab, transform, simulationSettings, city: this);
        passengers.Add(passenger);
        return passenger;
    }

    public float GetNumExpectedPassengersPerHour(float simulationTime)
    {
        // Get the demand index for the two hours surrounding the current time and get the weighted average of them
        int currentHour = Mathf.FloorToInt(simulationTime);
        float percentOfHour = simulationTime - currentHour;
        float[] expectedPassengersByHour = simulationSettings.expectedPassengersByHour;

        // Hacky way to smooth out the curve by weighting the average
        float weighting = EaseUtils.EaseInOutQuadratic(percentOfHour);
        float expectedPassengersPerHour = Mathf.Lerp(expectedPassengersByHour[currentHour], expectedPassengersByHour[(currentHour + 1) % 24], weighting);
        return expectedPassengersPerHour;
    }

    public int CalculateNumStartedTripsInLastInterval(float intervalHours)
    {
        float intervalStartTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time) - intervalHours;
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
        PassengerPerson[] passengersSpawnedInLastInterval = GetPassengersSpawnedInLastInterval(intervalHours);

        return passengersSpawnedInLastInterval.Length;
    }

    public PassengerPerson[] GetPassengersSpawnedInLastInterval(float intervalHours)
    {
        float intervalStartTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time) - intervalHours;
        return passengerAgents.Where(passenger => passenger.timeSpawned > intervalStartTime).ToArray();
    }

    public PassengerPerson[] GetPassengerPeople()
    {
        return passengerAgents.ToArray();
    }

    public Passenger[] GetPassengers()
    {
        return passengers.ToArray();
    }

    public (float totalUtilitySurplusValue, float totalUtilitySurplusValuePerCapita, int population, float[] quartiledUtilitySurplusValuePerCapita, int[] quartiledPopulation) CalculatePassengerUtilitySurplusData()
    {
        float[] quartiledUtilitySurplusValuePerCapita = new float[4];
        float[] quartiledUtilitySurplusValue = new float[4];
        int[] quartiledPopulation = new int[4];
        float totalUtilitySurplusValue = 0;
        int population = 0;
        // FIXME: Hard-coded values for now based on mu=0.9, median 16 + 4 fixed income
        float[] quartiledIncomeTopRange = { 12.72f, 20.0f, 33.36f, float.PositiveInfinity };
        foreach (PassengerPerson passengerPerson in passengerAgents)
        {
            if (passengerPerson.state == PassengerState.Idling || passengerPerson.state == PassengerState.AssignedToTrip)
            {
                // Passengers who are not dropped off yet are not contributing to surplus, so are not counted in the population either so they don't influence the per capita calculation
                continue;
            }
            population++;
            float valueSurplus = passengerPerson.state == PassengerState.RejectedRideOffer || passengerPerson.state == PassengerState.NoRideOffer ? 0 : passengerPerson.trip.droppedOffPassengerData.valueSurplus;
            totalUtilitySurplusValue += valueSurplus;
            float hourlyIncome = passengerPerson.economicParameters.hourlyIncome;

            if (hourlyIncome < quartiledIncomeTopRange[0])
            {
                quartiledUtilitySurplusValue[0] += valueSurplus;
                quartiledPopulation[0]++;
            }
            else if (hourlyIncome < quartiledIncomeTopRange[1])
            {
                quartiledUtilitySurplusValue[1] += valueSurplus;
                quartiledPopulation[1]++;
            }
            else if (hourlyIncome < quartiledIncomeTopRange[2])
            {
                quartiledUtilitySurplusValue[2] += valueSurplus;
                quartiledPopulation[2]++;
            }
            else
            {
                quartiledUtilitySurplusValue[3] += valueSurplus;
                quartiledPopulation[3]++;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (quartiledPopulation[i] != 0)
            {
                quartiledUtilitySurplusValuePerCapita[i] = quartiledUtilitySurplusValue[i] / quartiledPopulation[i];
            }
        }

        float totalUtilitySurplusValuePerCapita = totalUtilitySurplusValue / population;

        return (totalUtilitySurplusValue, totalUtilitySurplusValuePerCapita, population, quartiledUtilitySurplusValuePerCapita, quartiledPopulation);
    }

    public List<Trip> GetTrips()
    {
        return trips;
    }

    // private void DispatchDriver(Driver driver, Trip trip)
    // {
    //     trip.DispatchDriver(driver.transform.localPosition);
    //     driver.HandleDriverDispatched(trip);
    //     // Debug.Log("Dispatching taxi " + driver.id + " to passenger " + passenger.id + " at " + passenger.positionActual);
    // }

    public Trip AcceptRideOffer(TripCreatedData tripCreatedData, TripCreatedPassengerData tripCreatedPassengerData, Driver driver)
    {
        Trip trip = new Trip(tripCreatedData, tripCreatedPassengerData);

        trips.Add(trip);

        trip.AssignDriver(driver);
        driver.HandleDriverAssigned(trip);

        // TODO: Figure out how to keep track of the number of currently queued trips for the surge calculation

        return trip;
    }

    public void HandleTripCompleted(Driver driver)
    {
        LogAverageTripTime();
    }

    private void LogAverageTripTime()
    {
        // Calculate late average enroute time and ontrip time based on all trips
        float totalEnrouteTime = 0;
        float totalOnTripTime = 0;
        int numTrips = 0;
        foreach (Trip trip in trips)
        {
            if (trip.droppedOffData != null)
            {
                totalEnrouteTime += trip.pickedUpData.timeSpentEnRoute;
                totalOnTripTime += trip.droppedOffData.timeSpentOnTrip;
                numTrips += 1;
            }
        }

        float averageEnrouteTime = totalEnrouteTime / numTrips;
        float averageOnTripTime = totalOnTripTime / numTrips;

        // Debug.Log($"Average enroute time: {averageEnrouteTime} average on trip time: {averageOnTripTime}, based on {numTrips} trips");
    }

    public class GetFastestDriverResponse
    {
        public bool areDriversAvailable;
        public Driver fastestDriver;
        public float fastestTime;

        public float enRouteDistance;
    }

    public GetFastestDriverResponse GetFastestDriver(Vector3 pickUpPosition)
    {
        Driver[] availableDrivers = drivers.Where(driver => driver.nextTrip == null).ToArray();
        // If there are less than half of the drivers available, don't assign any drivers. This is to prevent the "Wild goose chase" effect where drivers are assigned to passengers that are too far away
        if (availableDrivers.Length <= Mathf.Max(drivers.Count / 2, 3))
        {
            return new GetFastestDriverResponse { areDriversAvailable = false };
        }

        float fastestTime = Mathf.Infinity;
        float enRouteDistance = Mathf.Infinity;
        Driver? fastestDriver = null;
        foreach (Driver driver in availableDrivers)
        {
            bool isDriverIdle = driver.currentTrip == null;
            if (isDriverIdle)
            {
                WaypointSegment segment = driver.CalculateWaypointSegment(driver.transform.localPosition, pickUpPosition);
                float drivingTime = segment.duration;
                if (drivingTime < fastestTime)
                {
                    fastestTime = drivingTime;
                    fastestDriver = driver;
                    enRouteDistance = segment.distance;
                }
            }
            else
            {
                float prevPassengerPickupTime = driver.currentTrip!.pickedUpData != null ? driver.currentTrip.pickedUpData.pickedUpTime : driver.currentTrip.tripCreatedData.expectedPickupTime;
                float expectedDropOffTime = prevPassengerPickupTime + driver.currentTrip.tripCreatedData.expectedTripTime;
                float currentTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
                float timeLeftOnTrip = expectedDropOffTime - currentTime;

                WaypointSegment enRouteSegment = driver.CalculateWaypointSegment(driver.currentTrip.tripCreatedData.destination, pickUpPosition);
                float timeSpentBetweenTrips = simulationSettings.timeSpentWaitingForPassenger;
                float totalTime = enRouteSegment.duration + timeSpentBetweenTrips + timeLeftOnTrip;
                if (totalTime < fastestTime)
                {
                    fastestTime = totalTime;
                    fastestDriver = driver;
                    enRouteDistance = enRouteSegment.distance;
                }
            }
        }
        return new GetFastestDriverResponse { areDriversAvailable = true, fastestDriver = fastestDriver!, fastestTime = fastestTime, enRouteDistance = enRouteDistance };
    }

    private Fare GetFare(float distance)
    {
        float baseFare = simulationSettings.baseStartingFare + (distance * simulationSettings.baseFarePerKm);
        float total = baseFare * surgeMultiplier;
        float driverCut = total * simulationSettings.driverFareCutPercentage;
        float uberCut = total * simulationSettings.uberFareCutPercentage;
        Fare fare = new Fare()
        {
            baseFare = baseFare,
            surgeMultiplier = surgeMultiplier,
            total = total,
            driverCut = driverCut,
            uberCut = uberCut
        };

        return fare;
    }

    public (RideOffer?, Driver?) RequestRideOffer(Vector3 position, Vector3 destination)
    {
        float tripDistance = GridUtils.GetDistance(position, destination);
        Fare fare = GetFare(tripDistance);
        GetFastestDriverResponse fastestDriverResponse = GetFastestDriver(position);
        bool areDriversAvailable = fastestDriverResponse.areDriversAvailable;
        if (!areDriversAvailable)
        {
            return (null, null);
        }
        float expectedWaitingTime = fastestDriverResponse.fastestTime;
        Driver fastestDriver = fastestDriverResponse.fastestDriver;

        WaypointSegment tripSegment = fastestDriver.CalculateWaypointSegment(position, destination);
        float expectedTripTime = tripSegment.duration + simulationSettings.timeSpentWaitingForPassenger;

        RideOffer rideOffer = new RideOffer
        {
            expectedWaitingTime = expectedWaitingTime,
            expectedTripTime = expectedTripTime,
            fare = fare,
        };

        return (rideOffer, fastestDriver);

    }

}
