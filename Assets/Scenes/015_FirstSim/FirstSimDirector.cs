using System.Collections;
using UnityEngine;
using Random = System.Random;


public class FirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    [SerializeField] public float simulationStartTime = 20;

    Vector3 lookAtPosition = new Vector3(5.5f, 3, 5.5f);
    City city;

    Vector3 cityPosition = new Vector3(-4.5f, 0, 0f);
    public Random driverSpawnRandom;

    void Awake()
    {
        city = City.Create(cityPrefab, cityPosition.x, cityPosition.y, simSettings, graphSettings, spawnInitialDrivers: false);
        Time.captureFramerate = 60;


    }

    void Start()
    {

        driverSpawnRandom = new Random(simSettings.randomSeed);

        Camera.main.transform.position = new Vector3(10f, 10f, 0);
        Camera.main.transform.LookAt(lookAtPosition);
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        float cityRightEdge = cityPosition.z + 8;
        Vector3 finalCameraPosition = new Vector3(14, 16, cityRightEdge);
        Vector3 newLookAtPosition = new Vector3(5.5f, 5.7f, cityRightEdge);
        Quaternion finalCameraRotation = Quaternion.LookRotation(newLookAtPosition - finalCameraPosition, Vector3.up);
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Sim);
        PassengerTripTypeGraph.Create(city, PassengerSpawnGraphMode.Sim);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalCameraPosition, finalCameraRotation, 8, Ease.Cubic, 30));
        yield return new WaitForSeconds(2);
        StartCoroutine(SpawnDrivers());
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city.StartSimulation());

        yield return null;
    }

    IEnumerator SpawnDrivers()
    {
        city.SpawnInitialDrivers();
        yield return null;
    }
}
