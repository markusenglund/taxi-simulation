using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = System.Random;


public class PassengerBase : MonoBehaviour
{
    static int incrementalId = 1;
    public int id;
    private float spawnDuration;
    private float despawnDuration = 1.5f;
    [SerializeField] public Transform spawnAnimationPrefab;
    [SerializeField] public Transform despawnAnimationPrefab;

    public Random passengerSpawnRandom;

    public bool hasAcceptedRideOffer = false;
    public PassengerEconomicParameters passengerEconomicParameters;
    private float timeCreated;

    Animator passengerAnimator;

    public Vector3 pickUpPosition;
    public Vector3 destination;

    public SimulationSettings simulationSettings;



    public static PassengerBase CreateRaw(Transform prefab, Vector3 position, Quaternion rotation, float spawnDuration, Random passengerSpawnRandom, SimulationSettings simSettings)
    {
        Transform passengerTransform = Instantiate(prefab, position, rotation);
        PassengerBase passenger = passengerTransform.GetComponent<PassengerBase>();
        passenger.spawnDuration = spawnDuration;
        passenger.passengerSpawnRandom = passengerSpawnRandom;
        passenger.simulationSettings = simSettings;
        passenger.pickUpPosition = position;
        return passenger;
    }

    public static PassengerBase Create(Transform prefab, Vector3 position, float spawnDuration, Random passengerSpawnRandom, SimulationSettings simSettings)
    {
        Quaternion rotation = Quaternion.identity;

        float xVisual = position.x;
        float zVisual = position.z;

        if (position.x % GridUtils.blockSize == 0)
        {
            xVisual = position.x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (position.z % GridUtils.blockSize == 0)
        {
            zVisual = position.z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Vector3 transformPosition = new Vector3(xVisual, position.y, zVisual);
        Quaternion transformRotation = rotation;

        Transform passengerTransform = Instantiate(prefab, transformPosition, transformRotation);
        PassengerBase passenger = passengerTransform.GetComponent<PassengerBase>();
        passenger.spawnDuration = spawnDuration;
        passenger.passengerSpawnRandom = passengerSpawnRandom;
        passenger.pickUpPosition = position;
        passenger.simulationSettings = simSettings;
        return passenger;
    }


    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        timeCreated = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        passengerAnimator = this.GetComponentInChildren<Animator>();
    }

    void Start()
    {
        destination = GridUtils.GetRandomPosition(passengerSpawnRandom);
        GenerateEconomicParameters();

        StartCoroutine(ScheduleActions());
    }

    IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnPassenger());
        yield return new WaitForSeconds(2);
    }

    IEnumerator SpawnPassenger()
    {
        Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        transform.localScale = Vector3.zero;
        float startTime = Time.time;
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseInOutCubic(t);
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    public IEnumerator DespawnPassenger()
    {
        Instantiate(despawnAnimationPrefab, transform.position, Quaternion.identity);
        passengerAnimator.SetTrigger("Celebrate");
        yield return new WaitForSeconds(0.5f);
        Quaternion startRotation = transform.localRotation;
        float endRotationY = 360 * 5;

        float startTime = Time.time;
        while (Time.time < startTime + despawnDuration)
        {
            float t = (Time.time - startTime) / despawnDuration;
            float shrinkFactor = EaseInOutCubic(t);
            float spinFactor = EaseUtils.EaseInCubic(t);
            transform.localScale = Vector3.one * (1 - shrinkFactor);
            Quaternion newRotation = Quaternion.AngleAxis(startRotation.eulerAngles.y + endRotationY * spinFactor, Vector3.up);
            transform.localRotation = newRotation;
            yield return null;
        }
        Destroy(gameObject);
    }

    void GenerateEconomicParameters()
    {
        float hourlyIncome = simulationSettings.GetRandomHourlyIncome(passengerSpawnRandom);
        float tripUtilityScore = GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        float timePreference = Mathf.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(passengerSpawnRandom, 1.5f, 0.5f, 0, 3f);
        float waitingCostPerHour = hourlyIncome * timePreference;
        // Practically speaking tripUtilityValue will be on average 2x the hourly income (20$) which is 40$ (will have to refined later to be more realistic)
        float tripUtilityValue = tripUtilityScore * hourlyIncome;
        // Debug.Log("Passenger " + id + " time preference: " + timePreference + ", waiting cost per hour: " + waitingCostPerHour + ", trip utility value: " + tripUtilityValue);

        Substitute bestSubstitute = GetBestSubstituteForRideOffer(waitingCostPerHour, tripUtilityValue, hourlyIncome);
        passengerEconomicParameters = new PassengerEconomicParameters()
        {
            hourlyIncome = hourlyIncome,
            tripUtilityScore = tripUtilityScore,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            tripUtilityValue = tripUtilityValue,
            bestSubstitute = bestSubstitute
        };
    }

    float GenerateTripUtilityScore()
    {
        float tripDistance = GridUtils.GetDistance(pickUpPosition, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        float mu = 0;
        float sigma = 0.4f;
        float tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(passengerSpawnRandom, mu, sigma);
        // Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
        return tripUtilityScore;
    }

    Substitute GetBestSubstituteForRideOffer(float waitingCostPerHour, float tripUtilityValue, float hourlyIncome)
    {
        float tripDistance = GridUtils.GetDistance(pickUpPosition, destination);

        // Public transport
        float publicTransportTime = tripDistance / simulationSettings.publicTransportSpeed;
        // Public transport adds a random time between 20 minutes and 2 hours to the arrival time due to going to the bus stop, waiting for the bus, and walking to the destination
        float publicTransportAdditionalTime = Mathf.Lerp(20f / 60f, 2, (float)passengerSpawnRandom.NextDouble());
        float publicTransportTimeCost = (publicTransportTime + publicTransportAdditionalTime) * waitingCostPerHour;
        float publicTransportMoneyCost = 3;
        float publicTransportUtilityCost = publicTransportTimeCost + publicTransportMoneyCost;
        float netValueOfPublicTransport = tripUtilityValue - publicTransportUtilityCost;
        Substitute publicTransportSubstitute = new Substitute()
        {
            type = TripType.PublicTransport,
            timeCost = publicTransportTimeCost,
            moneyCost = publicTransportMoneyCost,
            totalCost = publicTransportUtilityCost,
            netValue = netValueOfPublicTransport,
            netUtility = netValueOfPublicTransport / hourlyIncome
        };

        // Walking
        float walkingTime = tripDistance / simulationSettings.walkingSpeed;
        float timeCostOfWalking = walkingTime * waitingCostPerHour;
        float moneyCostOfWalking = 0;
        float utilityCostOfWalking = timeCostOfWalking + moneyCostOfWalking;
        float netValueOfWalking = tripUtilityValue - utilityCostOfWalking;
        Substitute walkingSubstitute = new Substitute()
        {
            type = TripType.Walking,
            timeCost = timeCostOfWalking,
            moneyCost = moneyCostOfWalking,
            totalCost = utilityCostOfWalking,
            netValue = netValueOfWalking,
            netUtility = netValueOfWalking / hourlyIncome
        };

        // Private vehicle - the idea here is that if a taxi ride going to cost you more than 100$, you're gonna find a way to have your own vehicle
        float privateVehicleTime = tripDistance / simulationSettings.driverSpeed;
        // Add a 5 minute waiting cost for getting into the car
        float privateVehicleWaitingTime = 5 / 60f;
        float privateVehicleTimeCost = (privateVehicleTime + privateVehicleWaitingTime) * waitingCostPerHour;
        float marginalCostEnRoute = tripDistance * simulationSettings.driverMarginalCostPerKm;
        float privateVehicleMoneyCost = simulationSettings.privateVehicleCost + marginalCostEnRoute;
        float privateVehicleUtilityCost = privateVehicleTimeCost + privateVehicleMoneyCost;
        float netValueOfPrivateVehicle = tripUtilityValue - privateVehicleUtilityCost;
        Substitute privateVehicleSubstitute = new Substitute()
        {
            type = TripType.SkipTrip,
            timeCost = privateVehicleTimeCost,
            moneyCost = privateVehicleMoneyCost,
            totalCost = privateVehicleUtilityCost,
            netValue = netValueOfPrivateVehicle,
            netUtility = netValueOfPrivateVehicle / hourlyIncome
        };

        // Skip trip
        Substitute skipTripSubstitute = new Substitute()
        {
            type = TripType.SkipTrip,
            timeCost = 0,
            moneyCost = 0,
            totalCost = 0,
            netValue = 0
        };

        List<Substitute> substitutes = new List<Substitute> { publicTransportSubstitute, walkingSubstitute, privateVehicleSubstitute, skipTripSubstitute };

        Substitute bestSubstitute = substitutes.OrderByDescending(substitute => substitute.netValue).First();


        return bestSubstitute;
    }

    float EaseInOutCubic(float t)
    {
        float t2;
        if (t <= 0.5f)
        {
            t2 = Mathf.Pow(t * 2, 3) / 2;
        }
        else
        {
            t2 = (2 - Mathf.Pow((1 - t) * 2, 3)) / 2;
        }
        return t2;
    }
}
