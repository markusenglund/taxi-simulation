using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class UtilityIncomeScatterPlot : MonoBehaviour
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

    float margin = 40f;
    float marginTop = 50f;
    float maxUtilityScore = 10f;
    float minUtilityScore = 0f;
    float maxIncome = 120f;
    float minIncome = 0f;


    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();
    }

    public void AppendPassenger(Passenger passenger, TripCreatedPassengerData tripCreatedPassengerData)
    {
        Vector2 point = new Vector2(passenger.passengerEconomicParameters.hourlyIncome, tripCreatedPassengerData.expectedNetUtilityBeforeVariableCosts);
        Vector2 graphPosition = ConvertValueToGraphPosition(point);

        CreateDot(graphPosition, passenger.hasAcceptedRideOffer);
    }


    private void CreateAxes()
    {
        // Create x axis with the line renderer
        LineRenderer xLineRenderer = Instantiate(lrPrefab, graphContainer);
        xLineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxIncome, 0));
        xLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        xLineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));

        // Create y axis with the line renderer
        LineRenderer yLineRenderer = Instantiate(lrPrefab, graphContainer);
        yLineRenderer.positionCount = 2;
        Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxUtilityScore));
        yLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
        yLineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));

    }

    private void CreateAxisValues()
    {
        // Create y axis values
        int step = Mathf.RoundToInt((maxUtilityScore - minUtilityScore) / 5f);
        for (int i = (int)minUtilityScore; i <= maxUtilityScore; i += step)
        {
            TMP_Text text = Instantiate(textPrefab, graphContainer);
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
            text.text = i.ToString();
            text.rectTransform.anchoredPosition = textPosition;
        }

        // Create x axis values
        step = Mathf.RoundToInt((maxIncome - minIncome) / 6f);
        for (int i = (int)minIncome; i <= maxIncome; i += step)
        {
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

    private void CreateAxisLabels()
    {
        // Create y axis label
        TMP_Text text = Instantiate(textPrefab, graphContainer);
        Vector2 textPosition = new Vector2(0f, 0f);
        text.text = "Utility of trip excl costs";
        text.rectTransform.anchoredPosition = textPosition;
        text.rectTransform.Rotate(0, 0, -90);
        float graphHeight = graphContainer.sizeDelta.y;
        text.rectTransform.sizeDelta = new Vector2(graphHeight, 20);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.pivot = new Vector2(1f, 0f);

        // Create x axis label
        TMP_Text text2 = Instantiate(textPrefab, graphContainer);
        Vector2 textPosition2 = new Vector2(0f, 0f);
        text2.text = "Hourly income ($)";
        text2.rectTransform.anchoredPosition = textPosition2;
        float graphWidth = graphContainer.sizeDelta.x;
        text2.rectTransform.sizeDelta = new Vector2(graphWidth, 20);
        text2.alignment = TextAlignmentOptions.Center;
        text2.rectTransform.pivot = new Vector2(0f, 0f);
    }

    private void CreateHeaderText()
    {
        TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
        Vector2 textPosition = new Vector2(-52f, 124f);
        text.text = "Passengers";
        text.rectTransform.anchoredPosition = textPosition;
    }


    private void CreateLegend()
    {
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
        CreateAxisValues();
        CreateAxisLabels();
        CreateHeaderText();
        CreateLegend();
    }


    private Vector2 ConvertValueToGraphPosition(Vector2 vector)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;

        float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minUtilityScore) / (maxUtilityScore - minUtilityScore));
        float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minUtilityScore) / (maxIncome - minUtilityScore));

        return new Vector2(x, y);
    }



    private void CreateDot(Vector2 position, bool wasServed)
    {
        Transform dot = Instantiate(dotPrefab, graphContainer);
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);
        if (wasServed)
        {
            dot.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            dot.GetComponent<Renderer>().material.color = Color.red;

        }
    }
}

