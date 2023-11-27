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
    public Vector3 positionActual;

    public PassengerState state = PassengerState.Idling;

    public static Transform Create(Transform prefab, float x, float z)
    {

        Quaternion rotation = Quaternion.identity;

        float xVisual = x;
        float zVisual = z;

        if (x % 4 == 0)
        {
            xVisual = x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (z % 4 == 0)
        {
            zVisual = z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Transform passenger = Instantiate(prefab, new Vector3(xVisual, 0.08f, zVisual), rotation);
        passenger.name = "Passenger";
        passenger.GetComponent<PassengerBehavior>().positionActual = new Vector3(x, 0.08f, z);
        return passenger;
    }

    void Start()
    {
        // Hail a cab
        GameManager.Instance.HailTaxi(this);
    }
}
