using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;


public class PostFirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    [SerializeField] public float simulationStartTime = 4;

    Vector3 lookAtPosition = new Vector3(5.5f, 3, 5.5f);
    City city;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    Vector3 focusPassengerPosition = new Vector3(2.67f - 4.5f, 0.5f, 0);

    public Random driverSpawnRandom;

    // A set of passenger IDs that have already spawned a PassengerStats object
    HashSet<int> spawnedPassengerStats = new HashSet<int>();

    void Awake()
    {
        city = City.Create(cityPrefab, cityPosition.x, cityPosition.y, simSettings, graphSettings);
        Time.captureFramerate = 60;
    }

    void Start()
    {

        driverSpawnRandom = new Random(simSettings.randomSeed);
        Camera.main.transform.position = new Vector3(2.67f - 4.5f, 1.5f, -2f);
        Camera.main.transform.LookAt(focusPassengerPosition);
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        Time.timeScale = 1f;
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Sim);
        PassengerTripTypeGraph.Create(city, PassengerSpawnGraphMode.Sim);
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city.StartSimulation());


        float timeWhenFocusPassengerSpawns = 2.47f;
        yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursTimeToRealSeconds(timeWhenFocusPassengerSpawns));
        yield return new WaitForSeconds(0.5f);

        float cityRightEdge = cityPosition.z + 8;

        Vector3 finalCameraPosition = new Vector3(14, 16, cityRightEdge);
        Vector3 newLookAtPosition = new Vector3(5.5f, 5.7f, cityRightEdge);
        Quaternion finalCameraRotation = Quaternion.LookRotation(newLookAtPosition - finalCameraPosition, Vector3.up);
        float duration = TimeUtils.ConvertSimulationHoursTimeToRealSeconds(city.simulationSettings.simulationLengthHours - timeWhenFocusPassengerSpawns);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalCameraPosition, finalCameraRotation, duration, Ease.Cubic, 30));

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
            Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
            Vector3 statsPosition = new Vector3(-0.15f, 0.2f, 0);
            PassengerStats passengerStats = PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, Quaternion.identity, passenger.person.economicParameters);
            spawnedPassengerStats.Add(passenger.person.id);
        }
    }
}
