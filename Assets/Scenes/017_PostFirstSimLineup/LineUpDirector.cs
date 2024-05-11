using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.Linq;


public class LineUpDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;

    Random random = new Random();

    GameObject canvas;
    LineRenderer[] lineRenderers;

    List<Vector3> lineUpPositions = new List<Vector3>
    { };

    List<Vector3> incomeDistributionPositions = new List<Vector3>
    { };

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        canvas = GameObject.Find("Canvas");
        canvas.GetComponent<CanvasGroup>().alpha = 0;

        lineRenderers = canvas.GetComponentsInChildren<LineRenderer>();
        foreach (LineRenderer lineRenderer in lineRenderers)
        {
            Color lineRendererColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, 0);
            lineRenderer.startColor = lineRendererColor;
            lineRenderer.endColor = lineRendererColor;
        }
    }

    void Start()
    {
        Camera.main.transform.position = new Vector3(4.5f, 5f, -16f);
        Camera.main.transform.LookAt(cityMiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return new WaitForSeconds(0.5f);
        Passenger[] passengers = city.SpawnSavedPassengers();
        StartCoroutine(FadeInCanvas());
        StartCoroutine(MovePassengersToLineUp(passengers));
        yield return new WaitForSeconds(3f);
        StartCoroutine(TriggerIdleVariations(passengers));
        yield return new WaitForSeconds(2f);
        StartCoroutine(ShowPassengerResults(passengers));
        yield return new WaitForSeconds(15f);
        StartCoroutine(ChangeGraphToIncome(passengers));
    }

    IEnumerator FadeInCanvas()
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        float duration = 1.5f;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float alpha = EaseUtils.EaseInCubic(t);
            canvasGroup.alpha = alpha;
            // Set the alpha of the line renderers to the same value
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                Color lineRendererColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, alpha);
                lineRenderer.startColor = lineRendererColor;
                lineRenderer.endColor = lineRendererColor;
            }
            yield return null;
        }
    }

    IEnumerator MovePassengersToLineUp(Passenger[] passengers)
    {
        float duration = 1.5f;
        foreach (Passenger passenger in passengers)
        {
            StartCoroutine(MovePassengerToLineUp(passenger, duration));
            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator TriggerIdleVariations(Passenger[] passengers)
    {
        while (true)
        {
            // Pick a random passenger
            Passenger randomPassenger1 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator = randomPassenger1.GetComponentInChildren<Animator>();
            passengerAnimator.SetTrigger("IdleVariation1");
            yield return new WaitForSeconds(3f);
            Passenger randomPassenger2 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator2 = randomPassenger2.GetComponentInChildren<Animator>();
            passengerAnimator2.SetTrigger("IdleVariation2");
            yield return new WaitForSeconds(2f);

            Passenger randomPassenger3 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator3 = randomPassenger3.GetComponentInChildren<Animator>();
            passengerAnimator3.SetTrigger("IdleVariation1");
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator MovePassengerToLineUp(Passenger passenger, float duration)
    {
        Animator passengerAnimator = passenger.GetComponentInChildren<Animator>();
        Vector3 startPosition = passenger.transform.position;
        float linePosition = ConvertUtilityScoreToLinePosition(passenger.person.economicParameters.tripUtilityScore);
        Vector3 endPosition = new Vector3(linePosition, 0, -6.7f);
        // Check if there's a lineUpPosition within 0.3f of the endPosition
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingLineUpPosition in lineUpPositions)
            {
                if (Vector3.Distance(existingLineUpPosition, endPosition) < 0.35f)
                {
                    endPosition = endPosition + new Vector3(0, 0, 0.2f);
                    isEndPositionFree = false;
                }
            }
        }
        lineUpPositions.Add(endPosition);
        float startTime = Time.time;
        passengerAnimator.SetTrigger("SlowJump");
        Quaternion startRotation = passenger.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(0, 180 - 30 + 60f * (float)random.NextDouble(), 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float positionFactor = EaseUtils.EaseInOutQuadratic(t);
            // Make the vertical position an upside down parabola
            // float verticalPosition = EaseUtils.EaseInOutCubic(verticalPositionBase);
            // float positionFactor = EaseUtils.EaseInOutCubic(t);
            float verticalPositionBase = 1 - 4 * (positionFactor - 0.5f) * (positionFactor - 0.5f);
            passenger.transform.position = Vector3.Lerp(startPosition, endPosition, positionFactor) + new Vector3(0, verticalPositionBase * 4, 0);
            passenger.transform.rotation = Quaternion.Lerp(startRotation, finalRotation, t);
            yield return null;
        }
        // passenger.transform.position = endPosition;
    }

    private IEnumerator ShowPassengerResults(Passenger[] passengers)
    {
        Dictionary<TripType, string> tripTypeToEmoji = new Dictionary<TripType, string>()
        {
            { TripType.RentalCar, "🚗" },
            { TripType.Uber, "🚕" },
            { TripType.Walking, "🚶" },
            { TripType.PublicTransport, "🚌" },
            { TripType.SkipTrip, "🏠" }
        };

        // Order passengers by utility score
        List<Passenger> orderedPassengers = new List<Passenger>(passengers);
        orderedPassengers.Sort((a, b) => a.person.economicParameters.tripUtilityScore.CompareTo(b.person.economicParameters.tripUtilityScore));

        List<Passenger> passengersWhoGotAnUber = new List<Passenger>();
        List<Passenger> passengersWhoRejectedRideOffer = new List<Passenger>();
        List<Passenger> passengersWhoDidNotReceiveRideOffer = new List<Passenger>();
        foreach (Passenger passenger in orderedPassengers)
        {
            TripType tripType = passenger.person.tripTypeChosen;
            if (tripType == TripType.Uber)
            {
                passengersWhoGotAnUber.Add(passenger);
            }
            else if (passenger.person.state == PassengerState.NoRideOffer)
            {
                passengersWhoDidNotReceiveRideOffer.Add(passenger);
            }
            else
            {
                passengersWhoRejectedRideOffer.Add(passenger);
            }
        }


        foreach (Passenger passenger in passengersWhoGotAnUber)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.2f);
            string reaction = "✅";
            Color reactionColor = Color.green;
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, reaction, reactionColor, isBold: false, durationBeforeFade: 10f);
            yield return new WaitForSeconds(0.04f);
        }

        Transform averageUberLineTransform = canvas.transform.Find("Graph/AverageUberLine");
        LineRenderer averageUberLine = averageUberLineTransform.GetComponent<LineRenderer>();

        float averageUberUtilityScore = passengersWhoGotAnUber.Average(p => p.person.economicParameters.tripUtilityScore);
        averageUberLine.SetPosition(0, new Vector3(ConvertUtilityScoreToLinePosition(averageUberUtilityScore), 0.01f, -7.5f));
        averageUberLine.SetPosition(1, new Vector3(ConvertUtilityScoreToLinePosition(averageUberUtilityScore), 0.01f, -2.5f));
        averageUberLineTransform.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);

        foreach (Passenger passenger in passengersWhoRejectedRideOffer)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.2f);
            string reaction = "❎";
            Color reactionColor = Color.red;
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, reaction, reactionColor, isBold: false, durationBeforeFade: 10f);
            yield return new WaitForSeconds(0.05f);
        }

        Transform averageRejectedLineTransform = canvas.transform.Find("Graph/AverageRejectedLine");
        LineRenderer averageRejectedLine = averageRejectedLineTransform.GetComponent<LineRenderer>();

        float averageRejectedUtilityScore = passengersWhoRejectedRideOffer.Average(p => p.person.economicParameters.tripUtilityScore);
        averageRejectedLine.SetPosition(0, new Vector3(ConvertUtilityScoreToLinePosition(averageRejectedUtilityScore), 0.01f, -7.5f));
        averageRejectedLine.SetPosition(1, new Vector3(ConvertUtilityScoreToLinePosition(averageRejectedUtilityScore), 0.01f, -2.5f));
        averageRejectedLineTransform.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);


        foreach (Passenger passenger in passengersWhoDidNotReceiveRideOffer)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.2f);
            string reaction = "📵";
            Color reactionColor = Color.red;
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, reaction, reactionColor, isBold: false, durationBeforeFade: 10f);
            yield return new WaitForSeconds(0.04f);
        }


        Transform averageNoOfferLineTransform = canvas.transform.Find("Graph/AverageNoOfferLine");
        LineRenderer averageNoOfferLine = averageNoOfferLineTransform.GetComponent<LineRenderer>();

        float averageNoOfferUtilityScore = passengersWhoDidNotReceiveRideOffer.Average(p => p.person.economicParameters.tripUtilityScore);
        averageNoOfferLine.SetPosition(0, new Vector3(ConvertUtilityScoreToLinePosition(averageNoOfferUtilityScore), 0.01f, -7.5f));
        averageNoOfferLine.SetPosition(1, new Vector3(ConvertUtilityScoreToLinePosition(averageNoOfferUtilityScore), 0.01f, -2.5f));
        averageNoOfferLineTransform.gameObject.SetActive(true);


        yield return null;
    }

    private IEnumerator ChangeGraphToIncome(Passenger[] passengers)
    {
        foreach (Passenger passenger in passengers)
        {
            StartCoroutine(MovePassengerToIncomeDistribution(passenger));
            yield return new WaitForSeconds(0.01f);
        }
        yield return null;
    }

    private IEnumerator MovePassengerToIncomeDistribution(Passenger passenger)
    {
        float duration = 1;
        Vector3 startPosition = passenger.transform.position;
        float linePosition = ConvertHourlyIncomeToLinePosition(passenger.person.economicParameters.hourlyIncome);
        Vector3 endPosition = new Vector3(linePosition, 0, -6.7f);
        // Check if there's a lineUpPosition within 0.3f of the endPosition
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingIncomeDistributionPosition in incomeDistributionPositions)
            {
                if (Vector3.Distance(existingIncomeDistributionPosition, endPosition) < 0.35f)
                {
                    endPosition = endPosition + new Vector3(0, 0, 0.2f);
                    isEndPositionFree = false;
                }
            }
        }
        incomeDistributionPositions.Add(endPosition);
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float positionFactor = EaseUtils.EaseInOutQuadratic(t);

            passenger.transform.position = Vector3.Lerp(startPosition, endPosition, positionFactor);
            yield return null;
        }
        passenger.transform.position = endPosition;
    }

    private float ConvertHourlyIncomeToLinePosition(float hourlyIncome)
    {
        float linePositionStart = -5f;
        float linePositionStep = 3 / 20f;
        return hourlyIncome * linePositionStep + linePositionStart;
    }


    private float ConvertUtilityScoreToLinePosition(float utilityScore)
    {
        float linePositionStart = -5f;
        float linePositionStep = 3f;
        return utilityScore * linePositionStep + linePositionStart;
    }

}
