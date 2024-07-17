using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using System.Linq;

public class EvanFollowUpDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 1;

    City city1;
    City city2;

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 lookAtPosition = new Vector3(6 + 4.5f, 3, 12f);
    Vector3 cameraStartPosition = new Vector3(1f, 1.3f, -4f);

    bool hasFoundFocusPassenger = false;

    CanvasGroup worldSpaceCanvasGroup;

    void Awake()
    {
        Time.captureFramerate = 60;
        city1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        city2 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
    }

    void Start()
    {

        worldSpaceCanvasGroup = GameObject.Find("WorldSpaceCanvas").GetComponent<CanvasGroup>();
        Camera.main.transform.position = cameraStartPosition;
        Vector3 cameraLookAtPosition = lookAtPosition;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        Camera.main.fieldOfView = 60f;
        StartCoroutine(Scene());
    }

    void Update()
    {
        Passenger[] passengers = city2.GetPassengers();
        Passenger focusPassenger = Array.Find(passengers, passenger => passenger.person.id == 14);

        if (focusPassenger != null && !hasFoundFocusPassenger)
        {
            hasFoundFocusPassenger = true;
            StartCoroutine(ShowFocusPassenger(focusPassenger));
        }
    }

    IEnumerator ShowFocusPassenger(Passenger focusPassenger)
    {
        Time.timeScale = 0.8f;
        Vector3 focusPassengerPosition = focusPassenger.transform.position;
        Quaternion focusPassengerRotation = focusPassenger.transform.rotation;
        // Move the camera to just in front of the focus passenger
        Vector3 cameraPosition = focusPassengerPosition - focusPassengerRotation * new Vector3(0, 0, -3.5f) + Vector3.up * 0.75f;
        Vector3 cameraLookAtPosition = focusPassengerPosition + Vector3.up * 0.6f;
        Camera.main.transform.position = cameraPosition;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        Vector3 secondCameraPosition = focusPassengerPosition - focusPassengerRotation * new Vector3(0, 0, -2.5f) + Vector3.up * 0.75f;
        StartCoroutine(CameraUtils.MoveCamera(secondCameraPosition, 1.4f, Ease.Cubic));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(FollowPassenger(focusPassenger.transform, 14));
        yield return null;
    }


    IEnumerator FollowPassenger(Transform target, float duration)
    {
        float startTime = Time.time;
        Vector3 startTargetDirection = target.position + Vector3.up * 0.6f - Camera.main.transform.position;
        while (Time.time < startTime + duration && target != null)
        {
            Vector3 desiredPosition = target.position + Vector3.up * 0.6f - (startTargetDirection * 1.02f);
            Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            // Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
            // Vector3 desiredPosition = new Vector3(middlePosition.x, Camera.main.transform.position.y, middlePosition.z);
            // Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.05f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
            yield return null;
        }
    }


    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        StartCoroutine(FadeInWorldSpaceCanvas(1));

        yield return null;
    }

    IEnumerator SetSimulationStart()
    {
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city1.StartSimulation());
        StartCoroutine(city2.StartSimulation());
    }

    IEnumerator FadeInWorldSpaceCanvas(float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseUtils.EaseInCubic(t);
            worldSpaceCanvasGroup.alpha = t;
            yield return null;
        }
        worldSpaceCanvasGroup.alpha = 1;
    }

}