using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TripState
{
    Queued,
    EnRoute,
    OnTrip,
    Completed
}

public class Trip
{
    public Driver driver { get; set; }
    public Passenger passenger { get; set; }

    public TripState state { get; set; }
    public Vector3 driverStartPosition { get; set; }
    public Vector3 pickUpPosition { get; set; }
    public Vector3 destination { get; set; }

    public float timeSpentEnRoute { get; set; }
    public float timeSpentOnTrip { get; set; }

    public float distanceEnRoute { get; set; }
    public float distanceOnTrip { get; set; }


    public float baseFare { get; set; }
    public float surgeMultiplier { get; set; }
    public float fare { get; set; }

    // Driver's cut of the fare, before expenses
    public float driverRevenue { get; set; }

    public float uberRevenue { get; set; }
}
