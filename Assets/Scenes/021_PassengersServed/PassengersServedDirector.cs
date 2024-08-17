using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PassengersServedDirector : MonoBehaviour
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

    bool hasSavedPassengerData = false;

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


    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        StartCoroutine(FadeInWorldSpaceCanvas(1));
        StartCoroutine(CameraUtils.RotateCameraAroundRealTime(lookAtPosition, Vector3.up, -60, 100, Ease.Linear));


        GetHorizontalBarValue GetPassengersServed = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            // Get number of passengers who have ordered an Uber and are not queued
            int numPassengersServed = passengers.Count(p => p.trip != null && (p.trip.state == TripState.Completed || p.trip.state == TripState.OnTrip || p.trip.state == TripState.DriverWaiting || p.trip.state == TripState.DriverEnRoute));
            return numPassengersServed;
        };

        HorizontalBarGraph.Create(city1, city2, new Vector3(11, 6), "Which pricing regime serves more passengers during peak hours?", GetPassengersServed);

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