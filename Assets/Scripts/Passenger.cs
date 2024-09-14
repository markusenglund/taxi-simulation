using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#nullable enable


public enum DespawnReason
{
    RejectedRideOffer,
    NoRideOffer,
    DroppedOff
}

public enum PassengerMode
{
    Active,
    Inactive
}
public class Passenger : MonoBehaviour
{
    [SerializeField] public Transform spawnAnimationPrefab;


    private float spawnDuration = 1;


    [SerializeField] public Transform agentStatusTextPrefab;
    public Transform agentReactionTextPrefab;

    private City city;

    private Transform parentTransform;



    public PassengerPerson person;

    public float passengerScale;

    PassengerMode mode;

    Animator passengerAnimator;



    public static Passenger Create(PassengerPerson person, Transform prefab, Transform parentTransform, SimulationSettings simSettings, City? city, PassengerMode mode = PassengerMode.Active, float spawnDuration = 1f)
    {

        (Vector3 position, Quaternion rotation) = GetSideWalkPositionRotation(person.startPosition, person.id);

        Transform passengerTransform = Instantiate(prefab, parentTransform, false);
        passengerTransform.rotation = rotation;
        passengerTransform.localPosition = position;
        Passenger passenger = passengerTransform.GetComponent<Passenger>();
        passenger.city = city;
        passenger.parentTransform = parentTransform;
        passenger.spawnDuration = spawnDuration;
        passenger.person = person;
        passenger.person.timeSpawned = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        passenger.passengerScale = simSettings.passengerScale + 0.1f * simSettings.passengerScale * Mathf.Pow(Mathf.Min(person.economicParameters.hourlyIncome, 100), 1f / 3f);
        passenger.mode = mode;
        if (passenger.person.state == PassengerState.BeforeSpawn)
        {
            passenger.person.state = PassengerState.Idling;
        }
        SetDressColor(passenger, person.economicParameters.hourlyIncome);
        return passenger;
    }

    public void SetMode(PassengerMode mode)
    {
        this.mode = mode;
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
        if (hourlyIncome <= 15)
        {
            dressBaseColor = Color.Lerp(ColorScheme.dressRed, ColorScheme.orange, Mathf.InverseLerp(10, 15, hourlyIncome));
        }
        else if (hourlyIncome <= 17)
        {
            dressBaseColor = ColorScheme.orange;
        }
        else if (hourlyIncome <= 25)
        {
            dressBaseColor = Color.Lerp(ColorScheme.orange, ColorScheme.dressYellow, Mathf.InverseLerp(17, 25, hourlyIncome));
        }
        else if (hourlyIncome <= 45)
        {
            dressBaseColor = Color.Lerp(ColorScheme.dressYellow, ColorScheme.dressGreen, Mathf.InverseLerp(25, 45, hourlyIncome));
        }
        else
        {
            dressBaseColor = Color.Lerp(ColorScheme.dressGreen, ColorScheme.darkGreen, Mathf.InverseLerp(45, 120, hourlyIncome));
        }
        // We just assume that the first material is DressBase and second is DressAccent, let's hope it doesn't change
        dressMaterials[0].color = dressBaseColor;
        armMaterials[0].color = dressBaseColor;

        Color accentColor = Color.Lerp(dressBaseColor, Color.black, 0.3f);
        dressMaterials[1].color = accentColor;
        armMaterials[1].color = accentColor;
    }
    private static (Vector3 position, Quaternion rotation) GetSideWalkPositionRotation(Vector3 roadPosition, int passengerId)
    {
        float positionX = roadPosition.x;
        float positionZ = roadPosition.z;
        Quaternion rotation = Quaternion.identity;
        bool isVerticalRoad = roadPosition.z % GridUtils.blockSize == 0;
        bool isHorizontalRoad = roadPosition.x % GridUtils.blockSize == 0;
        if (isVerticalRoad || isHorizontalRoad)
        {
            if (isVerticalRoad)
            {
                positionZ = roadPosition.z + (passengerId % 2 == 0 ? -.23f : .23f);
                rotation = Quaternion.LookRotation(new Vector3(0, 0, passengerId % 2 == 0 ? 1 : -1));
            }
            if (isHorizontalRoad)
            {
                positionX = roadPosition.x + (passengerId % 2 == 0 ? -.23f : .23f);
                rotation = Quaternion.LookRotation(new Vector3(passengerId % 2 == 0 ? 1 : -1, 0, 0));
            }

        }
        else
        {
            // Find the direction of the closest road
            float closestVerticalRoadZ = roadPosition.z % GridUtils.blockSize < GridUtils.blockSize / 2 ? Mathf.Floor(roadPosition.z / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Ceil(roadPosition.z / GridUtils.blockSize) * GridUtils.blockSize;
            float closestHorizontalRoadX = roadPosition.x % GridUtils.blockSize < GridUtils.blockSize / 2 ? Mathf.Floor(roadPosition.x / GridUtils.blockSize) * GridUtils.blockSize : Mathf.Ceil(roadPosition.x / GridUtils.blockSize) * GridUtils.blockSize;
            float distanceToVerticalRoad = Mathf.Abs(roadPosition.z - closestVerticalRoadZ);
            float distanceToHorizontalRoad = Mathf.Abs(roadPosition.x - closestHorizontalRoadX);
            if (distanceToVerticalRoad < distanceToHorizontalRoad)
            {
                Vector3 roadDirection = roadPosition.z < closestVerticalRoadZ ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
                rotation = Quaternion.LookRotation(roadDirection);
            }
            else
            {
                Vector3 roadDirection = roadPosition.x < closestHorizontalRoadX ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
                rotation = Quaternion.LookRotation(roadDirection);
            }
        }


        return (new Vector3(positionX, GridUtils.curbHeight, positionZ), rotation);
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
        if (mode == PassengerMode.Active && !city.simulationEnded)
        {
            MakeTripDecision();
        }
        yield return null;
    }

    IEnumerator SpawnPassenger()
    {
        Transform spawnAnimation = Instantiate(spawnAnimationPrefab, transform.position, Quaternion.identity);
        StartCoroutine(DestroySpawnAnimation(spawnAnimation));
        spawnAnimation.localScale = Vector3.one * Mathf.Sqrt(passengerScale) * 2.5f;

        transform.localScale = Vector3.zero;
        Vector3 finalScale = Vector3.one * passengerScale;
        float startTime = Time.time;
        if (mode == PassengerMode.Active)
        {
            passengerAnimator.SetTrigger("LookAtPhone");
        }
        while (Time.time < startTime + spawnDuration)
        {
            float t = (Time.time - startTime) / spawnDuration;
            t = EaseUtils.EaseInOutCubic(t);
            transform.localScale = finalScale * t;
            yield return null;
        }
        transform.localScale = finalScale;
    }

    IEnumerator DestroySpawnAnimation(Transform spawnAnimation)
    {
        yield return new WaitForSeconds(3);
        Destroy(spawnAnimation.gameObject);
    }

    void MakeTripDecision()
    {
        // Debug.Log("Passenger " + person.id + " is making a trip decision at start position " + person.startPosition);
        (RideOffer? rideOffer, Driver? driver, int numTripsAssigned, int numIdleDrivers) = city.RequestRideOffer(person.startPosition, person.destination);
        city.idleDriversData.Add(numIdleDrivers);
        if (rideOffer == null)
        {
            person.SetState(PassengerState.NoRideOffer);
            person.tripTypeChosen = person.economicParameters.GetBestSubstitute().type;
            person.rideOfferStatus = RideOfferStatus.NoneReceived;
            StartCoroutine(DespawnPassenger(duration: 1.5f, DespawnReason.NoRideOffer));
            ShowPassengerReaction(person.tripTypeChosen, receivedRideOffer: false);

        }
        else
        {
            person.rideOffer = rideOffer;
            float tripCreatedTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
            float expectedPickupTime = tripCreatedTime + rideOffer.expectedWaitingTime;

            TripCreatedData tripCreatedData = new TripCreatedData()
            {
                passenger = this,
                createdTime = tripCreatedTime,
                pickUpPosition = person.startPosition,
                destination = person.destination,
                tripDistance = person.distanceToDestination,
                expectedWaitingTime = rideOffer.expectedWaitingTime,
                expectedTripTime = rideOffer.expectedTripTime,
                fare = rideOffer.fare,
                expectedPickupTime = expectedPickupTime,
                numTripsAssigned = numTripsAssigned,
                numIdleDrivers = numIdleDrivers
            };
            float expectedTotalTime = rideOffer.expectedWaitingTime + rideOffer.expectedTripTime;
            float expectedWaitingCost = rideOffer.expectedWaitingTime * person.economicParameters.valueOfTime;
            float expectedTripTimeCost = rideOffer.expectedTripTime * person.economicParameters.valueOfTime;
            float expectedTotalTimeCost = expectedWaitingCost + expectedTripTimeCost;

            float totalCost = expectedWaitingCost + expectedTripTimeCost + rideOffer.fare.total;



            // Debug.Log("Passenger " + id + " - fare $: " + rideOffer.fare.total + ", waiting cost $: " + expectedWaitingCost + " for waiting " + rideOffer.expectedWaitingTime + " hours");


            TripOption uberTripOption = new TripOption()
            {
                type = TripType.Uber,
                timeHours = expectedTotalTime,
                timeCost = expectedTotalTimeCost,
                moneyCost = rideOffer.fare.total,
                totalCost = totalCost
            };
            person.uberTripOption = uberTripOption;

            TripOption bestSubstitute = person.economicParameters.GetBestSubstitute();
            float expectedValueSurplus = bestSubstitute.totalCost - uberTripOption.totalCost;
            float expectedUtilitySurplus = expectedValueSurplus / person.economicParameters.hourlyIncome;
            TripType tripTypeChosen = expectedValueSurplus > 0 ? TripType.Uber : bestSubstitute.type;
            bool hasAcceptedRideOffer = tripTypeChosen == TripType.Uber;

            person.tripTypeChosen = tripTypeChosen;


            TripCreatedPassengerData tripCreatedPassengerData = new TripCreatedPassengerData()
            {
                expectedWaitingCost = expectedWaitingCost,
                expectedTripTimeCost = expectedTripTimeCost,
                expectedTotalTimeCost = expectedTotalTimeCost,
                expectedValueSurplus = expectedValueSurplus,
                expectedUtilitySurplus = expectedUtilitySurplus,
            };

            if (hasAcceptedRideOffer)
            {
                person.rideOfferStatus = RideOfferStatus.Accepted;
                // Debug.Log("Passenger " + id + " is hailing a taxi");
                person.trip = city.AcceptRideOffer(tripCreatedData, tripCreatedPassengerData, driver!);
                person.SetState(PassengerState.AssignedToTrip);
                ShowPassengerReaction(TripType.Uber, receivedRideOffer: true);

            }
            else
            {
                person.rideOfferStatus = RideOfferStatus.Rejected;
                // Debug.Log("Passenger " + id + " is giving up");
                person.SetState(PassengerState.RejectedRideOffer);
                StartCoroutine(DespawnPassenger(duration: 1.5f, DespawnReason.RejectedRideOffer));
                ShowPassengerReaction(person.tripTypeChosen, receivedRideOffer: true);

            }
        }


    }


    public void HandleDriverArrivedAtPickUp()
    {
        if (city.simulationSettings.showPassengerCosts)
        {

            AgentOverheadText.Create(agentStatusTextPrefab, transform, Vector3.up * (passengerScale * 0.3f + 0.3f), $"-${person.trip.tripCreatedData.fare.total.ToString("F2")}", ColorScheme.darkRed);
        }

    }

    public PickedUpPassengerData HandlePassengerPickedUp(PickedUpData pickedUpData)
    {
        float waitingCost = pickedUpData.waitingTime * person.economicParameters.valueOfTime;

        // Debug.Log($"Passenger {id} was picked up at {TimeUtils.ConvertSimulationHoursToTimeString(pickedUpData.pickedUpTime)}, expected pickup time was {TimeUtils.ConvertSimulationHoursToTimeString(person.trip.tripCreatedData.expectedPickupTime)}, difference is {(pickedUpData.pickedUpTime - person.trip.tripCreatedData.expectedPickupTime) * 60f} minutes");
        PickedUpPassengerData pickedUpPassengerData = new PickedUpPassengerData()
        {
            waitingCost = waitingCost,
        };

        return pickedUpPassengerData;
    }

    public DroppedOffPassengerData HandlePassengerDroppedOff(DroppedOffData droppedOffData)
    {
        TripOption bestSubstitute = person.economicParameters.GetBestSubstitute();
        float tripTimeCost = droppedOffData.timeSpentOnTrip * person.economicParameters.valueOfTime;
        float totalTimeCost = droppedOffData.totalTime * person.economicParameters.valueOfTime;
        float totalCost = totalTimeCost + person.trip.tripCreatedData.fare.total;
        float valueSurplus = bestSubstitute.totalCost - totalCost;
        float utilitySurplus = valueSurplus / person.economicParameters.hourlyIncome;

        DroppedOffPassengerData droppedOffPassengerData = new DroppedOffPassengerData()
        {
            tripTimeCost = tripTimeCost,
            totalTimeCost = totalTimeCost,
            totalCost = totalCost,
            valueSurplus = valueSurplus,
            utilitySurplus = utilitySurplus
        };

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
        // I'm flummoxed by why the passengers feet are not on the ground when they are at the top of the car unless we do this hack
        float topTaxiY = 1.45f - passengerScale * 0.06f;
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
        transform.SetParent(parentTransform);
        float startTime = Time.time;
        Vector3 startPosition = transform.localPosition;
        Vector3 finalPosition = GetSideWalkPositionRotation(person.destination, this.person.id).position;

        Quaternion startRotation = transform.localRotation;
        Quaternion finalRotation = Quaternion.LookRotation(finalPosition - new Vector3(startPosition.x, GridUtils.curbHeight, startPosition.z), Vector3.up);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float verticalT = EaseUtils.EaseInCubic(t) * 1.1f - 0.1f;
            float horizontalT = EaseUtils.EaseInOutCubic(t);
            transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, horizontalT);
            transform.localPosition = new Vector3(Mathf.Lerp(startPosition.x, finalPosition.x, horizontalT), Mathf.Lerp(startPosition.y, finalPosition.y, verticalT), Mathf.Lerp(startPosition.z, finalPosition.z, horizontalT));
            yield return null;
        }
        transform.localPosition = finalPosition;
        transform.localRotation = finalRotation;
        yield return null;
    }

    public void ShowPassengerReaction(TripType tripType, bool receivedRideOffer)
    {
        if (!city.simulationSettings.showPassengerReactions)
        {
            return;
        }
        Dictionary<TripType, string> tripTypeToEmoji = new Dictionary<TripType, string>()
        {
            { TripType.Uber, "🚕" },
            { TripType.Walking, "🚶" },
            { TripType.PublicTransport, "🚌" },
        };
        Vector3 pointAbovePassengersHead = new Vector3(0, passengerScale * 0.3f + 0.2f, 0);
        Vector3 awayFromCamera = (transform.position - Camera.main.transform.position).normalized * 0.5f;
        awayFromCamera.y = 0;
        Vector3 reactionPosition = pointAbovePassengersHead + awayFromCamera;
        // if (receivedRideOffer)
        // {
        if (tripType == TripType.Uber)
        {
            string reaction = tripTypeToEmoji[tripType];
            AgentOverheadReaction.Create(transform, reactionPosition, reaction, ColorScheme.yellow);
        }
        else
        {
            string reaction = tripTypeToEmoji[tripType];
            AgentOverheadReaction.Create(transform, reactionPosition, reaction, ColorScheme.purple, receivedOffer: receivedRideOffer);
        }
    }

    public IEnumerator DespawnPassenger(float duration, DespawnReason reason)
    {
        // if (city.simulationSettings.showPassengerReactions)
        // {
        // if (reason == DespawnReason.DroppedOff)
        // {
        //     float surplus = person.trip.droppedOffPassengerData.utilitySurplus;
        //     int surplusCeil = Mathf.CeilToInt(surplus);
        //     // Add one smiley face per unit of utility surplus
        //     if (surplusCeil > 0)
        //     {
        //         string reaction = new string('+', surplusCeil);

        //         AgentOverheadReaction.Create(transform, reactionPosition, reaction, ColorScheme.green, isBold: true, addPadding: true);
        //     }
        //     else
        //     {
        //         string reaction = "😐";
        //         AgentOverheadReaction.Create(transform, reactionPosition, reaction, ColorScheme.yellow, isBold: false);

        //     }
        // }
        // }

        Transform despawnAnimationPrefab = Resources.Load<Transform>("DespawnAnimation");

        Transform despawnAnimation = Instantiate(despawnAnimationPrefab, transform.position, Quaternion.identity);
        despawnAnimation.localScale = Vector3.one * passengerScale;
        passengerAnimator.SetTrigger(reason == DespawnReason.DroppedOff ? "Celebrate" : "BeDisappointed");
        yield return new WaitForSeconds(1f);
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
        Destroy(despawnAnimation.gameObject);
        Destroy(gameObject);
    }
}


