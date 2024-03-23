using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class PriceSceneDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    [SerializeField] public Transform passengerPrefab;
    public Random passengerSpawnRandom;

    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
    }

    void Start()
    {
        passengerSpawnRandom = new Random(1);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Time.timeScale = 0.4f;
        DriverPerson driverPerson = CreateGenericDriverPerson();
        Driver driver = city.CreateDriver(driverPerson, new Vector3(9, 0, 4));
        city.CreatePassenger(new Vector3(9, 0.08f, 6));
        Camera.main.transform.position = new Vector3(10, 1, 6);
        Camera.main.transform.LookAt(new Vector3(9, 0, 6));
        yield return new WaitForSeconds(5);
        EditorApplication.isPlaying = false;
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
