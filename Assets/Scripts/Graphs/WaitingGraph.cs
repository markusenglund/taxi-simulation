using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System;


public class WaitingGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;



    LineRenderer staticLine;
    LineRenderer staticLineDot;
    LineRenderer surgeLine;
    LineRenderer surgeLineDot;

    List<LineRenderer> separatorLines = new List<LineRenderer>();

    int startHour = 18;
    Vector2 graphSize = new Vector2(1200, 800);
    Vector3 graphPosition = new Vector3(1100, 700);
    float margin = 100f;
    float marginTop = 220f;
    float marginBottom = 140f;
    float maxY = 50f;
    float minY = 0f;
    float maxX;
    float minX = 0f;

    string headingText = "Average expected waiting time";

    City staticCity;
    City surgeCity;

    Color surgeLineColor = ColorScheme.orange;
    Color staticLineColor = ColorScheme.blue;

    Color separatorColor = new Color(96 / 255f, 96 / 255f, 96 / 255f, 1f);


    LineRenderer staticLegendLine;
    LineRenderer surgeLegendLine;
    LineRenderer xLineRenderer;
    LineRenderer yLineRenderer;

    Color axisColor = new Color(192f / 255f, 192f / 255f, 192f / 255f, 1f);

    CanvasGroup canvasGroup;

    TMP_Text staticValueText;
    TMP_Text surgeValueText;

    TMP_Text staticTotalValueText;
    TMP_Text surgeTotalValueText;

    float defaultLineWidth;


    public static WaitingGraph Create(City city1, City city2)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform graphPrefab = Resources.Load<Transform>("Graphs/WaitingGraph");
        Transform graphTransform = Instantiate(graphPrefab, canvas);

        WaitingGraph graph = graphTransform.GetComponent<WaitingGraph>();
        graph.maxX = city1.simulationSettings.simulationLengthHours;
        graph.staticCity = city1;
        graph.surgeCity = city2;
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
        graphContainer.sizeDelta = graphSize;
        defaultLineWidth = lrPrefab.widthMultiplier;
        InstantiateGraph();
        InstantiateInfoGroup();


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
        StartCoroutine(UpdateInfoGroup());
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
            // Loop through all line renderers and set the alpha value
            foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
            {
                Color color = line.startColor;
                color.a = alpha;
                line.startColor = color;
                line.endColor = color;
            }
            yield return null;
        }
        canvasGroup.alpha = finalAlpha;
    }

    private List<Trip> GetTripsCreatedInLastInterval(City city, float intervalHours)
    {
        List<Trip> trips = city.GetTrips();
        float intervalStartTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time) - intervalHours;
        return trips.Where(trip => trip.tripCreatedData.createdTime >= intervalStartTime).ToList();
    }

    private float GetAverageExpectedWaitTime(List<Trip> trips)
    {
        if (trips.Count == 0)
        {
            return 0;
        }
        return trips.Average(trip => trip.tripCreatedData.expectedWaitingTime);
    }

    private IEnumerator UpdateCurves()
    {
        float numPositions = 200;
        float timeResolution = 0.4f;

        Queue<(float value, float time)> lastSurgeValues = new Queue<(float value, float time)>();
        Queue<(float value, float time)> lastStaticValues = new Queue<(float value, float time)>();

        float simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
        Vector2 zeroGraphPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        while (simulationTime < staticCity.simulationSettings.simulationLengthHours)
        {
            simulationTime = TimeUtils.ConvertRealSecondsTimeToSimulationHours(Time.time);
            float t = simulationTime / staticCity.simulationSettings.simulationLengthHours;
            List<Trip> staticTrips = GetTripsCreatedInLastInterval(staticCity, timeResolution);
            List<Trip> surgeTrips = GetTripsCreatedInLastInterval(surgeCity, timeResolution);
            float averageStaticWaitTime = GetAverageExpectedWaitTime(staticTrips);
            float averageStaticWaitTimeMinutes = averageStaticWaitTime * 60;
            float averageSurgeWaitTime = GetAverageExpectedWaitTime(surgeTrips);
            float averageSurgeWaitTimeMinutes = averageSurgeWaitTime * 60;

            lastStaticValues.Enqueue((averageStaticWaitTimeMinutes, simulationTime));
            if (t * numPositions > staticLine.positionCount)
            {
                staticLine.positionCount++;
            }
            if (lastStaticValues.Count > 100)
            {
                lastStaticValues.Dequeue();
            }
            lastSurgeValues.Enqueue((averageSurgeWaitTimeMinutes, simulationTime));
            if (t * numPositions > surgeLine.positionCount)
            {
                surgeLine.positionCount++;
            }
            if (lastSurgeValues.Count > 100)
            {
                lastSurgeValues.Dequeue();
            }

            float smoothedAverageStaticWaitTime = lastStaticValues.Average(x => x.value);
            float smoothedAverageSurgeWaitTime = lastSurgeValues.Average(x => x.value);

            float timeInterval = lastStaticValues.Last().time - lastStaticValues.First().time;
            // Offset the time axis to the average time of the values that were used to calculate the spawn rate
            float graphTime = simulationTime - timeResolution / 2 - timeInterval / 2;
            Vector3 staticPosition = ConvertValueToGraphPosition(new Vector3(graphTime, smoothedAverageStaticWaitTime, 0));
            staticLine.SetPosition(staticLine.positionCount - 1, staticPosition);
            staticLineDot.SetPosition(0, staticPosition);
            staticLineDot.SetPosition(1, staticPosition);

            Vector3 surgePosition = ConvertValueToGraphPosition(new Vector3(graphTime, smoothedAverageSurgeWaitTime, 0));
            surgeLine.SetPosition(surgeLine.positionCount - 1, surgePosition);
            surgeLineDot.SetPosition(0, surgePosition);
            surgeLineDot.SetPosition(1, surgePosition);
            bool isSurgeHigher = smoothedAverageSurgeWaitTime > smoothedAverageStaticWaitTime;
            float avoidanceOffset = isSurgeHigher ? 15 : -15;
            staticValueText.text = Math.Round(averageStaticWaitTimeMinutes).ToString() + " min";
            Vector3 staticValuePosition = new Vector3(staticPosition.x + 18, Mathf.Max(staticPosition.y, zeroGraphPosition.y + 25) - avoidanceOffset, 0);
            staticValueText.rectTransform.anchoredPosition = staticValuePosition;

            surgeValueText.text = Math.Round(averageSurgeWaitTimeMinutes).ToString() + " min";
            Vector3 surgeValuePosition = new Vector3(surgePosition.x + 18, Mathf.Max(surgePosition.y, zeroGraphPosition.y + 25) + avoidanceOffset, 0);
            surgeValueText.rectTransform.anchoredPosition = surgeValuePosition;

            yield return null;
        }
    }

    private IEnumerator UpdateInfoGroup()
    {
        while (true)
        {
            float staticTotalWaitTime = GetAverageExpectedWaitTime(staticCity.GetTrips()) * 60;
            float surgeTotalWaitTime = GetAverageExpectedWaitTime(surgeCity.GetTrips()) * 60;

            staticTotalValueText.text = Math.Round(staticTotalWaitTime).ToString() + " min";
            surgeTotalValueText.text = Math.Round(surgeTotalWaitTime).ToString() + " min";
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
        staticLegendLine = Instantiate(lrPrefab);
        surgeLegendLine = Instantiate(lrPrefab);


        CreateLegend(x: 80, staticLegendLine, staticLineColor, "Static average");
        CreateLegend(x: 560, surgeLegendLine, surgeLineColor, "Surge average");

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
        staticLine = Instantiate(lrPrefab, graphContainer);
        staticLine.positionCount = 0;
        staticLine.startColor = staticLineColor;
        staticLine.endColor = staticLineColor;
        staticLine.sortingOrder = 1;
        staticLine.numCornerVertices = 1;
        staticLine.widthCurve = AnimationCurve.Constant(0, 1, 1.2f);

        surgeLine = Instantiate(lrPrefab, graphContainer);
        surgeLine.positionCount = 0;
        surgeLine.startColor = surgeLineColor;
        surgeLine.endColor = surgeLineColor;
        surgeLine.sortingOrder = 2;
        surgeLine.numCornerVertices = 1;
        surgeLine.widthCurve = AnimationCurve.Constant(0, 1, 1.5f);

        Vector3 originPosition = ConvertValueToGraphPosition(new Vector3(0, 0, 0));


        staticLineDot = Instantiate(lrPrefab, graphContainer);
        staticLineDot.positionCount = 2;
        staticLineDot.startColor = staticLineColor;
        staticLineDot.endColor = staticLineColor;
        staticLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        staticLineDot.sortingOrder = 3;
        staticLineDot.SetPositions(new Vector3[] { originPosition, originPosition });

        surgeLineDot = Instantiate(lrPrefab, graphContainer);
        surgeLineDot.positionCount = 2;
        surgeLineDot.startColor = surgeLineColor;
        surgeLineDot.endColor = surgeLineColor;
        surgeLineDot.widthCurve = AnimationCurve.Constant(0, 1, 6f);
        surgeLineDot.sortingOrder = 4;
        surgeLineDot.SetPositions(new Vector3[] { originPosition, originPosition });

        staticValueText = Instantiate(legendTextPrefab, graphContainer);
        staticValueText.rectTransform.pivot = new Vector2(0, 0);
        staticValueText.rectTransform.anchorMin = new Vector2(0, 0);
        staticValueText.rectTransform.anchorMax = new Vector2(0, 0);
        staticValueText.text = "";
        staticValueText.rectTransform.sizeDelta = new Vector2(300, 30);
        staticValueText.fontSize = 42;
        staticValueText.color = staticLineColor;
        staticValueText.fontStyle = FontStyles.Bold;

        surgeValueText = Instantiate(legendTextPrefab, graphContainer);
        surgeValueText.rectTransform.pivot = new Vector2(0, 0);
        surgeValueText.rectTransform.anchorMin = new Vector2(0, 0);
        surgeValueText.rectTransform.anchorMax = new Vector2(0, 0);
        surgeValueText.text = "";
        surgeValueText.rectTransform.sizeDelta = new Vector2(300, 30);
        surgeValueText.fontSize = 42;
        surgeValueText.color = surgeLineColor;
        surgeValueText.fontStyle = FontStyles.Bold;


    }

    private void InstantiateGraph()
    {
        InstantiateLines();
        CreateAxes();
        CreateAxisValues();
        CreateHeaderText();
        CreateLegends();
    }

    private void InstantiateInfoGroup()
    {
        staticTotalValueText = transform.Find("SimulationInfoGroup/Group1/ValueText").GetComponent<TMP_Text>();
        staticTotalValueText.color = staticLineColor;
        surgeTotalValueText = transform.Find("SimulationInfoGroup/Group2/ValueText").GetComponent<TMP_Text>();
        surgeTotalValueText.color = surgeLineColor;
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

