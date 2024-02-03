using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public enum TaxiState
{
    Idling,
    AssignedToTrip
}

public class Driver : MonoBehaviour
{



    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private Vector3 destination;
    public TaxiState state = TaxiState.Idling;

    static int incrementalId = 1;
    public int id;

    private Trip currentTrip = null;
    private Trip nextTrip = null;

    private bool isEndingSession = false;

    public DriverPerson driverPerson;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
    }

    public static Driver Create(DriverPerson person, Transform prefab, float x, float z)
    {
        Transform taxi = Instantiate(prefab, new Vector3(x, 0.05f, z), Quaternion.identity);
        Driver driver = taxi.GetComponent<Driver>();
        driver.driverPerson = person;
        driver.driverPerson.isCurrentlyDriving = true;
        taxi.name = "Taxi";
        return driver;
    }

    IEnumerator PickUpPassenger()
    {
        yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(SimulationSettings.timeSpentWaitingForPassenger));
        // Put the passenger on top of the taxi cab
        Passenger passenger = currentTrip.tripCreatedData.passenger;
        passenger.transform.SetParent(transform);
        float middleTaxiX = 0.09f;
        float topTaxiY = 0.08f;
        passenger.transform.localPosition = new Vector3(middleTaxiX, topTaxiY, 0);
        passenger.transform.localRotation = Quaternion.identity;

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
            marginalCostEnRoute = currentTrip.driverAssignedData.enRouteDistance * SimulationSettings.driverMarginalCostPerKm
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
        float opportunityCostPerHour = driverPerson.GetOpportunityCostForHour(Mathf.FloorToInt(currentTrip.pickedUpData.pickedUpTime));

        float timeCostOnTrip = timeSpentOnTrip * opportunityCostPerHour;
        float marginalCostOnTrip = currentTrip.tripCreatedData.tripDistance * SimulationSettings.driverMarginalCostPerKm;
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
            SetTaxiColor();
            GameManager.Instance.HandleTripCompleted(this);
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
            float realSpeed = TimeUtils.ConvertSimulationSpeedPerHourToRealSpeed(SimulationSettings.driverSpeed);

            float distanceDelta = realSpeed * Time.deltaTime;

            // If the taxi is very close to the destination, drive slower
            if ((destination - transform.position).magnitude < 0.15f)
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
