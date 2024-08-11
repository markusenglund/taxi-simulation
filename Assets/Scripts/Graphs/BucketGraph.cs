using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public delegate SimStatistic[] GetBucketGraphValues(City[] cities);
public delegate string FormatBucketGraphValue(float value);

public class BucketGraph : MonoBehaviour
{
    City[] staticCities;
    City[] surgeCities;
    GetBucketGraphValues getValues;
    FormatBucketGraphValue formatValue;
    CanvasGroup canvasGroup;
    Transform[] deltaLabels = new Transform[4];

    private RectTransform graphContainer;

    float minValue = 0;
    float maxValue = 1;

    static Color staticColor = ColorScheme.blue;
    static Color surgeColor = ColorScheme.surgeRed;
    public static BucketGraph Create(City[] staticCities, City[] surgeCities, Vector3 position, string labelText, string axisLabelText, GetBucketGraphValues getValues, FormatBucketGraphValue formatValue, string[] labels, float maxValue)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/BucketGraph");
        Transform bucketGraphTransform = Instantiate(prefab, canvas);
        BucketGraph bucketGraph = bucketGraphTransform.GetComponent<BucketGraph>();
        bucketGraph.staticCities = staticCities;
        bucketGraph.surgeCities = surgeCities;
        bucketGraph.getValues = getValues;
        bucketGraph.formatValue = formatValue;
        bucketGraph.maxValue = maxValue;

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

        bucketGraph.graphContainer.Find("Axis/AxisLabel").GetComponent<TMPro.TMP_Text>().text = axisLabelText;

        // Set labels
        for (int i = 0; i < 4; i++)
        {
            bucketGraph.graphContainer.Find($"BarGroup{i + 1}/Label").GetComponent<TMPro.TMP_Text>().text = labels[i];
            bucketGraph.deltaLabels[i] = bucketGraph.graphContainer.Find($"BarGroup{i + 1}/Delta");
        }


        return bucketGraph;
    }

    private void Start()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(1));
        StartCoroutine(UpdateValueLoop());
    }

    private string FormatDeltaValue(float value)
    {
        string sign = "";
        if (value > 0)
        {
            sign = "+";
        }
        else if (value < 0)
        {
            sign = "-";
        }
        return $"{sign}{formatValue(Mathf.Abs(value))}";
    }

    private string FormatDeltaPercentage(float value)
    {
        return FormatUtils.formatPercentage(value, "0");
    }

    IEnumerator UpdateValueLoop()
    {
        Transform graphContainerTransform = transform.Find("GraphContainer");
        while (true)
        {
            SimStatistic[] staticBuckets = getValues(staticCities);
            SimStatistic[] surgeBuckets = getValues(surgeCities);
            RectTransform[] staticBars = new RectTransform[4];
            RectTransform[] surgeBars = new RectTransform[4];

            for (int i = 0; i < 4; i++)
            {
                staticBars[i] = graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar").GetComponent<RectTransform>();
                surgeBars[i] = graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar").GetComponent<RectTransform>();

                staticBars[i].sizeDelta = new Vector2(staticBars[i].sizeDelta.x, ConvertValueToGraphPosition(staticBuckets[i].value));
                surgeBars[i].sizeDelta = new Vector2(surgeBars[i].sizeDelta.x, ConvertValueToGraphPosition(surgeBuckets[i].value));

                string formattedStaticValue = formatValue(staticBuckets[i].value);
                graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedStaticValue;

                string formattedSurgeValue = formatValue(surgeBuckets[i].value);
                graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedSurgeValue;

                string staticSampleSize = $"n = {staticBuckets[i].sampleSize}";
                graphContainerTransform.Find($"BarGroup{i + 1}/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = staticSampleSize;
                string surgeSampleSize = $"n = {surgeBuckets[i].sampleSize}";
                graphContainerTransform.Find($"BarGroup{i + 1}/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = surgeSampleSize;

                // Set delta label text to the difference between static and surge values
                float delta = surgeBuckets[i].value - staticBuckets[i].value;
                float deltaPercentage = staticBuckets[i].value == 0 ? 0 : delta / staticBuckets[i].value;
                deltaLabels[i].GetComponent<TMPro.TMP_Text>().text = FormatDeltaPercentage(deltaPercentage);
                // deltaLabels[i].GetComponent<TMPro.TMP_Text>().text = FormatDeltaValue(delta);
                // Set the y position of the delta label to be above the surge bar
                RectTransform deltaRectTransform = deltaLabels[i].GetComponent<RectTransform>();
                deltaRectTransform.anchoredPosition = new Vector2(deltaRectTransform.anchoredPosition.x, ConvertValueToGraphPosition(surgeBuckets[i].value) + 120);

                // Set the z-rotation of the delta label arrow based on the delta value
                float rotation = Mathf.Lerp(-135, -45, Mathf.InverseLerp(-0.3f, 0.3f, deltaPercentage));
                deltaLabels[i].Find("Arrow").localRotation = Quaternion.Euler(0, 0, rotation);
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

    public IEnumerator FadeInDeltaLabels(float duration)
    {
        float startFrameCount = Time.frameCount;
        float frameCountDuration = duration * 60;

        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            float percentage = EaseUtils.EaseInQuadratic(t);
            foreach (Transform deltaLabel in deltaLabels)
            {
                deltaLabel.GetComponent<CanvasGroup>().alpha = percentage;
            }
            yield return null;
        }
        foreach (Transform deltaLabel in deltaLabels)
        {
            deltaLabel.GetComponent<CanvasGroup>().alpha = 1;
        }
    }

    public IEnumerator scaleGraph(float toScale, Vector2 toPosition)
    {
        int startFrameCount = Time.frameCount;
        float frameCountDuration = 60 * 1.5f;
        float startScale = this.transform.localScale.x;
        RectTransform rt = transform.GetComponent<RectTransform>();
        Vector2 startPosition = rt.anchoredPosition;
        Vector2 scaledUpPosition = new Vector2(1900, 1080);
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            t = EaseUtils.EaseInOutQuadratic(t);
            float scale = Mathf.Lerp(startScale, toScale, t);
            this.transform.localScale = scale * Vector3.one;
            rt.anchoredPosition = Vector2.Lerp(startPosition, toPosition, t);
            yield return null;
        }
        this.transform.localScale = toScale * Vector3.one;
    }
    private float ConvertValueToGraphPosition(float value)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * graphHeight / maxValue, 10);
    }
}
