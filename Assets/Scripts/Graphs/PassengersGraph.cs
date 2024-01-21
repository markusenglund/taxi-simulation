using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PassengersGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;



    LineRenderer passengersLine;
    LineRenderer tripsLine;


    float margin = 26f;
    float marginTop = 50f;
    float maxY = 100f;
    float minY = 0f;
    float maxX = 24f;
    float minX = 0f;

    float timeInterval = 20f / 60f;


    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();

        StartCoroutine(UpdateGraphAtInterval());
    }

    IEnumerator UpdateGraphAtInterval()
    {
        while (true)
        {
            yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursToRealSeconds(timeInterval));
            UpdateGraph();
        }
    }


    private void UpdateGraph()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);

        // Update passengers line
        passengersLine.positionCount += 1;
        int numPassengersSpawnedPerHour = GameManager.Instance.CalculateNumPassengersSpawnedInLastInterval(1);

        Vector2 passengersPosition = ConvertValueToGraphPosition(new Vector2(simulationTime, numPassengersSpawnedPerHour));
        passengersLine.SetPosition(passengersLine.positionCount - 1, new Vector3(passengersPosition.x, passengersPosition.y, 0));

        // Update trips line
        tripsLine.positionCount += 1;
        int numTripsStartedPerHour = GameManager.Instance.CalculateNumStartedTripsInLastInterval(1);

        Vector2 tripsPosition = ConvertValueToGraphPosition(new Vector2(simulationTime, numTripsStartedPerHour));
        tripsLine.SetPosition(tripsLine.positionCount - 1, new Vector3(tripsPosition.x, tripsPosition.y, 0));
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
        Vector2 textPosition = new Vector2(-52f, 70f);
        text.text = "Passengers";
        text.rectTransform.anchoredPosition = textPosition;
    }

    private void CreateLegend()
    {
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

    private void InstantiateLines()
    {
        passengersLine = Instantiate(lrPrefab, graphContainer);
        passengersLine.positionCount = 0;
        passengersLine.startColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        passengersLine.endColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);

        tripsLine = Instantiate(lrPrefab, graphContainer);
        tripsLine.positionCount = 0;
        tripsLine.startColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
        tripsLine.endColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
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

