using UnityEngine;

public enum TripState
{
    Queued,
    DriverAssigned,
    DriverEnRoute,
    DriverWaiting,
    OnTrip,
    Completed
}

public class TripCreatedData
{
    public Passenger passenger { get; set; }

    public float createdTime { get; set; }
    public Vector3 pickUpPosition { get; set; }
    public Vector3 destination { get; set; }
    public float tripDistance { get; set; }
    public float expectedWaitingTime { get; set; }
    public float expectedTripTime { get; set; }
    public Fare fare { get; set; }
    public float expectedPickupTime { get; set; }
}

public class TripCreatedPassengerData
{
    public bool hasAcceptedRideOffer { get; set; }

    public float tripUtilityValue { get; set; }

    public float expectedWaitingCost { get; set; }

    public float expectedTripTimeCost { get; set; }

    public float expectedNetValue { get; set; }

    public float expectedNetUtility { get; set; }

    public float expectedValueSurplus { get; set; }

    public float expectedNetUtilityBeforeVariableCosts { get; set; }
}

public class DriverAssignedData
{
    public float matchedTime { get; set; }
    public Driver driver { get; set; }

    public float enRouteDistance { get; set; }

}

public class DriverDispatchedData
{
    public float driverDispatchedTime { get; set; }
    public Vector3 startPosition { get; set; }
}

public class PickedUpData
{
    public float pickedUpTime { get; set; }
    public float timeSpentEnRoute { get; set; }

    public float waitingTime { get; set; }

}

public class PickedUpPassengerData
{
    public float waitingCost { get; set; }
}

public class PickedUpDriverData
{
    public float timeCostEnRoute { get; set; }
    public float marginalCostEnRoute { get; set; }
}

public class DroppedOffData
{
    public float droppedOffTime { get; set; }
    public float timeSpentOnTrip { get; set; }

}

public class DroppedOffPassengerData
{
    public float tripTimeCost { get; set; }
    public float netValue { get; set; }
    public float netUtility { get; set; }
    public float valueSurplus { get; set; }
    public float utilitySurplus { get; set; }

}

public class DroppedOffDriverData
{
    public float timeCostOnTrip { get; set; }
    public float marginalCostOnTrip { get; set; }
    // Revenue minus marginal costs (not including cost of time)

    public float grossProfit { get; set; }
    // Revenue minus marginal costs minus opportunity cost of time

    public float valueSurplus { get; set; }
    // Dubious measure of driver welfare created from surplus value

    public float utilitySurplus { get; set; }
}

public class Trip
{

    public TripState state { get; set; }

    public TripCreatedData tripCreatedData { get; set; }
    public TripCreatedPassengerData tripCreatedPassengerData { get; set; }
    public DriverAssignedData driverAssignedData { get; set; }
    public DriverDispatchedData driverDispatchedData { get; set; }
    public PickedUpData pickedUpData { get; set; }
    public PickedUpPassengerData pickedUpPassengerData { get; set; }
    public PickedUpDriverData pickedUpDriverData { get; set; }
    public DroppedOffData droppedOffData { get; set; }
    public DroppedOffPassengerData droppedOffPassengerData { get; set; }
    public DroppedOffDriverData droppedOffDriverData { get; set; }

    public Trip(TripCreatedData tripCreatedData, TripCreatedPassengerData tripCreatedPassengerData)
    {
        state = TripState.Queued;
        this.tripCreatedData = tripCreatedData;
        this.tripCreatedPassengerData = tripCreatedPassengerData;
    }

    public void AssignDriver(
        Driver driver,
        float enRouteDistance
    )
    {
        state = TripState.DriverAssigned;
        float matchedTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        driverAssignedData = new DriverAssignedData
        {
            matchedTime = matchedTime,
            driver = driver,
            enRouteDistance = enRouteDistance
        };
    }

    public void DispatchDriver(Vector3 startPosition)
    {
        state = TripState.DriverEnRoute;
        float driverDispatchedTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);

        driverDispatchedData = new DriverDispatchedData
        {
            startPosition = startPosition,
            driverDispatchedTime = driverDispatchedTime
        };
    }

    public void HandleDriverArrivedAtPickUp()
    {
        state = TripState.DriverWaiting;
        this.tripCreatedData.passenger.HandleDriverArrivedAtPickUp();
    }

    public void PickUpPassenger(PickedUpData pickedUpData, PickedUpDriverData pickedUpDriverData)
    {
        state = TripState.OnTrip;
        this.pickedUpData = pickedUpData;
        this.pickedUpDriverData = pickedUpDriverData;
        this.pickedUpPassengerData = this.tripCreatedData.passenger.HandlePassengerPickedUp(pickedUpData);
    }

    public void DropOffPassenger(DroppedOffData droppedOffData, DroppedOffDriverData droppedOffDriverData)
    {
        state = TripState.Completed;
        this.droppedOffData = droppedOffData;
        this.droppedOffDriverData = droppedOffDriverData;
        this.droppedOffPassengerData = this.tripCreatedData.passenger.HandlePassengerDroppedOff(droppedOffData);
    }
}
