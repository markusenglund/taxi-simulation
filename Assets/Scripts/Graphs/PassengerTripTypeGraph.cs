using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;


public class PassengerTripTypeGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;



    LineRenderer uberLine;
    LineRenderer uberLineDot;
    LineRenderer substituteLine;
    LineRenderer substituteLineDot;

    List<LineRenderer> separatorLines = new List<LineRenderer>();

    int startHour = 18;
    Vector2 graphSize = new Vector2(1200, 800);
    Vector3 graphPosition = new Vector3(3100, 600);
    float margin = 100f;
    float marginTop = 220f;
    float marginBottom = 140f;
    float maxY = 60f;
    float minY = 0f;
    float maxX;
    float minX = 0f;

    string headingText = "Passenger decision";

    City city;

    Color substituteLineColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    Color uberLineColor = new Color(0.2f, 0.9f, 1.0f, 1.0f);

    Color separatorColor = new Color(192 / 255f, 192 / 255f, 192 / 255f, 0.1f);


    LineRenderer uberLegendLine;
    LineRenderer substituteLegendLine;
    LineRenderer xLineRenderer;
    LineRenderer yLineRenderer;

    Color axisColor = new Color(192f / 255f, 192f / 255f, 192f / 255f, 1f);

    CanvasGroup canvasGroup;

    TMP_Text substituteNumberText;

    float defaultLineWidth;


    public static PassengerTripTypeGraph Create(City city, PassengerSpawnGraphMode mode)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform graphPrefab = Resources.Load<Transform>("Graphs/PassengerTripTypeGraph");
        Transform graphTransform = Instantiate(graphPrefab, canvas);

        PassengerTripTypeGraph graph = graphTransform.GetComponent<PassengerTripTypeGraph>();
        graph.maxX = city.simulationSettings.simulationLengthHours;
        graph.city = city;
        return graph;
    }

    private void Start()
    {
        // Set size delta of the graph (not the container, the graph itself)
        RectTransform graphRectTransform = transform.GetComponent<RectTransform>();
        graphRectTransform.sizeDelta = graphSize;
        graphRectTransform.anchoredPosition = graphPosition;

        // Add canvas group to graph
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        graphContainer.sizeDelta = graphSize; //new Vector2(graphSize.x - 2 * margin, graphSize.y - 2 * margin);
        defaultLineWidth = lrPrefab.widthMultiplier;
        InstantiateGraph();



        StartCoroutine(Schedule());

    }

    private void Update()
    {
        // Scale all line renderers to camera.main.fov / 60
        float scale = defaultLineWidth * Camera.main.fieldOfView / 60;
        foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
        {
            line.widthMultiplier = scale;
        }
    }

    private IEnumerator Schedule()
    {
        StartCoroutine(SpawnCard(duration: 1));
        yield return new WaitForSeconds(1);
        StartCoroutine(UpdateCurves());
        yield return null;
    }

    private IEnumerator SpawnCard(float duration)
    {
        float startTime = Time.time;
        float startAlpha = 0;
        float finalAlpha = 1;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float percentage = EaseUtils.EaseInQuadratic(t);
            float alpha = Mathf.Lerp(startAlpha, finalAlpha, percentage);
            canvasGroup.alpha = alpha;
            Color uberLineColor = new Color(uberLegendLine.startColor.r, uberLegendLine.startColor.g, uberLegendLine.startColor.b, alpha);
            uberLegendLine.startColor = uberLineColor;
            uberLegendLine.endColor = uberLineColor;

            Color axisLineColor = new Color(axisColor.r, axisColor.g, axisColor.b, alpha);
            xLineRenderer.startColor = axisLineColor;
            xLineRenderer.endColor = axisLineColor;
            yLineRenderer.startColor = axisLineColor;
            yLineRenderer.endColor = axisLineColor;

            Color separatorLineColor = new Color(separatorColor.r, separatorColor.g, separatorColor.b, separatorColor.a * t);
            foreach (LineRenderer line in separatorLines)
            {
                line.startColor = separatorLineColor;
                line.endColor = separatorLineColor;
            }

            yield return null;
        }
    }

    private IEnumerator CreatePassengerCurve(float duration)
    {
        float startTime = Time.time;

        // float previousTime = Time.time;
        float numPositions = 200;
        Vector2 firstGraphPosition = ConvertValueToGraphPosition(new Vector2(0, city.GetNumExpectedPassengersPerHour(0)));

        uberLine.positionCount = 1;
        uberLine.SetPosition(0, new Vector3(firstGraphPosition.x, firstGraphPosition.y, 0));
        float i = 0;
        uberLineDot.positionCount = 2;

        while (Time.time - startTime < duration)
        {
            // float time = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            float t = (Time.time - startTime) / duration;
            float graphTime = Mathf.Lerp(0, city.simulationSettings.simulationLengthHours, t);
            float passengersPerHour = city.GetNumExpectedPassengersPerHour(graphTime);
            Vector2 graphPosition = ConvertValueToGraphPosition(new Vector2(graphTime, passengersPerHour));
            Vector3 passengersPosition = new Vector3(graphPosition.x, graphPosition.y, 0);
            if (t * numPositions >= i)
            {
                uberLine.positionCount++;
                i++;
            }
            uberLine.SetPosition(uberLine.positionCount - 1, passengersPosition);
            uberLineDot.SetPosition(0, passengersPosition);
            uberLineDot.SetPosition(1, passengersPosition);
            yield return null;
        }
    }

    private IEnumerator UpdateCurves()
    {
        float numPositions = 200;
        float i = 0;
        float timeResolution = 0.5f;
        substituteLineDot.positionCount = 2;
        uberLineDot.positionCount = 2;

        Queue<(float value, float time)> lastSubstituteValues = new Queue<(float value, float time)>();
        Queue<(float value, float time)> lastUberValues = new Queue<(float value, float time)>();
        Queue<(float value, float time)> lastSkippedTripValues = new Queue<(float value, float time)>();
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        Vector2 zeroGraphPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        while (simulationTime < city.simulationSettings.simulationLengthHours)
        {
            simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            float t = simulationTime / city.simulationSettings.simulationLengthHours;
            // int numPassengersSpawnedPerTimeResolutionInterval = city.CalculateNumPassengersSpawnedInLastInterval(timeResolution);
            PassengerPerson[] passengers = city.GetPassengersSpawnedInLastInterval(timeResolution);
            // Count the number of passengers that chose uber, a substitute and those who chose to stay at home
            // float numSubstitutePassengers = passengers.Count(x => x.tripTypeChosen == TripType.Walking || x.tripTypeChosen == TripType.PublicTransport || x.tripTypeChosen == TripType.RentalCar);
            int numSubstitutePassengers = 0;
            int numUberPassengers = 0;
            int numSkippedTrip = 0;

            foreach (PassengerPerson passenger in passengers)
            {
                if (passenger.tripTypeChosen == TripType.Uber)
                {
                    numUberPassengers++;
                }
                else if (passenger.tripTypeChosen == TripType.Walking || passenger.tripTypeChosen == TripType.PublicTransport || passenger.tripTypeChosen == TripType.RentalCar)
                {
                    numSubstitutePassengers++;
                }
                else if (passenger.tripTypeChosen == TripType.SkipTrip)
                {
                    numSkippedTrip++;
                }
            }


            float numSubstitutePassengersPerHour = numSubstitutePassengers * (1 / timeResolution);
            lastSubstituteValues.Enqueue((numSubstitutePassengersPerHour, simulationTime));
            if (t * numPositions >= i)
            {
                substituteLine.positionCount++;
                i++;
            }
            if (lastSubstituteValues.Count > 100)
            {
                lastSubstituteValues.Dequeue();
            }
            float smoothedNumPassengersPerHour = lastSubstituteValues.Average(x => x.value);
            float timeInterval = lastSubstituteValues.Last().time - lastSubstituteValues.First().time;
            // Offset the time axis to the average time of the values that were used to calculate the spawn rate
            float graphTime = simulationTime - timeResolution / 2 - timeInterval / 2;
            Vector3 passengersPosition = ConvertValueToGraphPosition(new Vector3(graphTime, smoothedNumPassengersPerHour, 0));
            substituteLine.SetPosition(substituteLine.positionCount - 1, passengersPosition);
            substituteLineDot.SetPosition(0, passengersPosition);
            substituteLineDot.SetPosition(1, passengersPosition);
            Vector3 passengersTextPosition = new Vector3(passengersPosition.x + 18, Mathf.Max(passengersPosition.y - 5, zeroGraphPosition.y + 15), 0);
            substituteNumberText.rectTransform.anchoredPosition = passengersTextPosition;
            substituteNumberText.text = smoothedNumPassengersPerHour.ToString("n0");

            // TODO - START HERE, fix the weird curve behavior
            // Uber passengers
            float numUberPassengersPerHour = numSubstitutePassengers * (1 / timeResolution);
            lastUberValues.Enqueue((numUberPassengersPerHour, simulationTime));
            if (t * numPositions >= i)
            {
                uberLine.positionCount++;
                i++;
            }
            if (lastUberValues.Count > 100)
            {
                lastUberValues.Dequeue();
            }
            float smoothedNumUberPassengersPerHour = lastUberValues.Average(x => x.value);
            float uberTimeInterval = lastUberValues.Last().time - lastUberValues.First().time;
            // Offset the time axis to the average time of the values that were used to calculate the spawn rate
            float uberGraphTime = simulationTime - timeResolution / 2 - timeInterval / 2;
            Vector3 uberLinePosition = ConvertValueToGraphPosition(new Vector3(graphTime, smoothedNumUberPassengersPerHour, 0));
            uberLine.SetPosition(uberLine.positionCount - 1, uberLinePosition);
            uberLineDot.SetPosition(0, uberLinePosition);
            uberLineDot.SetPosition(1, uberLinePosition);
            // Vector3 passengersTextPosition = new Vector3(uberLinePosition.x + 18, Mathf.Max(uberLinePosition.y - 5, zeroGraphPosition.y + 15), 0);
            // substituteNumberText.rectTransform.anchoredPosition = passengersTextPosition;
            // substituteNumberText.text = smoothedNumUberPassengersPerHour.ToString("n0");
            yield return null;
        }
    }

    private void CreateAxes()
    {
        AnimationCurve axisLineWidthCurve = AnimationCurve.Constant(0, 1, 2);
        // Create x axis with the line renderer
        xLineRenderer = Instantiate(lrPrefab, graphContainer);
        xLineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxX, 0));
        xLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        xLineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));
        xLineRenderer.widthCurve = axisLineWidthCurve;

        // Create y axis with the line renderer
        yLineRenderer = Instantiate(lrPrefab, graphContainer);
        yLineRenderer.positionCount = 2;
        Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxY));
        yLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        yLineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));
        yLineRenderer.widthCurve = axisLineWidthCurve;

        xLineRenderer.sortingOrder = 2;
        yLineRenderer.sortingOrder = 2;
        // Set end cap vertices to one
        xLineRenderer.numCapVertices = 1;
        yLineRenderer.numCapVertices = 1;



    }

    private void CreateAxisValues()
    {
        // Create y axis values
        int step = 10;
        for (int i = (int)minY; i <= maxY; i += step)
        {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
            text.rectTransform.sizeDelta = new Vector2(200, 30);
            text.fontSize = 42;

        }

        // Create y axis separator lines
        for (int i = (int)minY + step; i <= maxY; i += step)
        {
            LineRenderer line = Instantiate(lrPrefab, graphContainer);
            line.startColor = separatorColor;
            line.endColor = separatorColor;
            line.widthCurve = AnimationCurve.Constant(0, 1, 0.3f);
            line.positionCount = 2;
            Vector2 linePosition1 = ConvertValueToGraphPosition(new Vector2(0, i));
            Vector2 linePosition2 = ConvertValueToGraphPosition(new Vector2(maxX, i));
            line.SetPosition(0, new Vector3(linePosition1.x, linePosition1.y, 0));
            line.SetPosition(1, new Vector3(linePosition2.x, linePosition2.y, 0));
            line.sortingOrder = 1;

            separatorLines.Add(line);
        }

        // Create x axis values
        step = Mathf.RoundToInt((maxX - minX) / 6f);
        for (int i = (int)minX; i <= maxX; i += step)
        {
            int hour = i + startHour;
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(i, 0));
            // Set pivot to top center
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Set textmeshpro text alignment to center
            text.alignment = TextAlignmentOptions.Center;
            text.text = TimeUtils.ConvertSimulationHoursToTimeString(hour);
            text.rectTransform.anchoredPosition = textPosition;
            text.rectTransform.sizeDelta = new Vector2(200, 80);
            text.fontSize = 42;
        }
    }

    private void CreateHeaderText()
    {
        TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
        RectTransform textRectTransform = text.rectTransform;
        textRectTransform.sizeDelta = graphSize;
        text.fontSize = 90;
        Vector2 textPosition = new Vector2(0f, graphSize.y / 2 - 80f);
        text.text = headingText;

        text.rectTransform.anchoredPosition = textPosition;
    }

    private void CreateLegends()
    {
        uberLegendLine = Instantiate(lrPrefab);
        substituteLegendLine = Instantiate(lrPrefab);


        CreateLegend(x: 0, uberLegendLine, uberLineColor, "Uber passengers");
        CreateLegend(x: 540, substituteLegendLine, substituteLineColor, "Substitute passengers");

    }

    private void CreateLegend(float x, LineRenderer legendDot, Color color, string text)
    {


        // Create a tiny green line with the line renderer
        // Create empty legend game object inside the graph container with scale 1
        GameObject legend = new GameObject("Legend", typeof(RectTransform));
        legendDot.transform.SetParent(legend.transform, false);
        // Add canvasgroup to legend
        legend.transform.SetParent(graphContainer);
        legend.transform.localScale = new Vector3(1, 1, 1);
        legend.transform.localRotation = Quaternion.identity;
        // Set pivot to bottom left
        RectTransform legendRectTransform = legend.GetComponent<RectTransform>();
        legendRectTransform.pivot = new Vector2(0, 0);
        legendRectTransform.anchorMin = new Vector2(0, 0);
        legendRectTransform.anchorMax = new Vector2(0, 0);

        // Set size delta of the legend
        float legendHeight = 42;
        float legendWidth = 600;
        float legendMargin = 20;
        legend.transform.localPosition = new Vector3(-legendWidth + margin + x, graphSize.y / 2 - marginTop + legendMargin, 0);
        legendRectTransform.sizeDelta = new Vector2(legendWidth, legendHeight);

        legendDot.positionCount = 2;
        // Offset with 6 px to get it properly centered
        Vector2 passengerLegendLinePosition = new Vector3(0, legendHeight / 2 - 6, 0);
        legendDot.SetPosition(0, passengerLegendLinePosition);
        legendDot.SetPosition(1, passengerLegendLinePosition);
        legendDot.startColor = color;
        legendDot.endColor = color;
        legendDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        legendDot.sortingOrder = 1;

        TMP_Text legendText = Instantiate(legendTextPrefab, legend.transform);
        Vector2 textPosition = new Vector2(20, 0);
        legendText.text = text;
        legendText.rectTransform.anchoredPosition = textPosition;
        legendText.rectTransform.sizeDelta = new Vector2(legendWidth, legendHeight);
        legendText.fontSize = legendHeight;
        legendText.color = color;

    }

    private void InstantiateLines()
    {
        uberLine = Instantiate(lrPrefab, graphContainer);
        uberLine.positionCount = 0;
        uberLine.startColor = uberLineColor;
        uberLine.endColor = uberLineColor;
        uberLine.sortingOrder = 1;
        uberLine.numCornerVertices = 1;
        uberLine.widthCurve = AnimationCurve.Constant(0, 1, 1.2f);

        substituteLine = Instantiate(lrPrefab, graphContainer);
        substituteLine.positionCount = 0;
        substituteLine.startColor = substituteLineColor;
        substituteLine.endColor = substituteLineColor;
        substituteLine.sortingOrder = 2;
        substituteLine.numCornerVertices = 1;
        substituteLine.widthCurve = AnimationCurve.Constant(0, 1, 1.5f);

        substituteLineDot = Instantiate(lrPrefab, graphContainer);
        substituteLineDot.positionCount = 0;
        substituteLineDot.startColor = substituteLineColor;
        substituteLineDot.endColor = substituteLineColor;
        substituteLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        substituteLineDot.sortingOrder = 3;

        uberLineDot = Instantiate(lrPrefab, graphContainer);
        uberLineDot.positionCount = 0;
        uberLineDot.startColor = uberLineColor;
        uberLineDot.endColor = uberLineColor;
        uberLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        uberLineDot.sortingOrder = 3;


        substituteNumberText = Instantiate(legendTextPrefab, graphContainer);
        substituteNumberText.rectTransform.pivot = new Vector2(0, 0);
        substituteNumberText.rectTransform.anchorMin = new Vector2(0, 0);
        substituteNumberText.rectTransform.anchorMax = new Vector2(0, 0);
        substituteNumberText.text = "";
        substituteNumberText.rectTransform.sizeDelta = new Vector2(300, 30);
        substituteNumberText.fontSize = 42;
        substituteNumberText.color = substituteLineColor;
        substituteNumberText.fontStyle = FontStyles.Bold;

    }

    private void InstantiateGraph()
    {
        InstantiateLines();
        CreateAxes();
        CreateAxisValues();
        CreateHeaderText();
        CreateLegends();
    }


    private Vector2 ConvertValueToGraphPosition(Vector2 vector)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;

        float y = Mathf.Lerp(marginBottom, graphHeight - marginTop, (vector.y - minY) / (maxY - minY));
        float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minX) / (maxX - minX));

        return new Vector2(x, y);
    }
}

