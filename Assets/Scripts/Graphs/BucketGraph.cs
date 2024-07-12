using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public delegate BucketInfo[] GetBucketGraphValues(City[] cities);
public delegate string FormatBucketGraphValue(float value);

public class BucketGraph : MonoBehaviour
{
    City[] staticCities;
    City[] surgeCities;
    GetBucketGraphValues getValues;
    FormatBucketGraphValue formatValue;
    CanvasGroup canvasGroup;

    private RectTransform graphContainer;

    float minValue = 0;
    float maxValue = 1;
    public static BucketGraph Create(City[] staticCities, City[] surgeCities, Vector3 position, string labelText, GetBucketGraphValues getValues, FormatBucketGraphValue formatValue, string[] labels, Color staticColor, Color surgeColor)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/BucketGraph");
        Transform bucketGraphTransform = Instantiate(prefab, canvas);
        BucketGraph bucketGraph = bucketGraphTransform.GetComponent<BucketGraph>();
        bucketGraph.staticCities = staticCities;
        bucketGraph.surgeCities = surgeCities;
        bucketGraph.getValues = getValues;
        bucketGraph.formatValue = formatValue;

        RectTransform rt = bucketGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        bucketGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;
        bucketGraph.graphContainer = bucketGraphTransform.Find("GraphContainer").GetComponent<RectTransform>();

        bucketGraph.graphContainer.Find("BarGroup1/StaticBar").GetComponent<UnityEngine.UI.Image>().color = staticColor;
        bucketGraph.graphContainer.Find("BarGroup1/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = surgeColor;
        bucketGraph.graphContainer.Find("BarGroup2/StaticBar").GetComponent<UnityEngine.UI.Image>().color = staticColor;
        bucketGraph.graphContainer.Find("BarGroup2/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = surgeColor;
        bucketGraph.graphContainer.Find("BarGroup3/StaticBar").GetComponent<UnityEngine.UI.Image>().color = staticColor;
        bucketGraph.graphContainer.Find("BarGroup3/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = surgeColor;
        bucketGraph.graphContainer.Find("BarGroup4/StaticBar").GetComponent<UnityEngine.UI.Image>().color = staticColor;
        bucketGraph.graphContainer.Find("BarGroup4/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = surgeColor;


        // Set labels
        for (int i = 0; i < 4; i++)
        {
            bucketGraph.graphContainer.Find($"BarGroup{i + 1}/Label").GetComponent<TMPro.TMP_Text>().text = labels[i];
        }


        return bucketGraph;
    }

    private void Start()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(1));
        StartCoroutine(UpdateValueLoop());
    }

    IEnumerator UpdateValueLoop()
    {
        Transform graphContainerTransform = transform.Find("GraphContainer");
        while (true)
        {
            BucketInfo[] staticBuckets = getValues(staticCities);
            BucketInfo[] surgeBuckets = getValues(surgeCities);
            RectTransform[] staticBars = new RectTransform[4];
            RectTransform[] surgeBars = new RectTransform[4];

            for (int i = 0; i < 4; i++)
            {
                staticBars[i] = graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar").GetComponent<RectTransform>();
                surgeBars[i] = graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar").GetComponent<RectTransform>();

                staticBars[i].sizeDelta = new Vector2(staticBars[i].sizeDelta.x, ConvertValueToGraphPosition(staticBuckets[i].percentageWhoGotAnUber));
                surgeBars[i].sizeDelta = new Vector2(surgeBars[i].sizeDelta.x, ConvertValueToGraphPosition(surgeBuckets[i].percentageWhoGotAnUber));

                string formattedStaticValue = formatValue(staticBuckets[i].percentageWhoGotAnUber);
                graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedStaticValue;

                string formattedSurgeValue = formatValue(surgeBuckets[i].percentageWhoGotAnUber);
                graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedSurgeValue;

                string staticSampleSize = $"n = {staticBuckets[i].sampleSize}";
                graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = staticSampleSize;
                string surgeSampleSize = $"n = {surgeBuckets[i].sampleSize}";
                graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = surgeSampleSize;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator FadeIn(float duration)
    {
        float startTime = Time.time;
        float startAlpha = 0;
        float finalAlpha = 1;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float percentage = EaseUtils.EaseInQuadratic(t);
            float alpha = Mathf.Lerp(startAlpha, finalAlpha, percentage);
            canvasGroup.alpha = alpha;
            // Loop through all line renderers and set the alpha value
            foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
            {
                Color color = line.startColor;
                color.a = alpha;
                line.startColor = color;
                line.endColor = color;
            }
            yield return null;
        }
        canvasGroup.alpha = finalAlpha;
    }
    private float ConvertValueToGraphPosition(float value)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * graphHeight / maxValue, 10);
    }
}
