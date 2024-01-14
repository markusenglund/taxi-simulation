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

public class TripData
{
    Vector3 startPosition { get; set; }
    Vector3 pickUpPosition { get; set; }
    Vector3 destination { get; set; }

    float timeSpentEnRoute { get; set; }
    float timeSpentOnTrip { get; set; }

    float distanceEnRoute { get; set; }
    float distanceOnTrip { get; set; }

    float marginalCostEnRoute { get; set; }
    float marginalCostOnTrip { get; set; }

    float timeCostEnRoute { get; set; }
    float timeCostOnTrip { get; set; }

    float baseFare { get; set; }
    float surgeMultiplier { get; set; }
    float fare { get; set; }

    // Driver's cut of the fare, before expenses
    float driverRevenue { get; set; }

    float uberRevenue { get; set; }

    // Revenue minus marginal costs (not including cost of time)
    float operationalProfit { get; set; }

    // Revenue minus marginal costs minus opportunity cost of time
    float surplusValue { get; set; }

    // Dubious measure of driver welfare created from surplus value
    float utilitySurplus { get; set; }
}


public class Driver : MonoBehaviour
{
    private static float realSpeed = 1f;

    public static float simulationSpeed = TimeUtils.ConvertRealSpeedToSimulationSpeedPerHour(realSpeed);

    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3 destination;

    private Passenger passenger;

    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;

    // Economic parameters
    // Cut percentages are not public for Uber but hover around 33% for Lyft according to both official statements and third-party analysis https://therideshareguy.com/how-much-is-lyft-really-taking-from-your-pay/

    const float driverFareCutPercentage = 0.67f;
    const float uberFareCutPercentage = 0.33f;
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

    public TripData currentTrip = null;


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

    public void SetState(TaxiState newState, Vector3 destination, Passenger passenger = null)
    {
        // Put the passenger inside the taxi cab
        if (newState == TaxiState.Dispatched)
        {
            // TODO: Calculate cost of driving to the passenger's location


            // TODO: Figure out when and what more to calculate
        }
        if (newState == TaxiState.DrivingPassenger)
        {
            passenger.transform.SetParent(transform);
            float middleTaxiX = 0.09f;
            float topTaxiY = 0.08f;
            passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
            passenger.transform.localRotation = Quaternion.identity;
        }
        if (this.state == TaxiState.DrivingPassenger && newState != TaxiState.DrivingPassenger)
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

    IEnumerator waitForPassenger()
    {
        yield return new WaitForSeconds(1);
        Vector3 newDestination = GridUtils.GetRandomPosition();
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
                Passenger nextPassenger = GameManager.Instance.GetNextPassenger();
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
