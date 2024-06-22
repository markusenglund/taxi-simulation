using System.Collections;
using UnityEngine;
using Random = System.Random;


public class FirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    private float simulationStartTime = 35;

    Vector3 lookAtPosition = new Vector3(5.5f, 3, 5.5f);
    City city;
    Vector3 cityMiddlePositionBeforeMove = new Vector3(4.5f, -3.5f, 4.5f);

    Vector3 cityPositionAfterMove = new Vector3(4.5f, 0, 0f);
    public Random driverSpawnRandom;

    SimulationInfoGroup simulationInfoGroup;

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings, spawnInitialDrivers: false);
        Time.captureFramerate = 60;


    }

    void Start()
    {
        driverSpawnRandom = new Random(simSettings.randomSeed);

        // Camera.main.transform.LookAt(lookAtPosition);
        // Camera.main.transform.position = new Vector3(10f, 10f, 0);
        Camera.main.transform.position = new Vector3(12f, 10f, 4.5f);
        Camera.main.transform.LookAt(cityMiddlePositionBeforeMove);
        StartCoroutine(SetSimulationStart());
        StartCoroutine(Scene());
        simulationInfoGroup = GameObject.Find("SimulationInfoGroup").GetComponent<SimulationInfoGroup>();
    }

    IEnumerator Scene()
    {
        float preMoveDuration = 6;
        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePositionBeforeMove, Vector3.up, -90, preMoveDuration, Ease.Linear));
        yield return new WaitForSeconds(preMoveDuration - 0.5f);
        StartCoroutine(MoveCity(cityPositionAfterMove, 0.9f));
        yield return new WaitForSeconds(0.5f);
        float rotateCameraDuration = 20;
        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePositionBeforeMove + cityPositionAfterMove, Vector3.up, -180, rotateCameraDuration - 5, Ease.Linear));
        yield return new WaitForSeconds(1);
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.FirstSim);
        yield return new WaitForSeconds(rotateCameraDuration - 5 - 1);
        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePositionBeforeMove + cityPositionAfterMove, Vector3.up, -45, 4, Ease.Linear));
        StartCoroutine(SpawnDrivers());

        yield return new WaitForSeconds(4);
        float cityRightEdge = cityPositionAfterMove.z + 8;
        Vector3 finalCameraPosition = new Vector3(23, 16, cityRightEdge);
        Vector3 newLookAtPosition = new Vector3(14.5f, 5.7f, cityRightEdge);
        Quaternion finalCameraRotation = Quaternion.LookRotation(newLookAtPosition - finalCameraPosition, Vector3.up);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalCameraPosition, finalCameraRotation, 8, Ease.QuadraticOut, 30));
        yield return new WaitForSeconds(7);
        PassengerTripTypeGraph.Create(city);
        StartCoroutine(simulationInfoGroup.FadeInSchedule());
        yield return new WaitForSeconds(2);


        yield return null;
    }

    IEnumerator SetSimulationStart()
    {
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city.StartSimulation());
    }

    IEnumerator SpawnDrivers()
    {
        city.SpawnInitialDrivers();
        yield return null;
    }

    IEnumerator MoveCity(Vector3 finalPosition, float duration)
    {
        float startTime = Time.time;
        Vector3 startPosition = city.transform.position;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseUtils.EaseInOutCubic(t);
            city.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
            yield return null;
        }
        city.transform.position = finalPosition;
    }
}
