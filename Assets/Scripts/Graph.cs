using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Graph : MonoBehaviour
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
    float maxY = 50f;
    float minY = 0f;
    float maxX = 180f;
    float minX = 0f;


    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
        InstantiateGraph();

        // StartCoroutine(GetNewValues());
    }

    // IEnumerator GetNewValues()
    // {
    //     List<int> values = new List<int>() { 45, 10, 85, 100, 15, 94, 14, 44, 5, 10 };
    //     // Do a for loop of the values list
    //     for (int i = 0; i < values.Count; i++)
    //     {
    //         int random = UnityEngine.Random.Range(0, 4);
    //         yield return new WaitForSeconds(random);
    //         lineRenderer.positionCount++;
    //         Vector2 graphPosition = ConvertValueToGraphPosition(new Vector2(Time.time, values[i]));
    //         lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
    //     }
    // }

    public void SetNewValue(float value)
    {
        float time = Time.time;
        Vector2 point = new Vector2(time, value);
        values.Add(point);
        // lineRenderer.positionCount++;
        Vector2 graphPosition = ConvertValueToGraphPosition(point);
        // lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
        // Debug.Log("graphPosition: " + graphPosition);
        // Debug.Log("point: " + point);
        CreateDot(graphPosition);
    }


    private void CreateAxes() {
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
        text.text = "Waiting time (s)";
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

        Debug.Log("graphHeight: " + graphHeight);
        Debug.Log("graphWidth: " + graphWidth);

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

   /* private void CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(11, 11);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
    }*/
}

