using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;


public enum PassengerSpawnGraphMode
{
    FirstSim,
    Regular
}

public class PredictedSupplyDemandGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;



    LineRenderer passengersLine;
    LineRenderer predictedPassengersLineDot;
    LineRenderer actualPassengersLine;
    LineRenderer actualPassengersLineDot;

    List<LineRenderer> separatorLines = new List<LineRenderer>();

    int startHour = 18;
    Vector2 graphSize = new Vector2(1200, 800);
    Vector3 graphPosition = new Vector3(3100, 1620);
    float margin = 100f;
    float marginTop = 220f;
    float marginBottom = 140f;
    float maxY = 60f;
    float minY = 0f;
    float maxX;
    float minX = 0f;

    string headingText = "Passenger spawn rate";

    City city;

    PassengerSpawnGraphMode mode;

    Color actualPassengersLineColor = ColorScheme.blue;
    Color predictedPassengersLineColor = ColorScheme.orange;

    Color separatorColor = new Color(96 / 255f, 96 / 255f, 96 / 255f, 1f);


    LineRenderer passengersLegendLine;
    LineRenderer actualPassengersLegendLine;
    LineRenderer xLineRenderer;
    LineRenderer yLineRenderer;

    Color axisColor = new Color(192f / 255f, 192f / 255f, 192f / 255f, 1f);

    CanvasGroup canvasGroup;

    TMP_Text actualPassengersNumberText;

    float defaultLineWidth;


    public static PredictedSupplyDemandGraph Create(City city, PassengerSpawnGraphMode mode)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform graphPrefab = Resources.Load<Transform>("Graphs/PredictedSupplyDemandGraph");
        Transform graphTransform = Instantiate(graphPrefab, canvas);

        PredictedSupplyDemandGraph graph = graphTransform.GetComponent<PredictedSupplyDemandGraph>();
        graph.maxX = city.simulationSettings.simulationLengthHours;
        graph.city = city;
        graph.mode = mode;
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


        if (mode == PassengerSpawnGraphMode.FirstSim)
        {
            StartCoroutine(FirstSimSchedule());
        }
        else
        {
            StartCoroutine(SimSchedule());
        }
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

    private IEnumerator FirstSimSchedule()
    {
        StartCoroutine(SpawnCard(1f));
        yield return new WaitForSeconds(1);
        StartCoroutine(CreatePassengerCurve(duration: 6));
        yield return new WaitForSeconds(27);
        InstantiateActualPassengerLine();
        StartCoroutine(UpdateActualPassengerCurve());
    }

    private IEnumerator SimSchedule()
    {
        StartCoroutine(CreatePassengerCurve(duration: 0));
        StartCoroutine(UpdateActualPassengerCurve());
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
            Color predictedPassengersLineColor = new Color(passengersLegendLine.startColor.r, passengersLegendLine.startColor.g, passengersLegendLine.startColor.b, alpha);
            passengersLegendLine.startColor = predictedPassengersLineColor;
            passengersLegendLine.endColor = predictedPassengersLineColor;

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
        foreach (LineRenderer line in separatorLines)
        {
            line.startColor = separatorColor;
            line.endColor = separatorColor;
        }
        canvasGroup.alpha = finalAlpha;
    }

    private IEnumerator CreatePassengerCurve(float duration)
    {
        float startTime = Time.time;

        // float previousTime = Time.time;
        float numPositions = 200;
        Vector2 firstGraphPosition = ConvertValueToGraphPosition(new Vector2(0, city.GetNumExpectedPassengersPerHour(0)));

        passengersLine.positionCount = 1;
        passengersLine.SetPosition(0, new Vector3(firstGraphPosition.x, firstGraphPosition.y, 0));
        float i = 0;
        if (duration > 0)
        {
            predictedPassengersLineDot.positionCount = 2;

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
                    passengersLine.positionCount++;
                    i++;
                }
                passengersLine.SetPosition(passengersLine.positionCount - 1, passengersPosition);
                predictedPassengersLineDot.SetPosition(0, passengersPosition);
                predictedPassengersLineDot.SetPosition(1, passengersPosition);
                yield return null;
            }
        }
        else
        {
            while (i < numPositions)
            {
                float t = (float)i / numPositions;
                float graphTime = Mathf.Lerp(0, city.simulationSettings.simulationLengthHours, t);
                float passengersPerHour = city.GetNumExpectedPassengersPerHour(graphTime);
                Vector2 graphPosition = ConvertValueToGraphPosition(new Vector2(graphTime, passengersPerHour));
                passengersLine.positionCount++;
                passengersLine.SetPosition(passengersLine.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
                i++;
            }
            yield return null;
        }
    }

    private IEnumerator UpdateActualPassengerCurve()
    {
        float numPositions = 200;
        float i = 0;
        float timeResolution = 0.5f;
        Queue<(float value, float time)> lastFewValues = new Queue<(float value, float time)>();
        float simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        Vector2 zeroGraphPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        while (simulationTime < city.simulationSettings.simulationLengthHours)
        {
            simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
            float t = simulationTime / city.simulationSettings.simulationLengthHours;
            int numPassengersSpawnedPerTimeResolutionInterval = city.CalculateNumPassengersSpawnedInLastInterval(timeResolution);
            float numPassengersPerHour = numPassengersSpawnedPerTimeResolutionInterval * (1 / timeResolution);
            lastFewValues.Enqueue((numPassengersPerHour, simulationTime));
            if (t * numPositions >= i)
            {
                actualPassengersLine.positionCount++;
                i++;
            }
            if (lastFewValues.Count > 100)
            {
                lastFewValues.Dequeue();
            }
            float smoothedNumPassengersPerHour = lastFewValues.Average(x => x.value);
            float timeInterval = lastFewValues.Last().time - lastFewValues.First().time;
            // Offset the time axis to the average time of the values that were used to calculate the spawn rate
            float graphTime = simulationTime - timeResolution / 2 - timeInterval / 2;
            Vector3 passengersPosition = ConvertValueToGraphPosition(new Vector3(graphTime, smoothedNumPassengersPerHour, 0));
            actualPassengersLine.SetPosition(actualPassengersLine.positionCount - 1, passengersPosition);
            actualPassengersLineDot.SetPosition(0, passengersPosition);
            actualPassengersLineDot.SetPosition(1, passengersPosition);
            Vector3 passengersTextPosition = new Vector3(passengersPosition.x + 18, Mathf.Max(passengersPosition.y - 5, zeroGraphPosition.y + 15), 0);
            actualPassengersNumberText.rectTransform.anchoredPosition = passengersTextPosition;
            actualPassengersNumberText.text = smoothedNumPassengersPerHour.ToString("n0");
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
        passengersLegendLine = Instantiate(lrPrefab);
        actualPassengersLegendLine = Instantiate(lrPrefab);

        CreateLegend(x: 30, passengersLegendLine, predictedPassengersLineColor, "Predicted passengers/hr");
        CreateLegend(x: 570, actualPassengersLegendLine, actualPassengersLineColor, "Actual passengers/hr");
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

        // Set text to lower left corner by default by setting anchor min and max to 0, 0


    }

    private void InstantiateLines()
    {
        passengersLine = Instantiate(lrPrefab, graphContainer);
        passengersLine.positionCount = 0;
        passengersLine.startColor = predictedPassengersLineColor;
        passengersLine.endColor = predictedPassengersLineColor;
        passengersLine.sortingOrder = 1;
        passengersLine.numCornerVertices = 1;
        passengersLine.widthCurve = AnimationCurve.Constant(0, 1, 1.2f);

        if (mode == PassengerSpawnGraphMode.FirstSim)
        {
            predictedPassengersLineDot = Instantiate(lrPrefab, graphContainer);
            predictedPassengersLineDot.positionCount = 0;
            predictedPassengersLineDot.startColor = predictedPassengersLineColor;
            predictedPassengersLineDot.endColor = predictedPassengersLineColor;
            predictedPassengersLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
            predictedPassengersLineDot.sortingOrder = 3;
        }

        if (mode == PassengerSpawnGraphMode.Regular)
        {
            InstantiateActualPassengerLine();
        }

    }

    private void InstantiateActualPassengerLine()
    {
        actualPassengersLine = Instantiate(lrPrefab, graphContainer);
        actualPassengersLine.positionCount = 0;
        actualPassengersLine.startColor = actualPassengersLineColor;
        actualPassengersLine.endColor = actualPassengersLineColor;
        actualPassengersLine.sortingOrder = 2;
        actualPassengersLine.numCornerVertices = 1;
        actualPassengersLine.widthCurve = AnimationCurve.Constant(0, 1, 1.5f);

        actualPassengersLineDot = Instantiate(lrPrefab, graphContainer);
        actualPassengersLineDot.positionCount = 2;
        actualPassengersLineDot.startColor = actualPassengersLineColor;
        actualPassengersLineDot.endColor = actualPassengersLineColor;
        actualPassengersLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        actualPassengersLineDot.sortingOrder = 3;


        actualPassengersNumberText = Instantiate(legendTextPrefab, graphContainer);
        actualPassengersNumberText.rectTransform.pivot = new Vector2(0, 0);
        actualPassengersNumberText.rectTransform.anchorMin = new Vector2(0, 0);
        actualPassengersNumberText.rectTransform.anchorMax = new Vector2(0, 0);
        actualPassengersNumberText.text = "Actual rate";
        actualPassengersNumberText.rectTransform.sizeDelta = new Vector2(300, 30);
        actualPassengersNumberText.fontSize = 42;
        actualPassengersNumberText.color = actualPassengersLineColor;
        actualPassengersNumberText.fontStyle = FontStyles.Bold;
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

