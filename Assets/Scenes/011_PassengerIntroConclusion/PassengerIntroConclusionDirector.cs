using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class PassengerIntroConclusionDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
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
        PassengerEconomicParameters economicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = 10.80f,
            tripUtilityScore = 4,
            timePreference = 3.5f,
            waitingCostPerHour = 37.40f,
            tripUtilityValue = 43.40f,
            bestSubstitute = new Substitute()
            {
                type = SubstituteType.SkipTrip,
                timeCost = 0,
                moneyCost = 0,
                totalCost = 0,
                netValue = 0,
                netUtility = 0
            }

        };

        DriverPerson driverPerson = CreateGenericDriverPerson();
        driver = city.CreateDriver(driverPerson, new Vector3(7, 0, 0));
        // Passenger passenger = city.CreatePassenger(passengerPosition, economicParameters);
        PassengerBase passenger = PassengerBase.Create(passengerPrefab, passengerPosition, spawnDuration: 1.5f, passengerSpawnRandom, simSettings);
        passengerAnimator = passenger.GetComponentInChildren<Animator>();

        Camera.main.transform.position = new Vector3(passenger.transform.position.x, 0.2f, passenger.transform.position.z - 0.2f);
        Camera.main.transform.rotation = Quaternion.Euler(15, 0, 0);
        // Get child component of passenger
        // Transform passengerChild = passenger.transform.GetChild(0);
        // passengerChild.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        yield return new WaitForSeconds(3);
        // Pan camera towards the incoming taxi
        // StartCoroutine(CameraUtils.RotateCamera(Quaternion.Euler(15, 60, 0), 1.5f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(passenger.transform.position + new Vector3(0, 0.2f, -0.3f), Quaternion.Euler(15, 70, 0), 3f, Ease.Cubic));
        StartCoroutine(CameraUtils.RotateCameraAround(passenger.transform.position + new Vector3(0, 0, -0.1f), Vector3.up, 70, 2f, Ease.Cubic));
        yield return new WaitForSeconds(0.8f);
        driver.SetDestination(new Vector3(1.7f, 0, 0));
        yield return new WaitForSeconds(1.2f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(new Vector3(1.7f, 1f, 0), Quaternion.Euler(90, 0, 0), 3f, Ease.Cubic));
        // StartCoroutine(CameraUtils.MoveCamera(Camera.main.transform.position + new Vector3(-0.2f, 0.2f, 0f), 3f, Ease.Cubic));
        passengerAnimator.SetTrigger("EnterTaxi");
        yield return new WaitForSeconds(3.4f);
        StartCoroutine(MoveToCarRoof(passenger, 0.8f));
    }

    IEnumerator MoveToCarRoof(PassengerBase passenger, float duration)
    {
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
