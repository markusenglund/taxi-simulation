using System.Collections;
using UnityEngine;
using Random = System.Random;


public class SimIntroductionDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;
    public Random driverSpawnRandom;

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
    }

    void Start()
    {
        driverSpawnRandom = new Random(simSettings.randomSeed);

        Camera.main.transform.position = new Vector3(0f, 11f, 0f);
        Camera.main.transform.LookAt(cityMiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {

        // DriverPerson driverPerson = CreateGenericDriverPerson();
        // driver = city.CreateDriver(driverPerson, new Vector3(7, 0, 0));

        StartCoroutine(CameraUtils.RotateCameraAround(cityMiddlePosition, Vector3.up, -405, 100, Ease.Linear));
        yield return new WaitForSeconds(3);

        // DriverPerson driverPerson = CreateGenericDriverPerson();
        // city.CreateDriver(driverPerson, new Vector3(7, 0, 0));
        int numDrivers = 6;
        for (int i = 0; i < numDrivers; i++)
        {
            {
                Vector3 randomPosition = GridUtils.GetRandomPosition(driverSpawnRandom);
                DriverPerson driverPerson = CreateGenericDriverPerson();
                city.CreateDriver(driverPerson, randomPosition);
                yield return new WaitForSeconds(0.5f);
            }
        }
        yield return new WaitForSeconds(0.5f);
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
