using UnityEngine;

public enum PassengerState
{
    Idling,
    AssignedToTrip,
    DroppedOff
}

public class PassengerEconomicParameters
{
    // Base values
    public float hourlyIncome { get; set; }
    public float tripUtilityScore { get; set; }
    public float timePreference { get; set; }

    // Derived values
    public float waitingCostPerHour { get; set; }
    public float tripUtilityValue { get; set; }
}

public class Passenger : MonoBehaviour
{

    [SerializeField] public Transform spawnAnimationPrefab;
    public Vector3 positionActual;

    static int incrementalId = 1;
    public int id;

    public float timeCreated;

    public PassengerState state = PassengerState.Idling;

    public Vector3 destination;

    private WaitingTimeGraph waitingTimeGraph;

    private UtilityIncomeScatterPlot utilityIncomeScatterPlot;

    private PassengerSurplusGraph passengerSurplusGraph;


    public bool hasAcceptedRideOffer = false;
    public Trip currentTrip;

    public PassengerEconomicParameters passengerEconomicParameters;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        timeCreated = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        destination = GridUtils.GetRandomPosition();
        GenerateEconomicParameters();
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<WaitingTimeGraph>();
        utilityIncomeScatterPlot = GameObject.Find("UtilityIncomeScatterPlot").GetComponent<UtilityIncomeScatterPlot>();

        passengerSurplusGraph = GameObject.Find("PassengerSurplusGraph").GetComponent<PassengerSurplusGraph>();
    }

    void GenerateEconomicParameters()
    {
        float hourlyIncome = SimulationSettings.GetRandomHourlyIncome();
        float tripUtilityScore = GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        float timePreference = Mathf.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(1.5f, 0.5f, 0, 3f);
        float waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        float tripUtilityValue = tripUtilityScore * hourlyIncome;
        // Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);
        passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            tripUtilityScore = tripUtilityScore,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            tripUtilityValue = tripUtilityValue
        };
    }


    float GenerateTripUtilityScore()
    {
        float tripDistance = GridUtils.GetDistance(positionActual, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        float mu = 0;
        float sigma = 0.4f;
        float tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);
        // Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
        return tripUtilityScore;
    }
    void Start()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        Invoke("MakeTripDecision", 1f);
    }

    void MakeTripDecision()
    {
        RideOffer rideOffer = GameManager.Instance.RequestRideOffer(positionActual, destination);


        float tripCreatedTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float expectedPickupTime = tripCreatedTime + rideOffer.expectedWaitingTime;

        TripCreatedData tripCreatedData = new TripCreatedData()
        {
            passenger = this,
            createdTime = tripCreatedTime,
            pickUpPosition = positionActual,
            destination = destination,
            tripDistance = GridUtils.GetDistance(positionActual, destination),
            expectedWaitingTime = rideOffer.expectedWaitingTime,
            fare = rideOffer.fare,
            expectedPickupTime = expectedPickupTime
        };

        float expectedWaitingCost = rideOffer.expectedWaitingTime * passengerEconomicParameters.waitingCostPerHour;

        float expectedValueSurplus = passengerEconomicParameters.tripUtilityValue - expectedWaitingCost - rideOffer.fare.total;
        float expectedUtilitySurplus = expectedValueSurplus / passengerEconomicParameters.hourlyIncome;
        hasAcceptedRideOffer = expectedValueSurplus > 0;

        // Debug.Log("Passenger " + id + " - fare $: " + rideOffer.fare.total + ", waiting cost $: " + expectedWaitingCost + " for waiting " + rideOffer.expectedWaitingTime + " hours");
        // Debug.Log("Passenger " + id + " Net expected utility $ from ride: " + expectedValueSurplus);
        TripCreatedPassengerData tripCreatedPassengerData = new TripCreatedPassengerData()
        {
            hasAcceptedRideOffer = hasAcceptedRideOffer,
            tripUtilityValue = passengerEconomicParameters.tripUtilityValue,
            expectedWaitingCost = expectedWaitingCost,
            expectedValueSurplus = expectedValueSurplus,
            expectedUtilitySurplus = expectedUtilitySurplus
        };

        utilityIncomeScatterPlot.AppendPassenger(this);

        if (hasAcceptedRideOffer)
        {
            // Debug.Log("Passenger " + id + " is hailing a taxi");
            currentTrip = GameManager.Instance.AcceptRideOffer(tripCreatedData, tripCreatedPassengerData);
            SetState(PassengerState.AssignedToTrip);
        }
        else
        {
            // Debug.Log("Passenger " + id + " is giving up");
            passengerSurplusGraph.AppendPassenger(this);

            Destroy(gameObject);
        }


    }


    public static Passenger Create(Transform prefab, float x, float z)
    {

        Quaternion rotation = Quaternion.identity;

        float xVisual = x;
        float zVisual = z;

        if (x % GridUtils.blockSize == 0)
        {
            xVisual = x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (z % GridUtils.blockSize == 0)
        {
            zVisual = z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Transform passengerTransform = Instantiate(prefab, new Vector3(xVisual, 0.08f, zVisual), rotation);
        passengerTransform.name = "Passenger";
        Passenger passenger = passengerTransform.GetComponent<Passenger>();
        passenger.positionActual = new Vector3(x, 0.08f, z);
        return passenger;
    }

    public PickedUpPassengerData HandlePassengerPickedUp(PickedUpData pickedUpData)
    {
        float waitingCost = pickedUpData.waitingTime * passengerEconomicParameters.waitingCostPerHour;
        float valueSurplus = currentTrip.tripCreatedPassengerData.tripUtilityValue - waitingCost - currentTrip.tripCreatedData.fare.total;

        float utilitySurplus = valueSurplus / passengerEconomicParameters.hourlyIncome;
        Debug.Log($"Passenger {id} was picked up at {TimeUtils.ConvertSimulationHoursToTimeString(pickedUpData.pickedUpTime)}, expected pickup time was {TimeUtils.ConvertSimulationHoursToTimeString(currentTrip.tripCreatedData.expectedPickupTime)}, difference is {(pickedUpData.pickedUpTime - currentTrip.tripCreatedData.expectedPickupTime) * 60f} minutes");

        Debug.Log($"Surplus gained by passenger {id} is {utilitySurplus}");

        PickedUpPassengerData passengerPickedUpData = new PickedUpPassengerData()
        {
            waitingCost = waitingCost,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

        waitingTimeGraph.SetNewValue(pickedUpData.waitingTime);
        passengerSurplusGraph.AppendPassenger(this);

        return passengerPickedUpData;
    }
    public void SetState(PassengerState state)
    {
        this.state = state;
    }

    public void HandlePassengerDroppedOff()
    {
        this.transform.parent = null;
        SetState(PassengerState.DroppedOff);
        Destroy(gameObject);
    }
}


