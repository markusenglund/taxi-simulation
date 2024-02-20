using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TestSceneDirector : MonoBehaviour
{

    [SerializeField] public Transform passengerPrefab;
    Vector3 passengerPosition = new Vector3(1.7f, 0.08f, 0.22f);

    void Start()
    {
        Camera.main.transform.LookAt(passengerPosition);
        GridUtils.GenerateStreetGrid(null);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return new WaitForSeconds(1);
        Transform passenger = Instantiate(passengerPrefab, passengerPosition, Quaternion.identity);
        Vector3 lookAt = Camera.main.transform.position;
        lookAt.y = passenger.position.y;
        passenger.LookAt(lookAt);
        yield return new WaitForSeconds(1);
        // Zoom out
        StartCoroutine(MoveAway(1, 1));
        yield return new WaitForSeconds(1);
        StartCoroutine(RotateCamera(360, 2));

    }

    IEnumerator MoveAway(float distance, float duration)
    {
        float startTime = Time.time;
        Vector3 startPosition = Camera.main.transform.position;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            // Figure out the direction we want to move in
            Vector3 direction = -Camera.main.transform.forward;
            // Calculate the new position based t * the distance between the start and end position
            t = EaseInOutCubic(t);
            Vector3 newPosition = startPosition + direction * distance * t;
            // Set the camera's position to the new position
            Camera.main.transform.position = newPosition;
            yield return null;
        }
    }

    IEnumerator RotateCamera(float angle, float duration)
    {
        float startTime = Time.time;
        float prevT = 0;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseInOutCubic(t);
            Camera.main.transform.RotateAround(passengerPosition, Vector3.up, angle * (t - prevT));
            prevT = t;
            yield return null;
        }
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
