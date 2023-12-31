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

    private Graph waitingTimeGraph;

    private PassengersGraph passengersGraph;

    private PassengersScatterPlot passengersScatterPlot;


    // Economic parameters
    private double hourlyIncome;

    private float tripUtilityScore;

    private float timePreference;

    private float waitingCostPerHour;

    private float utilityScorePerDollar;

    // Represents the utility of the ride measured in dollars
    private float moneyWillingToSpend;

    private float timeWillingToWait;

    // private float moneyWillingToSpend;

    private float utilityFromGettingTaxi;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        GenerateEconomicParameters();
        timeWillingToWait = UnityEngine.Random.Range(20f, 70f);
        moneyWillingToSpend = UnityEngine.Random.Range(20f, 180f);
        utilityFromGettingTaxi = timeWillingToWait + UnityEngine.Random.Range(0f, 10f);
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<Graph>();
        passengersGraph = GameObject.Find("PassengersGraph").GetComponent<PassengersGraph>();
        passengersScatterPlot = GameObject.Find("PassengersScatterPlot").GetComponent<PassengersScatterPlot>();
    }

    void GenerateEconomicParameters()
    {
        // mu=3, sigma=0.7 creates a distribution with mean=25.7, median=20 and 1.1% of the population with income > 100
        double mu = 3;
        double sigma = 0.7;
        hourlyIncome = StatisticsUtils.getRandomFromLogNormalDistribution(mu, sigma);

        Debug.Log("Hourly income is " + hourlyIncome);
    }

    void Start()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        Invoke("HailTaxiOrBeDestroyed", 1f);
    }

    void HailTaxiOrBeDestroyed()
    {
        float expectedWaitingTime = GameManager.Instance.GetExpectedWaitingTime(this);
        Debug.Log("Expected waiting time for passenger " + id + " is " + expectedWaitingTime + ", is willing to wait " + timeWillingToWait);
        if (expectedWaitingTime < timeWillingToWait)
        {
            GameManager.Instance.HailTaxi(this);
            expectedPickupTime = Time.time + expectedWaitingTime;
            hailTime = Time.time;
            passengersScatterPlot.AppendPassenger(timeWillingToWait, true, moneyWillingToSpend);
        }
        else
        {
            Debug.Log("Passenger " + id + " is giving up");
            passengersGraph.IncrementNumUnservedPassengers();
            passengersScatterPlot.AppendPassenger(timeWillingToWait, false
            , moneyWillingToSpend);

            Destroy(gameObject);
        }

        // TODO: Calculate the actual waiting time and compare to the expected time

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
            float actualPickupTime = Time.time;
            float actualWaitingTime = actualPickupTime - hailTime;
            float utilitySurplus = utilityFromGettingTaxi - actualWaitingTime;
            Debug.Log("Passenger " + id + " was picked up at " + actualPickupTime + ", expected pickup time was " + expectedPickupTime + ", difference is " + (actualPickupTime - expectedPickupTime));
            Debug.Log("Surplus gained by passenger " + id + " is " + utilitySurplus);
            waitingTimeGraph.SetNewValue(actualWaitingTime);
            passengersGraph.IncrementNumPickedUpPassengers();
        }
    }
}
