using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public delegate float GetValue(City city);
public delegate string FormatValue(float value);

public class InfoBox : MonoBehaviour
{
    City city;
    GetValue getValue;
    FormatValue formatValue;
    public static InfoBox Create(City city, Vector3 position, string labelText, GetValue getValue, FormatValue formatValue, Color color)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform prefab = Resources.Load<Transform>("InfoBox");
        Transform infoBoxTransform = Instantiate(prefab, canvas);
        InfoBox infoBox = infoBoxTransform.GetComponent<InfoBox>();
        infoBox.city = city;
        infoBox.getValue = getValue;
        infoBox.formatValue = formatValue;

        // Set position
        RectTransform rt = infoBoxTransform.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        // Set label text
        infoBoxTransform.Find("Label").GetComponent<TMPro.TMP_Text>().text = labelText;

        // Set color of the text
        infoBoxTransform.Find("Value").GetComponent<TMPro.TMP_Text>().color = color;

        return infoBox;
    }

    private void Start()
    {
        StartCoroutine(UpdateValueLoop());
    }

    IEnumerator UpdateValueLoop()
    {
        while (true)
        {
            float value = getValue(city);
            string formattedValue = formatValue(value);
            transform.Find("Value").GetComponent<TMPro.TMP_Text>().text = formattedValue;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
