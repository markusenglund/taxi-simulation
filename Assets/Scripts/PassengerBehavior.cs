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

public class PassengerBehavior : MonoBehaviour
{

    [SerializeField] public Transform spawnAnimationPrefab;
    public Vector3 positionActual;

    private TaxiBehavior taxi;

    static int incrementalId = 1;
    public int id;

    public PassengerState state = PassengerState.Idling;


    private float expectedPickupTime;

    private float hailTime;

    private Vector3 destination;

    private Graph waitingTimeGraph;

    private PassengersGraph passengersGraph;

    private PassengersScatterPlot passengersScatterPlot;


    // Economic parameters

    const double medianIncome = 20;
    private double hourlyIncome;

    private double tripUtilityScore;

    private double tripUtilityValue;

    private double timePreference;

    private double waitingCostPerHour;

    private float utilityFromGettingTaxi;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        destination = Utils.GetRandomPosition();
        GenerateEconomicParameters();
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<Graph>();
        passengersGraph = GameObject.Find("PassengersGraph").GetComponent<PassengersGraph>();
        passengersScatterPlot = GameObject.Find("PassengersScatterPlot").GetComponent<PassengersScatterPlot>();
    }

    void GenerateEconomicParameters()
    {
        GenerateHourlyIncome();
        GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        timePreference = Math.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(1.5, 0.5, 0, 3);
        waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        tripUtilityValue = tripUtilityScore * hourlyIncome;
        Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);
    }

    void GenerateHourlyIncome()
    {
        double mu = 0;
        double sigma = 0.7;
        // with mu=0, sigma=0.7, medianIncome=20  this a distribution with mean=25.6, median=20 and 1.1% of the population with income > 100
        hourlyIncome = medianIncome * StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);
        Debug.Log("Passenger " + id + " hourly income is " + hourlyIncome);
    }


    void GenerateTripUtilityScore()
    {
        float tripDistance = Utils.GetDistance(positionActual, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        double mu = 0;
        double sigma = 0.4;
        tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);
        Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
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
        double waitingCost = expectedWaitingTime * waitingCostPerHour;
        double netUtilityValueFromRide = tripUtilityValue - waitingCost - fare;
        Debug.Log("Passenger " + id + " Net utility $ from ride: " + netUtilityValueFromRide);
        Debug.Log("Passenger " + id + " - fare $: " + fare + ", waiting cost $: " + waitingCost + " for waiting " + expectedWaitingTime + " hours");
        if (netUtilityValueFromRide > 0)
        {
            Debug.Log("Passenger " + id + " is hailing a taxi");
            GameManager.Instance.HailTaxi(this);
            hailTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            expectedPickupTime = hailTime + expectedWaitingTime;
            passengersScatterPlot.AppendPassenger(tripUtilityScore, hourlyIncome, true);
        }
        else
        {
            Debug.Log("Passenger " + id + " is giving up");
            passengersGraph.IncrementNumUnservedPassengers();
            passengersScatterPlot.AppendPassenger(tripUtilityScore, hourlyIncome, false);

            Destroy(gameObject);
        }

    }


    public static Transform Create(Transform prefab, float x, float z)
    {

        Quaternion rotation = Quaternion.identity;

        float xVisual = x;
        float zVisual = z;

        if (x % (Utils.blockSize) == 0)
        {
            xVisual = x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (z % (Utils.blockSize) == 0)
        {
            zVisual = z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Transform passenger = Instantiate(prefab, new Vector3(xVisual, 0.08f, zVisual), rotation);
        passenger.name = "Passenger";
        PassengerBehavior passengerComponent = passenger.GetComponent<PassengerBehavior>();
        passengerComponent.positionActual = new Vector3(x, 0.08f, z);
        return passenger;
    }


    public void SetState(PassengerState state, TaxiBehavior taxi = null)
    {
        this.state = state;
        this.taxi = taxi;

        if (state == PassengerState.PickedUp)
        {
            float actualPickupTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            float actualWaitingTime = actualPickupTime - hailTime;
            float utilitySurplus = utilityFromGettingTaxi - actualWaitingTime;
            Debug.Log("Passenger " + id + " was picked up at " + actualPickupTime + ", expected pickup time was " + expectedPickupTime + ", difference is " + (actualPickupTime - expectedPickupTime));
            Debug.Log("Surplus gained by passenger " + id + " is " + utilitySurplus);
            waitingTimeGraph.SetNewValue(actualWaitingTime);
            passengersGraph.IncrementNumPickedUpPassengers();
        }
    }
}
