using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AgentOverheadReaction : MonoBehaviour
{
    // [SerializeField] public Transform statTextPrefab;
    TextMeshProUGUI textMeshPro;
    Color color;
    string text;
    bool isBold;

    bool addPadding;

    float durationBeforeFade = 0.8f;
    CanvasGroup canvasGroup;
    void Start()
    {
        Transform textContainer = transform.GetChild(0);
        canvasGroup = transform.GetComponent<CanvasGroup>();
        Transform textComponent = textContainer.Find("Text");
        textMeshPro = textComponent.GetComponent<TextMeshProUGUI>();
        textMeshPro.color = color;
        textMeshPro.text = text;
        textMeshPro.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        if (addPadding)
        {
            HorizontalLayoutGroup horizontalLayoutGroup = textContainer.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.padding = new RectOffset(30, 30, 0, 0);
        }

        StartCoroutine(ScheduleActions());
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(-Camera.main.transform.rotation.eulerAngles.x / 2, Camera.main.transform.rotation.eulerAngles.y + 180, 0);
    }

    private IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnCard(duration: 0.3f));
        yield return new WaitForSeconds(durationBeforeFade);
        yield return StartCoroutine(Fade(duration: 1.3f));
    }

    private IEnumerator SpawnCard(float duration)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 finalScale = transform.localScale;
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

    private IEnumerator Fade(float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = 1 - t;
            yield return null;
        }
        Destroy(this.gameObject);
    }

    public static AgentOverheadReaction Create(Transform parent, Vector3 positionOffset, string text, Color color, bool isBold = false, float durationBeforeFade = 0.8f, bool addPadding = false)
    {
        Transform statTextPrefab = Resources.Load<Transform>("AgentReaction");
        Transform agentStatusText = Instantiate(statTextPrefab, parent.position + positionOffset, Quaternion.identity, parent);
        AgentOverheadReaction agentStatusTextComponent = agentStatusText.GetComponent<AgentOverheadReaction>();
        agentStatusTextComponent.text = text;
        agentStatusTextComponent.color = color;
        agentStatusTextComponent.durationBeforeFade = durationBeforeFade;
        // Set sort order to 1
        agentStatusTextComponent.GetComponent<Canvas>().sortingOrder = 1;
        // Set font style
        agentStatusTextComponent.isBold = isBold;
        agentStatusTextComponent.addPadding = addPadding;
        return agentStatusTextComponent;
    }
}

