using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class DriverUberGraph : MonoBehaviour
{
    City[] staticCities;
    City[] surgeCities;
    GetStatistic getDriverIncome;
    GetStatistic getUberIncome;
    FormatValue formatValue;
    CanvasGroup canvasGroup;

    Transform[] deltaLabels = new Transform[2];


    private RectTransform graphContainer;

    float minValue = 0;
    float maxValue = 30000;
    public static DriverUberGraph Create(City[] staticCities, City[] surgeCities, Vector3 position, string labelText, GetStatistic getUberIncome, GetStatistic getDriverIncome, FormatValue formatValue)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/DriverUberGraph");
        Transform driverUberGraphTransform = Instantiate(prefab, canvas);
        DriverUberGraph driverUberGraph = driverUberGraphTransform.GetComponent<DriverUberGraph>();
        driverUberGraph.staticCities = staticCities;
        driverUberGraph.surgeCities = surgeCities;
        driverUberGraph.getDriverIncome = getDriverIncome;
        driverUberGraph.getUberIncome = getUberIncome;
        driverUberGraph.formatValue = formatValue;

        RectTransform rt = driverUberGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        driverUberGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;
        driverUberGraph.graphContainer = driverUberGraphTransform.Find("GraphContainer").GetComponent<RectTransform>();

        driverUberGraph.graphContainer.Find("BarGroup1/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        driverUberGraph.graphContainer.Find("BarGroup1/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;
        driverUberGraph.graphContainer.Find("BarGroup2/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        driverUberGraph.graphContainer.Find("BarGroup2/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;

        for (int i = 0; i < 2; i++)
        {
            driverUberGraph.deltaLabels[i] = driverUberGraph.graphContainer.Find($"BarGroup{i + 1}/Delta");
        }

        // driverUberGraph.graphContainer.Find($"BarGroup1/Label").GetComponent<TMPro.TMP_Text>().text = "Driver Income";
        // driverUberGraph.graphContainer.Find($"BarGroup2/Label").GetComponent<TMPro.TMP_Text>().text = "Uber Income";


        return driverUberGraph;
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
            SimStatistic driverIncomeStatic = getDriverIncome(staticCities);
            SimStatistic driverIncomeSurge = getDriverIncome(surgeCities);

            RectTransform staticBar = graphContainerTransform.Find($"BarGroup1/StaticBar").GetComponent<RectTransform>();
            RectTransform surgeBar = graphContainerTransform.Find($"BarGroup1/SurgeBar").GetComponent<RectTransform>();

            staticBar.sizeDelta = new Vector2(staticBar.sizeDelta.x, ConvertValueToGraphPosition(driverIncomeStatic.value));
            surgeBar.sizeDelta = new Vector2(surgeBar.sizeDelta.x, ConvertValueToGraphPosition(driverIncomeSurge.value));

            string formattedStaticValue = formatValue(driverIncomeStatic.value);
            graphContainerTransform.Find($"BarGroup1/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedStaticValue;

            string formattedSurgeValue = formatValue(driverIncomeSurge.value);
            graphContainerTransform.Find($"BarGroup1/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedSurgeValue;

            // string staticSampleSize = $"n = {driverIncomeStatic.sampleSize}";
            // graphContainerTransform.Find($"BarGroup1/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = staticSampleSize;
            // string surgeSampleSize = $"n = {driverIncomeSurge.sampleSize}";
            // graphContainerTransform.Find($"BarGroup1/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = surgeSampleSize;



            SimStatistic uberIncomeStatic = getUberIncome(staticCities);
            SimStatistic uberIncomeSurge = getUberIncome(surgeCities);

            RectTransform uberStaticBar = graphContainerTransform.Find($"BarGroup2/StaticBar").GetComponent<RectTransform>();
            RectTransform uberSurgeBar = graphContainerTransform.Find($"BarGroup2/SurgeBar").GetComponent<RectTransform>();

            uberStaticBar.sizeDelta = new Vector2(uberStaticBar.sizeDelta.x, ConvertValueToGraphPosition(uberIncomeStatic.value));
            uberSurgeBar.sizeDelta = new Vector2(uberSurgeBar.sizeDelta.x, ConvertValueToGraphPosition(uberIncomeSurge.value));

            string formattedUberStaticValue = formatValue(uberIncomeStatic.value);
            graphContainerTransform.Find($"BarGroup2/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedUberStaticValue;

            string formattedUberSurgeValue = formatValue(uberIncomeSurge.value);
            graphContainerTransform.Find($"BarGroup2/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedUberSurgeValue;

            float driverDelta = driverIncomeSurge.value - driverIncomeStatic.value;
            float driverDeltaPercentage = driverIncomeStatic.value == 0 ? 0 : driverDelta / driverIncomeStatic.value;
            float uberDelta = uberIncomeSurge.value - uberIncomeStatic.value;
            float uberDeltaPercentage = uberIncomeStatic.value == 0 ? 0 : uberDelta / uberIncomeStatic.value;
            deltaLabels[0].GetComponent<TMPro.TMP_Text>().text = FormatUtils.formatPercentage(driverDeltaPercentage, "0");
            deltaLabels[1].GetComponent<TMPro.TMP_Text>().text = FormatUtils.formatPercentage(uberDeltaPercentage, "0");

            RectTransform driverDeltaRectTransform = deltaLabels[0].GetComponent<RectTransform>();
            driverDeltaRectTransform.anchoredPosition = new Vector2(driverDeltaRectTransform.anchoredPosition.x, ConvertValueToGraphPosition(driverIncomeSurge.value) + 170);

            // Set the z-rotation of the delta label arrow based on the delta value
            float rotation = Mathf.Lerp(-135, -45, Mathf.InverseLerp(-200, 200, driverDelta));
            deltaLabels[0].Find("Arrow").localRotation = Quaternion.Euler(0, 0, rotation);


            RectTransform uberDeltaRectTransform = deltaLabels[1].GetComponent<RectTransform>();
            uberDeltaRectTransform.anchoredPosition = new Vector2(uberDeltaRectTransform.anchoredPosition.x, ConvertValueToGraphPosition(uberIncomeSurge.value) + 170);

            // Set the z-rotation of the delta label arrow based on the delta value
            float uberRotation = Mathf.Lerp(-135, -45, Mathf.InverseLerp(-0.3f, 0.3f, uberDeltaPercentage));
            deltaLabels[1].Find("Arrow").localRotation = Quaternion.Euler(0, 0, uberRotation);


            // string uberStaticSampleSize = $"n = {uberIncomeStatic.sampleSize}";
            // graphContainerTransform.Find($"BarGroup2/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = uberStaticSampleSize;
            // string uberSurgeSampleSize = $"n = {uberIncomeSurge.sampleSize}";
            // graphContainerTransform.Find($"BarGroup2/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = uberSurgeSampleSize;
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
            float t = (Time.frameCount - startFrameCount) / duration;
            float percentage = EaseUtils.EaseInQuadratic(t);
            foreach (Transform deltaLabel in deltaLabels)
            {
                deltaLabel.GetComponent<CanvasGroup>().alpha = t;
            }
            yield return null;
        }
        foreach (Transform deltaLabel in deltaLabels)
        {
            deltaLabel.GetComponent<CanvasGroup>().alpha = 1;
        }
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

    private float ConvertValueToGraphPosition(float value)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * graphHeight / maxValue, 10);
    }
}
