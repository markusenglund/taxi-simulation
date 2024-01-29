using System.Collections.Generic;

public class DriverPerson
{
    public float[] opportunityCostProfile { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public int preferredSessionLength { get; set; }

    public SessionInterval interval { get; set; }

    public float? actualSessionEndTime { get; set; }

    public float expectedSurplusValue { get; set; }

    public float? actualSurplusValue { get; set; }

    public List<Trip> completedTrips = new List<Trip>();

}
