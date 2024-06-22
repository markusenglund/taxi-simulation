using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.Linq;
using System;

public class PostFirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    [SerializeField] public float simulationStartTime = 4;

    Vector3 lookAtPosition = new Vector3(5.5f, 3, 5.5f);
    City city;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    Vector3 focusPassengerPosition = new Vector3(2.67f - 4.5f, 1f, 0);
    Vector3 passengerCameraPosition = new Vector3(2.67f - 4.5f, 0.9f, -1.1f);

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
    }

    IEnumerator Scene()
    {
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Regular);
        PassengerTripTypeGraph.Create(city);
        StartCoroutine(simulationInfoGroup.FadeInSchedule());

        yield return new WaitForSeconds(0.1f);
        // Set the canvas to world space
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city.StartSimulation());


        float timeWhenFocusPassengerSpawns = 2.47f;
        float realTimeWhenFocusPassengerSpawns = TimeUtils.ConvertSimulationHoursTimeToRealSeconds(timeWhenFocusPassengerSpawns);

        Quaternion passengerCameraRotation = Quaternion.LookRotation(focusPassengerPosition - passengerCameraPosition, Vector3.up);

        // StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(passengerCameraPosition, passengerCameraRotation, realTimeWhenFocusPassengerSpawns, Ease.Cubic, 60));
        StartCoroutine(CameraUtils.MoveCamera(passengerCameraPosition, realTimeWhenFocusPassengerSpawns, Ease.Cubic));
        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns / 3f);

        StartCoroutine(CameraUtils.RotateCamera(passengerCameraRotation, realTimeWhenFocusPassengerSpawns * 2 / 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.ZoomCamera(75, realTimeWhenFocusPassengerSpawns * 2 / 3f, Ease.Cubic));
        yield return new WaitForSeconds(realTimeWhenFocusPassengerSpawns * 2 / 3f);
        yield return new WaitForSeconds(1.5f);



        Quaternion finalCameraRotation = Quaternion.LookRotation(finalLookAtPosition - finalCameraPosition, Vector3.up);
        float duration = TimeUtils.ConvertSimulationHoursTimeToRealSeconds(city.simulationSettings.simulationLengthHours - timeWhenFocusPassengerSpawns);
        StartCoroutine(CameraUtils.MoveCamera(finalCameraPosition, duration, Ease.Cubic));
        StartCoroutine(CameraUtils.RotateCamera(finalCameraRotation, duration * 2 / 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.ZoomCamera(30, duration * 2 / 3f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalCameraPosition, finalCameraRotation, duration, Ease.Cubic, 30));

        yield return null;
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

            if (passenger.person.id == 47)
            {
                Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
                Vector3 statsPosition = new Vector3(-0.15f, 0.2f, 0);
                PassengerStats passengerStats = PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, Quaternion.identity, passenger.person);
                spawnedPassengerStats.Add(passenger.person.id);


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
            }
        }
    }
}
