using System.Collections;
using TMPro;
using UnityEngine;

public class AgentOverheadText : MonoBehaviour
{
    [SerializeField] public Transform statTextPrefab;
    TextMeshProUGUI textMeshPro;
    Color color;
    string text;
    void Start()
    {
        Transform passengerStatsSheet = transform.GetChild(0);
        Transform textComponent = passengerStatsSheet.Find("Text");
        textMeshPro = textComponent.GetComponent<TextMeshProUGUI>();
        textMeshPro.color = color;
        textMeshPro.text = text;
        StartCoroutine(ScheduleActions());
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(-Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y + 180, Camera.main.transform.rotation.eulerAngles.z);
    }

    private IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnCard(duration: 0.3f));
        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(FadeIntoTheSky(duration: 1.3f));
    }

    private IEnumerator SpawnCard(float duration)
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        float distanceToCamera = Vector3.Distance(cameraPosition, transform.position);
        Vector3 startScale = Vector3.zero;
        Vector3 finalScale = transform.localScale * (0.5f + (distanceToCamera / 30f));
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        transform.localScale = finalScale;
        Debug.Log($"Distance to camera: {distanceToCamera}, scale: {transform.localScale}");
    }

    private IEnumerator FadeIntoTheSky(float duration)
    {
        float startTime = Time.time;
        Vector3 startPosition = transform.localPosition;
        Vector3 finalPosition = transform.localPosition + Vector3.up * 0.6f;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            textMeshPro.color = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, 1 - t);
            transform.localPosition = Vector3.Lerp(startPosition, finalPosition, EaseUtils.EaseInCubic(t));
            yield return null;
        }
        Destroy(this.gameObject);
    }

    public static AgentOverheadText Create(Transform prefab, Transform parent, Vector3 positionOffset, string text, Color color)
    {
        Transform agentStatusText = Instantiate(prefab, parent.position + positionOffset, Quaternion.identity);
        AgentOverheadText agentStatusTextComponent = agentStatusText.GetComponent<AgentOverheadText>();
        agentStatusTextComponent.text = text;
        agentStatusTextComponent.color = color;
        return agentStatusTextComponent;
    }
}

