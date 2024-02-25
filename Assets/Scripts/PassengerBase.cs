using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerBase : MonoBehaviour
{
    private float spawnDuration;
    [SerializeField] public Transform spawnAnimationPrefab;

    public static PassengerBase Create(Transform prefab, Vector3 position, Vector3 lookAt, float spawnDuration)
    {
        Transform passengerTransform = Instantiate(prefab, position, Quaternion.identity);
        passengerTransform.LookAt(lookAt);
        PassengerBase passenger = passengerTransform.GetComponent<PassengerBase>();
        passenger.spawnDuration = spawnDuration;
        return passenger;
    }

    void Start()
    {
        StartCoroutine(SpawnPassenger());
    }

    IEnumerator SpawnPassenger()
    {
        Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);

        transform.localScale = Vector3.zero;
        float startTime = Time.time;
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseInOutCubic(t);
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    float EaseInOutCubic(float t)
    {
        float t2;
        if (t <= 0.5f)
        {
            t2 = Mathf.Pow(t * 2, 3) / 2;
        }
        else
        {
            t2 = (2 - Mathf.Pow((1 - t) * 2, 3)) / 2;
        }
        return t2;
    }
}
