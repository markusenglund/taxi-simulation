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
    Vector3 focusPassengerPosition = new Vector3(0f - 4.5f, 1f, 4.33f);
    float timeWhenFocusPassengerSpawns = 2.3f;


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

        Vector3 passengerCameraPosition = focusPassengerPosition + new Vector3(-1.8f, -0.1f, 0f);

        Camera.main.transform.position = passengerCameraPosition;
        Camera.main.transform.LookAt(focusPassengerPosition);
        Camera.main.fieldOfView = 75;

        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns);
        // Set time to 1/10th of the simulation time
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(SlowTime(2));

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
            if (passengers[i].person.id == 44)
            {
                passengers[i].SetMode(PassengerMode.Inactive);
            }
            // Skip passengers that hasn't received a ride offer yet
            // if (passengers[i].person.state == PassengerState.BeforeSpawn || passengers[i].person.state == PassengerState.Idling)
            // {
            //     continue;
            // }


            Passenger passenger = passengers[i];


            if (passenger.person.id == 44 || passenger.person.id == 3)
            {
                StartCoroutine(SpawnPassengerStats(passenger));
            }


        }
    }

    IEnumerator SlowTime(float duration)
    {
        float startTimeScale = Time.timeScale;
        float startTime = Time.time;
        float finalTimeScale = 0.1f;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            Time.timeScale = Mathf.Lerp(startTimeScale, finalTimeScale, t);
            yield return null;
        }
    }

    IEnumerator SpawnPassengerStats(Passenger passenger)
    {
        spawnedPassengerStats.Add(passenger.person.id);
        yield return new WaitForSeconds(0.5f);
        Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
        Vector3 statsPosition = new Vector3(-0.24f, 0.19f, -0.02f);
        Quaternion rotation = Quaternion.Euler(0, 5, 0);

        PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, rotation, passenger.person);
    }
}
