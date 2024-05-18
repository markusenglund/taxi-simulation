using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.Linq;
using System;
using TMPro;


public class LineUpDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;

    Random random = new Random(1);

    GameObject canvas;
    LineRenderer[] lineRenderers;

    List<Vector3> lineUpPositions = new List<Vector3>
    { };

    List<Vector3> incomeDistributionPositions = new List<Vector3>
    { };

    Vector3 canvasPosition = new Vector3(4.5f, 0, -6);


    Transform uberAverageLineTransform;
    Transform uberAverageDotTransform;
    Transform uberAverageLabelTransform;


    Transform rejectedAverageLineTransform;
    Transform rejectedAverageDotTransform;
    Transform rejectedAverageLabelTransform;


    Transform noOfferAverageLineTransform;
    Transform noOfferAverageDotTransform;
    Transform noOfferAverageLabelTransform;


    List<Passenger> passengersWhoGotAnUber = new List<Passenger>();
    List<Passenger> passengersWhoRejectedRideOffer = new List<Passenger>();
    List<Passenger> passengersWhoDidNotReceiveRideOffer = new List<Passenger>();

    Color uberColor = ColorScheme.blue;
    Color rejectedColor = ColorScheme.purple;
    Color noOfferColor = ColorScheme.red;

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
        Camera.main.transform.position = new Vector3(-4.5f, 3f, -2f);
        Camera.main.transform.LookAt(cityMiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return new WaitForSeconds(0.5f);
        Passenger[] passengers = city.SpawnSavedPassengers(1);
        Vector3 cameraPosition = new Vector3(0f, 4, -12);
        Quaternion cameraRotation = Quaternion.LookRotation(cityMiddlePosition - cameraPosition, Vector3.up);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(cameraPosition, cameraRotation, 2.5f, Ease.Cubic));
        StartCoroutine(MovePassengersToLineUp(passengers));
        yield return new WaitForSeconds(3f);
        Vector3 newCameraPosition = new Vector3(4.5f, 1, -10);
        Quaternion newCameraRotation = Quaternion.LookRotation(canvasPosition - newCameraPosition, Vector3.up);
        Camera.main.transform.position = newCameraPosition;
        Camera.main.transform.rotation = newCameraRotation;
        StartCoroutine(FadeInCanvas());
        yield return new WaitForSeconds(1f);
        StartCoroutine(CameraUtils.RotateCameraAround(canvasPosition, Vector3.right, 45, 3, Ease.Cubic));
        yield return new WaitForSeconds(1f);
        StartCoroutine(TriggerIdleVariations(passengers));
        yield return new WaitForSeconds(2f);
        StartCoroutine(ShowPassengerResults(passengers));
        yield return new WaitForSeconds(2f);
        // StartCoroutine(FadeInLegend());
        yield return new WaitForSeconds(15f);
        StartCoroutine(ChangeGraphToIncome(passengers));
        yield return new WaitForSeconds(3f);
        Vector3 currentCameraPosition = Camera.main.transform.position;
        StartCoroutine(CameraUtils.MoveCamera(currentCameraPosition + new Vector3(1.5f, 2, -1), 0.5f, Ease.Cubic));
        StartCoroutine(ChangeGraphToBestSubstitute(passengers));
        yield return new WaitForSeconds(3f);
        StartCoroutine(ChangeGraphToSubstitutePriceDividedByIncome(passengers));
        yield return new WaitForSeconds(3f);
        StartCoroutine(ChangeGraphToDistance(passengers));
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

    IEnumerator FadeInLegend(string path)
    {
        Transform legend = canvas.transform.Find(path);
        CanvasGroup legendCanvasGroup = legend.GetComponent<CanvasGroup>();
        float duration = 1.5f;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float alpha = EaseUtils.EaseInCubic(t);
            legendCanvasGroup.alpha = alpha;
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
        float linePosition = ConvertTimePreferenceToLinePosition(passenger.person.economicParameters.timePreference);
        Vector3 endPosition = new Vector3(linePosition, 0, -6.7f);
        // Check if there's a lineUpPosition within 0.3f of the endPosition
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingLineUpPosition in lineUpPositions)
            {
                if (Vector3.Distance(existingLineUpPosition, endPosition) < 0.35f / 4f)
                {
                    endPosition = endPosition + new Vector3(0, 0, 0.05f);
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
        passenger.transform.position = endPosition;
    }

    private IEnumerator ShowPassengerResults(Passenger[] passengers)
    {
        Dictionary<TripType, string> tripTypeToEmoji = new Dictionary<TripType, string>()
        {
            { TripType.Uber, "🚕" },
            { TripType.Walking, "🚶" },
            { TripType.PublicTransport, "🚌" },
        };

        // Order passengers by utility score
        List<Passenger> orderedPassengers = new List<Passenger>(passengers);
        orderedPassengers.Sort((a, b) => a.person.economicParameters.timePreference.CompareTo(b.person.economicParameters.timePreference));

        passengersWhoGotAnUber = new List<Passenger>();
        passengersWhoRejectedRideOffer = new List<Passenger>();
        passengersWhoDidNotReceiveRideOffer = new List<Passenger>();
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


        string uberReactionEmoji = "✔️";
        StartCoroutine(FadeInLegend("Legend/UberLegend"));
        foreach (Passenger passenger in passengersWhoGotAnUber)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.1f);
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, uberReactionEmoji, uberColor, isBold: false, durationBeforeFade: 30f);
            yield return new WaitForSeconds(0.04f);
        }
        // float averageUberTimePreference = passengersWhoGotAnUber.Average(p => p.person.economicParameters.timePreference);
        float medianUberTimePreference = CalculateMedian(passengersWhoGotAnUber.Select(p => p.person.economicParameters.timePreference).ToList());
        Transform averageLineTransform = canvas.transform.Find("Graph/AverageLine");
        Transform averageDotTransform = canvas.transform.Find("Graph/AverageDot");
        Transform averageLabelTransform = canvas.transform.Find("Graph/AverageLabel");

        uberAverageLineTransform = Instantiate(averageLineTransform, canvas.transform);
        uberAverageDotTransform = Instantiate(averageDotTransform, canvas.transform);
        uberAverageLabelTransform = Instantiate(averageLabelTransform, canvas.transform);
        yield return StartCoroutine(SpawnAverageLine(uberAverageLineTransform, uberAverageDotTransform, uberAverageLabelTransform, medianUberTimePreference, uberColor, -4.5f, new Vector3(0.2f, 0, 0), uberReactionEmoji));

        yield return new WaitForSeconds(2f);

        string rejectedReactionEmoji = "✖️";
        StartCoroutine(FadeInLegend("Legend/RejectedLegend"));
        foreach (Passenger passenger in passengersWhoRejectedRideOffer)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.1f);
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, rejectedReactionEmoji, rejectedColor, isBold: false, durationBeforeFade: 30f);
            yield return new WaitForSeconds(0.05f);
        }

        // float averageRejectedTimePreference = passengersWhoRejectedRideOffer.Average(p => p.person.economicParameters.timePreference);
        float medianRejectedTimePreference = CalculateMedian(passengersWhoRejectedRideOffer.Select(p => p.person.economicParameters.timePreference).ToList());
        rejectedAverageLineTransform = Instantiate(averageLineTransform, canvas.transform);
        rejectedAverageDotTransform = Instantiate(averageDotTransform, canvas.transform);
        rejectedAverageLabelTransform = Instantiate(averageLabelTransform, canvas.transform);
        yield return StartCoroutine(SpawnAverageLine(rejectedAverageLineTransform, rejectedAverageDotTransform, rejectedAverageLabelTransform, medianRejectedTimePreference, rejectedColor, -4.5f, new Vector3(-0.2f, 0, 0f), rejectedReactionEmoji));

        yield return new WaitForSeconds(2f);

        StartCoroutine(FadeInLegend("Legend/NoOfferLegend"));
        string noOfferReactionEmoji = "📵";
        foreach (Passenger passenger in passengersWhoDidNotReceiveRideOffer)
        {
            Vector3 reactionPosition = Vector3.up * (passenger.passengerScale * 0.3f + 0.1f);
            AgentOverheadReaction.Create(passenger.transform, reactionPosition, noOfferReactionEmoji, noOfferColor, isBold: true, durationBeforeFade: 30f);
            yield return null;
        }


        // float averageNoOfferTimePreference = passengersWhoDidNotReceiveRideOffer.Average(p => p.person.economicParameters.timePreference);
        float medianNoOfferTimePreference = CalculateMedian(passengersWhoDidNotReceiveRideOffer.Select(p => p.person.economicParameters.timePreference).ToList());
        noOfferAverageLineTransform = Instantiate(averageLineTransform, canvas.transform);
        noOfferAverageDotTransform = Instantiate(averageDotTransform, canvas.transform);
        noOfferAverageLabelTransform = Instantiate(averageLabelTransform, canvas.transform);

        yield return StartCoroutine(SpawnAverageLine(noOfferAverageLineTransform, noOfferAverageDotTransform, noOfferAverageLabelTransform, medianNoOfferTimePreference, noOfferColor, -4.46f, new Vector3(0f, 0, 0.1f), noOfferReactionEmoji));


        yield return null;
    }

    public delegate float SelectPassengerValue(Passenger passenger);


    private IEnumerator ChangeGraphToIncome(Passenger[] passengers)
    {
        Func<Passenger, float> selectPassengerValue = (Passenger passenger) => passenger.person.economicParameters.hourlyIncome;
        StartCoroutine(ChangeGraphAxis(passengers, "Hourly Income", selectPassengerValue, stepSize: 20f, "$"));

        yield return null;
    }

    private IEnumerator ChangeGraphToBestSubstitute(Passenger[] passengers)
    {
        Func<Passenger, float> selectPassengerValue = (Passenger passenger) => passenger.person.economicParameters.GetBestSubstitute().totalCost;
        StartCoroutine(ChangeGraphAxis(passengers, "Max acceptable total cost", selectPassengerValue, stepSize: 80f, "$"));
        yield return null;
    }

    private IEnumerator ChangeGraphToSubstitutePriceDividedByIncome(Passenger[] passengers)
    {
        Func<Passenger, float> selectPassengerValue = (Passenger passenger) => passenger.person.economicParameters.GetBestSubstitute().totalCost / passenger.person.economicParameters.hourlyIncome;
        StartCoroutine(ChangeGraphAxis(passengers, "Max cost / hourly income", selectPassengerValue, stepSize: 2f, ""));
        yield return null;
    }

    private IEnumerator ChangeGraphToDistance(Passenger[] passengers)
    {
        Func<Passenger, float> selectPassengerValue = (Passenger passenger) => passenger.person.distanceToDestination;
        StartCoroutine(ChangeGraphAxis(passengers, "Distance to destination", selectPassengerValue, stepSize: 3f, ""));
        yield return null;
    }


    private IEnumerator ChangeGraphAxis(Passenger[] passengers, string labelText, Func<Passenger, float> selectPassengerValue, float stepSize, string axisLabelPrefix = "")
    {
        Transform mainLabel = canvas.transform.Find("Graph/Label");

        StartCoroutine(ChangeLabel(mainLabel, labelText));
        for (int i = 0; i < 7; i++)
        {
            Transform label = canvas.transform.Find($"Graph/AxisValue{i}");
            StartCoroutine(ChangeLabel(label, $"{axisLabelPrefix}{i * stepSize}"));
        }

        // float uberAverageIncome = passengersWhoGotAnUber.Average(selectPassengerValue);
        float uberMedianValue = CalculateMedian(passengersWhoGotAnUber.Select(selectPassengerValue).ToList());
        // float rejectedAverageIncome = passengersWhoRejectedRideOffer.Average(selectPassengerValue);
        float rejectedMedianValue = CalculateMedian(passengersWhoRejectedRideOffer.Select(selectPassengerValue).ToList());
        // float noOfferAverageIncome = passengersWhoDidNotReceiveRideOffer.Average(selectPassengerValue);
        float noOfferMedianValue = CalculateMedian(passengersWhoDidNotReceiveRideOffer.Select(selectPassengerValue).ToList());

        // Set the average line label of uber to the average of the hourly income
        TMP_Text uberAverageTmp = uberAverageLabelTransform.GetComponent<TMP_Text>();
        string uberAverageNewText = $"{axisLabelPrefix}{uberMedianValue:F2}";
        StartCoroutine(ChangeLabel(uberAverageLabelTransform, uberAverageNewText));

        // Set the average line label of rejected to the average of the hourly income
        TMP_Text rejectedAverageTmp = rejectedAverageLabelTransform.GetComponent<TMP_Text>();
        string rejectedAverageNewText = $"{axisLabelPrefix}{rejectedMedianValue:F2}";
        StartCoroutine(ChangeLabel(rejectedAverageLabelTransform, rejectedAverageNewText));

        // Set the average line label of no offer to the average of the hourly income
        TMP_Text noOfferAverageTmp = noOfferAverageLabelTransform.GetComponent<TMP_Text>();
        string noOfferAverageNewText = $"{axisLabelPrefix}{noOfferMedianValue:F2}";
        StartCoroutine(ChangeLabel(noOfferAverageLabelTransform, noOfferAverageNewText));

        List<Vector3> passengerPositions = new List<Vector3> { };
        foreach (Passenger passenger in passengers)
        {
            StartCoroutine(MovePassengerToNewDistribution(passenger, selectPassengerValue, stepSize, passengerPositions));
            // yield return new WaitForSeconds(0.01f);
        }


        StartCoroutine(MoveAverageLineToNewDistribution(uberAverageLineTransform, uberAverageDotTransform, uberAverageLabelTransform, uberMedianValue, new Vector3(0.3f, 0, 0), stepSize));
        StartCoroutine(MoveAverageLineToNewDistribution(rejectedAverageLineTransform, rejectedAverageDotTransform, rejectedAverageLabelTransform, rejectedMedianValue, new Vector3(-0.3f, 0, 0), stepSize));
        StartCoroutine(MoveAverageLineToNewDistribution(noOfferAverageLineTransform, noOfferAverageDotTransform, noOfferAverageLabelTransform, noOfferMedianValue, new Vector3(0, 0, 0.1f), stepSize));
        yield return null;
    }



    private IEnumerator ChangeLabel(Transform label, string text)
    {
        TMP_Text labelTMP = label.GetComponent<TMP_Text>();
        // Shrink the label to nothing
        float duration = 0.5f;
        Vector3 startScale = labelTMP.transform.localScale;
        Vector3 endScale = Vector3.zero;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scale = EaseUtils.EaseInOutQuadratic(t);
            labelTMP.transform.localScale = Vector3.Lerp(startScale, endScale, scale);
            yield return null;
        }

        labelTMP.text = text;
        // Grow it back
        startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scale = EaseUtils.EaseInOutQuadratic(t);
            labelTMP.transform.localScale = Vector3.Lerp(endScale, startScale, scale);
            yield return null;
        }
    }

    private IEnumerator MovePassengerToNewDistribution(Passenger passenger, Func<Passenger, float> selectPassengerValue, float stepSize, List<Vector3> passengerDistributionPositions)
    {
        float newValue = selectPassengerValue(passenger);
        float duration = 1;
        Vector3 startPosition = passenger.transform.position;
        float linePosition = ConvertValueToLinePosition(newValue, stepSize);
        Vector3 endPosition = new Vector3(linePosition, 0, -6.7f);
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingDistributionPosition in passengerDistributionPositions)
            {
                if (Vector3.Distance(existingDistributionPosition, endPosition) < (0.35f / 4f))
                {
                    endPosition = endPosition + new Vector3(0, 0, 0.2f / 4f);
                    isEndPositionFree = false;
                }
            }
        }
        passengerDistributionPositions.Add(endPosition);
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

    IEnumerator SpawnAverageLine(Transform lineTransform, Transform dotTransform, Transform labelTransform, float averageValue, Color lineColor, float topPosition, Vector3 labelOffset, string emoji)
    {



        LineRenderer averageLine = lineTransform.GetComponent<LineRenderer>();
        LineRenderer averageDot = dotTransform.GetComponent<LineRenderer>();
        CanvasGroup labelCanvasGroup = labelTransform.GetComponent<CanvasGroup>();

        // Get the TMPro text component and set its text to the average time preference
        TMP_Text medianValueText = labelTransform.GetComponent<TMP_Text>();
        TMP_Text medianText = labelTransform.Find("Median").GetComponent<TMP_Text>();

        lineTransform.gameObject.SetActive(true);
        dotTransform.gameObject.SetActive(true);
        labelTransform.gameObject.SetActive(true);
        medianValueText.text = $"{averageValue:F2}";
        medianText.text = $"{emoji}Median:";
        medianText.color = lineColor;
        float lineDuration = 1f;
        float startTime = Time.time;
        while (Time.time < startTime + lineDuration)
        {
            float t = (Time.time - startTime) / lineDuration;
            float positionFactor = EaseUtils.EaseInOutQuadratic(t);
            float linePosition = ConvertTimePreferenceToLinePosition(Mathf.Lerp(0, averageValue, positionFactor));
            averageLine.SetPosition(0, new Vector3(linePosition, 0.01f, -6.8f));
            averageLine.SetPosition(1, new Vector3(linePosition, 0.01f, topPosition));
            averageDot.SetPosition(0, new Vector3(linePosition, 0.01f, topPosition));
            averageDot.SetPosition(1, new Vector3(linePosition, 0.01f, topPosition));
            labelTransform.position = new Vector3(linePosition, 0.01f, topPosition) + labelOffset;
            // Set the alpha of the average uber text
            labelCanvasGroup.alpha = EaseUtils.EaseInCubic(t);
            float alpha = EaseUtils.EaseInCubic(t);
            Color lineRendererColor = new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
            averageLine.startColor = lineRendererColor;
            averageLine.endColor = lineRendererColor;
            averageDot.startColor = lineRendererColor;
            averageDot.endColor = lineRendererColor;
            yield return null;
        }
    }


    private IEnumerator MoveAverageLineToNewDistribution(Transform lineTransform, Transform dotTransform, Transform labelTransform, float averageIncome, Vector3 labelOffset, float stepSize)
    {
        LineRenderer averageLine = lineTransform.GetComponent<LineRenderer>();
        LineRenderer averageDot = dotTransform.GetComponent<LineRenderer>();

        float duration = 1f;
        Vector3 startPosition0 = averageLine.GetPosition(0);
        Vector3 startPosition1 = averageLine.GetPosition(1);
        float endPositionX = ConvertValueToLinePosition(averageIncome, stepSize);
        Vector3 endPosition0 = new Vector3(endPositionX, startPosition0.y, startPosition0.z);
        Vector3 endPosition1 = new Vector3(endPositionX, startPosition1.y, startPosition1.z);
        Vector3 labelStartPosition = labelTransform.position;
        Vector3 labelEndPosition = endPosition1 + labelOffset;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float positionFactor = EaseUtils.EaseInOutQuadratic(t);
            averageLine.SetPosition(0, Vector3.Lerp(startPosition0, endPosition0, positionFactor));
            averageLine.SetPosition(1, Vector3.Lerp(startPosition1, endPosition1, positionFactor));
            averageDot.SetPosition(0, Vector3.Lerp(startPosition1, endPosition1, positionFactor));
            averageDot.SetPosition(1, Vector3.Lerp(startPosition1, endPosition1, positionFactor));
            labelTransform.position = Vector3.Lerp(labelStartPosition, labelEndPosition, positionFactor);
            yield return null;
        }
        averageLine.SetPosition(0, endPosition0);
        averageLine.SetPosition(1, endPosition1);
    }

    private float ConvertValueToLinePosition(float value, float stepSize)
    {
        float linePositionStart = 2f;
        float linePositionStep = 3 / (stepSize * 4f);
        return value * linePositionStep + linePositionStart;
    }


    private float ConvertTimePreferenceToLinePosition(float timePreference)
    {
        float linePositionStart = 2f;
        float linePositionStep = 3 / 4f;
        return timePreference * linePositionStep + linePositionStart;
    }

    private float CalculateMedian(List<float> values)
    {
        List<float> sortedList = values.OrderBy(x => x).ToList();
        int count = sortedList.Count;
        if (count % 2 == 0)
        {
            return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2;
        }
        else
        {
            return sortedList[count / 2];
        }
    }

}
