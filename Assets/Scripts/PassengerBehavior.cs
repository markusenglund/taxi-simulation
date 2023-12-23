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

    private float timeWillingToWait;

    private float utilityFromGettingTaxi;

    private float expectedPickupTime;

    private float hailTime;

    private Graph waitingTimeGraph;

    private UnservedPassengersGraph unservedPassengersGraph;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        timeWillingToWait = Random.Range(20f, 70f);
        utilityFromGettingTaxi = timeWillingToWait + Random.Range(0f, 10f);
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<Graph>();
        unservedPassengersGraph = GameObject.Find("UnservedPassengersGraph").GetComponent<UnservedPassengersGraph>();
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
        }
        else
        {
            Debug.Log("Passenger " + id + " is giving up");
            unservedPassengersGraph.IncrementNumUnservedPassengers();

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
        }
    }
}
