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

    private float passengerSpawnRate = 0.01f;

    private List<Transform> taxis = new List<Transform>();


    void Awake()
    {
        Instance = this;

        Utils.GenerateStreetGrid(intersectionPrefab, streetPrefab);
        // Create taxis in random places
        for (int i = 0; i < 1; i++)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            taxis.Add(TaxiBehavior.Create(taxiPrefab, randomPosition.x, randomPosition.z));
        }

        StartCoroutine(createPassengers());
    }

    IEnumerator createPassengers()
    {

        yield return new WaitForSeconds(1);
        Vector3 randomPosition = Utils.GetRandomPosition();
        Transform passenger = PassengerBehavior.Create(passengerPrefab, randomPosition.x, randomPosition.z);
    }

    public void HailTaxi(PassengerBehavior passenger)
    {
        // Find the closest taxi
        float closestTaxiDistance = Mathf.Infinity;
        TaxiBehavior closestTaxi = null;

        foreach (Transform taxi in taxis)
        {
            TaxiBehavior taxiBehavior = taxi.GetComponent<TaxiBehavior>();
            if (taxiBehavior.state != TaxiState.Idling)
            {
                continue;
            }
            float distance = Math.Abs(taxi.position.x - passenger.positionActual.x) + Math.Abs(taxi.position.z - passenger.transform.position.z);
            if (distance < closestTaxiDistance)
            {
                closestTaxiDistance = distance;
                closestTaxi = taxiBehavior;
            }
        }


        if (closestTaxi != null)
        {
            closestTaxi.SetDestination(passenger.positionActual, TaxiState.Dispatched);
            passenger.state = PassengerState.Dispatched;
            Debug.Log("Dispatching taxi " + closestTaxi.id + " to passenger " + passenger.id + " at " + passenger.positionActual);

        }
        else
        {
            passenger.state = PassengerState.Waiting;
            Debug.Log("No taxis available for passenger " + passenger.id);
        }
    }



}
