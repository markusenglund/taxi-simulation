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
    float maxY = 60f;
    float minY = 0f;
    float maxX;
    float minX = 0f;

    string headingText = "Passenger spawn rate";

    City city;

    Color tripsLineColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    Color passengersLineColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);


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

        // Set order in layer to 1
        xLineRenderer.sortingOrder = 1;
        yLineRenderer.sortingOrder = 1;
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
        TMP_Text text1 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition1 = new Vector2(140, 74f);
        text1.text = "Potential riders/hr";
        text1.rectTransform.anchoredPosition = textPosition1;
        text1.rectTransform.sizeDelta = new Vector2(130, 30);

        // Create a tiny green line with the line renderer
        LineRenderer passengersLegendLine = Instantiate(lrPrefab, graphContainer);
        passengersLegendLine.positionCount = 2;
        Vector2 greenLinePosition1 = new Vector2(255, 181);
        Vector2 greenLinePosition2 = new Vector2(265, 181);
        passengersLegendLine.SetPosition(0, new Vector3(greenLinePosition1.x, greenLinePosition1.y, 0));
        passengersLegendLine.SetPosition(1, new Vector3(greenLinePosition2.x, greenLinePosition2.y, 0));
        passengersLegendLine.startColor = passengersLineColor;
        passengersLegendLine.endColor = passengersLineColor;

        TMP_Text text2 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition2 = new Vector2(140, 54f);
        text2.text = "Trips started/hr";
        text2.rectTransform.anchoredPosition = textPosition2;
        text2.rectTransform.sizeDelta = new Vector2(130, 30);

        // Create a tiny red line with the line renderer
        LineRenderer tripsLegendLine = Instantiate(lrPrefab, graphContainer);
        tripsLegendLine.positionCount = 2;
        Vector2 redLinePosition1 = new Vector2(255, 162);
        Vector2 redLinePosition2 = new Vector2(265, 162);
        tripsLegendLine.SetPosition(0, new Vector3(redLinePosition1.x, redLinePosition1.y, 0));
        tripsLegendLine.SetPosition(1, new Vector3(redLinePosition2.x, redLinePosition2.y, 0));
        tripsLegendLine.startColor = tripsLineColor;
        tripsLegendLine.endColor = tripsLineColor;
    }

    private void InstantiateLines()
    {
        passengersLine = Instantiate(lrPrefab, graphContainer);
        passengersLine.positionCount = 0;
        passengersLine.startColor = passengersLineColor;
        passengersLine.endColor = passengersLineColor;

        tripsLine = Instantiate(lrPrefab, graphContainer);
        tripsLine.positionCount = 0;
        tripsLine.startColor = tripsLineColor;
        tripsLine.endColor = tripsLineColor;
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

        float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minY) / (maxY - minY));
        float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minX) / (maxX - minX));

        return new Vector2(x, y);
    }
}

