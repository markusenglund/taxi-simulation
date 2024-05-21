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
        Time.captureFramerate = 60;
    }

    void Start()
    {
        passengerSpawnRandom = new Random(1);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Time.timeScale = 0.6f;
        DriverPerson driverPerson = CreateGenericDriverPerson();
        city.CreateDriver(driverPerson, new Vector3(9, 0, 3));
        Vector3 passengerPosition = new Vector3(9, 0.08f, 6);
        city.CreatePassenger(passengerPosition);
        Camera.main.transform.position = new Vector3(7, 1, 6);
        Camera.main.transform.LookAt(new Vector3(9, 0.3f, 6));
        yield return new WaitForSeconds(0.4f);
        StartCoroutine(CameraUtils.RotateCameraAround(passengerPosition, Vector3.up, 180, 3f, Ease.Cubic));
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
