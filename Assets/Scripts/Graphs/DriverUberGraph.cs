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
    private float ConvertValueToGraphPosition(float value)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * graphHeight / maxValue, 10);
    }
}
