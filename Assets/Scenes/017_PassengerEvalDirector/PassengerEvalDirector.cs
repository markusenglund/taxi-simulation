using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System;

public class PassengerEvalDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    City city;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    Vector3 focusPassengerPosition = new Vector3(0f - 4.5f, 1f, 4.67f);
    Vector3 firstCameraOffset = new Vector3(-1.8f, -0.1f, 0.1f);
    Vector3 zoomedInCameraOffset = new Vector3(-1.5f, -0.1f, -0.1f);
    float timeWhenFocusPassengerSpawns = 3.47f;


    Vector3 finalCameraPosition;
    Vector3 finalLookAtPosition;

    SimulationInfoGroup simulationInfoGroup;

    // A set of passenger IDs that have already spawned a PassengerStats object
    HashSet<int> spawnedPassengerStats = new HashSet<int>();

    void Awake()
    {
        city = City.Create(cityPrefab, cityPosition.x, cityPosition.y, simSettings, graphSettings);
        Time.captureFramerate = 60;

        float cityRightEdge = cityPosition.z + 8;

        finalCameraPosition = new Vector3(14, 16, cityRightEdge);
        finalLookAtPosition = new Vector3(5.5f, 5.7f, cityRightEdge);
    }

    async void Start()
    {

        Camera.main.transform.position = finalCameraPosition;
        Camera.main.transform.LookAt(finalLookAtPosition);
        Camera.main.fieldOfView = 30;
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        Time.timeScale = 1f;
        // PassengerPerson[] savedPersons = SaveData.LoadObject<PassengerPerson[]>(simSettings.randomSeed + "_016");
        // Debug.Log(savedPersons.Length);
        simulationInfoGroup = GameObject.Find("SimulationInfoGroup").GetComponent<SimulationInfoGroup>();
        StartCoroutine(Scene());


    }
    IEnumerator Scene()
    {
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Regular);
        PassengerTripTypeGraph.Create(city);
        StartCoroutine(simulationInfoGroup.FadeInSchedule());

        // Set the canvas to world space
        yield return new WaitForSeconds(simulationStartTime);
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        StartCoroutine(city.StartSimulation());


        float realTimeWhenFocusPassengerSpawns = TimeUtils.ConvertSimulationHoursTimeToRealSeconds(timeWhenFocusPassengerSpawns);

        Vector3 passengerCameraPosition = focusPassengerPosition + firstCameraOffset;

        Camera.main.transform.position = passengerCameraPosition;
        Camera.main.transform.LookAt(focusPassengerPosition);
        Camera.main.fieldOfView = 75;

        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns);
        // Set time to 1/10th of the simulation time
        // yield return new WaitForSeconds(0.5f);
        // yield return StartCoroutine(SlowTime(2));

    }

    void Update()
    {
        Passenger[] passengers = city.GetPassengers();
        // For each passenger, create a PassengerStats object next to them
        for (int i = 0; i < passengers.Length; i++)
        {
            if (spawnedPassengerStats.Contains(passengers[i].person.id))
            {
                continue;
            }
            Passenger passenger = passengers[i];
            if (passenger.person.id == 55)
            {
                StartCoroutine(FocusPassengerSchedule(passenger));
            }
        }
    }

    IEnumerator SlowTime(float duration, float finalTimeScale)
    {
        float startTimeScale = Time.timeScale;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            Time.timeScale = Mathf.Lerp(startTimeScale, finalTimeScale, t);
            yield return null;
        }
    }

    IEnumerator FocusPassengerSchedule(Passenger passenger)
    {
        // passenger.SetMode(PassengerMode.Inactive);
        spawnedPassengerStats.Add(passenger.person.id);

        // city.PauseSimulation();
        yield return new WaitForSeconds(0.8f);
        StartCoroutine(SlowTime(0.7f, 0.02f));
        // Time.timeScale = 0.02f;
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForFrames(30);
        StartCoroutine(SpawnPassengerStats(passenger));
        Quaternion newCameraRotation = Quaternion.Euler(-3.175f, 110, 0);
        StartCoroutine(CameraUtils.RotateCamera(newCameraRotation, 4, Ease.Cubic));
        StartCoroutine(CameraUtils.MoveCamera(focusPassengerPosition + zoomedInCameraOffset, 4, Ease.Cubic));
        yield return new WaitForFrames(4 * 60);
        // yield return new WaitForFrames(4 * 60);
        StartCoroutine(CameraUtils.RotateCameraAround(passenger.transform.position + new Vector3(0, 0, -0.33f), Vector3.up, 10, duration: 10, Ease.Cubic));
        yield return new WaitForFrames(10 * 60);
    }

    IEnumerator SpawnPassengerStats(Passenger passenger)
    {
        Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
        Vector3 statsPosition = new Vector3(-0.25f, 0.19f, 0.1f);
        Quaternion rotation = Quaternion.Euler(0, 25, 0);

        PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, rotation, passenger.person);
        yield return null;
    }


    // IEnumerator ShrinkPassengers(Passenger[] passengers)
    // {
    //     foreach (Passenger passenger in passengers)
    //     {
    //         bool isPassengerDestroyed = passenger == null;
    //         if (isPassengerDestroyed)
    //         {
    //             continue;
    //         }
    //         StartCoroutine(ShrinkPassenger(passenger));
    //         yield return new WaitForSeconds(0.1f);
    //     }
    //     yield return null;
    // }

    // IEnumerator HoverPassengers(Passenger[] passengers)
    // {
    //     foreach (Passenger passenger in passengers)
    //     {
    //         bool isPassengerDestroyed = passenger == null;
    //         if (isPassengerDestroyed)
    //         {
    //             continue;
    //         }
    //         StartCoroutine(HoverPassenger(passenger));
    //         yield return new WaitForSeconds(0.1f);
    //     }
    //     yield return null;
    // }

    // IEnumerator ShrinkPassenger(Passenger passenger)
    // {
    //     passenger.SetMode(PassengerMode.Inactive);
    //     yield return new WaitForSeconds(1);
    //     float shrinkDuration = 1;
    //     if (passenger == null)
    //     {
    //         yield break;
    //     }
    //     Vector3 startScale = passenger.transform.localScale;
    //     Vector3 finalScale = Vector3.one;
    //     float shrinkStartTime = Time.time;
    //     while (Time.time < shrinkStartTime + shrinkDuration)
    //     {
    //         float t = (Time.time - shrinkStartTime) / shrinkDuration;
    //         float scaleFactor = EaseUtils.EaseInOutCubic(t);
    //         if (passenger == null)
    //         {
    //             yield break;
    //         }
    //         passenger.transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
    //         yield return null;
    //     }
    // }

    // IEnumerator HoverPassenger(Passenger passenger)
    // {
    //     yield return new WaitForSeconds(0.5f);
    //     float hoverDuration = 1;
    //     Vector3 startPosition = passenger.transform.position;
    //     Vector3 hoverPosition = startPosition + new Vector3(0, 6f, 0);
    //     float hoverStartTime = Time.time;
    //     while (Time.time < hoverStartTime + hoverDuration)
    //     {
    //         float t = (Time.time - hoverStartTime) / hoverDuration;
    //         float hoverFactor = EaseUtils.EaseInOutCubic(t);
    //         passenger.transform.position = Vector3.Lerp(startPosition, hoverPosition, hoverFactor);
    //         yield return null;
    //     }
    // }


}
