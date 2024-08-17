using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System;

public class PostFirstSimFreezeDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    City city;


    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    Vector3 focusPassengerPosition = new Vector3(0f - 4.5f, 1f, 6.33f);
    float timeWhenFocusPassengerSpawns = 2.9f;


    Vector3 finalCameraPosition;
    Vector3 finalLookAtPosition;

    public Random driverSpawnRandom;
    SimulationInfoGroup simulationInfoGroup;

    bool hasSavedPassengerData = false;

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

    void Start()
    {

        driverSpawnRandom = new Random(simSettings.randomSeed);
        Camera.main.transform.position = finalCameraPosition;
        Camera.main.transform.LookAt(finalLookAtPosition);
        Camera.main.fieldOfView = 30;
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        Time.timeScale = 1f;
        // PassengerPerson[] savedPersons = SaveData.LoadObject<PassengerPerson[]>(simSettings.randomSeed + "_016");
        // Debug.Log(savedPersons.Length);
        simulationInfoGroup = GameObject.Find("SimulationInfoGroup").GetComponent<SimulationInfoGroup>();
        StartCoroutine(Scene());
        // LogFocusPassengerOptions();
    }

    IEnumerator Scene()
    {
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Regular);
        PassengerTripTypeGraph.Create(city);
        StartCoroutine(simulationInfoGroup.FadeInSchedule());
        Debug.Log("Scene started");
        // Set the canvas to world space
        yield return new WaitForSeconds(simulationStartTime);
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        StartCoroutine(city.StartSimulation());

        float realDurationToWhenFocusPassengerSpawns = TimeUtils.ConvertSimulationHoursDurationToRealSeconds(timeWhenFocusPassengerSpawns);
        Debug.Log(realDurationToWhenFocusPassengerSpawns);
        Vector3 passengerCameraPosition = new Vector3(1.8f, 5, cityPosition.z + 4.6f);
        yield return new WaitForSeconds(realDurationToWhenFocusPassengerSpawns);
        Debug.Log("Focus passenger spawns");
        Time.timeScale = 0.1f;
        StartCoroutine(CameraUtils.MoveCamera(passengerCameraPosition, 1, Ease.Cubic));
        yield return new WaitForSeconds(1);
        StartCoroutine(CameraUtils.MoveCamera(finalCameraPosition, 0.3f, Ease.Cubic));
        Time.timeScale = 1f;
        yield return new WaitForSeconds(1f);
        EditorApplication.isPlaying = false;
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
            // Skip passengers that hasn't received a ride offer yet
            if (passengers[i].person.state == PassengerState.BeforeSpawn || passengers[i].person.state == PassengerState.Idling)
            {
                continue;
            }
        }
        if (city.simulationEnded && !hasSavedPassengerData)
        {
            List<PassengerPerson> persons = new List<PassengerPerson>();
            foreach (Passenger p in passengers)
            {
                persons.Add(p.person);
            }

        }
    }
}
