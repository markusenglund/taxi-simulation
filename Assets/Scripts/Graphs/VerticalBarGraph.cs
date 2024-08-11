using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate SimStatistic GetVerticalBarValue(City[] cities);

public class VerticalBarGraph : MonoBehaviour
{
    City[] staticCities;
    City[] surgeCities;

    GetVerticalBarValue getValue;
    FormatValue formatValue;

    CanvasGroup canvasGroup;

    Transform graphContainer;

    float maxHeight;
    float maxValue;

    public static VerticalBarGraph Create(City[] staticCities, City[] surgeCities, Vector3 position, string labelText, string axisLabelText, GetVerticalBarValue getValue, FormatValue formatValue, float maxValue = 1, string[] tickLabels = null)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/VerticalBarGraph");
        Transform verticalBarGraphTransform = Instantiate(prefab, canvas);
        VerticalBarGraph verticalBarGraph = verticalBarGraphTransform.GetComponent<VerticalBarGraph>();
        verticalBarGraph.staticCities = staticCities;
        verticalBarGraph.surgeCities = surgeCities;
        verticalBarGraph.getValue = getValue;
        verticalBarGraph.formatValue = formatValue;
        verticalBarGraph.maxValue = maxValue;

        RectTransform rt = verticalBarGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        verticalBarGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;

        verticalBarGraphTransform.Find("GraphContainer/BarGroup1/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        verticalBarGraphTransform.Find("GraphContainer/BarGroup1/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;

        float barGroupHeight = verticalBarGraphTransform.Find("GraphContainer/BarGroup1").GetComponent<RectTransform>().rect.height;
        verticalBarGraph.maxHeight = barGroupHeight;

        if (tickLabels != null)
        {
            for (int i = 0; i < tickLabels.Length; i++)
            {
                Transform tickLabel = verticalBarGraphTransform.Find($"GraphContainer/Axis/TickLabel{i + 1}");
                tickLabel.GetComponent<TMPro.TMP_Text>().text = tickLabels[i];
            }
        }
        verticalBarGraphTransform.Find("GraphContainer/Axis/AxisLabel").GetComponent<TMPro.TMP_Text>().text = axisLabelText;

        return verticalBarGraph;
    }
    private void Start()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(1));
        StartCoroutine(UpdateValueLoop());
    }


    IEnumerator UpdateValueLoop()
    {
        graphContainer = transform.Find("GraphContainer");
        while (true)
        {
            SimStatistic staticStatistic = getValue(staticCities);
            SimStatistic surgeStatistic = getValue(surgeCities);
            float staticValue = staticStatistic.value;
            float surgeValue = surgeStatistic.value;
            int sampleSize = staticStatistic.sampleSize;

            RectTransform staticBar = graphContainer.Find("BarGroup1/StaticBar").GetComponent<RectTransform>();
            RectTransform surgeBar = graphContainer.Find("BarGroup1/SurgeBar").GetComponent<RectTransform>();
            float staticBarHeight = ConvertValueToGraphPosition(staticValue);
            float surgeBarHeight = ConvertValueToGraphPosition(surgeValue);
            staticBar.sizeDelta = new Vector2(staticBar.sizeDelta.x, staticBarHeight);
            surgeBar.sizeDelta = new Vector2(surgeBar.sizeDelta.x, surgeBarHeight);

            graphContainer.Find("BarGroup1/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = formatValue(staticValue);
            graphContainer.Find("BarGroup1/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = formatValue(surgeValue);
            transform.Find("SampleSizeLabel").GetComponent<TMPro.TMP_Text>().text = $"Sample Size: {sampleSize}";
            yield return new WaitForSeconds(0.1f);
        }
    }


    private IEnumerator FadeIn(float duration)
    {
        int startFrameCount = Time.frameCount;
        float frameCountDuration = duration * 60;
        float startAlpha = 0;
        float finalAlpha = 1;
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
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
        // 10 is the minimum height of the bar so it can still be seen when the value is zero
        return Mathf.Max(value * maxHeight / maxValue, 10);
    }
}
