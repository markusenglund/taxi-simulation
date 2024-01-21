using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fare
{
    public float baseFare { get; set; }
    public float surgeMultiplier { get; set; }
    public float total { get; set; }

    // Cut percentages are not public for Uber but hover around 33% for Lyft according to both official statements and third-party analysis https://therideshareguy.com/how-much-is-lyft-really-taking-from-your-pay/
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

    private List<Transform> taxis = new List<Transform>();
    private Queue<Trip> queuedTrips = new Queue<Trip>();

    private List<Trip> trips = new List<Trip>();

    private float surgeMultiplier = 1f;

    const float driverFareCutPercentage = 0.67f;
    const float uberFareCutPercentage = 0.33f;

    // Based on real friday data, demand is indexed by as 1 being the lowest measured number
    Dictionary<int, float> demandIndexByHour = new Dictionary<int, float>()
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
        { 21, 12f},
        { 22, 14f},
        { 23, 16f},
        { 24, 12f}
    };


    void Awake()
    {
        Instance = this;

        GridUtils.GenerateStreetGrid(intersectionPrefab, streetPrefab);
        // Create taxis in random places
        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            taxis.Add(Driver.Create(taxiPrefab, randomPosition.x, randomPosition.z));
        }

        StartCoroutine(createPassengers());
    }

    IEnumerator createPassengers()
    {
        // Create 8 passengers to start
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }

        yield return new WaitForSeconds(2f);

        while (true)
        {
            int random = Random.Range(0, 4);
            yield return new WaitForSeconds(random);
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }
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
    }

    private (Driver, float) GetClosestAvailableDriver(Vector3 position)
    {
        float closestTaxiDistance = Mathf.Infinity;
        Driver closestTaxi = null;

        foreach (Transform taxi in taxis)
        {
            Driver taxiBehavior = taxi.GetComponent<Driver>();
            if (taxiBehavior.state != TaxiState.Idling)
            {
                continue;
            }
            float distance = GridUtils.GetDistance(taxi.position, position);
            if (distance < closestTaxiDistance)
            {
                closestTaxiDistance = distance;
                closestTaxi = taxiBehavior;
            }
        }
        return (closestTaxi, closestTaxiDistance);
    }

    private float GetExpectedWaitingTime(Vector3 position)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableDriver(position);
        if (closestTaxi != null)
        {
            float extraPickUpTime = 1.6f / 60f; // 1.6 simulation minutes
            float expectedWaitingTime = (closestTaxiDistance / Driver.simulationSpeed) + extraPickUpTime;
            return expectedWaitingTime;
        }

        float avgTimePerTrip = 18f / 60f; // 18 simulation minutes
        float numTaxis = taxis.Count;
        float queueSize = queuedTrips.Count;

        float avgTaxiArrivalTime = 5f / 60f; // 5 simulation minutes

        float expectedWaitingTimeForQueue = (avgTimePerTrip * queueSize / numTaxis) + avgTaxiArrivalTime;
        return expectedWaitingTimeForQueue;

    }

    private Fare GetFare(float distance)
    {
        // This formula was empirically chosen to approximate the fare for a getting a ride in Utrecht
        // $3.30 + $0.87 ⇥ (predicted miles) + $0.11 ⇥ (predicted minutes) was the formula used in the "Who benefits?" paper, which is a bit less than the formula below
        float startingFare = 4f;
        float baseFare = startingFare + (distance * 2f);
        float total = baseFare * surgeMultiplier;
        float driverCut = total * driverFareCutPercentage;
        float uberCut = total * uberFareCutPercentage;
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
