using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    // TODO: Make this value a random number in some reasonable range
    private float timeWillingToWait = 30f;


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
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
        }
        else
        {
            Debug.Log("Passenger " + id + " is giving up");

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


    // TODO: Implement a method that sets the state of the passenger and allocates a taxi car to it
    public void SetState(PassengerState state, TaxiBehavior taxi = null)
    {
        this.state = state;
        this.taxi = taxi;
    }
}
