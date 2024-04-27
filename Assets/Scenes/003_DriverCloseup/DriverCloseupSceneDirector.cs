using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class DriverCloseupSceneDirector : MonoBehaviour
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
        DriverPerson driverPerson = CreateGenericDriverPerson();
        Driver driver = city.CreateDriver(driverPerson, new Vector3(9, 0, 6));
        yield return null; // Wait for the city to run the Start method before generating passenger
        city.CreatePassenger(new Vector3(9, 0.08f, 6));
        Camera.main.transform.SetParent(driver.transform);
        Camera.main.transform.localPosition = new Vector3(0, 1.5f, 2.15f);
        Camera.main.transform.localRotation = Quaternion.Euler(30, 180, 0);
        // yield return StartCoroutine(FollowObject(driver.transform, duration: 5));
        yield return new WaitForSeconds(1);
        Time.timeScale = 0.3f;
        StartCoroutine(CameraUtils.RotateCameraAroundMovingObject(driver.transform, distance: 0.37f, Vector3.up, -20, 2.5f));
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(new Vector3(-0.6f, 2.3f, 2f), Quaternion.Euler(0, 160, 0), 0.5f));
        yield return new WaitForSeconds(5);
        EditorApplication.isPlaying = false;
    }

    // IEnumerator FollowObject(Transform target, float duration)
    // {
    //     Camera.main.transform.position = target.position + target.forward * 1f;
    //     Camera.main.transform.rotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
    //     float startTime = Time.time;
    //     while (Time.time < startTime + duration && target != null)
    //     {
    //         Vector3 normalizedTargetDirection = (target.position - Camera.main.transform.position).normalized;
    //         Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
    //         Vector3 desiredPosition = new Vector3(middlePosition.x, 1.5f, middlePosition.z);
    //         Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
    //         Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.1f);
    //         Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
    //         yield return null;
    //     }
    // }

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
