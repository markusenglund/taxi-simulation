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


    public void SetDestination(Vector3 destination, TaxiState state)
    {
        this.destination = destination;
        this.state = state;
        SetWaypoints();
    }

    public void SetWaypoints()
    {
        // Set up the waypoints
        Vector3 taxiPosition = transform.position;
        Vector3 taxiDestination = destination;

        Vector3 taxiDirection = taxiDestination - taxiPosition;
        if (taxiPosition.x % 4 != 0)
        {
            float bestFirstIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / 4) * 4 : Mathf.Floor(taxiDestination.x / 4) * 4;
            waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, taxiPosition.z));
            if (taxiDestination.x % 4 != 0)
            {
                float bestSecondIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / 4) * 4 : Mathf.Floor(taxiDestination.z / 4) * 4;
                waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, bestSecondIntersectionZ));
            }
        }
        else
        {
            float bestFirstIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / 4) * 4 : Mathf.Floor(taxiDestination.z / 4) * 4;
            waypoints.Enqueue(new Vector3(taxiPosition.x, 0.05f, bestFirstIntersectionZ));
            if (taxiDestination.z % 4 != 0)
            {
                float bestSecondIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / 4) * 4 : Mathf.Floor(taxiDestination.x / 4) * 4;
                waypoints.Enqueue(new Vector3(bestSecondIntersectionX, 0.05f, bestFirstIntersectionZ));
            }
        }
        waypoints.Enqueue(taxiDestination);
    }

    void Update()
    {
        // Set a new random destination if the taxi has reached its destination but is idling
        if (waypoints.Count == 0)
        {
            if (state == TaxiState.Dispatched)
            {
                return;
            }
            else if (state == TaxiState.Idling)
            {
                destination = Utils.GetRandomPosition();
                SetWaypoints();
                Debug.Log("Taxi " + id + " idling at " + transform.position + " heading to " + destination);
            }
        }
        // Read the first waypoint from the queue without dequeuing it
        Vector3 waypoint = waypoints.Peek();

        Vector3 direction = waypoint - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);

        // Move the taxi
        transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);

        // Log the taxi's position
        // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
        if (transform.position == waypoint)
        {
            waypoints.Dequeue();

        }


    }


}
