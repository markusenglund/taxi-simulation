using UnityEngine;
using TMPro;

public class SurgeMultiplierGraphic : MonoBehaviour
{

    private RectTransform textContainer;

    [SerializeField] private TMP_Text textPrefab;

    TMP_Text text;

    // Start is called before the first frame update
    void Awake()
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
