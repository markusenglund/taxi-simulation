using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class SurplusGraph : MonoBehaviour
{
    City[] staticCities;
    City[] surgeCities;
    GetStatistic getRichestSurplus;
    GetStatistic getPoorestSurplus;
    FormatValue formatValue;
    CanvasGroup canvasGroup;

    private RectTransform graphContainer;

    float minValue = 0;
    float maxValue = 20000;
    public static SurplusGraph Create(City[] staticCities, City[] surgeCities, Vector3 position, string labelText, GetStatistic getRichestSurplus, GetStatistic getPoorestSurplus, FormatValue formatValue)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/SurplusGraph");
        Transform surplusGraphTransform = Instantiate(prefab, canvas);
        SurplusGraph surplusGraph = surplusGraphTransform.GetComponent<SurplusGraph>();
        surplusGraph.staticCities = staticCities;
        surplusGraph.surgeCities = surgeCities;
        surplusGraph.getRichestSurplus = getRichestSurplus;
        surplusGraph.getPoorestSurplus = getPoorestSurplus;
        surplusGraph.formatValue = formatValue;

        RectTransform rt = surplusGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        surplusGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;
        surplusGraph.graphContainer = surplusGraphTransform.Find("GraphContainer").GetComponent<RectTransform>();

        surplusGraph.graphContainer.Find("BarGroup1/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        surplusGraph.graphContainer.Find("BarGroup1/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;
        surplusGraph.graphContainer.Find("BarGroup2/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        surplusGraph.graphContainer.Find("BarGroup2/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;


        surplusGraph.graphContainer.Find($"BarGroup1/Label").GetComponent<TMPro.TMP_Text>().text = "Surplus";


        return surplusGraph;
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
            SimStatistic richestStaticSurplus = getRichestSurplus(staticCities);
            SimStatistic RichestSurgeSurplus = getRichestSurplus(surgeCities);

            RectTransform staticBar = graphContainerTransform.Find($"BarGroup1/StaticBar").GetComponent<RectTransform>();
            RectTransform surgeBar = graphContainerTransform.Find($"BarGroup1/SurgeBar").GetComponent<RectTransform>();

            staticBar.sizeDelta = new Vector2(staticBar.sizeDelta.x, ConvertValueToGraphPosition(richestStaticSurplus.value));
            surgeBar.sizeDelta = new Vector2(surgeBar.sizeDelta.x, ConvertValueToGraphPosition(RichestSurgeSurplus.value));

            string formattedStaticValue = formatValue(richestStaticSurplus.value);
            graphContainerTransform.Find($"BarGroup1/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedStaticValue;

            string formattedSurgeValue = formatValue(RichestSurgeSurplus.value);
            graphContainerTransform.Find($"BarGroup1/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedSurgeValue;

            string staticSampleSize = $"n = {richestStaticSurplus.sampleSize}";
            graphContainerTransform.Find($"BarGroup1/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = staticSampleSize;
            string surgeSampleSize = $"n = {RichestSurgeSurplus.sampleSize}";
            graphContainerTransform.Find($"BarGroup1/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = surgeSampleSize;



            SimStatistic poorestStaticSurplus = getPoorestSurplus(staticCities);
            SimStatistic poorestSurgeSurplus = getPoorestSurplus(surgeCities);

            RectTransform poorestStaticBar = graphContainerTransform.Find($"BarGroup2/StaticBar").GetComponent<RectTransform>();
            RectTransform poorestSurgeBar = graphContainerTransform.Find($"BarGroup2/SurgeBar").GetComponent<RectTransform>();

            poorestStaticBar.sizeDelta = new Vector2(poorestStaticBar.sizeDelta.x, ConvertValueToGraphPosition(poorestStaticSurplus.value));
            poorestSurgeBar.sizeDelta = new Vector2(poorestSurgeBar.sizeDelta.x, ConvertValueToGraphPosition(poorestSurgeSurplus.value));

            string formattedPoorestStaticValue = formatValue(poorestStaticSurplus.value);
            graphContainerTransform.Find($"BarGroup2/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedPoorestStaticValue;

            string formattedPoorestSurgeValue = formatValue(poorestSurgeSurplus.value);
            graphContainerTransform.Find($"BarGroup2/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formattedPoorestSurgeValue;

            string poorestStaticSampleSize = $"n = {poorestStaticSurplus.sampleSize}";
            graphContainerTransform.Find($"BarGroup2/StaticBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = poorestStaticSampleSize;
            string richestSurgeSampleSize = $"n = {poorestSurgeSurplus.sampleSize}";
            graphContainerTransform.Find($"BarGroup2/SurgeBar/SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = richestSurgeSampleSize;
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
