using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DriverProfitGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    [SerializeField] private LineRenderer lrPrefab;
    [SerializeField] private Transform dotPrefab;
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Text headerTextPrefab;
    [SerializeField] private TMP_Text legendTextPrefab;

    LineRenderer grossProfitLine;
    LineRenderer surplusValueLine;

    Color[] colors = { new Color(1.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 0.0f, 1.0f, 1.0f), new Color(0.5f, 0.0f, 0.5f, 1.0f) };

    float margin = 26f;
    float marginTop = 50f;
    float maxY = 30f;
    float minY = -10f;
    float maxX = 24f;
    float minX = 0f;

    // Update graph every 15 in sim minutes
    const float timeInterval = 30f / 60f;

    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();

        InstantiateGraph();

    }

    private void InstantiateGraph()
    {
        CreateAxes();
        CreateAxisValues();
        CreateHeaderText();
        InstantiateLines();
        StartCoroutine(UpdateGraphAtInterval());

    }

    IEnumerator UpdateGraphAtInterval()
    {
        while (true)
        {
            float intervalRealSeconds = TimeUtils.ConvertSimulationHoursToRealSeconds(timeInterval);
            yield return new WaitForSeconds(intervalRealSeconds);
            UpdateGraph();
        }
    }

    private void UpdateGraph()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        (float grossProfitLastHour, float surplusValueLastHour) = DriverPool.CalculateAverageHourlyGrossProfitLastHour();
        Vector2 grossProfitPoint = new Vector2(simulationTime, grossProfitLastHour);
        Vector2 grossProfitGraphPosition = ConvertValueToGraphPosition(grossProfitPoint);
        grossProfitLine.positionCount++;
        grossProfitLine.SetPosition(grossProfitLine.positionCount - 1, new Vector3(grossProfitGraphPosition.x, grossProfitGraphPosition.y, 0));

        Vector2 surplusValuePoint = new Vector2(simulationTime, surplusValueLastHour);
        Vector2 surplusValueGraphPosition = ConvertValueToGraphPosition(surplusValuePoint);
        surplusValueLine.positionCount++;
        surplusValueLine.SetPosition(surplusValueLine.positionCount - 1, new Vector3(surplusValueGraphPosition.x, surplusValueGraphPosition.y, 0));

    }

    private void InstantiateLines()
    {
        grossProfitLine = Instantiate(lrPrefab, graphContainer);
        grossProfitLine.positionCount = 0;
        grossProfitLine.startColor = colors[0];
        grossProfitLine.endColor = colors[0];

        surplusValueLine = Instantiate(lrPrefab, graphContainer);
        surplusValueLine.positionCount = 0;
        surplusValueLine.startColor = colors[1];
        surplusValueLine.endColor = colors[1];
    }

    private void CreateHeaderText()
    {
        TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
        Vector2 textPosition = new Vector2(-52f, 70f);
        text.text = "Driver gross profit per hour";
        text.rectTransform.anchoredPosition = textPosition;
    }


    private void CreateAxes()
    {
        // Create x axis with the line renderer
        LineRenderer xLineRenderer = Instantiate(lrPrefab, graphContainer);
        xLineRenderer.positionCount = 2;
        Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(minX, minY));
        Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxX, minY));
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
            Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(i, minY));
            // Set pivot to top center
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Set textmeshpro text alignment to center
            text.alignment = TextAlignmentOptions.Center;
            text.text = TimeUtils.ConvertSimulationHoursToTimeString(i);
            text.rectTransform.anchoredPosition = textPosition;
        }
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
