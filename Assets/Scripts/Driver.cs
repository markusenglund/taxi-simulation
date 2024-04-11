using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum TaxiState
{
    Idling,
    AssignedToTrip
}

public class WaypointSegment
{
    public float startTime;
    public float distance;
    public float accelerationDistance;
    public float duration;
    public float accelerationDuration;
    public Vector3 startPosition;

    public Vector3 endPosition;
}

public class Driver : MonoBehaviour
{



    [SerializeField] private Queue<Vector3> waypoints = new Queue<Vector3>();
    [SerializeField] private Vector3 destination;
    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;

    private float spawnDuration = 1;

    private Trip currentTrip = null;
    private Trip nextTrip = null;

    const float y = 0.05f;

    private bool isEndingSession = false;

    public DriverPerson driverPerson;

    private City city;

    private float acceleration = 1000;
    private float maxSpeed = 40;

    private WaypointSegment currentWaypointSegment;

    [SerializeField] public Transform agentStatusTextPrefab;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
    }

    void Start()
    {
        StartCoroutine(SpawnDriver());
    }

    public static Driver Create(DriverPerson person, Transform prefab, float x, float z, City city)
    {
        Quaternion rotation = x % GridUtils.blockSize == 0 ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

        Transform taxi = Instantiate(prefab, city.transform, false);
        taxi.localPosition = new Vector3(x, y, z);
        taxi.localRotation = rotation;
        Driver driver = taxi.GetComponent<Driver>();
        driver.city = city;
        driver.driverPerson = person;
        driver.driverPerson.isCurrentlyDriving = true;
        taxi.name = "Taxi";
        return driver;
    }

    IEnumerator SpawnDriver()
    {
        Transform spawnAnimationPrefab = Resources.Load<Transform>("RespawnAnimation");
        Transform animation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        animation.localScale = Vector3.one * 7f;
        Vector3 finalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float startTime = Time.time;
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseUtils.EaseInOutCubic(t);
            transform.localScale = finalScale * t;
            yield return null;
        }
        transform.localScale = finalScale;
        city.AssignDriverToNextTrip(this);
    }

    IEnumerator PickUpPassenger()
    {
        // Calculate trip pickup data
        float pickedUpTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float timeSpentEnRoute = pickedUpTime - currentTrip.driverDispatchedData.driverDispatchedTime;
        float waitingTime = pickedUpTime - currentTrip.tripCreatedData.createdTime;
        PickedUpData pickedUpData = new PickedUpData
        {
            pickedUpTime = pickedUpTime,
            timeSpentEnRoute = timeSpentEnRoute,
            waitingTime = waitingTime
        };

        float opportunityCostPerHour = driverPerson.GetOpportunityCostForHour(Mathf.FloorToInt(pickedUpTime));
        // Create driver pickup data
        PickedUpDriverData pickedUpDriverData = new PickedUpDriverData
        {
            timeCostEnRoute = timeSpentEnRoute * opportunityCostPerHour,
            marginalCostEnRoute = currentTrip.driverAssignedData.enRouteDistance * city.simulationSettings.driverMarginalCostPerKm
        };


        AgentStatusText.Create(agentStatusTextPrefab, transform, Vector3.up * 0.9f, $"+${currentTrip.tripCreatedData.fare.driverCut.ToString("F2")}", Color.green);

        yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(city.simulationSettings.timeSpentWaitingForPassenger));
        // Put the passenger on top of the taxi cab
        Passenger passenger = currentTrip.tripCreatedData.passenger;
        passenger.transform.SetParent(transform);
        float middleTaxiX = 0.09f;
        float topTaxiY = 1.44f;
        passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
        passenger.transform.localRotation = Quaternion.identity;

        currentTrip.PickUpPassenger(pickedUpData, pickedUpDriverData);

        SetDestination(currentTrip.tripCreatedData.destination);
    }


    public void HandleDriverAssigned(Trip trip)
    {
        nextTrip = trip;
        SetState(TaxiState.AssignedToTrip);
        Debug.Log($"Driver {id} assigned to trip at {TimeUtils.ConvertRealSecondsToSimulationHours(Time.time)}");
    }

    public void HandleDriverDispatched(Trip trip)
    {
        currentTrip = trip;
        nextTrip = null;
        SetDestination(trip.tripCreatedData.passenger.positionActual);
        Debug.Log($"Driver {id} dispatched at {TimeUtils.ConvertRealSecondsToSimulationHours(Time.time)}");
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
        float opportunityCostPerHour = driverPerson.GetOpportunityCostForHour(Mathf.FloorToInt(currentTrip.pickedUpData.pickedUpTime));

        float timeCostOnTrip = timeSpentOnTrip * opportunityCostPerHour;
        float marginalCostOnTrip = currentTrip.tripCreatedData.tripDistance * city.simulationSettings.driverMarginalCostPerKm;
        float grossProfit = currentTrip.tripCreatedData.fare.driverCut - marginalCostOnTrip - currentTrip.pickedUpDriverData.marginalCostEnRoute;
        float valueSurplus = grossProfit - timeCostOnTrip;
        float utilitySurplus = valueSurplus / driverPerson.baseOpportunityCostPerHour;

        DroppedOffDriverData droppedOffDriverData = new DroppedOffDriverData
        {
            timeCostOnTrip = timeCostOnTrip,
            marginalCostOnTrip = marginalCostOnTrip,
            grossProfit = grossProfit,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

        // Debug.Log($"Driver {id} profit: {droppedOffDriverData.grossProfit}, fare cut: {currentTrip.tripCreatedData.fare.driverCut} marginal cost: {droppedOffDriverData.marginalCostOnTrip + currentTrip.pickedUpDriverData.marginalCostEnRoute}, time spent: {timeSpentOnTrip + currentTrip.pickedUpData.timeSpentEnRoute}");

        currentTrip.DropOffPassenger(droppedOffData, droppedOffDriverData);

        // Debug.Log($"On trip time: {droppedOffData.timeSpentOnTrip} On trip distance: {currentTrip.tripCreatedData.tripDistance} km, En route time: {currentTrip.pickedUpData.timeSpentEnRoute} En route distance: {currentTrip.driverAssignedData.enRouteDistance} km");


        SetState(TaxiState.Idling);
        currentTrip.tripCreatedData.passenger.HandlePassengerDroppedOff();
        driverPerson.completedTrips.Add(currentTrip);
        currentTrip = null;
        if (isEndingSession)
        {
            EndSession();
        }
        else
        {
            city.HandleTripCompleted(this);
            Debug.Log($"Driver {id} completed trip at {droppedOffTime}");
        }

    }

    public float CalculateGrossProfitLastInterval(float intervalHours)
    {
        float grossProfitInterval = 0;
        foreach (Trip trip in driverPerson.completedTrips)
        {
            float intervalStartTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time) - intervalHours;
            if (trip.droppedOffData.droppedOffTime > intervalStartTime)
            {
                grossProfitInterval += trip.droppedOffDriverData.grossProfit;
            }
        }
        return grossProfitInterval;
    }

    public void SetState(TaxiState newState)
    {
        this.state = newState;
    }

    public void HandleEndOfSession()
    {
        isEndingSession = true;
        if (state == TaxiState.Idling)
        {
            EndSession();
        }
    }

    public void EndSession()
    {
        driverPerson.actualSessionEndTimes.Add(TimeUtils.ConvertRealSecondsToSimulationHours(Time.time));
        driverPerson.isCurrentlyDriving = false;
        Debug.Log($"Driver {id} ended session at {TimeUtils.ConvertRealSecondsToSimulationHours(Time.time)}");
        Destroy(gameObject);
    }



    public void SetDestination(Vector3 destination)
    {

        this.destination = destination;
        SetWaypoints();
        Debug.Log($"Driver {id} set destination to {destination}");
    }

    public void SetWaypoints()
    {
        waypoints.Clear();

        // Set up the waypoints
        Vector3 taxiPosition = transform.localPosition;
        Vector3 taxiDestination = destination;

        Vector3 taxiDirection = taxiDestination - taxiPosition;
        if ((taxiPosition.x % GridUtils.blockSize == 0 && taxiDirection.x == 0) || (taxiPosition.z % GridUtils.blockSize == 0 && taxiDirection.z == 0))
        {
            waypoints.Enqueue(taxiDestination);
            SetNewEndpoint();
            return;
        }
        if (taxiPosition.x % GridUtils.blockSize != 0)
        {
            float bestFirstIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize;
            waypoints.Enqueue(new Vector3(bestFirstIntersectionX, y, taxiPosition.z));
            if (taxiDestination.x % GridUtils.blockSize != 0)
            {
                float bestSecondIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize;
                waypoints.Enqueue(new Vector3(bestFirstIntersectionX, y, bestSecondIntersectionZ));
            }
        }
        else
        {
            float bestFirstIntersectionZ = taxiPosition.z > taxiDestination.z ? Mathf.Ceil(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.z / GridUtils.blockSize) * GridUtils.blockSize;
            waypoints.Enqueue(new Vector3(taxiPosition.x, y, bestFirstIntersectionZ));
            if (taxiDestination.z % GridUtils.blockSize != 0)
            {
                float bestSecondIntersectionX = taxiPosition.x > taxiDestination.x ? Mathf.Ceil(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Floor(taxiDestination.x / GridUtils.blockSize) * GridUtils.blockSize;
                waypoints.Enqueue(new Vector3(bestSecondIntersectionX, y, bestFirstIntersectionZ));
            }
        }
        waypoints.Enqueue(taxiDestination);
        SetNewEndpoint();
    }

    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

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
            Vector3 currentPosition = transform.localPosition;

            Vector3 direction = waypoint - currentPosition;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // float currentTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);

            // float waypointT = (currentTime - currentWaypointSegment.startTime) / currentWaypointSegment.duration;
            // float positionPercentage = EaseUtils.EaseInOutQuadratic(waypointT);
            float positionPercentage = CalculatePercentageTravelled();

            Vector3 newPosition = Vector3.Lerp(currentWaypointSegment.startPosition, currentWaypointSegment.endPosition, positionPercentage);

            transform.localPosition = newPosition;


            // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
            if (transform.localPosition == waypoint)
            {
                waypoints.Dequeue();
                SetNewEndpoint();
            }
        }
    }

    private float CalculatePercentageTravelled()
    {
        float currentTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);

        float distanceTravelled;
        float accelerationEndTime = currentWaypointSegment.startTime + currentWaypointSegment.accelerationDuration;
        float decelerationStartTime = currentWaypointSegment.startTime + currentWaypointSegment.duration - currentWaypointSegment.accelerationDuration;
        float time = currentTime - currentWaypointSegment.startTime;
        if (currentTime < accelerationEndTime)
        {
            distanceTravelled = 0.5f * acceleration * Mathf.Pow(currentTime - currentWaypointSegment.startTime, 2);
        }
        else if (currentTime < decelerationStartTime)
        {
            distanceTravelled = currentWaypointSegment.accelerationDistance + maxSpeed * (currentTime - accelerationEndTime);
        }
        else if (currentTime < currentWaypointSegment.startTime + currentWaypointSegment.duration)
        {
            distanceTravelled = currentWaypointSegment.distance - 0.5f * acceleration * Mathf.Pow(currentWaypointSegment.duration - time, 2);
        }
        else
        {
            distanceTravelled = currentWaypointSegment.distance;
        }

        return distanceTravelled / currentWaypointSegment.distance;

    }

    private void SetNewEndpoint()
    {
        if (waypoints.Count == 0)
        {
            return;
        }
        Vector3 nextWaypoint = waypoints.Peek();
        float distance = (nextWaypoint - transform.localPosition).magnitude;

        float accelerationDistance = Mathf.Min(0.5f * acceleration * Mathf.Pow(maxSpeed / acceleration, 2), distance / 2);
        float maxSpeedDistance = distance - 2 * accelerationDistance;
        float maxSpeedDuration = maxSpeedDistance / maxSpeed;

        float accelerationDuration = Mathf.Sqrt(2 * accelerationDistance / acceleration);
        // float topSpeed = accelerationDistance
        // float accelerationDuration = maxSpeed / acceleration;
        // float decelerationDuration = accelerationDuration;
        float totalDuration = 2 * accelerationDuration + maxSpeedDuration;

        currentWaypointSegment = new WaypointSegment
        {
            startTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time),
            distance = distance,
            accelerationDistance = accelerationDistance,
            duration = totalDuration,
            accelerationDuration = accelerationDuration,
            startPosition = transform.localPosition,
            endPosition = nextWaypoint
        };
    }


}
