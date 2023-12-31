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
    private Queue<PassengerBehavior> waitingPassengers = new Queue<PassengerBehavior>();


    void Awake()
    {
        Instance = this;

        Utils.GenerateStreetGrid(intersectionPrefab, streetPrefab);
        // Create taxis in random places
        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            taxis.Add(TaxiBehavior.Create(taxiPrefab, randomPosition.x, randomPosition.z));
        }

        StartCoroutine(createPassengers());
    }

    IEnumerator createPassengers()
    {
        // Create 8 passengers to start
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            Transform passenger = PassengerBehavior.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }

        while (true)
        {
            int random = UnityEngine.Random.Range(0, 4);
            yield return new WaitForSeconds(random);
            Vector3 randomPosition = Utils.GetRandomPosition();
            Transform passenger = PassengerBehavior.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }
    }

    public PassengerBehavior GetNextPassenger()
    {
        // TODO: This creates an inefficiency, since the passenger at the front of the queue might not be the closest one to the taxi
        if (waitingPassengers.Count > 0)
        {
            return waitingPassengers.Dequeue();
        }
        return null;
    }

    public void HailTaxi(PassengerBehavior passenger)
    {
        (TaxiBehavior closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(passenger.positionActual);

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

    private (TaxiBehavior, float) GetClosestAvailableTaxi(Vector3 position)
    {
        float closestTaxiDistance = Mathf.Infinity;
        TaxiBehavior closestTaxi = null;

        foreach (Transform taxi in taxis)
        {
            TaxiBehavior taxiBehavior = taxi.GetComponent<TaxiBehavior>();
            if (taxiBehavior.state != TaxiState.Idling)
            {
                continue;
            }
            float distance = Utils.GetDistance(taxi.position, position);
            if (distance < closestTaxiDistance)
            {
                closestTaxiDistance = distance;
                closestTaxi = taxiBehavior;
            }
        }
        return (closestTaxi, closestTaxiDistance);
    }

    public float GetExpectedWaitingTime(PassengerBehavior passenger)
    {
        (TaxiBehavior closestTaxi, float closestTaxiDistance) = GetClosestAvailableTaxi(passenger.positionActual);
        if (closestTaxi != null)
        {
            float expectedWaitingTime = (closestTaxiDistance / TaxiBehavior.speed) + 1.6f;
            return expectedWaitingTime;
        }

        float avgTimePerTrip = 18f;
        float numTaxis = taxis.Count;
        float queueSize = waitingPassengers.Count;

        float avgTaxiArrivalTime = 5f;

        float expectedWaitingTimeForQueue = (avgTimePerTrip * queueSize / numTaxis) + avgTaxiArrivalTime;
        return expectedWaitingTimeForQueue;

    }

    public float GetFare(PassengerBehavior passenger, Vector3 destination)
    {
        float distance = Utils.GetDistance(passenger.positionActual, destination);
        // This formula was empirically chosen to approximate the fare for a getting a ride in Utrecht
        float startingFare = 4f;
        float baseFare = startingFare + (distance * 2f);
        return baseFare;
    }



}
