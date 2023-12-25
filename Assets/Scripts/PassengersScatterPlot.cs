using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Passenger
{
    public float timeWillingToWait { get; set; }
    public bool wasServed { get; set; }
    public float moneyWillingToSpend { get; set; }
    public float time { get; set; }
}


public class PassengersScatterPlot : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;
    
    

    LineRenderer lineRenderer;

    Transform dot;

    List<Vector2> values = new List<Vector2>();

    List<Passenger> passengers = new List<Passenger>();
    
    float margin = 26f;
    float marginTop = 50f;
    float maxWillingnessToWait = 70f;
    float minWillingnessToWait = 0f;
    float maxWillingnessToSpend = 180f;
    float minWillingnessToSpend = 0f;


    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();
    }

    public void AppendPassenger(
        float timeWillingToWait,
        bool wasServed,
        float moneyWillingToSpend
    )
    {
        float time = Time.time;
        
        // Create a passenger
        Passenger passenger = new Passenger()
        {
            timeWillingToWait = timeWillingToWait,
            wasServed = wasServed,
            moneyWillingToSpend = moneyWillingToSpend,
            time = time
        };

        

        passengers.Add(passenger);
        Vector2 point = new Vector2(moneyWillingToSpend, timeWillingToWait);
        Vector2 graphPosition = ConvertValueToGraphPosition(point);

        CreateDot(graphPosition, wasServed);
    }


    private void CreateAxes() {
        // Create x axis with the line renderer
    LineRenderer xLineRenderer = Instantiate(lrPrefab, graphContainer);
        xLineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxWillingnessToSpend, 0));
        xLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        xLineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));

        // Create y axis with the line renderer
        LineRenderer yLineRenderer = Instantiate(lrPrefab, graphContainer);
        yLineRenderer.positionCount = 2;
        Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxWillingnessToWait));
        yLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        yLineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));

    }

    private void CreateAxisLabels() {
        // Create y axis labels
        int step = Mathf.RoundToInt((maxWillingnessToWait - minWillingnessToWait) / 5f);
        for (int i = (int)minWillingnessToWait; i <= maxWillingnessToWait; i += step) {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
        }

        // Create x axis labels
        step = Mathf.RoundToInt((maxWillingnessToSpend - minWillingnessToSpend) / 6f);
        for (int i = (int)minWillingnessToSpend; i <= maxWillingnessToSpend; i += step) {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(i, 0));
            // Set pivot to top center
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Set textmeshpro text alignment to center
            text.alignment = TextAlignmentOptions.Center;
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
        }
    }

    private void CreateHeaderText() {
        TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
        Vector2 textPosition = new Vector2(-52f, 124f);
        text.text = "Passengers";
        text.rectTransform.anchoredPosition = textPosition;
    }

        
    private void CreateLegend() {
        TMP_Text text1 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition1 = new Vector2(80, 128f);
        text1.text = "Hailed taxi";
        text1.rectTransform.anchoredPosition = textPosition1;

        Transform greenDot = Instantiate(dotPrefab, graphContainer);
        RectTransform greenDotTransform = greenDot.GetComponent<RectTransform>();
        greenDotTransform.anchoredPosition = new Vector3(32f, 135f, -1);
        greenDotTransform.localScale = new Vector3(8, 8, 1);
        greenDot.GetComponent<Renderer>().material.color = Color.green;

        TMP_Text text2 = Instantiate(legendTextPrefab, graphContainer);
        Vector2 textPosition2 = new Vector2(80, 108f);
        text2.text = "Unserved";
        text2.rectTransform.anchoredPosition = textPosition2;

        Transform redDot = Instantiate(dotPrefab, graphContainer);
        RectTransform redDotTransform = redDot.GetComponent<RectTransform>();
        redDotTransform.anchoredPosition = new Vector3(32f, 115f, -1);
        redDotTransform.localScale = new Vector3(8, 8, 1);
        redDot.GetComponent<Renderer>().material.color = Color.red;
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
        CreateLegend();
    }


    private Vector2 ConvertValueToGraphPosition(Vector2 vector)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;

        float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minWillingnessToWait) / (maxWillingnessToWait - minWillingnessToWait));
        float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minWillingnessToWait) / (maxWillingnessToSpend - minWillingnessToWait));

        return new Vector2(x, y);
    }



    private void CreateDot(Vector2 position, bool wasServed) {
        Transform dot = Instantiate(dotPrefab, graphContainer);
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);
        if (wasServed) {
            dot.GetComponent<Renderer>().material.color = Color.green;
        } else {
             dot.GetComponent<Renderer>().material.color = Color.red;

        }
    }
}

