using System;
using System.Collections.Generic;

public class DriverPerson
{

    public bool isCurrentlyDriving = false;
    public float[] opportunityCostProfile { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public int preferredSessionLength { get; set; }

    public SessionInterval interval { get; set; }

    public List<float> actualSessionEndTimes = new List<float>();

    public float expectedSurplusValue { get; set; }

    public float[] expectedSurplusValueByHour { get; set; }

    public float[] expectedGrossProfitByHour { get; set; }

    public float? actualSurplusValue { get; set; }

    public List<Trip> completedTrips = new List<Trip>();


    public float[] GetOpportunityCostPerHour()
    {
        float[] opportunityCostPerHour = new float[24];
        for (int i = 0; i < 24; i++)
        {
            float baseOpportunityCost = baseOpportunityCostPerHour * opportunityCostProfile[i];
            opportunityCostPerHour[i] = baseOpportunityCost;
        }
        return opportunityCostPerHour;
    }

    // public float GetOvertimeCostForHour(int hoursSinceSessionStart)
    // {
    //     int numHoursOverPreferredLength = Math.Max(hoursSinceSessionStart + 1 - preferredSessionLength, 0);
    //     // Cost of deviatiating one additional hour, is the size of the deviation * baseOpportunityCostPerHour / 4
    //     float overtimeCost = baseOpportunityCostPerHour * numHoursOverPreferredLength / 4;

    //     return overtimeCost;

    // }

}
