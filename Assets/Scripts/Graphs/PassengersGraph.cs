using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PassengersGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;
    
    

    LineRenderer unservedLine;
    LineRenderer pickedUpLine;

    Transform dot;

    List<Vector2> unservedLinePoints = new List<Vector2>();
    List<Vector2> pickedUpLinePoints = new List<Vector2>();

    int numUnservedPassengers = 0;
    int numPickedUpPassengers = 0;
    
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

    public void IncrementNumPickedUpPassengers()
    {
        numPickedUpPassengers += 1;
    }

    private void UpdateGraph()
    {
        float time = Time.time;
        Vector2 point = new Vector2(time, numUnservedPassengers);
        unservedLinePoints.Add(point);
        unservedLine.positionCount++;
        Vector2 graphPosition = ConvertValueToGraphPosition(point);
        unservedLine.SetPosition(unservedLine.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
        // Debug.Log("graphPosition: " + graphPosition);
        // Debug.Log("point: " + point);
        // CreateDot(graphPosition);

        // Update picked up line
        Vector2 point2 = new Vector2(time, numPickedUpPassengers);
        pickedUpLinePoints.Add(point2);
        pickedUpLine.positionCount++;
        Vector2 pickedUpGraphPosition = ConvertValueToGraphPosition(point2);
        pickedUpLine.SetPosition(pickedUpLine.positionCount - 1, new Vector3(pickedUpGraphPosition.x, pickedUpGraphPosition.y, 0));


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
        Vector2 textPosition = new Vector2(-52f, 70f);
        text.text = "Passengers";
        text.rectTransform.anchoredPosition = textPosition;
    }

    private void CreateLegend() {
        TMP_Text text1 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition1 = new Vector2(80, 74f);
        text1.text = "Picked up";
        text1.rectTransform.anchoredPosition = textPosition1;

        // Create a tiny green line with the line renderer
        LineRenderer greenLine = Instantiate(lrPrefab, graphContainer);
        greenLine.positionCount = 2;
        Vector2 greenLinePosition1 = new Vector2(225, 181);
        Vector2 greenLinePosition2 = new Vector2(235, 181);
        greenLine.SetPosition(0, new Vector3(greenLinePosition1.x, greenLinePosition1.y, 0));
        greenLine.SetPosition(1, new Vector3(greenLinePosition2.x, greenLinePosition2.y, 0));
        greenLine.startColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        greenLine.endColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

        TMP_Text text2 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition2 = new Vector2(80, 54f);
        text2.text = "Unserved";
        text2.rectTransform.anchoredPosition = textPosition2;

        // Create a tiny red line with the line renderer
        LineRenderer redLine = Instantiate(lrPrefab, graphContainer);
        redLine.positionCount = 2;
        Vector2 redLinePosition1 = new Vector2(225, 162);
        Vector2 redLinePosition2 = new Vector2(235, 162);
        redLine.SetPosition(0, new Vector3(redLinePosition1.x, redLinePosition1.y, 0));
        redLine.SetPosition(1, new Vector3(redLinePosition2.x, redLinePosition2.y, 0));
        redLine.startColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        redLine.endColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    }

    private void InstantiateGraph()
    {
        unservedLine = Instantiate(lrPrefab, graphContainer);
        unservedLine.positionCount = 1;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        unservedLine.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        // Set the color of unserved line to red
        unservedLine.startColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        unservedLine.endColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        pickedUpLine = Instantiate(lrPrefab, graphContainer);
        pickedUpLine.positionCount = 1;
        pickedUpLine.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        // Set the color of picked up line to green
        pickedUpLine.startColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        pickedUpLine.endColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        
        CreateAxes();
        CreateAxisLabels();
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


    private void CreateDot(Vector2 position) {
        Transform dot = Instantiate(dotPrefab, graphContainer);
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);
        
    }
}

