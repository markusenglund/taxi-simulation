using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System;

public class PostFirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    City city;

    // private float simulationStartTime = 0.1f;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    Vector3 focusPassengerPosition = new Vector3(0f - 4.5f, 1f, 4.67f);

    Vector3 firstCameraOffset = new Vector3(-1.8f, -0.1f, 0.1f);

    float timeWhenFocusPassengerSpawns = 3.47f;


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

    void LogFocusPassengerOptions()
    {
        PassengerPerson[] savedPersons = SaveData.LoadObject<PassengerPerson[]>(simSettings.randomSeed + "_016");
        // Sort passengers by the totalCost of bestSubstitute, starting from the higher cost
        if (savedPersons == null)
        {
            Debug.Log("No saved passengers found");
            return;
        }
        List<PassengerPerson> sortedPersons = savedPersons
            .Where(person => person.rideOfferStatus == RideOfferStatus.NoneReceived)
            .OrderByDescending(person => person.economicParameters.timePreference)
            .ToList();
        Debug.Log($"Spawned {savedPersons.Length} agents, No ride offer: {sortedPersons.Count}");
        // Show the best substitute cost, timeSensitivity, hourlyIncome, and time cost of best substitute for the top 5 passengers
        Debug.Log("Top 5 passengers who were screwed by not getting a ride offer:");
        for (int i = 0; i < 3; i++)
        {
            PassengerPerson person = sortedPersons[i];
            TripOption bestSubstitute = person.economicParameters.GetBestSubstitute();
            Debug.Log($"Person {person.id} - Spawn position: {person.startPosition} Spawn time: {person.timeSpawned} Best substitute cost: {bestSubstitute.totalCost}, timeSensitivity: {person.economicParameters.timePreference}, hourlyIncome: {person.economicParameters.hourlyIncome}, time of best substitute: {bestSubstitute.timeHours}");
        }

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
        Quaternion passengerCameraRotation = Quaternion.LookRotation(focusPassengerPosition - passengerCameraPosition, Vector3.up);


        StartCoroutine(CameraUtils.MoveCamera(passengerCameraPosition, realTimeWhenFocusPassengerSpawns, Ease.Cubic));
        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns / 3f);

        StartCoroutine(CameraUtils.RotateCamera(passengerCameraRotation, realTimeWhenFocusPassengerSpawns * 2 / 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.ZoomCamera(75, realTimeWhenFocusPassengerSpawns * 2 / 3f, Ease.Cubic));
        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns * 2 / 3f);
        yield return new WaitForSeconds(1.5f);

        Quaternion finalCameraRotation = Quaternion.LookRotation(finalLookAtPosition - finalCameraPosition, Vector3.up);
        float duration = -1.5f + TimeUtils.ConvertSimulationHoursTimeToRealSeconds(city.simulationSettings.simulationLengthHours - timeWhenFocusPassengerSpawns);
        StartCoroutine(CameraUtils.MoveCamera(finalCameraPosition, duration, Ease.Cubic));
        StartCoroutine(CameraUtils.RotateCamera(finalCameraRotation, duration * 2 / 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.ZoomCamera(30, duration * 2 / 3f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalCameraPosition, finalCameraRotation, duration, Ease.Cubic, 30));
        yield return new WaitForSeconds(duration);

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


            Passenger passenger = passengers[i];
            // List<PassengerPerson> savedPersons = SaveData.LoadObject<List<PassengerPerson>>(simSettings.randomSeed + "_016");
            // Debug.Log(savedPersons.Count);

            // if (passenger.person.id == 44)
            // {
            //     Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
            //     Vector3 statsPosition = new Vector3(-0.15f, 0.2f, 0);
            //     PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, Quaternion.identity, passenger.person);
            //     spawnedPassengerStats.Add(passenger.person.id);


            // }
        }
        if (city.simulationEnded && !hasSavedPassengerData)
        {
            List<PassengerPerson> persons = new List<PassengerPerson>();
            foreach (Passenger p in passengers)
            {
                persons.Add(p.person);
            }
            Debug.Log($"Saving passenger data from {persons.Count} passengers");
            SaveData.SaveObject(simSettings.randomSeed + "_016", persons);
            hasSavedPassengerData = true;
            LogFocusPassengerOptions();

        }
    }
}
