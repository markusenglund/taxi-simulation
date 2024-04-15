using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PredictedSupplyDemandGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;



    LineRenderer passengersLine;
    LineRenderer tripsLine;


    Vector2 graphSize = new Vector2(1200, 800);
    Vector3 graphPosition = new Vector3(2500, 700);
    float margin = 100f;
    float marginTop = 180f;
    float marginBottom = 140f;
    float maxY = 50f;
    float minY = 0f;
    float maxX;
    float minX = 0f;

    string headingText = "Passenger spawn rate";

    City city;

    Color tripsLineColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    Color passengersLineColor = new Color(0.4f, 0.8f, 1.0f, 1.0f);


    public static PredictedSupplyDemandGraph Create(City city)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform graphPrefab = Resources.Load<Transform>("Graphs/PredictedSupplyDemandGraph");
        Transform graphTransform = Instantiate(graphPrefab, canvas);

        PredictedSupplyDemandGraph graph = graphTransform.GetComponent<PredictedSupplyDemandGraph>();
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

        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        graphContainer.sizeDelta = graphSize; //new Vector2(graphSize.x - 2 * margin, graphSize.y - 2 * margin);
        InstantiateGraph();
        StartCoroutine(CreatePassengerCurve(duration: 6));
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

        while (Time.time - startTime < duration)
        {
            // float time = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
            float t = (Time.time - startTime) / duration;
            float graphTime = Mathf.Lerp(0, city.simulationSettings.simulationLengthHours, t);
            float passengersPerHour = city.GetNumExpectedPassengersPerHour(graphTime);
            Vector2 graphPosition = ConvertValueToGraphPosition(new Vector2(graphTime, passengersPerHour));
            if (t * numPositions >= i)
            {
                passengersLine.positionCount++;
                i++;
            }
            passengersLine.SetPosition(passengersLine.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
            yield return null;
        }
    }

    private void CreateAxes()
    {
        // Create x axis with the line renderer
        LineRenderer xLineRenderer = Instantiate(lrPrefab, graphContainer);
        xLineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxX, 0));
        xLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        xLineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));

        // Create y axis with the line renderer
        LineRenderer yLineRenderer = Instantiate(lrPrefab, graphContainer);
        yLineRenderer.positionCount = 2;
        Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxY));
        yLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        yLineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));

        xLineRenderer.sortingOrder = 2;
        yLineRenderer.sortingOrder = 2;
        // Set end cap vertices to one
        xLineRenderer.numCapVertices = 1;
        yLineRenderer.numCapVertices = 1;



    }

    private void CreateAxisValues()
    {
        // Create y axis values
        int step = Mathf.RoundToInt((maxY - minY) / 5f);
        for (int i = (int)minY; i <= maxY; i += step)
        {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
            text.rectTransform.sizeDelta = new Vector2(200, 30);
            text.fontSize = 42;

        }

        // Create x axis values
        step = Mathf.RoundToInt((maxX - minX) / 6f);
        for (int i = (int)minX; i <= maxX; i += step)
        {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(i, 0));
            // Set pivot to top center
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Set textmeshpro text alignment to center
            text.alignment = TextAlignmentOptions.Center;
            text.text = TimeUtils.ConvertSimulationHoursToTimeString(i);
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
        Vector2 textPosition = new Vector2(0f, graphSize.y / 2 - 100f);
        text.text = headingText;

        text.rectTransform.anchoredPosition = textPosition;
    }

    private void CreateLegend()
    {


        // Create a tiny green line with the line renderer
        // Create empty legend game object inside the graph container with scale 1
        GameObject legend = new GameObject("Legend", typeof(RectTransform));
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
        legend.transform.localPosition = new Vector3(-legendWidth + margin, (-graphSize.y) / 2 + 0.15f * marginBottom, 0);
        legendRectTransform.sizeDelta = new Vector2(legendWidth, legendHeight);

        LineRenderer passengersLegendLine = Instantiate(lrPrefab, legend.transform);
        passengersLegendLine.positionCount = 2;
        // Offset with 6 px to get it properly centered
        Vector2 passengerLinePosition1 = new Vector2(0, legendHeight / 2 - 6);
        Vector2 passengerLinePosition2 = passengerLinePosition1 + new Vector2(1, 0);
        passengersLegendLine.SetPosition(0, new Vector3(passengerLinePosition1.x, passengerLinePosition1.y, 0));
        passengersLegendLine.SetPosition(1, new Vector3(passengerLinePosition2.x, passengerLinePosition2.y, 0));
        passengersLegendLine.startColor = passengersLineColor;
        passengersLegendLine.endColor = passengersLineColor;
        passengersLegendLine.widthCurve = AnimationCurve.Constant(0, 1, 1.4f);

        TMP_Text text1 = Instantiate(legendTextPrefab, legend.transform);
        Vector2 textPosition1 = new Vector2(30, 0);
        text1.text = "Number of passengers/hr";
        text1.rectTransform.anchoredPosition = textPosition1;
        text1.rectTransform.sizeDelta = new Vector2(legendWidth, legendHeight);
        text1.fontSize = legendHeight;
    }

    private void InstantiateLines()
    {
        passengersLine = Instantiate(lrPrefab, graphContainer);
        passengersLine.positionCount = 0;
        passengersLine.startColor = passengersLineColor;
        passengersLine.endColor = passengersLineColor;
        passengersLine.sortingOrder = 1;
        passengersLine.numCornerVertices = 1;
    }

    private void InstantiateGraph()
    {
        InstantiateLines();
        CreateAxes();
        CreateAxisValues();
        CreateHeaderText();
        CreateLegend();
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

