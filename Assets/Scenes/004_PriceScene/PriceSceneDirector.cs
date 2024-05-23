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
        Camera.main.transform.position = new Vector3(7, 1, 6);
        Camera.main.transform.LookAt(new Vector3(9, 0.3f, 6));
        passengerSpawnRandom = new Random(1);
        StartCoroutine(city.StartSimulation());
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {

        yield return null; // Wait for the city to run the Start method before generating passenger
        Time.timeScale = 0.8f;
        DriverPerson driverPerson = CreateGenericDriverPerson();
        city.CreateDriver(driverPerson, new Vector3(6.1f, 0, 0));
        Vector3 passengerPosition = new Vector3(9f, 0.08f, 6f);
        Passenger passenger = city.CreatePassenger(passengerPosition);
        Animator animator = passenger.GetComponentInChildren<Animator>();
        yield return new WaitForSeconds(1.6f);
        StartCoroutine(CameraUtils.RotateCameraAround(passengerPosition, Vector3.up, 180, 3.2f, Ease.Cubic));
        yield return new WaitForSeconds(3.8f);
        // Set camera to follow passenger
        StartCoroutine(FollowObject(passenger.transform, 4));
        yield return new WaitForSeconds(0.5f);
        animator.SetTrigger("BeDisappointed");

        yield return new WaitForSeconds(5f);
        EditorApplication.isPlaying = false;
    }

    IEnumerator FollowObject(Transform target, float duration)
    {
        float startTime = Time.time;
        Vector3 startTargetDirection = target.position - Camera.main.transform.position;
        while (Time.time < startTime + duration && target != null)
        {
            Vector3 desiredPosition = target.position - (startTargetDirection * 0.7f);
            Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            // Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
            // Vector3 desiredPosition = new Vector3(middlePosition.x, Camera.main.transform.position.y, middlePosition.z);
            // Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.03f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
            yield return null;
        }
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
