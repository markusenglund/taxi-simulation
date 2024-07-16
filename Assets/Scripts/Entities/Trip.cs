using Newtonsoft.Json;
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
    [JsonIgnore]
    public Passenger passenger { get; set; }

    public float createdTime { get; set; }
    public Vector3 pickUpPosition { get; set; }
    public Vector3 destination { get; set; }
    public float tripDistance { get; set; }
    public float expectedWaitingTime { get; set; }
    public float expectedTripTime { get; set; }
    public Fare fare { get; set; }
    public float expectedPickupTime { get; set; }
    public int numTripsAssigned { get; set; }
}

public class TripCreatedPassengerData
{

    public float expectedWaitingCost { get; set; }

    public float expectedTripTimeCost { get; set; }

    public float expectedValueSurplus { get; set; }

    public float expectedUtilitySurplus { get; set; }
}

public class DriverAssignedData
{
    public float matchedTime { get; set; }
    [JsonIgnore]
    public Driver driver { get; set; }


}

public class DriverDispatchedData
{
    public float driverDispatchedTime { get; set; }
    public Vector3 startPosition { get; set; }
    public float enRouteDistance { get; set; }
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
    public float totalTime { get; set; }
    public float droppedOffTime { get; set; }
    public float timeSpentOnTrip { get; set; }

}

public class DroppedOffPassengerData
{
    public float tripTimeCost { get; set; }
    public float valueSurplus { get; set; }
    public float utilitySurplus { get; set; }
    public float totalTimeCost { get; set; }
    public float totalCost { get; set; }

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
        Driver driver
    )
    {
        state = TripState.DriverAssigned;
        float matchedTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        driverAssignedData = new DriverAssignedData
        {
            matchedTime = matchedTime,
            driver = driver,
        };
    }

    public void DispatchDriver(Vector3 startPosition, float enRouteDistance)
    {
        state = TripState.DriverEnRoute;
        float driverDispatchedTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);

        driverDispatchedData = new DriverDispatchedData
        {
            startPosition = startPosition,
            driverDispatchedTime = driverDispatchedTime,
            enRouteDistance = enRouteDistance
        };
    }

    public void HandleDriverArrivedAtPickUp()
    {
        state = TripState.DriverWaiting;
        this.tripCreatedData.passenger.HandleDriverArrivedAtPickUp();
    }

    // The moment of pickup is when the pick up waiting time starts, so the trip time includes the waiting time
    public void PickUpPassenger(PickedUpData pickedUpData, PickedUpDriverData pickedUpDriverData)
    {
        state = TripState.OnTrip;
        this.pickedUpData = pickedUpData;
        this.pickedUpDriverData = pickedUpDriverData;
        this.pickedUpPassengerData = this.tripCreatedData.passenger.HandlePassengerPickedUp(pickedUpData);
        string minutesLate = ((pickedUpData.pickedUpTime - this.tripCreatedData.expectedPickupTime) * 60).ToString("F2");
        if (this.driverAssignedData.driver.id == 3)
        {
            Debug.Log($"PICKUP DIFF: {minutesLate} Driver {this.driverAssignedData.driver.id} Picked up passenger {this.tripCreatedData.passenger.person.id} at {pickedUpData.pickedUpTime}, expected pickup time was {this.tripCreatedData.expectedPickupTime}");
        }
    }

    public void DropOffPassenger(DroppedOffData droppedOffData, DroppedOffDriverData droppedOffDriverData)
    {
        state = TripState.Completed;
        this.droppedOffData = droppedOffData;
        this.droppedOffDriverData = droppedOffDriverData;
        this.droppedOffPassengerData = this.tripCreatedData.passenger.HandlePassengerDroppedOff(droppedOffData);
        if (this.driverAssignedData.driver.id == 3)
        {
            string minutesLate = ((droppedOffData.timeSpentOnTrip - tripCreatedData.expectedTripTime) * 60).ToString("F2");
            Debug.Log($"DROPOFF DIFF: {minutesLate} {this.driverAssignedData.driver.id} Dropped off passenger at {droppedOffData.droppedOffTime}");
        }
        // this.tripCreatedData.passenger = null;
    }
}
