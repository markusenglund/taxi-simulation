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
    [SerializeField] public Transform WaitingTimeGraphPrefab;
    [SerializeField] public Transform DriverProfitGraphPrefab;
    [SerializeField] public Transform SupplyDemandGraphPrefab;
    [SerializeField] public Transform PassengerSurplusGraphPrefab;
    [SerializeField] public Transform UtilityIncomeScatterPlotPrefab;
    [SerializeField] public Transform ResultsInfoBoxPrefab;
    [SerializeField] public Transform SurgeMultiplierGraphicPrefab;

    private List<Driver> drivers = new List<Driver>();
    private Queue<Trip> queuedTrips = new Queue<Trip>();

    private List<Trip> trips = new List<Trip>();

    private List<PassengerPerson> passengerAgents = new List<PassengerPerson>();

    private int currentHour = 0;

    public float surgeMultiplier = 1.0f;

    public Random passengerSpawnRandom;
    public Random driverSpawnRandom;


    private WaitingTimeGraph waitingTimeGraph;
    private DriverProfitGraph driverProfitGraph;
    private PassengerSurplusGraph passengerSurplusGraph;
    private UtilityIncomeScatterPlot utilityIncomeScatterPlot;
    private ResultsInfoBox resultsInfoBox;
    private SurgeMultiplierGraphic surgeMultiplierGraphic;

    private bool simulationEnded = false;

    public DriverPool driverPool;

    public SimulationSettings simulationSettings;
    public GraphSettings graphSettings;
    public static City Create(Transform cityPrefab, float x, float z, SimulationSettings simulationSettings, GraphSettings graphSettings)

    {
        Transform cityTransform = Instantiate(cityPrefab, new Vector3(x, 0, z), Quaternion.identity);
        City city = cityTransform.GetComponent<City>();
        city.simulationSettings = simulationSettings;
        city.graphSettings = graphSettings;
        return city;
    }



    void Start()
    {
        passengerSpawnRandom = new Random(simulationSettings.randomSeed);
        driverSpawnRandom = new Random(simulationSettings.randomSeed);
        GridUtils.GenerateStreetGrid(this.transform);
        driverPool = new DriverPool(this);

        if (graphSettings.showGraphs)
        {
            InstantiateGraphs();
        }

        SpawnInitialDrivers();
        if (simulationSettings.isActive)
        {
            StartCoroutine(createPassengers());
        }
    }

    void Update()
    {
        SpawnAndRemoveDrivers();
        EndSimulation();
        if (!simulationEnded)
        {
            UpdateSurgeMultiplier();
        }
    }

    void InstantiateGraphs()
    {
        waitingTimeGraph = WaitingTimeGraph.Create(WaitingTimeGraphPrefab, graphSettings.waitingTimeGraphPos, simulationSettings);
        driverProfitGraph = DriverProfitGraph.Create(DriverProfitGraphPrefab, graphSettings.driverProfitGraphPos, simulationSettings, driverPool);
        SupplyDemandGraph.Create(SupplyDemandGraphPrefab, graphSettings.supplyDemandGraphPos, this);
        passengerSurplusGraph = PassengerSurplusGraph.Create(PassengerSurplusGraphPrefab, graphSettings.passengerSurplusGraphPos, simulationSettings);
        utilityIncomeScatterPlot = UtilityIncomeScatterPlot.Create(UtilityIncomeScatterPlotPrefab, graphSettings.passengerScatterPlotPos);
        resultsInfoBox = ResultsInfoBox.Create(ResultsInfoBoxPrefab, graphSettings.resultsInfoPos, this);
        surgeMultiplierGraphic = SurgeMultiplierGraphic.Create(SurgeMultiplierGraphicPrefab, graphSettings.surgeMultiplierGraphicPos);
    }

    private void EndSimulation()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        if (simulationTime > simulationSettings.simulationLengthHours + 0.1 / 60f) // Add a small buffer to make sure all data collection at the top of the hour finishes
        {
            Time.timeScale = 0;
            simulationEnded = true;
        }
    }

    public Driver CreateDriver(DriverPerson driverPerson, Vector3 position)
    {
        Driver driver = Driver.Create(driverPerson, taxiPrefab, position.x, position.z, this);
        drivers.Add(driver);
        return driver;
    }

    private void SpawnInitialDrivers()
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
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        if (simulationTime > currentHour + 1)
        {
            currentHour = Mathf.FloorToInt(simulationTime);
            List<Driver> nextDrivers = new List<Driver>();
            DriverPerson[] driverToCreate = driverPool.GetDriversStartingAtHour(currentHour);
            for (int i = 0; i < driverToCreate.Length; i++)
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition(driverSpawnRandom);
                DriverPerson driverPerson = driverToCreate[i];

                nextDrivers.Add(Driver.Create(driverPerson, taxiPrefab, randomPosition.x, randomPosition.z, this));
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
        if (!simulationSettings.useConstantSurgeMultiplier)
        {
            float maxSurgeMultiplier = 5f;
            float expectedNumPassengersPerHour = GetNumExpectedPassengersPerHour(TimeUtils.ConvertRealSecondsToSimulationHours(Time.time));

            int numWaitingPassengers = queuedTrips.Count;
            int numOccupiedDrivers = drivers.Count(driver => driver.state == TaxiState.AssignedToTrip);
            float tripCapacityNextHour = Math.Max(drivers.Count * simulationSettings.driverAverageTripsPerHour - numOccupiedDrivers * 0.5f, 0);

            float totalExpectedPassengers = expectedNumPassengersPerHour / 1.3f + numWaitingPassengers;

            float minMultiplier = 0.7f;
            float uncertaintyModifier = Math.Min(1f / drivers.Count, 1f);

            float demandPerSupply = totalExpectedPassengers / tripCapacityNextHour;

            float newSurgeMultiplier = Mathf.Max(1f + (demandPerSupply - 1) * 1.8f, minMultiplier);

            // float[] expectedPassengersByHour = simulationSettings.expectedPassengersByHour;
            // Debug.Log("New surge multiplier: " + newSurgeMultiplier);

            surgeMultiplier = newSurgeMultiplier;

        }
        if (graphSettings.showGraphs)
        {
            surgeMultiplierGraphic.SetNewValue(surgeMultiplier);
        }
    }


    IEnumerator createPassengers()
    {

        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        while (simulationTime < simulationSettings.simulationLengthHours)
        {

            float expectedPassengersPerHour = GetNumExpectedPassengersPerHour(TimeUtils.ConvertRealSecondsToSimulationHours(Time.time));

            float interval = 1f / 30f;
            yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(interval));

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

    public Passenger CreatePassenger(Vector3 position)
    {
        // Passenger passenger = Passenger.Create(passengerPrefab, position.x, position.z, this, waitingTimeGraph, passengerSurplusGraph, utilityIncomeScatterPlot);
        PassengerPerson person = new PassengerPerson(position, simulationSettings, passengerSpawnRandom);
        passengerAgents.Add(person);
        Passenger passenger = Passenger.Create(person, passengerPrefab, this, waitingTimeGraph, passengerSurplusGraph, utilityIncomeScatterPlot);
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
        PassengerPerson[] passengersSpawnedInLastInterval = GetPassengersSpawnedInLastInterval(intervalHours);

        return passengersSpawnedInLastInterval.Length;
    }

    public PassengerPerson[] GetPassengersSpawnedInLastInterval(float intervalHours)
    {
        float intervalStartTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) - intervalHours;
        return passengerAgents.Where(passenger => passenger.timeSpawned > intervalStartTime).ToArray();
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
            float valueSurplus = passengerPerson.state == PassengerState.RejectedRideOffer ? 0 : passengerPerson.trip.droppedOffPassengerData.valueSurplus;
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

    private void DispatchDriver(Driver driver, Trip trip)
    {
        Passenger passenger = trip.tripCreatedData.passenger;
        trip.DispatchDriver(driver.transform.localPosition);
        driver.HandleDriverDispatched(trip);
        // Debug.Log("Dispatching taxi " + driver.id + " to passenger " + passenger.id + " at " + passenger.positionActual);
    }

    public Trip AcceptRideOffer(TripCreatedData tripCreatedData, TripCreatedPassengerData tripCreatedPassengerData)
    {
        Passenger passenger = tripCreatedData.passenger;
        // TODO: Driver will be assigned in the RequestTripOffer method and set in as an argument to this function
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableDriver(tripCreatedData.pickUpPosition);

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
        AssignDriverToNextTrip(driver);
        LogAverageTripTime();
    }

    public void AssignDriverToNextTrip(Driver driver)
    {
        Debug.Log("Assigning driver to next trip");
        // Assign driver to next trip if there is one
        Trip trip = GetNextTrip();
        if (trip != null)
        {
            float enRouteDistance = GridUtils.GetDistance(driver.transform.localPosition, trip.tripCreatedData.pickUpPosition);
            trip.AssignDriver(driver, enRouteDistance);
            driver.HandleDriverAssigned(trip);
            DispatchDriver(driver, trip);
        }
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
            float distance = GridUtils.GetDistance(driver.transform.localPosition, position);
            if (distance < closestTaxiDistance)
            {
                closestTaxiDistance = distance;
                closestTaxi = driver;
            }
        }
        return (closestTaxi, closestTaxiDistance);
    }

    private float? GetExpectedWaitingTime(Vector3 position)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableDriver(position);
        if (closestTaxi != null)
        {
            float extraPickUpTime = simulationSettings.timeSpentWaitingForPassenger + 0.6f / 60f;
            float expectedWaitingTime = (closestTaxiDistance / simulationSettings.driverSpeed) + extraPickUpTime;
            return expectedWaitingTime;
        }

        return null;

        // ! These approximations that will change based on how efficient the queueing algorithm is
        // float avgTimeEnRoute = 11f / 60f;
        // float avgTimeOnTrip = 10f / 60f;
        // float numTaxis = drivers.Count;
        // float queueSize = queuedTrips.Count;

        // float expectedWaitingTimeForQueue = ((avgTimeEnRoute + avgTimeOnTrip) * queueSize / numTaxis) + avgTimeEnRoute;
        // return expectedWaitingTimeForQueue;

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

    public RideOffer? RequestRideOffer(Vector3 position, Vector3 destination)
    {
        float tripDistance = GridUtils.GetDistance(position, destination);
        Fare fare = GetFare(tripDistance);
        float? expectedWaitingTime = GetExpectedWaitingTime(position);

        if (expectedWaitingTime == null)
        {
            return null;
        }

        float expectedTripTime = tripDistance / simulationSettings.driverSpeed + 0.6f / 60f;

        RideOffer tripOffer = new RideOffer
        {
            expectedWaitingTime = (float)expectedWaitingTime,
            expectedTripTime = expectedTripTime,
            fare = fare,
        };

        return tripOffer;

    }

}
