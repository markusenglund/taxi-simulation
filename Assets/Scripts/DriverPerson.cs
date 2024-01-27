public class DriverPerson
{
    public float[] opportunityCostProfile { get; set; }
    public float baseOpportunityCostPerHour { get; set; }
    public int preferredSessionLength { get; set; }

    public DriverSession session { get; set; }

    public float expectedSurplusValue { get; set; }
}
