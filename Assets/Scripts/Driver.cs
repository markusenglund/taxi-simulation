using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TaxiState
{
    Idling,
    AssignedToTrip
}

public class Driver : MonoBehaviour
{
    // 30km/hr is a reasonable average speed for a taxi in an urban area (including stopping at traffic lights)
    // Real data from Atlanta: https://www.researchgate.net/figure/Average-speed-in-miles-per-hour-for-rural-and-urban-roads_tbl3_238594974
    public static float simulationSpeed = 30f;
    private static float realSpeed = TimeUtils.ConvertSimulationSpeedPerHourToRealSpeed(simulationSpeed);


    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3 destination;
    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;

    private List<Trip> completedTrips = new List<Trip>();
    private Trip currentTrip = null;
    private Trip nextTrip = null;

    // Economic parameters

    // Minimum wage in Houston is $7.25 per hour, so let's say that drivers have an opportunity cost of a little higher than that
    const float averageOpportunityCostPerHour = 9f;
    // Marginal costs include fuel + the part of maintenance, repairs, and depreciation that is proportional to the distance driven, estimated at $0.21 per mile = $0.13 per km
    const float marginalCostPerKm = 0.13f;
    const float fixedCostsPerDay = 5f;

    // TODO: opportunity cost should vary based upon the time of day and also have a very tightly grouped random distribution around minimum wage
    // But let's stick with a constant value for now with a normal distribution around minimum wage
    float opportunityCostPerHour;
    float estimatedHourlyIncome;

    private DriverProfitGraph driverProfitGraph;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        GenerateEconomicParameters();
        driverProfitGraph = GameObject.Find("DriverProfitGraph").GetComponent<DriverProfitGraph>();
        driverProfitGraph.AppendDriver(this);

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
        // TODO: Measure actual average hourly income for drivers in the simulation and put it here, for now let's set it at 10$ per hour
        estimatedHourlyIncome = 10f;
    }

    IEnumerator PickUpPassenger()
    {
        yield return new WaitForSeconds(1);
        // Put the passenger on top of the taxi cab
        Passenger passenger = currentTrip.tripCreatedData.passenger;
        passenger.transform.SetParent(transform);
        float middleTaxiX = 0.09f;
        float topTaxiY = 0.08f;
        passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
        passenger.transform.localRotation = Quaternion.identity;

        // Calculate trip pickup data
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

        currentTrip.PickUpPassenger(pickedUpData, pickedUpDriverData, pickedUpPassengerData);

        SetDestination(currentTrip.tripCreatedData.destination);
        SetTaxiColor();
    }


    public void HandleDriverAssigned(Trip trip)
    {
        nextTrip = trip;
        SetState(TaxiState.AssignedToTrip);
    }

    public void HandleDriverDispatched(Trip trip)
    {
        currentTrip = trip;
        nextTrip = null;
        SetDestination(trip.tripCreatedData.passenger.positionActual);
        SetTaxiColor();
    }

    private void DropOffPassenger()
    {
        float droppedOffTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float timeSpentOnTrip = droppedOffTime - currentTrip.pickedUpData.pickedUpTime;

        DroppedOffData droppedOffData = new DroppedOffData
        {
            droppedOffTime = droppedOffTime,
            timeSpentOnTrip = timeSpentOnTrip

        };

        float timeCostOnTrip = timeSpentOnTrip * opportunityCostPerHour;
        float marginalCostOnTrip = currentTrip.tripCreatedData.tripDistance * marginalCostPerKm;
        float grossProfit = currentTrip.tripCreatedData.fare.driverCut - marginalCostOnTrip - currentTrip.pickedUpDriverData.marginalCostEnRoute;
        float valueSurplus = grossProfit - timeCostOnTrip;
        float utilitySurplus = valueSurplus / estimatedHourlyIncome;

        DroppedOffDriverData droppedOffDriverData = new DroppedOffDriverData
        {
            timeCostOnTrip = timeCostOnTrip,
            marginalCostOnTrip = marginalCostOnTrip,
            grossProfit = grossProfit,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

        Debug.Log($"Driver {id} profit: {droppedOffDriverData.grossProfit}, fare cut: {currentTrip.tripCreatedData.fare.driverCut} marginal cost: {droppedOffDriverData.marginalCostOnTrip + currentTrip.pickedUpDriverData.marginalCostEnRoute}, time spent: {timeSpentOnTrip + currentTrip.pickedUpData.timeSpentEnRoute}");

        currentTrip.DropOffPassenger(droppedOffData, droppedOffDriverData);


        SetState(TaxiState.Idling);
        currentTrip.tripCreatedData.passenger.HandlePassengerDroppedOff();
        completedTrips.Add(currentTrip);
        currentTrip = null;
        SetTaxiColor();
        GameManager.Instance.HandleTripCompleted(this);

    }

    public float CalculateGrossProfitLastHour()
    {
        float grossProfitLastHour = 0;
        foreach (Trip trip in completedTrips)
        {
            float oneHourAgo = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) - 1;
            if (trip.droppedOffData.droppedOffTime > oneHourAgo)
            {
                grossProfitLastHour += trip.droppedOffDriverData.grossProfit;
            }
        }
        return grossProfitLastHour;
    }

    public void SetState(TaxiState newState)
    {
        this.state = newState;
    }

    private void SetTaxiColor()
    {
        // Change the color of the taxi by going into its child called "TaxiVisual" which has a child called "Taxi" and switch the second material in the mesh renderer

        Transform taxiVisual = transform.Find("TaxiVisual");
        Transform taxi = taxiVisual.Find("Taxi");
        MeshRenderer meshRenderer = taxi.GetComponent<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        if (currentTrip == null || currentTrip.state == TripState.Queued)
        {
            materials[1].color = Color.black;
        }
        else if (currentTrip.state == TripState.DriverEnRoute)
        {
            materials[1].color = Color.red;
        }
        else if (currentTrip.state == TripState.OnTrip)
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




    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

        // Set a new random destination if the taxi has reached its destination but is idling
        if (waypoints.Count == 0)
        {
            if (currentTrip != null)
            {
                if (currentTrip.state == TripState.DriverEnRoute)
                {
                    currentTrip.HandleDriverArrivedAtPickUp();
                    StartCoroutine(PickUpPassenger());
                }
                else if (currentTrip.state == TripState.OnTrip)
                {
                    DropOffPassenger();

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
