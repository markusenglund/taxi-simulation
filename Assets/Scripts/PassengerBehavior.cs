using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerBehavior : MonoBehaviour
{
    public static Transform Create(Transform prefab, float x, float z)
    {
        Quaternion rotation = Quaternion.identity;
        if (x % 4 == 0)
        {
            x = x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (z % 4 == 0)
        {
            z = z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Transform passenger = Instantiate(prefab, new Vector3(x, 0.08f, z), rotation);
        passenger.name = "Passenger";
        return passenger;
    }
}
