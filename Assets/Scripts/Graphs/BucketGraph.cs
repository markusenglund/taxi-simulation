using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public delegate (float, float, float, float) GetBucketGraphValues(City[] cities);
public delegate string FormatBucketGraphValue(float value);

public class BucketGraph : MonoBehaviour
{
    City[] cities;
    GetBucketGraphValues getValues;
    FormatBucketGraphValue formatValue;

    private RectTransform graphContainer;

    float minValue = 0;
    float maxValue = 1;
    public static BucketGraph Create(City[] cities, Vector3 position, string labelText, GetBucketGraphValues getValues, FormatBucketGraphValue formatValue, Color color)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/BucketGraph");
        Transform bucketGraphTransform = Instantiate(prefab, canvas);
        BucketGraph bucketGraph = bucketGraphTransform.GetComponent<BucketGraph>();
        bucketGraph.cities = cities;
        bucketGraph.getValues = getValues;
        bucketGraph.formatValue = formatValue;

        // Set position
        RectTransform rt = bucketGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        // Set label text
        bucketGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;
        bucketGraph.graphContainer = bucketGraphTransform.Find("GraphContainer").GetComponent<RectTransform>();
        return bucketGraph;
    }

    private void Start()
    {
        StartCoroutine(UpdateValueLoop());
    }

    IEnumerator UpdateValueLoop()
    {
        Transform graphContainerTransform = transform.Find("GraphContainer");
        while (true)
        {
            (float value1, float value2, float value3, float value4) = getValues(cities);
            Debug.Log($"value1: {value1}, value2: {value2}, value3: {value3}, value4: {value4}");
            RectTransform bar1 = graphContainerTransform.Find("Bar1").GetComponent<RectTransform>();
            RectTransform bar2 = graphContainerTransform.Find("Bar2").GetComponent<RectTransform>();
            RectTransform bar3 = graphContainerTransform.Find("Bar3").GetComponent<RectTransform>();
            RectTransform bar4 = graphContainerTransform.Find("Bar4").GetComponent<RectTransform>();
            bar1.sizeDelta = new Vector2(bar1.sizeDelta.x, ConvertValueToGraphPosition(value1));
            bar2.sizeDelta = new Vector2(bar2.sizeDelta.x, ConvertValueToGraphPosition(value2));
            bar3.sizeDelta = new Vector2(bar3.sizeDelta.x, ConvertValueToGraphPosition(value3));
            bar4.sizeDelta = new Vector2(bar4.sizeDelta.x, ConvertValueToGraphPosition(value4));
            string formattedValue1 = formatValue(value1);
            graphContainerTransform.Find("Bar1/Value").GetComponent<TMPro.TMP_Text>().text = formattedValue1;
            string formattedValue2 = formatValue(value2);
            graphContainerTransform.Find("Bar2/Value").GetComponent<TMPro.TMP_Text>().text = formattedValue2;
            string formattedValue3 = formatValue(value3);
            graphContainerTransform.Find("Bar3/Value").GetComponent<TMPro.TMP_Text>().text = formattedValue3;
            string formattedValue4 = formatValue(value4);
            graphContainerTransform.Find("Bar4/Value").GetComponent<TMPro.TMP_Text>().text = formattedValue4;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private float ConvertValueToGraphPosition(float value)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * graphHeight / maxValue, 10);
    }
}
