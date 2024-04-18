using System.Collections;
using UnityEngine;
using Random = System.Random;


public class SimIntroductionDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    // Vector3 lookAtPosition = new Vector3()
    City city;
    public Random driverSpawnRandom;

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
    }

    void Start()
    {
        driverSpawnRandom = new Random(simSettings.randomSeed);

        Camera.main.transform.position = new Vector3(0f, 11f, 4.5f);
        Camera.main.transform.LookAt(cityMiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        // Should be 20 seconds in the end
        float preMoveDuration = 1f;
        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePosition, Vector3.up, -90, preMoveDuration, Ease.Linear));
        yield return new WaitForSeconds(preMoveDuration - 0.5f);
        StartCoroutine(MoveCity(new Vector3(-4.5f, 0, 0f), 0.9f));
        yield return new WaitForSeconds(0.5f);
        PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.PreSim);
        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePosition + new Vector3(-4.5f, 0, 0f), Vector3.up, -360, 80, Ease.Linear));

        int numDrivers = 6;
        for (int i = 0; i < numDrivers; i++)
        {
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition(driverSpawnRandom);
                DriverPerson driverPerson = CreateGenericDriverPerson();
                city.CreateDriver(driverPerson, randomPosition);
                yield return new WaitForSeconds(2f / (i + 1));
            }
        }
        yield return new WaitForSeconds(0.5f);
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

    DriverPerson CreateGenericDriverPerson()
    {
        return new DriverPerson()
        {
            opportunityCostProfile = DriverPool.normalDriverProfile,
            baseOpportunityCostPerHour = 10,
            preferredSessionLength = 4,
            interval = new SessionInterval()
            {
                startTime = 0,
                endTime = 4
            }
        };
    }
}
