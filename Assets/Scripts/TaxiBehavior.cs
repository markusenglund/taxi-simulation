using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum TaxiState
{
    Idling,
    Dispatched,
    DrivingPassenger
}


public class TaxiBehavior : MonoBehaviour
{
    private float speed = 1f;

    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3 destination;

    private PassengerBehavior passenger;

    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
    }

    public static Transform Create(Transform prefab, float x, float z)
    {
        Transform taxi = Instantiate(prefab, new Vector3(x, 0.05f, z), Quaternion.identity);
        taxi.name = "Taxi";
        return taxi;
    }

    public void SetState(TaxiState newState, Vector3 destination, PassengerBehavior passenger = null)
    {
        // Put the passenger inside the taxi cab
        if (newState == TaxiState.DrivingPassenger)
        {
            passenger.transform.SetParent(transform);
            passenger.transform.localPosition = new Vector3(0, 0.08f, 0);
        }
        else if (this.state == TaxiState.DrivingPassenger && newState != TaxiState.DrivingPassenger)
        {
            this.passenger.transform.parent = null;
        }
        this.passenger = passenger;

        SetDestination(destination);
        SetTaxiColor(newState);


        this.state = newState;
    }

    private void SetTaxiColor(TaxiState state)
    {
        // Change the color of the taxi by going into its child called "TaxiVisual" which has a child called "Taxi" and switch the second material in the mesh renderer

        Transform taxiVisual = transform.Find("TaxiVisual");
        Transform taxi = taxiVisual.Find("Taxi");
        MeshRenderer meshRenderer = taxi.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        if (state == TaxiState.Idling)
        {
            materials[1].color = Color.black;
        }
        else if (state == TaxiState.Dispatched)
        {
            materials[1].color = Color.red;
        }
        else if (state == TaxiState.DrivingPassenger)
        {
            materials[1].color = Color.green;
        }
    }


    public void SetDestination(Vector3 destination)
    {
        this.destination = destination;
        SetWaypoints();
    }

    public void SetWaypoints()
    {
        waypoints.Clear();

        // Set up the waypoints
        Vector3 taxiPosition = transform.position;
        Vector3 taxiDestination = destination;

        Vector3 taxiDirection = taxiDestination - taxiPosition;
        if ((taxiPosition.x % Utils.blockSize == 0 && taxiDirection.x == 0) || (taxiPosition.z % Utils.blockSize == 0 && taxiDirection.z == 0))
        {
            waypoints.Enqueue(taxiDestination);
            return;
        }
        if (taxiPosition.x % Utils.blockSize != 0)
        {
            float bestFirstIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / Utils.blockSize) * Utils.blockSize : Mathf.Floor(taxiDestination.x / Utils.blockSize) * Utils.blockSize;
            waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, taxiPosition.z));
            if (taxiDestination.x % Utils.blockSize != 0)
            {
                float bestSecondIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / Utils.blockSize) * Utils.blockSize : Mathf.Floor(taxiDestination.z / Utils.blockSize) * Utils.blockSize;
                waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, bestSecondIntersectionZ));
            }
        }
        else
        {
            float bestFirstIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / Utils.blockSize) * Utils.blockSize : Mathf.Floor(taxiDestination.z / Utils.blockSize) * Utils.blockSize;
            waypoints.Enqueue(new Vector3(taxiPosition.x, 0.05f, bestFirstIntersectionZ));
            if (taxiDestination.z % Utils.blockSize != 0)
            {
                float bestSecondIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / Utils.blockSize) * Utils.blockSize : Mathf.Floor(taxiDestination.x / Utils.blockSize) * Utils.blockSize;
                waypoints.Enqueue(new Vector3(bestSecondIntersectionX, 0.05f, bestFirstIntersectionZ));
            }
        }
        waypoints.Enqueue(taxiDestination);
    }

    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

        // Set a new random destination if the taxi has reached its destination but is idling
        if (waypoints.Count == 0)
        {
            if (state == TaxiState.Dispatched)
            {
                // TODO: The destination should be read from the passenger
                Vector3 newDestination = Utils.GetRandomPosition();
                SetState(TaxiState.DrivingPassenger, newDestination, passenger);
            }
            else if (state == TaxiState.DrivingPassenger)
            {
                // Check if there are waiting passengers
                PassengerBehavior nextPassenger = GameManager.Instance.GetNextPassenger();
                if (nextPassenger != null)
                {
                    SetState(TaxiState.Dispatched, nextPassenger.positionActual, nextPassenger);
                    nextPassenger.SetState(PassengerState.Dispatched, this);
                }
                else
                {
                    Vector3 newDestination = Utils.GetRandomPosition();
                    SetState(TaxiState.Idling, transform.position);
                }
            }
            else if (state == TaxiState.Idling)
            {
                Vector3 newDestination = Utils.GetRandomPosition();
                SetDestination(newDestination);
            }
        }
        // Read the first waypoint from the queue without dequeuing it
        Vector3 waypoint = waypoints.Peek();

        Vector3 direction = waypoint - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);

        transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);

        // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
        if (transform.position == waypoint)
        {
            waypoints.Dequeue();

        }


    }


}
