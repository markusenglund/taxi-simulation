using System.Collections;
using TMPro;
using UnityEngine;

public class AgentStatusText : MonoBehaviour
{
    [SerializeField] public Transform statTextPrefab;
    TextMeshProUGUI textMeshPro;
    string text;
    void Start()
    {
        Transform passengerStatsSheet = transform.GetChild(0);
        Transform textComponent = passengerStatsSheet.Find("Text");
        textMeshPro = textComponent.GetComponent<TextMeshProUGUI>();
        textMeshPro.color = Color.red;
        textMeshPro.text = text;
        StartCoroutine(ScheduleActions());
    }

    void Update()
    {
        transform.LookAt(Camera.main.transform);
    }

    private IEnumerator ScheduleActions()
    {
        // StartCoroutine(SpawnCard(duration: 1f));
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator SpawnCard(float duration)
    {
        Vector3 startScale = new Vector3(0.0005f, 0f, 0.001f);
        Vector3 finalScale = Vector3.one * 0.001f;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        transform.localScale = finalScale;
    }

    // private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    // {
    //     float startTime = Time.time;
    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         text.color = new Color(text.color.r, text.color.g, text.color.b, t);
    //         yield return null;
    //     }
    //     text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
    // }


    // private IEnumerator InstantiateStat(Transform passengerStatsSheet, Stat stat, int index, float duration)
    // {
    //     Transform statText = Instantiate(statTextPrefab, passengerStatsSheet, true);
    //     statText.localPosition = new Vector3(0, -4 - index * 20, 0);
    //     Transform statName = statText.Find("StatName");
    //     TextMeshProUGUI statNameText = statName.GetComponent<TextMeshProUGUI>();
    //     statNameText.text = stat.name;
    //     Transform statValue = statText.Find("StatValue");
    //     TextMeshProUGUI statValueText = statValue.GetComponent<TextMeshProUGUI>();
    //     statValueText.text = stat.value;
    //     Transform barValue = statText.Find("Bar").Find("BarValue");
    //     RectTransform barValueRect = barValue.GetComponent<RectTransform>();
    //     barValueRect.sizeDelta = new Vector2(stat.barValue, 1.5f);
    //     // statTexts.Add(statNameText)

    //     CanvasGroup canvasGroup = statText.GetComponent<CanvasGroup>();
    //     float startTime = Time.time;
    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         canvasGroup.alpha = t;
    //         yield return null;
    //     }

    // }

    public static AgentStatusText Create(Transform prefab, Transform parent, Vector3 position, string text)
    {
        Transform agentStatusText = Instantiate(prefab, parent, false);
        AgentStatusText agentStatusTextComponent = agentStatusText.GetComponent<AgentStatusText>();
        agentStatusTextComponent.text = text;
        // agentStatusText.localScale = Vector3.zero;
        return agentStatusTextComponent;
    }
}

