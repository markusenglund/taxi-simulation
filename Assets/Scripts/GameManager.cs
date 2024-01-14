using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public Driver driver { get; set; }
    public Passenger passenger { get; set; }
    public float expectedWaitingTime { get; set; }
    public Fare fare { get; set; }

    public float tripDistance { get; set; }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform intersectionPrefab;
    [SerializeField] public Transform streetPrefab;
    [SerializeField] public Transform passengerPrefab;

    private List<Transform> taxis = new List<Transform>();
    private Queue<Passenger> waitingPassengers = new Queue<Passenger>();

    private List<Trip> trips = new List<Trip>();

    private float surgeMultiplier = 1f;

    const float driverFareCutPercentage = 0.67f;
    const float uberFareCutPercentage = 0.33f;


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
            Transform passenger = Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }

        while (true)
        {
            int random = UnityEngine.Random.Range(0, 4);
            yield return new WaitForSeconds(random);
            Vector3 randomPosition = GridUtils.GetRandomPosition();
            Transform passenger = Passenger.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }
    }

    public Passenger GetNextPassenger()
    {
        // TODO: This creates an inefficiency, since the passenger at the front of the queue might not be the closest one to the taxi
        if (waitingPassengers.Count > 0)
        {
            return waitingPassengers.Dequeue();
        }
        return null;
    }

    public void HailTaxi(Passenger passenger, RideOffer rideOffer)
    {
        // TODO: Driver will be assigned in the RequestTripOffer method and set in as an argument to this function
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(passenger.positionActual);

        if (closestTaxi != null)
        {
            Trip trip = new Trip
            {
                driver = closestTaxi,
                passenger = passenger,
                state = TripState.EnRoute,
                driverStartPosition = closestTaxi.transform.position,
                pickUpPosition = passenger.positionActual,
                destination = passenger.destination,
                distanceEnRoute = closestTaxiDistance,
                distanceOnTrip = rideOffer.tripDistance,
                fare = rideOffer.fare,
            };

            trips.Add(trip);

            passenger.SetState(PassengerState.Dispatched, closestTaxi);
            closestTaxi.DispatchDriver(passenger, closestTaxiDistance);
            Debug.Log("Dispatching taxi " + closestTaxi.id + " to passenger " + passenger.id + " at " + passenger.positionActual);

        }
        else
        {
            passenger.SetState(PassengerState.Waiting);
            waitingPassengers.Enqueue(passenger);
            Debug.Log("No taxis available for passenger " + passenger.id + ", queued in waiting list at number " + waitingPassengers.Count);
        }
    }

    private (Driver, float) GetClosestAvailableTaxi(UnityEngine.Vector3 position)
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

    private float GetExpectedWaitingTime(UnityEngine.Vector3 position)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(position);
        if (closestTaxi != null)
        {
            // simulationSpeed = TimeUtils.ConvertRealSpeedToSimulationSpeedPerHour();
            float extraPickUpTime = 1.6f / 60f; // 1.6 simulation minutes
            float expectedWaitingTime = (closestTaxiDistance / Driver.simulationSpeed) + extraPickUpTime;
            return expectedWaitingTime;
        }

        float avgTimePerTrip = 18f / 60f; // 18 simulation minutes
        float numTaxis = taxis.Count;
        float queueSize = waitingPassengers.Count;

        float avgTaxiArrivalTime = 5f / 60f; // 5 simulation minutes

        float expectedWaitingTimeForQueue = (avgTimePerTrip * queueSize / numTaxis) + avgTaxiArrivalTime;
        return expectedWaitingTimeForQueue;

    }

    private Fare GetFare(float distance)
    {
        // This formula was empirically chosen to approximate the fare for a getting a ride in Utrecht
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

    public RideOffer RequestRideOffer(UnityEngine.Vector3 position, UnityEngine.Vector3 destination)
    {
        float tripDistance = GridUtils.GetDistance(position, destination);
        Fare fare = GetFare(tripDistance);
        float expectedWaitingTime = GetExpectedWaitingTime(position);

        RideOffer tripOffer = new RideOffer
        {
            expectedWaitingTime = expectedWaitingTime,
            fare = fare,
            tripDistance = tripDistance
        };

        return tripOffer;

    }

}
