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

    List<LineRenderer> lines = new List<LineRenderer>();

    List<Driver> drivers = new List<Driver>();

    Color[] colors = { new Color(1.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 0.0f, 1.0f, 1.0f), new Color(0.5f, 0.0f, 0.5f, 1.0f) };

    float margin = 26f;
    float marginTop = 50f;
    float maxY = 30f;
    float minY = 0f;
    float maxX = 6f;
    float minX = 0f;

    const float timeInterval = 5f;

    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();

        InstantiateGraph();

    }

    public void AppendDriver(Driver driver)
    {
        drivers.Add(driver);
        InstantiateLine(drivers.Count - 1);
    }

    private void InstantiateGraph()
    {
        CreateAxes();
        CreateAxisLabels();
        CreateHeaderText();
        // CreateLegend();

        StartCoroutine(UpdateGraphAtInterval());

    }

    IEnumerator UpdateGraphAtInterval()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeInterval);
            UpdateGraph();
        }
    }

    private void UpdateGraph()
    {
        float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
        for (int i = 0; i < drivers.Count; i++)
        {
            Driver driver = drivers[i];
            LineRenderer line = lines[i];
            if (driver != null)
            {
                float grossProfitLastHour = driver.CalculateGrossProfitLastHour();
                Vector2 point = new Vector2(simulationTime, grossProfitLastHour);
                Vector2 graphPosition = ConvertValueToGraphPosition(point);
                line.positionCount++;
                line.SetPosition(line.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
            }
        }
    }

    private void InstantiateLine(int i)
    {
        LineRenderer line = Instantiate(lrPrefab, graphContainer);
        line.positionCount = 0;
        line.startColor = colors[i];
        line.endColor = colors[i];
        lines.Add(line);
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

    private void CreateAxisLabels()
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
