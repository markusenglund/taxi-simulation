using UnityEngine;
using TMPro;

public class SurgeMultiplierGraphic : MonoBehaviour
{

    private RectTransform textContainer;
    private TMP_Text text;

    public static SurgeMultiplierGraphic Create(Transform prefab, Vector3 screenPos)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform transform = Instantiate(prefab, canvas);

        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = screenPos;
        SurgeMultiplierGraphic surgeMultiplierGraphic = transform.GetComponent<SurgeMultiplierGraphic>();
        return surgeMultiplierGraphic;
    }

    void Start()
    {
        textContainer = transform.Find("TextContainer").GetComponent<RectTransform>();

        text = textContainer.Find("Text").GetComponent<TMP_Text>();
    }
    public void SetNewValue(float surgeMultiplier)
    {
        string surgeMultiplierString = surgeMultiplier.ToString("0.0");
        text.text = $"{surgeMultiplierString}x";
    }
}
