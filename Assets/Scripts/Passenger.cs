using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

#nullable enable
public class Passenger : MonoBehaviour
{
    [SerializeField] public Transform spawnAnimationPrefab;


    private float spawnDuration = 1;


    [SerializeField] public Transform agentStatusTextPrefab;


    private WaitingTimeGraph? waitingTimeGraph;

    private UtilityIncomeScatterPlot? utilityIncomeScatterPlot;

    private PassengerSurplusGraph? passengerSurplusGraph;


    public bool hasAcceptedRideOffer = false;

    private City city;

    public PassengerPerson person;

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
        passenger.person.timeSpawned = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        return passenger;
    }

    // 
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
        Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);

        transform.localScale = Vector3.zero;
        float startTime = Time.time;
        passengerAnimator.SetTrigger("LookAtPhone");
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseUtils.EaseInOutCubic(t);
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    void MakeTripDecision()
    {
        RideOffer rideOffer = city.RequestRideOffer(person.startPosition, person.destination);


        float tripCreatedTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
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
            Destroy(gameObject);
        }


    }


    public void HandleDriverArrivedAtPickUp()
    {
        AgentStatusText.Create(agentStatusTextPrefab, transform, Vector3.up * 0.5f, $"-${person.trip.tripCreatedData.fare.total.ToString("F2")}", Color.red);

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


    public IEnumerator EndTripAnimation()
    {
        this.transform.parent = null;
        yield return StartCoroutine(SlideOffCarRoof(0.5f));
        // yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(DespawnPassenger(duration: 1.5f));

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

    public IEnumerator DespawnPassenger(float duration)
    {
        Transform despawnAnimationPrefab = Resources.Load<Transform>("DespawnAnimation");

        Instantiate(despawnAnimationPrefab, transform.position, Quaternion.identity);
        passengerAnimator.SetTrigger("Celebrate");
        yield return new WaitForSeconds(0.5f);
        Quaternion startRotation = transform.localRotation;
        float endRotationY = 360 * 5;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float shrinkFactor = EaseUtils.EaseInOutCubic(t);
            float spinFactor = EaseUtils.EaseInCubic(t);
            transform.localScale = Vector3.one * (1 - shrinkFactor);
            Quaternion newRotation = Quaternion.AngleAxis(startRotation.eulerAngles.y + endRotationY * spinFactor, Vector3.up);
            transform.localRotation = newRotation;
            yield return null;
        }
        Destroy(gameObject);
    }
}


