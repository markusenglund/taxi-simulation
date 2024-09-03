using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.Linq;
using System;
using TMPro;
using Unity.VisualScripting;


public class DancingSequenceDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    PassengerStats focusPassengerStats;
    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;

    Random random = new Random(1);

    GameObject canvas;
    LineRenderer[] lineRenderers;

    List<Vector3> passengerPositions = new List<Vector3>
    { };

    List<Vector3> incomeDistributionPositions = new List<Vector3>
    { };

    Vector3 canvasPosition = new Vector3(4.5f, 0, -6);


    Transform medianLineTransform;
    Transform medianDotTransform;
    Transform medianLabelTransform;


    Color uberColor = ColorScheme.yellow;

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        Time.captureFramerate = 60;

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
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Vector3 position1 = new Vector3(0, 0.4f, -0.1f);
        Quaternion rotation1 = Quaternion.Euler(18f, 49f, 0);

        Vector3 position2 = new Vector3(1.2f, 0.38f, -0.25f);
        Quaternion rotation2 = Quaternion.Euler(11, -9.6f, 0);

        Vector3 position3 = new Vector3(-0.43f, 0.35f, 6);
        Quaternion rotation3 = Quaternion.Euler(9, 71, 0);

        Vector3 position4 = new Vector3(4.5f, 11.3f, 4.5f);
        Quaternion rotation4 = Quaternion.Euler(90, 90, 0);

        Vector3 position5 = new Vector3(4.5f, 200, 4.5f);

        Camera.main.transform.position = position1;
        Camera.main.transform.rotation = rotation1;

        yield return new WaitForSeconds(0.01f);
        // Get all passenger who don't have state BeforeSpawn or Idling
        Passenger[] passengers = city.SpawnSavedPassengers("020").Where(p => p.person.state != PassengerState.BeforeSpawn && p.person.state != PassengerState.Idling).ToArray();
        RemovePassengerCollisions(passengers);

        Passenger focusPassenger = passengers.FirstOrDefault(p => p.person.id == 55);
        // MovePassengersToLineUp(passengers);
        StartDancing(passengers);

        if (focusPassenger != null)
        {
            StartCoroutine(SpawnFocusPassengerStats(focusPassenger));
        }
        else
        {
            Debug.Log("Passenger with id 55 not found.");
        }
        Vector3 twerkingPassengerPosition = new Vector3(0.33f, 0.075f, 0.23f);
        StartCoroutine(CameraUtils.RotateCameraAround(twerkingPassengerPosition, Vector3.up, -40, 8, Ease.Linear));
        yield return new WaitForSeconds(7.4f);
        StartCoroutine(CameraUtils.MoveCamera(position2, 0.6f, Ease.Cubic));
        yield return new WaitForSeconds(0.6f);
        Vector3 sadPassengerPosition = new Vector3(1.33f, 0.075f, 0.23f);
        StartCoroutine(CameraUtils.RotateCameraAround(sadPassengerPosition, Vector3.up, -15, 4, Ease.QuadraticOut));
        yield return new WaitForSeconds(4f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(position3, rotation3, 1.5f, Ease.Cubic));
        Vector3 evanPosition = focusPassenger.transform.position;
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(CameraUtils.RotateCameraAround(evanPosition, Vector3.up, 25, 5, Ease.Linear));
        yield return new WaitForSeconds(5f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(position4, rotation4, 4f, Ease.Linear));
        yield return new WaitForSeconds(4f);
        StartCoroutine(CameraUtils.MoveCamera(position5, 20f, Ease.Linear));
        yield return new WaitForSeconds(10f);
        // Stop playing
        UnityEditor.EditorApplication.isPlaying = false;
    }

    void StartDancing(Passenger[] passengers)
    {
        int i = 1;
        foreach (Passenger passenger in passengers)
        {
            Vector3 pointAbovePassengersHead = new Vector3(0, 0.36f, 0);
            // Vector3 awayFromCamera = (passenger.transform.position - Camera.main.transform.position).normalized * 0.05f;
            // awayFromCamera.y = 0;
            Vector3 reactionPosition = pointAbovePassengersHead;

            Animator passengerAnimator = passenger.GetComponentInChildren<Animator>();
            if (passenger.person.trip != null)
            {
                if (passenger.person.id == 65)
                {
                    passengerAnimator.SetTrigger("Celebrate5");
                }
                else if (passenger.person.id == 55)
                {
                    passengerAnimator.SetTrigger("Celebrate2");
                }
                else
                {
                    passengerAnimator.SetTrigger("Celebrate" + i);
                }
                string reaction = "🙂";
                AgentOverheadReaction.Create(passenger.transform.GetChild(0).transform, reactionPosition, reaction, ColorScheme.green, isBold: true, durationBeforeFade: 100f);
            }
            else
            {
                string reaction = "🙁";
                AgentOverheadReaction.Create(passenger.transform, reactionPosition, reaction, ColorScheme.surgeRed, isBold: true, durationBeforeFade: 100f);
                passengerAnimator.SetTrigger("Cry");
            }
            i++;
            if (i > 7)
            {
                i = 1;
            }
        }
    }

    IEnumerator SpawnFocusPassengerStats(Passenger passenger)
    {
        Transform passengerStatsPrefab = Resources.Load<Transform>("PassengerStatsCanvas");
        Vector3 statsPosition = new Vector3(-0.24f, 0.19f, -0.02f);
        Quaternion rotation = Quaternion.Euler(0, 5, 0);

        focusPassengerStats = PassengerStats.Create(passengerStatsPrefab, passenger.transform, statsPosition, rotation, passenger.person);
        yield return null;
    }

    IEnumerator FadeInCanvas()
    {
        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        float duration = 0.1f;
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

    void RemovePassengerCollisions(Passenger[] passengers)
    {
        foreach (Passenger passenger in passengers)
        {
            RemovePassengerCollision(passenger);
        }
    }

    void RemovePassengerCollision(Passenger passenger)
    {
        Vector3 position = passenger.transform.position;
        // Check if there's a lineUpPosition within 0.3f of the endPosition
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingPosition in passengerPositions)
            {
                if (Vector3.Distance(existingPosition, position) < 0.55f / 4f)
                {
                    position = position + new Vector3(0, 0, 0.05f);
                    isEndPositionFree = false;
                }
            }
        }
        passengerPositions.Add(position);

        passenger.transform.position = position;
    }

    private IEnumerator ShowPassengerResults(Passenger[] passengers)
    {
        // Order passengers by time sensitivity
        List<Passenger> orderedPassengers = new List<Passenger>(passengers);
        orderedPassengers.Sort((a, b) => a.person.economicParameters.timePreference.CompareTo(b.person.economicParameters.timePreference));

        float medianTimeSensitivity = StatisticsUtils.CalculateMedian(passengers.Select(p => p.person.economicParameters.timePreference).ToList());
        Transform averageLineTransformBase = canvas.transform.Find("Graph/AverageLine");
        Transform averageDotTransformBase = canvas.transform.Find("Graph/AverageDot");
        Transform averageLabelTransformBase = canvas.transform.Find("Graph/AverageLabel");

        medianLineTransform = Instantiate(averageLineTransformBase, canvas.transform);
        medianDotTransform = Instantiate(averageDotTransformBase, canvas.transform);
        medianLabelTransform = Instantiate(averageLabelTransformBase, canvas.transform);
        yield return StartCoroutine(SpawnAverageLine(medianLineTransform, medianDotTransform, medianLabelTransform, medianTimeSensitivity, uberColor, -4.5f, new Vector3(0.2f, 0, 0)));
    }

    public delegate float SelectPassengerValue(Passenger passenger);


    private IEnumerator ChangeGraphToIncome(Passenger[] passengers)
    {
        Func<Passenger, float> selectPassengerValue = (Passenger passenger) => passenger.person.economicParameters.hourlyIncome;
        StartCoroutine(ChangeGraphAxis(passengers, "Hourly Income", selectPassengerValue, stepSize: 10f, "$"));

        yield return null;
    }


    private IEnumerator ChangeGraphAxis(Passenger[] passengers, string labelText, Func<Passenger, float> selectPassengerValue, float stepSize, string axisLabelPrefix = "", string axisLabelSuffix = "")
    {
        Transform mainLabel = canvas.transform.Find("Graph/Label");

        StartCoroutine(ChangeLabel(mainLabel, labelText));
        for (int i = 0; i < 7; i++)
        {
            Transform label = canvas.transform.Find($"Graph/AxisValue{i}");
            StartCoroutine(ChangeLabel(label, $"{axisLabelPrefix}{i * stepSize}{axisLabelSuffix}"));
        }


        // Get the median value of the passengers
        float medianValue = StatisticsUtils.CalculateMedian(passengers.Select(selectPassengerValue).ToList());

        // Set the average line label of uber to the average of the hourly income
        TMP_Text medianTmp = medianLabelTransform.GetComponent<TMP_Text>();
        string medianNewText = $"{axisLabelPrefix}{medianValue:F2}";
        StartCoroutine(ChangeLabel(medianLabelTransform, medianNewText));

        List<Vector3> passengerPositions = new List<Vector3> { };
        foreach (Passenger passenger in passengers)
        {
            StartCoroutine(MovePassengerToNewDistribution(passenger, selectPassengerValue, stepSize, passengerPositions));
            // yield return new WaitForSeconds(0.01f);
        }

        StartCoroutine(MoveAverageLineToNewDistribution(medianLineTransform, medianDotTransform, medianLabelTransform, medianValue, new Vector3(0.3f, 0, 0), stepSize));
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

    IEnumerator SpawnAverageLine(Transform lineTransform, Transform dotTransform, Transform labelTransform, float averageValue, Color lineColor, float topPosition, Vector3 labelOffset)
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
        medianText.text = $"Median:";
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
        float linePositionStep = 6f / 4f;
        return timePreference * linePositionStep + linePositionStart;
    }



}
