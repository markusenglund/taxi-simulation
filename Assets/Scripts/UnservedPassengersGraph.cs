using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnservedPassengersGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    
    

    LineRenderer lineRenderer;

    Transform dot;

    List<Vector2> points = new List<Vector2>();

    int numUnservedPassengers = 0;
    
    float margin = 26f;
    float marginTop = 50f;
    float maxY = 50f;
    float minY = 0f;
    float maxX = 180f;
    float minX = 0f;

    float timeInterval = 2f;


    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();

        StartCoroutine(AddLatestCumulativeValue());
    }

    IEnumerator AddLatestCumulativeValue()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            UpdateGraph();
        }
    }

    public void IncrementNumUnservedPassengers()
    {
        numUnservedPassengers += 1;
    }

    private void UpdateGraph()
    {
        float time = Time.time;
        Vector2 point = new Vector2(time, numUnservedPassengers);
        points.Add(point);
        lineRenderer.positionCount++;
        Vector2 graphPosition = ConvertValueToGraphPosition(point);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
        // Debug.Log("graphPosition: " + graphPosition);
        // Debug.Log("point: " + point);
        // CreateDot(graphPosition);
    }


    private void CreateAxes() {
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

    }

    private void CreateAxisLabels() {
        // Create y axis labels
        int step = Mathf.RoundToInt((maxY - minY) / 5f);
        for (int i = (int)minY; i <= maxY; i += step) {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
        }
    }

    private void CreateHeaderText() {
        TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
        Vector2 textPosition = new Vector2(0, 70f);
        text.text = "Unserved passengers";
        text.rectTransform.anchoredPosition = textPosition;
    }

    private void InstantiateGraph()
    {
        lineRenderer = Instantiate(lrPrefab, graphContainer);
        lineRenderer.positionCount = 1;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        lineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        CreateAxes();
        CreateAxisLabels();
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


    private void CreateDot(Vector2 position) {
        Transform dot = Instantiate(dotPrefab, graphContainer);
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);
        
    }
}

