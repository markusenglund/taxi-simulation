using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class PassengersSpawningSceneDirector : MonoBehaviour
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
        Camera.main.transform.position = new Vector3(-1f, 0.7f, 3f);
        Camera.main.transform.rotation = Quaternion.Euler(10, 90, 0);
        StartCoroutine(CameraUtils.MoveCamera(toPosition: new Vector3(8f, 1, 3f), duration: 6, ease: Ease.Linear));
        yield return new WaitForSeconds(0.3f);
        DriverPerson driverPerson = CreateGenericDriverPerson();
        city.CreateDriver(driverPerson, new Vector3(6, 0, 6));
        city.CreatePassenger(new Vector3(2, 0.08f, 3f));
        yield return new WaitForSeconds(0.7f);
        SpawnPassenger(new Vector3(5, 0.08f, 3.23f), Quaternion.LookRotation(new Vector3(0, 0, -1)));
        SpawnPassenger(new Vector3(7f, 0.08f, 2.77f), Quaternion.LookRotation(new Vector3(0, 0, 1)));
        StartCoroutine(CameraUtils.RotateCamera(Quaternion.Euler(40, 90, 0), duration: 5, ease: Ease.QuadraticIn));
        yield return new WaitForSeconds(1.5f);

        SpawnPassenger(new Vector3(9.23f, 0.08f, 6f), Quaternion.LookRotation(new Vector3(-1, 0, 0f)));
        SpawnPassenger(new Vector3(6.23f, 0.08f, 2.5f), Quaternion.LookRotation(new Vector3(-1, 0, 0f)));
        yield return new WaitForSeconds(1);
        SpawnPassenger(new Vector3(9.23f, 0.08f, 2.77f), Quaternion.LookRotation(new Vector3(-1, 0, 0f)));
        yield return new WaitForSeconds(50);
        EditorApplication.isPlaying = false;
    }

    void SpawnPassenger(Vector3 position, Quaternion rotation)
    {
        PassengerPerson person = new PassengerPerson(position, simSettings, passengerSpawnRandom);
        Passenger passenger = Passenger.Create(person, passengerPrefab, city.transform, simSettings, null, mode: PassengerMode.Inactive, spawnDuration: 1);

        // Animator animator = passenger.GetComponentInChildren<Animator>();
        // animator.SetTrigger("LookAtPhone");

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
