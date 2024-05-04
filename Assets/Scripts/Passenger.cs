using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using Random = System.Random;
using System.Collections.Generic;

#nullable enable

public enum DespawnReason
{
    RejectedRideOffer,
    NoRideOffer,
    DroppedOff
}
public class Passenger : MonoBehaviour
{
    [SerializeField] public Transform spawnAnimationPrefab;


    private float spawnDuration = 1;


    [SerializeField] public Transform agentStatusTextPrefab;
    public Transform agentReactionTextPrefab;


    private WaitingTimeGraph? waitingTimeGraph;

    private UtilityIncomeScatterPlot? utilityIncomeScatterPlot;

    private PassengerSurplusGraph? passengerSurplusGraph;


    public bool hasAcceptedRideOffer = false;

    private City city;

    public PassengerPerson person;

    float passengerScaleFactor = 5f;

    Animator passengerAnimator;

    public static Passenger Create(PassengerPerson person, Transform prefab, City city, WaitingTimeGraph waitingTimeGraph, PassengerSurplusGraph passengerSurplusGraph, UtilityIncomeScatterPlot utilityIncomeScatterPlot)
    {

        (Vector3 position, Quaternion rotation) = GetSideWalkPositionRotation(person.startPosition);

        Transform passengerTransform = Instantiate(prefab, city.transform, false);
        passengerTransform.rotation = rotation;
        passengerTransform.localPosition = position;
        Passenger passenger = passengerTransform.GetComponent<Passenger>();
        passenger.city = city;
        passenger.waitingTimeGraph = waitingTimeGraph;
        passenger.passengerSurplusGraph = passengerSurplusGraph;
        passenger.utilityIncomeScatterPlot = utilityIncomeScatterPlot;
        passenger.person = person;
        passenger.person.state = PassengerState.Idling;
        passenger.person.timeSpawned = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);


        SetDressColor(passenger, person.economicParameters.hourlyIncome);
        return passenger;
    }

    static private void SetDressColor(Passenger passenger, float hourlyIncome)
    {

        Transform dressBody = passenger.transform.Find("blender-character-v5@Standing Greeting/DressBody");
        Material[] dressMaterials = dressBody.GetComponent<SkinnedMeshRenderer>().materials;

        Transform dressArms = passenger.transform.Find("blender-character-v5@Standing Greeting/DressArms");
        Material[] armMaterials = dressArms.GetComponent<SkinnedMeshRenderer>().materials;
        Color green = new Color(0, 0.8f, 0.3f);
        // For incomes below, the dress color is a gradient between red and yellow
        Color dressBaseColor;
        if (hourlyIncome <= 20)
        {
            dressBaseColor = Color.Lerp(Color.red, Color.yellow, Mathf.InverseLerp(10, 20, hourlyIncome));
        }
        else if (hourlyIncome <= 40)
        {
            dressBaseColor = Color.Lerp(Color.yellow, green, Mathf.InverseLerp(20, 40, hourlyIncome));
        }
        else
        {
            dressBaseColor = Color.Lerp(green, Color.black, Mathf.InverseLerp(40, 120, hourlyIncome));
        }
        // We just assume that the first material is DressBase and second is DressAccent, let's hope it doesn't change
        dressMaterials[0].color = dressBaseColor;
        armMaterials[0].color = dressBaseColor;

        Color accentColor = Color.Lerp(dressBaseColor, Color.black, 0.3f);
        dressMaterials[1].color = accentColor;
        armMaterials[1].color = accentColor;
    }
    private static (Vector3 position, Quaternion rotation) GetSideWalkPositionRotation(Vector3 roadPosition)
    {
        float positionX = roadPosition.x;
        float positionZ = roadPosition.z;
        Quaternion rotation = Quaternion.identity;
        if (roadPosition.x % GridUtils.blockSize == 0)
        {
            positionX = roadPosition.x + .23f;
            rotation = Quaternion.LookRotation(new Vector3(-1, 0, 0));
        }
        if (roadPosition.z % GridUtils.blockSize == 0)
        {
            positionZ = roadPosition.z + .23f;
            rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));
        }

        return (new Vector3(positionX, 0.08f, positionZ), rotation);
    }

    void Awake()
    {
        passengerAnimator = this.GetComponentInChildren<Animator>();
    }

    void Start()
    {
        agentReactionTextPrefab = Resources.Load<Transform>("AgentReaction");
        StartCoroutine(ScheduleActions());
    }

    IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnPassenger());
        yield return new WaitForSeconds(1);
        MakeTripDecision();
        yield return null;
    }

    IEnumerator SpawnPassenger()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        spawnAnimation.localScale = Vector3.one * passengerScaleFactor;

        transform.localScale = Vector3.zero;
        Vector3 finalScale = Vector3.one * passengerScaleFactor;
        float startTime = Time.time;
        passengerAnimator.SetTrigger("LookAtPhone");
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseUtils.EaseInOutCubic(t);
            transform.localScale = finalScale * t;
            yield return null;
        }
        transform.localScale = finalScale;
    }

    void MakeTripDecision()
    {
        RideOffer? rideOffer = city.RequestRideOffer(person.startPosition, person.destination);


        if (rideOffer == null)
        {
            person.SetState(PassengerState.NoRideOffer);
            person.tripTypeChosen = person.economicParameters.bestSubstitute.type;
            hasAcceptedRideOffer = false;
            StartCoroutine(DespawnPassenger(duration: 1.5f, DespawnReason.NoRideOffer));
        }
        else
        {


            float tripCreatedTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
            float expectedPickupTime = tripCreatedTime + rideOffer.expectedWaitingTime;

            TripCreatedData tripCreatedData = new TripCreatedData()
            {
                passenger = this,
                createdTime = tripCreatedTime,
                pickUpPosition = person.startPosition,
                destination = person.destination,
                tripDistance = GridUtils.GetDistance(person.startPosition, person.destination),
                expectedWaitingTime = rideOffer.expectedWaitingTime,
                expectedTripTime = rideOffer.expectedTripTime,
                fare = rideOffer.fare,
                expectedPickupTime = expectedPickupTime
            };

            float expectedWaitingCost = rideOffer.expectedWaitingTime * person.economicParameters.waitingCostPerHour;
            float expectedTripTimeCost = rideOffer.expectedTripTime * person.economicParameters.waitingCostPerHour;

            float totalCost = expectedWaitingCost + expectedTripTimeCost + rideOffer.fare.total;

            float expectedNetValue = person.economicParameters.tripUtilityValue - totalCost;
            float expectedNetUtility = expectedNetValue / person.economicParameters.hourlyIncome;
            float expectedTripTimeDisutility = expectedTripTimeCost / person.economicParameters.hourlyIncome;
            // 'expectedNetUtilityBeforeVariableCosts' represents the utility of the trip before the fare and waiting costs are taken into account - useful for comparing how much passengers of different income levels value getting a ride
            float expectedNetUtilityBeforeVariableCosts = person.economicParameters.tripUtilityScore - expectedTripTimeDisutility - person.economicParameters.bestSubstitute.netUtility;


            float expectedValueSurplus = expectedNetValue - person.economicParameters.bestSubstitute.netValue;
            TripType tripTypeChosen = expectedValueSurplus > 0 ? TripType.Uber : person.economicParameters.bestSubstitute.type;
            hasAcceptedRideOffer = tripTypeChosen == TripType.Uber;

            // Debug.Log("Passenger " + id + " - fare $: " + rideOffer.fare.total + ", waiting cost $: " + expectedWaitingCost + " for waiting " + rideOffer.expectedWaitingTime + " hours");
            // Debug.Log("Passenger " + id + " Net expected utility $ from ride: " + expectedNetValue);
            TripCreatedPassengerData tripCreatedPassengerData = new TripCreatedPassengerData()
            {
                hasAcceptedRideOffer = hasAcceptedRideOffer,
                tripUtilityValue = person.economicParameters.tripUtilityValue,
                expectedWaitingCost = expectedWaitingCost,
                expectedTripTimeCost = expectedTripTimeCost,
                expectedNetValue = expectedNetValue,
                expectedNetUtility = expectedNetUtility,
                expectedValueSurplus = expectedValueSurplus,
                expectedNetUtilityBeforeVariableCosts = expectedNetUtilityBeforeVariableCosts
            };

            person.tripTypeChosen = tripTypeChosen;

            if (utilityIncomeScatterPlot != null)
            {
                utilityIncomeScatterPlot.AppendPassenger(this, tripCreatedPassengerData);
            }
            if (hasAcceptedRideOffer)
            {
                // Debug.Log("Passenger " + id + " is hailing a taxi");
                person.trip = city.AcceptRideOffer(tripCreatedData, tripCreatedPassengerData);
                person.SetState(PassengerState.AssignedToTrip);
            }
            else
            {
                // Debug.Log("Passenger " + id + " is giving up");
                if (passengerSurplusGraph != null)
                {
                    passengerSurplusGraph.AppendPassenger(this);
                }
                person.SetState(PassengerState.RejectedRideOffer);
                StartCoroutine(DespawnPassenger(duration: 1.5f, DespawnReason.RejectedRideOffer));
            }
        }


    }


    public void HandleDriverArrivedAtPickUp()
    {
        AgentOverheadText.Create(agentStatusTextPrefab, transform, Vector3.up * (passengerScaleFactor * 0.3f + 0.3f), $"-${person.trip.tripCreatedData.fare.total.ToString("F2")}", Color.red);

    }

    public PickedUpPassengerData HandlePassengerPickedUp(PickedUpData pickedUpData)
    {
        float waitingCost = pickedUpData.waitingTime * person.economicParameters.waitingCostPerHour;
        float valueSurplus = person.trip.tripCreatedPassengerData.tripUtilityValue - waitingCost - person.trip.tripCreatedData.fare.total;

        float utilitySurplus = valueSurplus / person.economicParameters.hourlyIncome;
        // Debug.Log($"Passenger {id} was picked up at {TimeUtils.ConvertSimulationHoursToTimeString(pickedUpData.pickedUpTime)}, expected pickup time was {TimeUtils.ConvertSimulationHoursToTimeString(person.trip.tripCreatedData.expectedPickupTime)}, difference is {(pickedUpData.pickedUpTime - person.trip.tripCreatedData.expectedPickupTime) * 60f} minutes");

        // Debug.Log($"Surplus gained by passenger {id} is {utilitySurplus}");

        PickedUpPassengerData pickedUpPassengerData = new PickedUpPassengerData()
        {
            waitingCost = waitingCost,
        };

        if (waitingTimeGraph != null)
        {
            waitingTimeGraph.SetNewValue(pickedUpData.waitingTime);
        }


        return pickedUpPassengerData;
    }

    public DroppedOffPassengerData HandlePassengerDroppedOff(DroppedOffData droppedOffData)
    {
        float tripTimeCost = droppedOffData.timeSpentOnTrip * person.economicParameters.waitingCostPerHour;
        float netValue = person.trip.tripCreatedPassengerData.tripUtilityValue - tripTimeCost - person.trip.pickedUpPassengerData.waitingCost - person.trip.tripCreatedData.fare.total;
        float netUtility = netValue / person.economicParameters.hourlyIncome;
        float valueSurplus = netValue - person.economicParameters.bestSubstitute.netValue;
        float utilitySurplus = valueSurplus / person.economicParameters.hourlyIncome;

        DroppedOffPassengerData droppedOffPassengerData = new DroppedOffPassengerData()
        {
            tripTimeCost = tripTimeCost,
            netValue = netValue,
            netUtility = netUtility,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

        if (passengerSurplusGraph != null)
        {
            passengerSurplusGraph.AppendPassenger(this);
        }
        person.SetState(PassengerState.DroppedOff);

        StartCoroutine(EndTripAnimation());


        return droppedOffPassengerData;

    }

    public IEnumerator JumpToCarRoof(float duration, Driver driver)
    {
        passengerAnimator.SetTrigger("EnterTaxi");
        yield return new WaitForSeconds(0.3f);

        transform.SetParent(driver.transform);
        float startTime = Time.time;
        Vector3 startPosition = transform.localPosition;
        float topTaxiY = 1.2f;
        Vector3 finalPosition = new Vector3(0.09f, topTaxiY, 0);

        Quaternion startRotation = transform.localRotation;
        Quaternion finalRotation = Quaternion.Euler(0, 0, 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float verticalT = 1.2f * EaseUtils.EaseOutQuadratic(t);
            float horizontalT = EaseUtils.EaseInOutCubic(t);
            // transform.localPosition = Vector3.Lerp(startPosition, finalPosition, t);
            transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, horizontalT);
            transform.localPosition = new Vector3(Mathf.Lerp(startPosition.x, finalPosition.x, horizontalT), Mathf.Lerp(startPosition.y, finalPosition.y, verticalT), Mathf.Lerp(startPosition.z, finalPosition.z, horizontalT));
            yield return null;
        }
        transform.localPosition = finalPosition;
        transform.localRotation = finalRotation;
    }


    public IEnumerator EndTripAnimation()
    {
        this.transform.parent = null;
        yield return StartCoroutine(SlideOffCarRoof(0.5f));
        // yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(DespawnPassenger(duration: 1.5f, DespawnReason.DroppedOff));

        Destroy(gameObject);
    }

    IEnumerator SlideOffCarRoof(float duration)
    {
        transform.SetParent(city.transform);
        float startTime = Time.time;
        Vector3 startPosition = transform.localPosition;
        Vector3 finalPosition = GetSideWalkPositionRotation(person.destination).position;

        Quaternion startRotation = transform.localRotation;
        Quaternion finalRotation = Quaternion.LookRotation(finalPosition - new Vector3(startPosition.x, 0.08f, startPosition.z), Vector3.up);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float verticalT = EaseUtils.EaseInCubic(t);
            float horizontalT = EaseUtils.EaseInOutCubic(t);
            transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, horizontalT);
            transform.localPosition = new Vector3(Mathf.Lerp(startPosition.x, finalPosition.x, horizontalT), Mathf.Lerp(startPosition.y, finalPosition.y, verticalT), Mathf.Lerp(startPosition.z, finalPosition.z, horizontalT));
            yield return null;
        }
        transform.localPosition = finalPosition;
        transform.localRotation = finalRotation;
        yield return null;
    }

    public IEnumerator DespawnPassenger(float duration, DespawnReason reason)
    {
        Dictionary<TripType, string> tripTypeToEmoji = new Dictionary<TripType, string>()
        {
            { TripType.RentalCar, "🚗" },
            { TripType.Uber, "🚕" },
            { TripType.Walking, "🚶" },
            { TripType.PublicTransport, "🚌" },
            { TripType.SkipTrip, "🏠" }
        };
        if (reason == DespawnReason.RejectedRideOffer)
        {
            string reaction = tripTypeToEmoji[person.tripTypeChosen];
            AgentOverheadReaction.Create(transform, Vector3.up * (passengerScaleFactor * 0.3f + 0.5f), reaction, Color.red);
        }
        else if (reason == DespawnReason.NoRideOffer)
        {
            string reaction = tripTypeToEmoji[person.tripTypeChosen] + "📵";
            AgentOverheadReaction.Create(transform, Vector3.up * (passengerScaleFactor * 0.3f + 0.5f), reaction, Color.red);
        }
        else if (reason == DespawnReason.DroppedOff)
        {
            float surplus = person.trip.droppedOffPassengerData.utilitySurplus;
            int surplusCeil = Mathf.CeilToInt(surplus);
            // Add one smiley face per unit of utility surplus
            if (surplusCeil > 0)
            {
                string reaction = new string('+', surplusCeil);

                AgentOverheadReaction.Create(transform, Vector3.up * (passengerScaleFactor * 0.3f + 0.5f), reaction, Color.green, isBold: true);
            }
            else
            {
                string reaction = "😐";
                AgentOverheadReaction.Create(transform, Vector3.up * (passengerScaleFactor * 0.3f + 0.5f), reaction, Color.yellow, isBold: false);

            }
        }

        Transform despawnAnimationPrefab = Resources.Load<Transform>("DespawnAnimation");

        Transform despawnAnimation = Instantiate(despawnAnimationPrefab, transform.position, Quaternion.identity);
        despawnAnimation.localScale = Vector3.one * passengerScaleFactor;
        passengerAnimator.SetTrigger(reason == DespawnReason.DroppedOff ? "Celebrate" : "BeDisappointed");
        yield return new WaitForSeconds(0.5f);
        Quaternion startRotation = transform.localRotation;
        float endRotationY = 360 * 5;
        Vector3 startScale = transform.localScale;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float shrinkFactor = EaseUtils.EaseInOutCubic(t);
            float spinFactor = EaseUtils.EaseInCubic(t);
            transform.localScale = startScale * (1 - shrinkFactor);
            Quaternion newRotation = Quaternion.AngleAxis(startRotation.eulerAngles.y + endRotationY * spinFactor, Vector3.up);
            transform.localRotation = newRotation;
            yield return null;
        }
        Destroy(gameObject);
    }
}


