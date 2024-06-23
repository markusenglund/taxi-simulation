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

public enum DriverMode
{
    Active,
    Inactive
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

    DriverMode mode;

    SimulationSettings simulationSettings;

    private float acceleration = 1000;
    private float maxSpeed;

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

    public static Driver Create(DriverPerson person, Transform prefab, Transform parentTransform, float x, float z, SimulationSettings simSettings, City? city, DriverMode mode = DriverMode.Active)
    {
        Quaternion rotation = x % GridUtils.blockSize == 0 ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

        Transform taxi = Instantiate(prefab, parentTransform, false);
        taxi.localPosition = new Vector3(x, y, z);
        taxi.localRotation = rotation;
        Driver driver = taxi.GetComponent<Driver>();
        driver.city = city;
        driver.driverPerson = person;
        driver.driverPerson.isCurrentlyDriving = true;
        driver.maxSpeed = simSettings.driverSpeed;
        driver.mode = mode;
        driver.simulationSettings = simSettings;
        taxi.name = "Taxi";
        return driver;
    }

    IEnumerator SpawnDriver()
    {
        Transform spawnAnimationPrefab = Resources.Load<Transform>("RespawnAnimation");
        Transform animation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        animation.localScale = Vector3.one * 7f;
        Vector3 finalScale = simulationSettings.driverScale * 0.15f * Vector3.one;
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
        if (mode == DriverMode.Active)
        {
            city.AssignDriverToNextTrip(this);
        }
    }

    IEnumerator PickUpPassenger()
    {
        // Calculate trip pickup data
        float pickedUpTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
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
            marginalCostEnRoute = currentTrip.driverAssignedData.enRouteDistance * simulationSettings.driverMarginalCostPerKm
        };

        if (simulationSettings.showDriverEarnings)
        {
            AgentOverheadText.Create(agentStatusTextPrefab, transform, Vector3.up * 0.9f, $"+${currentTrip.tripCreatedData.fare.driverCut.ToString("F2")}", ColorScheme.green);
        }

        Passenger passenger = currentTrip.tripCreatedData.passenger;
        StartCoroutine(passenger.JumpToCarRoof(duration: 0.5f, this));
        yield return new WaitForSeconds(0.5f);

        // TODO: Figure out how to handle the passenger jump delay, it's now 0.8 seconds
        // yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(simulationSettings.timeSpentWaitingForPassenger));

        currentTrip.PickUpPassenger(pickedUpData, pickedUpDriverData);

        SetDestination(currentTrip.tripCreatedData.destination);
    }


    public void HandleDriverAssigned(Trip trip)
    {
        nextTrip = trip;
        SetState(TaxiState.AssignedToTrip);
        Debug.Log($"Driver {id} assigned to trip at {TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time)}");
    }

    public void HandleDriverDispatched(Trip trip)
    {
        currentTrip = trip;
        nextTrip = null;
        SetDestination(trip.tripCreatedData.pickUpPosition);
        Debug.Log($"Driver {id} dispatched at {TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time)}");
    }

    private IEnumerator DropOffPassenger()
    {
        float droppedOffTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        float timeSpentOnTrip = droppedOffTime - currentTrip.pickedUpData.pickedUpTime;
        float totalTime = droppedOffTime - currentTrip.tripCreatedData.createdTime;
        DroppedOffData droppedOffData = new DroppedOffData
        {
            totalTime = totalTime,
            droppedOffTime = droppedOffTime,
            timeSpentOnTrip = timeSpentOnTrip

        };
        float opportunityCostPerHour = driverPerson.GetOpportunityCostForHour(Mathf.FloorToInt(currentTrip.pickedUpData.pickedUpTime));

        float timeCostOnTrip = timeSpentOnTrip * opportunityCostPerHour;
        float marginalCostOnTrip = currentTrip.tripCreatedData.tripDistance * simulationSettings.driverMarginalCostPerKm;
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
        yield return new WaitForSeconds(0.5f);

        SetState(TaxiState.Idling);
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
            float intervalStartTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time) - intervalHours;
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
        driverPerson.actualSessionEndTimes.Add(TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time));
        driverPerson.isCurrentlyDriving = false;
        Debug.Log($"Driver {id} ended session at {TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time)}");
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
        waypoints = GridUtils.GetWaypoints(transform.localPosition, destination);
        SetNewSegment();
    }

    void Update()
    {
        Debug.DrawLine(transform.position, destination, Color.red);

        if (city.simulationEnded)
        {
            return;
        }

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
                    StartCoroutine(DropOffPassenger());

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

            Vector3 goalDirection = waypoint - currentPosition;
            float turningSpeed = 10f;
            if (goalDirection != Vector3.zero)
            {
                Vector3 newDirection = Vector3.RotateTowards(transform.forward, goalDirection, turningSpeed * Time.deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(newDirection);
            }

            float speed = TimeUtils.ConvertSimulationSpeedPerHourToRealSpeed(CalculateSpeed());

            // Vector3 newPosition = Vector3.Lerp(currentWaypointSegment.startPosition, waypoint, positionPercentage);
            float minSpeed = 0.1f;
            Vector3 newPosition = Vector3.MoveTowards(transform.localPosition, waypoint, Mathf.Max(speed * Time.deltaTime, minSpeed * Time.deltaTime));

            transform.localPosition = newPosition;


            // If the taxi has reached the first waypoint, remove the first endpoint from the endpoints array
            if (transform.localPosition == waypoint)
            {
                waypoints.Dequeue();
            }
        }
    }

    private float CalculateSpeed()
    {
        float currentTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        float time = currentTime - currentWaypointSegment.startTime;
        if (currentTime < currentWaypointSegment.startTime + currentWaypointSegment.accelerationDuration)
        {
            return acceleration * time;
        }
        else if (currentTime < currentWaypointSegment.startTime + currentWaypointSegment.duration - currentWaypointSegment.accelerationDuration)
        {
            return maxSpeed;
        }
        else
        {
            return maxSpeed - acceleration * (currentTime - currentWaypointSegment.startTime - currentWaypointSegment.duration + currentWaypointSegment.accelerationDuration);
        }
    }

    private void SetNewSegment()
    {
        if (waypoints.Count == 0)
        {
            return;
        }
        // Get last element from waypoints queue
        Vector3 lastWaypoint = waypoints.Last();
        // float distance = (nextWaypoint - transform.localPosition).magnitude;
        float distance = GridUtils.GetDistance(transform.localPosition, lastWaypoint);

        float accelerationDistance = Mathf.Min(0.5f * acceleration * Mathf.Pow(maxSpeed / acceleration, 2), distance / 2);
        float maxSpeedDistance = distance - 2 * accelerationDistance;
        float maxSpeedDuration = maxSpeedDistance / maxSpeed;

        float accelerationDuration = Mathf.Sqrt(2 * accelerationDistance / acceleration);

        float totalDuration = 2 * accelerationDuration + maxSpeedDuration;

        currentWaypointSegment = new WaypointSegment
        {
            startTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time),
            distance = distance,
            accelerationDistance = accelerationDistance,
            duration = totalDuration,
            accelerationDuration = accelerationDuration,
            startPosition = transform.localPosition,
            endPosition = lastWaypoint
        };
    }


}
