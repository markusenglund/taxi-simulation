using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WaitingTimeGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;



    LineRenderer lineRenderer;

    Transform dot;

    List<Vector2> values = new List<Vector2>();

    float margin = 26f;
    float marginTop = 50f;
    float maxY = 100f;
    float minY = 0f;
    float maxX;
    float minX = 0f;


    public static WaitingTimeGraph Create(Transform prefab, Vector3 screenPos, SimulationSettings simSettings)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform graphTransform = Instantiate(prefab, canvas);
        RectTransform graphRectTransform = graphTransform.GetComponent<RectTransform>();

        graphRectTransform.anchoredPosition = screenPos;
        WaitingTimeGraph graph = graphTransform.GetComponent<WaitingTimeGraph>();
        graph.maxX = simSettings.simulationLengthHours;
        return graph;
    }
    private void Start()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();
    }

    public void SetNewValue(float hoursWaited)
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);

        float minutesWaited = hoursWaited * 60;
        Vector2 point = new Vector2(simulationTime, minutesWaited);
        values.Add(point);
        Vector2 graphPosition = ConvertValueToGraphPosition(point);
        CreateDot(graphPosition);
    }


    private void CreateAxes()
    {
        // Create x axis with the line renderer
        lineRenderer = Instantiate(lrPrefab, graphContainer);
        lineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxX, 0));
        lineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        lineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));

        // Create y axis with the line renderer
        lineRenderer = Instantiate(lrPrefab, graphContainer);
        lineRenderer.positionCount = 2;
        Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxY));
        lineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        lineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));

    }

    private void CreateAxisValues()
    {
        // Create y axis labels
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
        Vector2 textPosition = new Vector2(0, 70f);
        text.text = "Waiting time (minutes)";
        text.rectTransform.anchoredPosition = textPosition;
    }

    private void InstantiateGraph()
    {
        lineRenderer = Instantiate(lrPrefab, graphContainer);
        lineRenderer.positionCount = 1;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        lineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        CreateAxes();
        CreateAxisValues();
        CreateHeaderText();
    }


    private Vector2 ConvertValueToGraphPosition(Vector2 vector)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;

        float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minY) / (maxY - minY));
        float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minX) / (maxX - minX));

        return new Vector2(x, y);
    }

    private void CreateDot(Vector2 position)
    {
        Transform dot = Instantiate(dotPrefab, graphContainer);
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);

    }
}

