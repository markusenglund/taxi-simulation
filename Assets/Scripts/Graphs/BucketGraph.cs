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
        bucketGraphTransform.Find("GraphContainer/MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;

        return bucketGraph;
    }

    private void Start()
    {
        StartCoroutine(UpdateValueLoop());
    }

    IEnumerator UpdateValueLoop()
    {
        Transform graphContent = transform.Find("GraphContainer/Content");
        while (true)
        {
            (float value1, float value2, float value3, float value4) = getValues(cities);
            Debug.Log($"value1: {value1}, value2: {value2}, value3: {value3}, value4: {value4}");
            RectTransform bar1 = graphContent.Find("Bar1").GetComponent<RectTransform>();
            RectTransform bar2 = graphContent.Find("Bar2").GetComponent<RectTransform>();
            RectTransform bar3 = graphContent.Find("Bar3").GetComponent<RectTransform>();
            RectTransform bar4 = graphContent.Find("Bar4").GetComponent<RectTransform>();
            bar1.sizeDelta = new Vector2(bar1.sizeDelta.x, 10 + value1 * 100 * 5);
            bar2.sizeDelta = new Vector2(bar2.sizeDelta.x, 10 + value2 * 100 * 5);
            bar3.sizeDelta = new Vector2(bar3.sizeDelta.x, 10 + value3 * 100 * 5);
            bar4.sizeDelta = new Vector2(bar4.sizeDelta.x, 10 + value4 * 100 * 5);
            // string formattedValue = formatValue(value);
            // transform.Find("Value").GetComponent<TMPro.TMP_Text>().text = formattedValue;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
