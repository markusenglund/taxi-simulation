using UnityEngine;
using TMPro;

public class SurgeMultiplierGraphic : MonoBehaviour
{

    private RectTransform textContainer;

    [SerializeField] private TMP_Text textPrefab;

    TMP_Text text;

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
        InstantiateText();
    }

    private void InstantiateText()
    {
        text = Instantiate(textPrefab, textContainer);
        text.text = "";
        text.fontSize = 20;
        text.rectTransform.anchoredPosition = new Vector2(0, 0);
        text.alignment = TextAlignmentOptions.Center;
    }

    public void SetNewValue(float surgeMultiplier)
    {
        string surgeMultiplierString = surgeMultiplier.ToString("0.0");
        text.text = $"{surgeMultiplierString}x";
    }
}
