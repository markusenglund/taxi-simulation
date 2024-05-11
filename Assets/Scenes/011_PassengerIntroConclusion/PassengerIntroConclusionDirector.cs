using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class PassengerIntroConclusionDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    [SerializeField] public Transform passengerStatsPrefab;

    [SerializeField] public Transform passengerPrefab;
    public Random passengerSpawnRandom;
    Vector3 passengerPosition = new Vector3(1.7f, 0.08f, 0f);
    Animator passengerAnimator;

    Driver driver;

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
        driver = city.CreateDriver(driverPerson, new Vector3(7, 0, 0));
        PassengerBase passenger = PassengerBase.Create(passengerPrefab, passengerPosition, spawnDuration: 1.5f, passengerSpawnRandom, simSettings);
        // TODO: All passengerStats stuff should be added back in when replacing passengerBase
        // PassengerStats passengerStats = SpawnPassengerStats(passenger);
        passengerAnimator = passenger.GetComponentInChildren<Animator>();

        Camera.main.transform.position = new Vector3(1.75f, 0.148f, 0);
        Camera.main.transform.rotation = Quaternion.Euler(1.3f, 5.4f, 0f);
        // Get child component of passenger
        // Transform passengerChild = passenger.transform.GetChild(0);
        // passengerChild.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        yield return new WaitForSeconds(3);

        // StartCoroutine(passengerStats.DespawnCard());
        // Pan camera towards the incoming taxi
        // StartCoroutine(CameraUtils.RotateCamera(Quaternion.Euler(15, 60, 0), 1.5f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(passenger.transform.position + new Vector3(0, 0.2f, -0.3f), Quaternion.Euler(15, 70, 0), 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.RotateCameraAround(passenger.transform.position + new Vector3(0, 0, 0f), Vector3.up, 70, 2f, Ease.Cubic));
        yield return new WaitForSeconds(0.8f);
        driver.SetDestination(new Vector3(1.7f, 0, 0));
        yield return new WaitForSeconds(1.2f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(new Vector3(1.7f, 1.3f, 0), Quaternion.Euler(90, 0, 0), 3f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveCamera(Camera.main.transform.position + new Vector3(-0.2f, 0.2f, 0f), 3f, Ease.Cubic));
        yield return new WaitForSeconds(2f);
        StartCoroutine(MoveToCarRoof(passenger, 0.8f));
        StartCoroutine(FollowObject(driver.transform, duration: 5));
        yield return new WaitForSeconds(0.8f);
        driver.SetDestination(new Vector3(0, 0, 5));
        yield return new WaitForSeconds(4.2f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(new Vector3(0.23f, 0.3f, 4.5f), Quaternion.LookRotation(new Vector3(0, 0, 1)), 2f, Ease.Cubic));
        StartCoroutine(MoveOffCarRoof(passenger, 0.8f));
        yield return new WaitForSeconds(0.8f);
        driver.SetDestination(new Vector3(0, 0, 0));
        StartCoroutine(passenger.DespawnPassenger());
    }

    IEnumerator MoveToCarRoof(PassengerBase passenger, float duration)
    {
        passengerAnimator.SetTrigger("EnterTaxi");
        yield return new WaitForSeconds(0.3f);

        passenger.transform.SetParent(driver.transform);
        float startTime = Time.time;
        Vector3 startPosition = passenger.transform.localPosition;
        float topTaxiY = 1.44f;
        Vector3 finalPosition = new Vector3(0.09f, topTaxiY, 0);

        Quaternion startRotation = passenger.transform.localRotation;
        Quaternion finalRotation = Quaternion.Euler(0, 0, 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float verticalT = 1.2f * EaseUtils.EaseOutQuadratic(t);
            float horizontalT = EaseUtils.EaseInOutCubic(t);
            // passenger.transform.localPosition = Vector3.Lerp(startPosition, finalPosition, t);
            passenger.transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, horizontalT);
            passenger.transform.localPosition = new Vector3(Mathf.Lerp(startPosition.x, finalPosition.x, horizontalT), Mathf.Lerp(startPosition.y, finalPosition.y, verticalT), Mathf.Lerp(startPosition.z, finalPosition.z, horizontalT));
            yield return null;
        }
        passenger.transform.localPosition = finalPosition;
        passenger.transform.localRotation = finalRotation;
    }


    IEnumerator MoveOffCarRoof(PassengerBase passenger, float duration)
    {
        passenger.transform.SetParent(null);
        float startTime = Time.time;
        Vector3 startPosition = passenger.transform.position;
        Vector3 finalPosition = new Vector3(startPosition.x + 0.23f, 0.08f, startPosition.z);

        Quaternion startRotation = passenger.transform.localRotation;
        Quaternion finalRotation = Quaternion.Euler(0, 90, 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float verticalT = EaseUtils.EaseInCubic(t);
            float horizontalT = EaseUtils.EaseInOutCubic(t);
            // passenger.transform.localPosition = Vector3.Lerp(startPosition, finalPosition, t);
            passenger.transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, horizontalT);
            passenger.transform.localPosition = new Vector3(Mathf.Lerp(startPosition.x, finalPosition.x, horizontalT), Mathf.Lerp(startPosition.y, finalPosition.y, verticalT), Mathf.Lerp(startPosition.z, finalPosition.z, horizontalT));
            yield return null;
        }
        passenger.transform.localPosition = finalPosition;
        passenger.transform.localRotation = finalRotation;
    }

    IEnumerator FollowObject(Transform target, float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration && target != null)
        {
            Vector3 normalizedTargetDirection = (target.position - Camera.main.transform.position).normalized;
            Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
            Vector3 desiredPosition = new Vector3(middlePosition.x, Camera.main.transform.position.y, middlePosition.z);
            Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.15f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
            yield return null;
        }
    }

    // PassengerStats SpawnPassengerStats(PassengerBase passenger)
    // {
    //     Vector3 position = new Vector3(1.8f, 0.18f, 0.2f);
    //     Quaternion rotation = Quaternion.Euler(0, 20, 0);
    //     PassengerStats passengerStats = PassengerStats.Create(passengerStatsPrefab, passenger.transform, position, rotation, passenger.passengerEconomicParameters);
    //     return passengerStats;
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
