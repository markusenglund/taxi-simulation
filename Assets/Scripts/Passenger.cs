using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PassengerState
{
    Idling,
    Waiting,
    Dispatched,
    PickedUp,
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

public class PassengerDecisionData
{

    public bool hasAcceptedRideOffer { get; set; }

    public float decisionTime { get; set; }

    public float expectedPickupTime { get; set; }
    public float expectedWaitingTime { get; set; }
    public float expectedWaitingCost { get; set; }

    public float fare { get; set; }

    public float expectedValueSurplus { get; set; }

    public float expectedUtilitySurplus { get; set; }
}

public class PassengerPickedUpData
{
    public float pickedUpTime { get; set; }
    public float waitingCost { get; set; }
    public float waitingTime { get; set; }
    public float valueSurplus { get; set; }
    public float utilitySurplus { get; set; }
}

public class Passenger : MonoBehaviour
{

    [SerializeField] public Transform spawnAnimationPrefab;
    public Vector3 positionActual;

    private Driver taxi;

    static int incrementalId = 1;
    public int id;

    public PassengerState state = PassengerState.Idling;

    public Vector3 destination;

    private WaitingTimeGraph waitingTimeGraph;

    private PassengersGraph passengersGraph;

    private UtilityIncomeScatterPlot passengersScatterPlot;

    private PassengerSurplusGraph passengerSurplusGraph;

    const float medianIncome = 20;

    public PassengerEconomicParameters passengerEconomicParameters;
    public PassengerDecisionData passengerDecisionData;
    public PassengerPickedUpData passengerPickedUpData;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        destination = GridUtils.GetRandomPosition();
        GenerateEconomicParameters();
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<WaitingTimeGraph>();
        passengersGraph = GameObject.Find("PassengersGraph").GetComponent<PassengersGraph>();
        passengersScatterPlot = GameObject.Find("UtilityIncomeScatterPlot").GetComponent<UtilityIncomeScatterPlot>();

        passengerSurplusGraph = GameObject.Find("PassengerSurplusGraph").GetComponent<PassengerSurplusGraph>();
    }

    void GenerateEconomicParameters()
    {
        float hourlyIncome = GenerateHourlyIncome();
        float tripUtilityScore = GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        float timePreference = Mathf.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(1.5f, 0.5f, 0, 3f);
        float waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        float tripUtilityValue = tripUtilityScore * hourlyIncome;
        Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);
        passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            tripUtilityScore = tripUtilityScore,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            tripUtilityValue = tripUtilityValue
        };
    }


    float GenerateHourlyIncome()
    {
        float mu = 0;
        float sigma = 0.7f;
        // with mu=0, sigma=0.7, medianIncome=20  this a distribution with mean=25.6, median=20 and 1.1% of the population with income > 100
        float hourlyIncome = medianIncome * StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);
        Debug.Log("Passenger " + id + " hourly income is " + hourlyIncome);

        return hourlyIncome;
    }


    float GenerateTripUtilityScore()
    {
        float tripDistance = GridUtils.GetDistance(positionActual, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        float mu = 0;
        float sigma = 0.4f;
        float tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);
        Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
        return tripUtilityScore;
    }
    void Start()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        Invoke("HailTaxiOrBeDestroyed", 1f);
    }

    void HailTaxiOrBeDestroyed()
    {
        float expectedWaitingTime = GameManager.Instance.GetExpectedWaitingTime(this);
        float fare = GameManager.Instance.GetFare(this, destination);
        float expectedWaitingCost = expectedWaitingTime * passengerEconomicParameters.waitingCostPerHour;

        float expectedValueSurplus = passengerEconomicParameters.tripUtilityValue - expectedWaitingCost - fare;
        float expectedUtilitySurplus = expectedValueSurplus;

        Debug.Log("Passenger " + id + " Net utility $ from ride: " + expectedValueSurplus);
        Debug.Log("Passenger " + id + " - fare $: " + fare + ", waiting cost $: " + expectedWaitingCost + " for waiting " + expectedWaitingTime + " hours");
        float decisionTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float expectedPickupTime = decisionTime + expectedWaitingTime;
        bool hasAcceptedRideOffer = expectedValueSurplus > 0;


        passengerDecisionData = new PassengerDecisionData()
        {
            hasAcceptedRideOffer = hasAcceptedRideOffer,
            decisionTime = decisionTime,
            expectedPickupTime = expectedPickupTime,
            expectedWaitingTime = expectedWaitingTime,
            expectedWaitingCost = expectedWaitingCost,
            fare = fare,
            expectedValueSurplus = expectedValueSurplus,
            expectedUtilitySurplus = expectedUtilitySurplus
        };

        passengersScatterPlot.AppendPassenger(this);

        if (hasAcceptedRideOffer)
        {
            Debug.Log("Passenger " + id + " is hailing a taxi");
            GameManager.Instance.HailTaxi(this);
        }
        else
        {
            Debug.Log("Passenger " + id + " is giving up");
            passengersGraph.IncrementNumUnservedPassengers();
            passengerSurplusGraph.AppendPassenger(this);

            Destroy(gameObject);
        }


    }


    public static Transform Create(Transform prefab, float x, float z)
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

        Transform passenger = Instantiate(prefab, new Vector3(xVisual, 0.08f, zVisual), rotation);
        passenger.name = "Passenger";
        Passenger passengerComponent = passenger.GetComponent<Passenger>();
        passengerComponent.positionActual = new Vector3(x, 0.08f, z);
        return passenger;
    }


    public void SetState(PassengerState state, Driver taxi = null)
    {
        this.state = state;
        this.taxi = taxi;

        if (state == PassengerState.PickedUp)
        {
            float pickedUpTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            float waitingTime = pickedUpTime - passengerDecisionData.decisionTime;
            float waitingCost = waitingTime * passengerEconomicParameters.waitingCostPerHour;
            float valueSurplus = passengerEconomicParameters.tripUtilityValue - waitingCost - passengerDecisionData.fare;

            float utilitySurplus = valueSurplus / passengerEconomicParameters.hourlyIncome;
            Debug.Log("Passenger " + id + " was picked up at " + pickedUpTime + ", expected pickup time was " + passengerDecisionData.expectedPickupTime + ", difference is " + (pickedUpTime - passengerDecisionData.expectedPickupTime));
            Debug.Log("Surplus gained by passenger " + id + " is " + utilitySurplus);

            passengerPickedUpData = new PassengerPickedUpData()
            {
                pickedUpTime = pickedUpTime,
                waitingCost = waitingCost,
                waitingTime = waitingTime,
                valueSurplus = valueSurplus,
                utilitySurplus = utilitySurplus
            };


            waitingTimeGraph.SetNewValue(waitingTime);
            passengersGraph.IncrementNumPickedUpPassengers();
            passengerSurplusGraph.AppendPassenger(this);
        }
    }
}
