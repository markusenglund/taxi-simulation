using System.Collections;
using TMPro;
using UnityEngine;

public class AgentOverheadReaction : MonoBehaviour
{
    // [SerializeField] public Transform statTextPrefab;
    TextMeshProUGUI textMeshPro;
    Color color;
    string text;
    bool isBold;
    void Start()
    {
        Transform passengerStatsSheet = transform.GetChild(0);
        Transform textComponent = passengerStatsSheet.Find("Text");
        textMeshPro = textComponent.GetComponent<TextMeshProUGUI>();
        textMeshPro.color = color;
        textMeshPro.text = text;
        textMeshPro.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        StartCoroutine(ScheduleActions());
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(-Camera.main.transform.rotation.eulerAngles.x / 2, Camera.main.transform.rotation.eulerAngles.y + 180, 0);
    }

    private IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnCard(duration: 0.3f));
        yield return new WaitForSeconds(0.8f);
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
            textMeshPro.color = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, 1 - t);
            yield return null;
        }
        Destroy(this.gameObject);
    }

    public static AgentOverheadReaction Create(Transform parent, Vector3 positionOffset, string text, Color color, bool isBold = false)
    {
        Transform statTextPrefab = Resources.Load<Transform>("AgentReaction");
        Transform agentStatusText = Instantiate(statTextPrefab, parent.position + positionOffset, Quaternion.identity, parent);
        AgentOverheadReaction agentStatusTextComponent = agentStatusText.GetComponent<AgentOverheadReaction>();
        agentStatusTextComponent.text = text;
        agentStatusTextComponent.color = color;
        // Set font style
        agentStatusTextComponent.isBold = isBold;
        return agentStatusTextComponent;
    }
}

