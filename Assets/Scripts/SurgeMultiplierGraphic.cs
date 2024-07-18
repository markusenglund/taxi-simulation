using UnityEngine;
using UnityEngine.UI; // Add this line
using TMPro;

public class SurgeMultiplierGraphic : MonoBehaviour
{

    private RectTransform textContainer;
    private TMP_Text text;

    public static SurgeMultiplierGraphic Create(Transform prefab, City city, Vector3 screenPos)
    {
        // Transform canvas = GameObject.Find("WorldSpaceCanvas").transform;
        Transform canvas = city.transform.Find("WorldSpaceCanvas");
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
        Transform background = transform.Find("Background");
        Image backgroundImage = background.GetComponent<Image>();
        // Darken color slightly to make the text more readable
        backgroundImage.color = ColorScheme.surgeRed * 0.9f;
    }
    public void SetNewValue(float surgeMultiplier)
    {
        string surgeMultiplierString = surgeMultiplier.ToString("0.0");
        text.text = $"{surgeMultiplierString}x";
    }
}
