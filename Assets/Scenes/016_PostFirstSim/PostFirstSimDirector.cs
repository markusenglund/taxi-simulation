using System;
using System.Collections;
using UnityEngine;
using Random = System.Random;


public class PostFirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    [SerializeField] public float simulationStartTime = 4;

    Vector3 lookAtPosition = new Vector3(5.5f, 3, 5.5f);
    City city;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    public Random driverSpawnRandom;

    void Awake()
    {
        city = City.Create(cityPrefab, cityPosition.x, cityPosition.y, simSettings, graphSettings);
        Time.captureFramerate = 60;
    }

    void Start()
    {

        driverSpawnRandom = new Random(simSettings.randomSeed);
        Vector3 focusPassengerPosition = new Vector3(2.67f - 4.5f, 0.5f, 0);
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
}
