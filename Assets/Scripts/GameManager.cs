using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform intersectionPrefab;
    [SerializeField] public Transform streetPrefab;
    [SerializeField] public Transform passengerPrefab;

    private List<Transform> taxis = new List<Transform>();
    private Queue<Passenger> waitingPassengers = new Queue<Passenger>();


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

    public void HailTaxi(Passenger passenger)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(passenger.positionActual);

        if (closestTaxi != null)
        {
            closestTaxi.SetState(TaxiState.Dispatched, passenger.positionActual, passenger);
            passenger.SetState(PassengerState.Dispatched, closestTaxi);
            Debug.Log("Dispatching taxi " + closestTaxi.id + " to passenger " + passenger.id + " at " + passenger.positionActual);
        }
        else
        {
            passenger.SetState(PassengerState.Waiting);
            waitingPassengers.Enqueue(passenger);
            Debug.Log("No taxis available for passenger " + passenger.id + ", queued in waiting list at number " + waitingPassengers.Count);
        }
    }

    private (Driver, float) GetClosestAvailableTaxi(Vector3 position)
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

    public float GetExpectedWaitingTime(Passenger passenger)
    {
        (Driver closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(passenger.positionActual);
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

    public float GetFare(Passenger passenger, Vector3 destination)
    {
        float distance = GridUtils.GetDistance(passenger.positionActual, destination);
        // This formula was empirically chosen to approximate the fare for a getting a ride in Utrecht
        float startingFare = 4f;
        float baseFare = startingFare + (distance * 2f);
        return baseFare;
    }



}
