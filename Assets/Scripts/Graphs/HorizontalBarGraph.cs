using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate int GetHorizontalBarValue(City city);

public class HorizontalBarGraph : MonoBehaviour
{
    City staticCity;
    City surgeCity;

    GetHorizontalBarValue getValue;

    CanvasGroup canvasGroup;

    Transform graphContainer;

    float maxWidth;

    public static HorizontalBarGraph Create(City staticCity, City surgeCity, Vector3 position, string labelText, GetHorizontalBarValue getValue)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("Graphs/HorizontalBarGraph");
        Transform horizontalBarGraphTransform = Instantiate(prefab, canvas);
        HorizontalBarGraph horizontalBarGraph = horizontalBarGraphTransform.GetComponent<HorizontalBarGraph>();
        horizontalBarGraph.staticCity = staticCity;
        horizontalBarGraph.surgeCity = surgeCity;
        horizontalBarGraph.getValue = getValue;

        RectTransform rt = horizontalBarGraphTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        horizontalBarGraphTransform.Find("MainLabel").GetComponent<TMPro.TMP_Text>().text = labelText;

        horizontalBarGraphTransform.Find("GraphContainer/BarGroup1/StaticBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.blue;
        horizontalBarGraphTransform.Find("GraphContainer/BarGroup1/SurgeBar").GetComponent<UnityEngine.UI.Image>().color = ColorScheme.surgeRed;

        float barGroupWidth = horizontalBarGraphTransform.Find("GraphContainer/BarGroup1").GetComponent<RectTransform>().rect.width;
        horizontalBarGraph.maxWidth = barGroupWidth;
        return horizontalBarGraph;
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
            float staticValue = getValue(staticCity);
            float surgeValue = getValue(surgeCity);

            RectTransform staticBar = graphContainer.Find("BarGroup1/StaticBar").GetComponent<RectTransform>();
            RectTransform surgeBar = graphContainer.Find("BarGroup1/SurgeBar").GetComponent<RectTransform>();
            float staticBarWidth = ConvertValueToGraphPosition(staticValue);
            float surgeBarWidth = ConvertValueToGraphPosition(surgeValue);
            staticBar.sizeDelta = new Vector2(staticBarWidth, staticBar.sizeDelta.y);
            surgeBar.sizeDelta = new Vector2(surgeBarWidth, surgeBar.sizeDelta.y);

            graphContainer.Find("BarGroup1/StaticBar/Value").GetComponent<TMPro.TMP_Text>().text = staticValue.ToString();
            graphContainer.Find("BarGroup1/SurgeBar/Value").GetComponent<TMPro.TMP_Text>().text = surgeValue.ToString();

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
        // 10 is the minimum width of the bar so it can still be seen when the value is zero
        float maxValue = 50;
        return Mathf.Max(value * maxWidth / maxValue, 10);
    }
}
