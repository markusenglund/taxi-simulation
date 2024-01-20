using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TaxiState
{
    Idling,
    Dispatched,
    WaitingForPassenger,
    DrivingPassenger
}

public class Driver : MonoBehaviour
{
    private static float realSpeed = 1f;

    public static float simulationSpeed = TimeUtils.ConvertRealSpeedToSimulationSpeedPerHour(realSpeed);

    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3 destination;
    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;

    private Trip currentTrip = null;

    // Economic parameters

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    const float averageOpportunityCostPerHour = 9f;
    // Marginal costs include fuel + the part of maintenance, repairs, and depreciation that is proportional to the distance driven, estimated at $0.21 per mile = $0.13 per km
    const float marginalCostPerKm = 0.13f;
    const float fixedCostsPerDay = 5f;

    // TODO: opportunity cost should vary based upon the time of day and also have a very tightly grouped random distribution around minimum wage
    // But let's stick with a constant value for now with a normal distribution around minimum wage
    float opportunityCostPerHour;

    public float accGrossRevenue = 0f;
    public float accCosts = fixedCostsPerDay;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        GenerateEconomicParameters();
    }

    public static Transform Create(Transform prefab, float x, float z)
    {
        Transform taxi = Instantiate(prefab, new Vector3(x, 0.05f, z), Quaternion.identity);
        taxi.name = "Taxi";
        return taxi;
    }

    void GenerateEconomicParameters()
    {
        // Ultra tight spread slightly above minimum wage, reflecting the reality that you don't drive for Uber if you're career is going great.
        opportunityCostPerHour = StatisticsUtils.GetRandomFromNormalDistribution(averageOpportunityCostPerHour, 1f);
    }

    public void HandleDriverDispatched(Trip trip)
    {
        currentTrip = trip;
        SetState(TaxiState.Dispatched, trip.tripCreatedData.passenger.positionActual);
    }

    public void SetState(TaxiState newState, Vector3 destination)
    {
        // Put the passenger inside the taxi cab
        if (newState == TaxiState.DrivingPassenger)
        {
            Passenger passenger = currentTrip.tripCreatedData.passenger;
            passenger.transform.SetParent(transform);
            float middleTaxiX = 0.09f;
            float topTaxiY = 0.08f;
            passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
            passenger.transform.localRotation = Quaternion.identity;
        }
        if (this.state == TaxiState.DrivingPassenger && newState != TaxiState.DrivingPassenger)
        {
            Passenger passenger = currentTrip.tripCreatedData.passenger;
            passenger.transform.parent = null;
            Destroy(passenger.transform.gameObject);
        }

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
        if ((taxiPosition.x % GridUtils.blockSize == 0 && taxiDirection.x == 0) || (taxiPosition.z % GridUtils.blockSize == 0 && taxiDirection.z == 0))
        {
            waypoints.Enqueue(taxiDestination);
            return;
        }
        if (taxiPosition.x % GridUtils.blockSize != 0)
        {
            float bestFirstIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize;
            waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, taxiPosition.z));
            if (taxiDestination.x % GridUtils.blockSize != 0)
            {
                float bestSecondIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize;
                waypoints.Enqueue(new Vector3(bestFirstIntersectionX, 0.05f, bestSecondIntersectionZ));
            }
        }
        else
        {
            float bestFirstIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize;
            waypoints.Enqueue(new Vector3(taxiPosition.x, 0.05f, bestFirstIntersectionZ));
            if (taxiDestination.z % GridUtils.blockSize != 0)
            {
                float bestSecondIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize;
                waypoints.Enqueue(new Vector3(bestSecondIntersectionX, 0.05f, bestFirstIntersectionZ));
            }
        }
        waypoints.Enqueue(taxiDestination);
    }

    IEnumerator pickUpPassenger()
    {
        yield return new WaitForSeconds(1);
        Vector3 newDestination = currentTrip.tripCreatedData.destination;
        SetState(TaxiState.DrivingPassenger, newDestination);

        float pickedUpTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float timeSpentEnRoute = pickedUpTime - currentTrip.driverAssignedData.matchedTime;
        float waitingTime = pickedUpTime - currentTrip.tripCreatedData.createdTime;
        PickedUpData pickedUpData = new PickedUpData
        {
            pickedUpTime = pickedUpTime,
            timeSpentEnRoute = timeSpentEnRoute,
            waitingTime = waitingTime
        };

        // Create driver pickup data
        PickedUpDriverData pickedUpDriverData = new PickedUpDriverData
        {
            timeCostEnRoute = timeSpentEnRoute * opportunityCostPerHour,
            marginalCostEnRoute = currentTrip.driverAssignedData.enRouteDistance * marginalCostPerKm
        };

        PickedUpPassengerData pickedUpPassengerData = currentTrip.tripCreatedData.passenger.HandlePassengerPickedUp(pickedUpData);

        currentTrip.pickUpPassenger(pickedUpData, pickedUpDriverData, pickedUpPassengerData);
    }

    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

        // Set a new random destination if the taxi has reached its destination but is idling
        if (waypoints.Count == 0)
        {
            if (state == TaxiState.Dispatched)
            {
                SetState(TaxiState.WaitingForPassenger, transform.position);
                StartCoroutine(pickUpPassenger());
            }
            else if (state == TaxiState.DrivingPassenger)
            {
                SetState(TaxiState.Idling, transform.position);
                GameManager.Instance.HandleDriverIdle(this);
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
            float distanceDelta = realSpeed * Time.deltaTime;

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
