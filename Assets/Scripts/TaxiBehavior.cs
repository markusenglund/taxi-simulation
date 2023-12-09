using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum TaxiState
{
    Idling,
    Dispatched,
    WaitingForPassenger,
    DrivingPassenger
}


public class TaxiBehavior : MonoBehaviour
{
    public static float speed = 1f;

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
            float middleTaxiX = 0.09f;
            float topTaxiY = 0.08f;
            passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
            passenger.transform.localRotation = Quaternion.identity;
        }
        else if (this.state == TaxiState.DrivingPassenger && newState != TaxiState.DrivingPassenger)
        {
            this.passenger.transform.parent = null;
            Destroy(this.passenger.transform.gameObject);
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

    IEnumerator waitForPassenger()
    {
        yield return new WaitForSeconds(1);
        Vector3 newDestination = Utils.GetRandomPosition();
        SetState(TaxiState.DrivingPassenger, newDestination, passenger);
        passenger.SetState(PassengerState.PickedUp, this);

    }

    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

        // Set a new random destination if the taxi has reached its destination but is idling
        if (waypoints.Count == 0)
        {
            if (state == TaxiState.Dispatched)
            {
                SetState(TaxiState.WaitingForPassenger, transform.position, passenger);
                StartCoroutine(waitForPassenger());
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
                    SetState(TaxiState.Idling, transform.position);
                }
            }
            else if (state == TaxiState.Idling)
            {
                // Just stay still
            }
        }
        else
        {

            // Read the first waypoint from the queue without dequeuing it
            Vector3 waypoint = waypoints.Peek();

            Vector3 direction = waypoint - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Distance delta should be lower if the taxi is close to the destination
            float distanceDelta = speed * Time.deltaTime;

            // If the taxi is close to the destination, set the distance delta to 0.01f
            if ((destination - transform.position).magnitude < 0.3f)
            {
                distanceDelta = distanceDelta / 3;
            }

            transform.position = Vector3.MoveTowards(transform.position, waypoint, distanceDelta);

            // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
            if (transform.position == waypoint)
            {
                waypoints.Dequeue();

            }
        }


    }


}
