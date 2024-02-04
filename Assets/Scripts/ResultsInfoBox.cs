using UnityEngine;
using TMPro;
using System.Collections;

public class ResultsInfoBox : MonoBehaviour
{

    private RectTransform textContainer;

    [SerializeField] private TMP_Text textPrefab;
    const float timeInterval = 15f / 60f;

    TMP_Text driverGrossProfitText;
    TMP_Text driverSurplusValueText;
    TMP_Text uberRevenueText;

    // Start is called before the first frame update
    void Awake()
    {
        textContainer = transform.Find("TextContainer").GetComponent<RectTransform>();
        InstantiateText();
        StartCoroutine(UpdateValues());
    }

    IEnumerator UpdateValues()
    {
        while (true)
        {
            float intervalRealSeconds = TimeUtils.ConvertSimulationHoursToRealSeconds(timeInterval);
            yield return new WaitForSeconds(intervalRealSeconds);
            UpdateDriverProfitability();
        }
    }

    private void UpdateDriverProfitability()
    {
        (float grossProfitLastHour, float surplusValueLastHour, float totalGrossProfit, float totalSurplusValue, float totalUberRevenue) = DriverPool.CalculateAverageGrossProfitInInterval(SimulationSettings.simulationLengthHours);

        driverGrossProfitText.text = $"Avg hourly gross profit: ${grossProfitLastHour:0.00}, total: ${totalGrossProfit:0.00}";
        driverSurplusValueText.text = $"Avg hourly surplus: ${surplusValueLastHour:0.00}, total: ${totalSurplusValue:0.00}";

        uberRevenueText.text = $"Total Uber revenue: ${totalUberRevenue:0.00}";
    }

    private void InstantiateText()
    {
        driverGrossProfitText = Instantiate(textPrefab, textContainer);
        driverGrossProfitText.text = "Avg hourly gross profit: $0.00";
        driverGrossProfitText.fontSize = 20;
        driverGrossProfitText.rectTransform.anchoredPosition = new Vector2(0, 0);
        driverGrossProfitText.alignment = TextAlignmentOptions.Center;
        // Set the width to be the same as the parent container
        driverGrossProfitText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, driverGrossProfitText.rectTransform.sizeDelta.y);

        driverSurplusValueText = Instantiate(textPrefab, textContainer);
        driverSurplusValueText.text = "Avg hourly surplus: $0.00";
        driverSurplusValueText.fontSize = 20;
        driverSurplusValueText.rectTransform.anchoredPosition = new Vector2(0, -30);
        driverSurplusValueText.alignment = TextAlignmentOptions.Center;
        driverSurplusValueText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, driverSurplusValueText.rectTransform.sizeDelta.y);


        uberRevenueText = Instantiate(textPrefab, textContainer);
        uberRevenueText.text = "Total Uber revenue: $0.00";
        uberRevenueText.fontSize = 20;
        uberRevenueText.rectTransform.anchoredPosition = new Vector2(0, -60);
        uberRevenueText.alignment = TextAlignmentOptions.Center;
        uberRevenueText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, uberRevenueText.rectTransform.sizeDelta.y);
    }

}
