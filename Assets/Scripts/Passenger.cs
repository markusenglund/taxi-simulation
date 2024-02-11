using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Random = System.Random;

public enum PassengerState
{
    Idling,
    AssignedToTrip,
    DroppedOff,
    RejectedRideOffer
}

public enum SubstituteType
{
    Walking,
    PublicTransport,
    SkipTrip
}
public class Substitute
{
    public SubstituteType type { get; set; }
    public float timeCost { get; set; }
    public float moneyCost { get; set; }
    public float totalCost { get; set; }
    public float netValue { get; set; }
    public float netUtility { get; set; }
}

public class PassengerEconomicParameters
{
    // Base values
    public float hourlyIncome { get; set; }
    public float tripUtilityScore { get; set; }
    public float timePreference { get; set; }

    // Derived values
    public float waitingCostPerHour { get; set; }
    public float tripUtilityValue { get; set; }

    public Substitute bestSubstitute { get; set; }
}

public class Passenger : MonoBehaviour
{

    [SerializeField] public Transform spawnAnimationPrefab;
    public Vector3 positionActual;

    static int incrementalId = 1;
    public int id;

    public float timeCreated;

    public PassengerState state = PassengerState.Idling;

    public Vector3 destination;

    private WaitingTimeGraph waitingTimeGraph;

    private UtilityIncomeScatterPlot utilityIncomeScatterPlot;

    private PassengerSurplusGraph passengerSurplusGraph;


    public bool hasAcceptedRideOffer = false;
    public Trip currentTrip;

    public PassengerEconomicParameters passengerEconomicParameters;

    void Awake()
    {
        id = incrementalId;
        incrementalId += 1;
        timeCreated = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        destination = GridUtils.GetRandomPosition(GameManager.Instance.passengerSpawnRandom);
        waitingTimeGraph = GameObject.Find("WaitingTimeGraph").GetComponent<WaitingTimeGraph>();
        utilityIncomeScatterPlot = GameObject.Find("UtilityIncomeScatterPlot").GetComponent<UtilityIncomeScatterPlot>();

        passengerSurplusGraph = GameObject.Find("PassengerSurplusGraph").GetComponent<PassengerSurplusGraph>();
    }

    void GenerateEconomicParameters()
    {
        float hourlyIncome = SimulationSettings.GetRandomHourlyIncome(GameManager.Instance.passengerSpawnRandom);
        float tripUtilityScore = GenerateTripUtilityScore();
        // TODO: Set a reasonable time preference based on empirical data. Passengers value their time on average 2.5x their hourly income, sqrt(tripUtilityScore) is on average around 1.7 so we multiply by a random variable that is normally distributed with mean 1.5 and std 0.5
        float timePreference = Mathf.Sqrt(tripUtilityScore) * StatisticsUtils.GetRandomFromNormalDistribution(GameManager.Instance.passengerSpawnRandom, 1.5f, 0.5f, 0, 3f);
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

    Substitute GetBestSubstituteForRideOffer(float waitingCostPerHour, float tripUtilityValue, float hourlyIncome)
    {
        float tripDistance = GridUtils.GetDistance(positionActual, destination);

        // Public transport
        float publicTransportTime = tripDistance / SimulationSettings.publicTransportSpeed;
        // Public transport
        Random random = GameManager.Instance.passengerSpawnRandom;
        // Public transport adds a random time between 20 minutes and 2 hours to the arrival time due to going to the bus stop, waiting for the bus, and walking to the destination
        float publicTransportAdditionalTime = Mathf.Lerp(20f / 60f, 2, (float)random.NextDouble());
        float publicTransportTimeCost = (publicTransportTime + publicTransportAdditionalTime) * waitingCostPerHour;
        float publicTransportMoneyCost = 3;
        float publicTransportUtilityCost = publicTransportTimeCost + publicTransportMoneyCost;
        float netValueOfPublicTransport = tripUtilityValue - publicTransportUtilityCost;
        Substitute publicTransportSubstitute = new Substitute()
        {
            type = SubstituteType.PublicTransport,
            timeCost = publicTransportTimeCost,
            moneyCost = publicTransportMoneyCost,
            totalCost = publicTransportUtilityCost,
            netValue = netValueOfPublicTransport,
            netUtility = netValueOfPublicTransport / hourlyIncome
        };

        // Walking
        float walkingTime = tripDistance / SimulationSettings.walkingSpeed;
        float timeCostOfWalking = walkingTime * waitingCostPerHour;
        float moneyCostOfWalking = 0;
        float utilityCostOfWalking = timeCostOfWalking + moneyCostOfWalking;
        float netValueOfWalking = tripUtilityValue - utilityCostOfWalking;
        Substitute walkingSubstitute = new Substitute()
        {
            type = SubstituteType.Walking,
            timeCost = timeCostOfWalking,
            moneyCost = moneyCostOfWalking,
            totalCost = utilityCostOfWalking,
            netValue = netValueOfWalking,
            netUtility = netValueOfWalking / hourlyIncome
        };

        // Private vehicle - the idea here is that if a taxi ride going to cost you more than 100$, you're gonna find a way to have your own vehicle
        float privateVehicleTime = tripDistance / SimulationSettings.driverSpeed;
        // Add a 5 minute waiting cost for getting into the car
        float privateVehicleWaitingTime = 5 / 60f;
        float privateVehicleTimeCost = (privateVehicleTime + privateVehicleWaitingTime) * waitingCostPerHour;
        float marginalCostEnRoute = tripDistance * SimulationSettings.driverMarginalCostPerKm;
        float privateVehicleMoneyCost = 100f + marginalCostEnRoute;
        float privateVehicleUtilityCost = privateVehicleTimeCost + privateVehicleMoneyCost;
        float netValueOfPrivateVehicle = tripUtilityValue - privateVehicleUtilityCost;
        Substitute privateVehicleSubstitute = new Substitute()
        {
            type = SubstituteType.SkipTrip,
            timeCost = privateVehicleTimeCost,
            moneyCost = privateVehicleMoneyCost,
            totalCost = privateVehicleUtilityCost,
            netValue = netValueOfPrivateVehicle,
            netUtility = netValueOfPrivateVehicle / hourlyIncome
        };

        // Skip trip
        Substitute skipTripSubstitute = new Substitute()
        {
            type = SubstituteType.SkipTrip,
            timeCost = 0,
            moneyCost = 0,
            totalCost = 0,
            netValue = 0
        };

        List<Substitute> substitutes = new List<Substitute> { publicTransportSubstitute, walkingSubstitute, privateVehicleSubstitute, skipTripSubstitute };

        Substitute bestSubstitute = substitutes.OrderByDescending(substitute => substitute.netValue).First();


        return bestSubstitute;
    }


    float GenerateTripUtilityScore()
    {
        float tripDistance = GridUtils.GetDistance(positionActual, destination);
        float tripDistanceUtilityModifier = Mathf.Sqrt(tripDistance);


        float mu = 0;
        float sigma = 0.4f;
        float tripUtilityScore = tripDistanceUtilityModifier * StatisticsUtils.getRandomFromLogNormalDistribution(GameManager.Instance.passengerSpawnRandom, mu, sigma);
        // Debug.Log("Passenger " + id + " trip utility score: " + tripUtilityScore + ", trip distance: " + tripDistance + ", trip distance utility modifier: " + tripDistanceUtilityModifier);
        return tripUtilityScore;
    }
    void Start()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        GenerateEconomicParameters();

        Invoke("MakeTripDecision", 1f);
    }

    void MakeTripDecision()
    {
        RideOffer rideOffer = GameManager.Instance.RequestRideOffer(positionActual, destination);


        float tripCreatedTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        float expectedPickupTime = tripCreatedTime + rideOffer.expectedWaitingTime;

        TripCreatedData tripCreatedData = new TripCreatedData()
        {
            passenger = this,
            createdTime = tripCreatedTime,
            pickUpPosition = positionActual,
            destination = destination,
            tripDistance = GridUtils.GetDistance(positionActual, destination),
            expectedWaitingTime = rideOffer.expectedWaitingTime,
            expectedTripTime = rideOffer.expectedTripTime,
            fare = rideOffer.fare,
            expectedPickupTime = expectedPickupTime
        };

        float expectedWaitingCost = rideOffer.expectedWaitingTime * passengerEconomicParameters.waitingCostPerHour;
        float expectedTripTimeCost = rideOffer.expectedTripTime * passengerEconomicParameters.waitingCostPerHour;

        float totalCost = expectedWaitingCost + expectedTripTimeCost + rideOffer.fare.total;

        float expectedNetValue = passengerEconomicParameters.tripUtilityValue - totalCost;
        float expectedNetUtility = expectedNetValue / passengerEconomicParameters.hourlyIncome;
        float expectedTripTimeDisutility = expectedTripTimeCost / passengerEconomicParameters.hourlyIncome;
        // 'expectedNetUtilityBeforeVariableCosts' represents the utility of the trip before the fare and waiting costs are taken into account - useful for comparing how much passengers of different income levels value getting a ride
        float expectedNetUtilityBeforeVariableCosts = passengerEconomicParameters.tripUtilityScore - expectedTripTimeDisutility - passengerEconomicParameters.bestSubstitute.netUtility;


        float expectedValueSurplus = expectedNetValue - passengerEconomicParameters.bestSubstitute.netValue;
        hasAcceptedRideOffer = expectedValueSurplus > 0;

        // Debug.Log("Passenger " + id + " - fare $: " + rideOffer.fare.total + ", waiting cost $: " + expectedWaitingCost + " for waiting " + rideOffer.expectedWaitingTime + " hours");
        // Debug.Log("Passenger " + id + " Net expected utility $ from ride: " + expectedNetValue);
        TripCreatedPassengerData tripCreatedPassengerData = new TripCreatedPassengerData()
        {
            hasAcceptedRideOffer = hasAcceptedRideOffer,
            tripUtilityValue = passengerEconomicParameters.tripUtilityValue,
            expectedWaitingCost = expectedWaitingCost,
            expectedTripTimeCost = expectedTripTimeCost,
            expectedNetValue = expectedNetValue,
            expectedNetUtility = expectedNetUtility,
            expectedValueSurplus = expectedValueSurplus,
            expectedNetUtilityBeforeVariableCosts = expectedNetUtilityBeforeVariableCosts
        };


        utilityIncomeScatterPlot.AppendPassenger(this, tripCreatedPassengerData);
        if (hasAcceptedRideOffer)
        {
            // Debug.Log("Passenger " + id + " is hailing a taxi");
            currentTrip = GameManager.Instance.AcceptRideOffer(tripCreatedData, tripCreatedPassengerData);
            SetState(PassengerState.AssignedToTrip);
        }
        else
        {
            // Debug.Log("Passenger " + id + " is giving up");
            passengerSurplusGraph.AppendPassenger(this);
            SetState(PassengerState.RejectedRideOffer);
            Destroy(gameObject);
        }


    }


    public static Passenger Create(Transform prefab, float x, float z)
    {

        Quaternion rotation = Quaternion.identity;

        float xVisual = x;
        float zVisual = z;

        if (x % GridUtils.blockSize == 0)
        {
            xVisual = x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (z % GridUtils.blockSize == 0)
        {
            zVisual = z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));

        }

        Transform passengerTransform = Instantiate(prefab, new Vector3(xVisual, 0.08f, zVisual), rotation);
        passengerTransform.name = "Passenger";
        Passenger passenger = passengerTransform.GetComponent<Passenger>();
        passenger.positionActual = new Vector3(x, 0.08f, z);
        return passenger;
    }

    public PickedUpPassengerData HandlePassengerPickedUp(PickedUpData pickedUpData)
    {
        float waitingCost = pickedUpData.waitingTime * passengerEconomicParameters.waitingCostPerHour;
        float valueSurplus = currentTrip.tripCreatedPassengerData.tripUtilityValue - waitingCost - currentTrip.tripCreatedData.fare.total;

        float utilitySurplus = valueSurplus / passengerEconomicParameters.hourlyIncome;
        // Debug.Log($"Passenger {id} was picked up at {TimeUtils.ConvertSimulationHoursToTimeString(pickedUpData.pickedUpTime)}, expected pickup time was {TimeUtils.ConvertSimulationHoursToTimeString(currentTrip.tripCreatedData.expectedPickupTime)}, difference is {(pickedUpData.pickedUpTime - currentTrip.tripCreatedData.expectedPickupTime) * 60f} minutes");

        // Debug.Log($"Surplus gained by passenger {id} is {utilitySurplus}");

        PickedUpPassengerData pickedUpPassengerData = new PickedUpPassengerData()
        {
            waitingCost = waitingCost,
        };

        waitingTimeGraph.SetNewValue(pickedUpData.waitingTime);

        return pickedUpPassengerData;
    }

    public DroppedOffPassengerData HandlePassengerDroppedOff(DroppedOffData droppedOffData)
    {
        float tripTimeCost = droppedOffData.timeSpentOnTrip * passengerEconomicParameters.waitingCostPerHour;
        float netValue = currentTrip.tripCreatedPassengerData.tripUtilityValue - tripTimeCost - currentTrip.pickedUpPassengerData.waitingCost - currentTrip.tripCreatedData.fare.total;
        float netUtility = netValue / passengerEconomicParameters.hourlyIncome;
        float valueSurplus = netValue - passengerEconomicParameters.bestSubstitute.netValue;
        float utilitySurplus = valueSurplus / passengerEconomicParameters.hourlyIncome;

        DroppedOffPassengerData droppedOffPassengerData = new DroppedOffPassengerData()
        {
            tripTimeCost = tripTimeCost,
            netValue = netValue,
            netUtility = netUtility,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

        passengerSurplusGraph.AppendPassenger(this);


        return droppedOffPassengerData;

    }
    public void SetState(PassengerState state)
    {
        this.state = state;
    }

    public void HandlePassengerDroppedOff()
    {
        this.transform.parent = null;
        SetState(PassengerState.DroppedOff);
        Destroy(gameObject);
    }
}


