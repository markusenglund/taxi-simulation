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

    TMP_Text passengerSurplusValueText;
    TMP_Text passengerSurplusQuartileText;
    TMP_Text totalSurplusText;

    // Start is called before the first frame update
    void Awake()
    {
        textContainer = transform.Find("TextContainer").GetComponent<RectTransform>();
        Invoke("InstantiateText", 0.1f);
        StartCoroutine(UpdateValues());
    }

    IEnumerator UpdateValues()
    {
        while (true)
        {
            float intervalRealSeconds = TimeUtils.ConvertSimulationHoursToRealSeconds(timeInterval);
            yield return new WaitForSeconds(intervalRealSeconds);
            UpdateSurplusValues();
        }
    }

    private string GetTextColor(float value)
    {
        if (value == 0)
        {
            return "white";
        }
        else if (value < 0)
        {
            return "red";
        }
        else
        {
            return "green";
        }
    }
    private void UpdateSurplusValues()
    {
        (float grossProfitLastHour, float surplusValueLastHour, float totalDriverGrossProfit, float totalDriverSurplusValue, float totalUberRevenue) = DriverPool.CalculateAverageGrossProfitInInterval(SimulationSettings.simulationLengthHours);

        driverGrossProfitText.text = $"Avg hourly gross profit: <color={GetTextColor(grossProfitLastHour)}><b>${grossProfitLastHour:0.00}</b></color>, total: <color={GetTextColor(totalDriverGrossProfit)}><b>${totalDriverGrossProfit:0.00}</b></color>";
        driverSurplusValueText.text = $"Avg hourly surplus: <color={GetTextColor(surplusValueLastHour)}><b>${surplusValueLastHour:0.00}</b></color>, total: <color={GetTextColor(totalDriverSurplusValue)}><b>${totalDriverSurplusValue:0.00}</b></color>";

        uberRevenueText.text = $"Total Uber revenue: <color={GetTextColor(totalUberRevenue)}><b>${totalUberRevenue:0.00}</b></color>";

        (float totalRiderUtilitySurplus, float totalRiderUtilitySurplusPerCapita, int numRiders, float[] quartiledUtilitySurplusPerCapita, int[] quartiledPopulation) = GameManager.Instance.CalculatePassengerUtilitySurplusData();

        passengerSurplusValueText.text = $"Rider surplus per ride: <color={GetTextColor(totalRiderUtilitySurplusPerCapita)}><b>${totalRiderUtilitySurplusPerCapita:0.00}</b></color>, total: <color={GetTextColor(totalRiderUtilitySurplus)}><b>${totalRiderUtilitySurplus:0.00}</b></color>";

        passengerSurplusQuartileText.text = $"Quartiles: <color={GetTextColor(quartiledUtilitySurplusPerCapita[0])}><b>${quartiledUtilitySurplusPerCapita[0]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[1])}><b>${quartiledUtilitySurplusPerCapita[1]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[2])}><b>${quartiledUtilitySurplusPerCapita[2]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[3])}><b>${quartiledUtilitySurplusPerCapita[3]:0.00}</b></color>";

        float totalSurplusValue = totalDriverSurplusValue + totalUberRevenue + totalRiderUtilitySurplus;
        totalSurplusText.text = $"Total market surplus: <color={GetTextColor(totalSurplusValue)}><b>${totalSurplusValue:0.00}</b></color>";
    }


    private void InstantiateText()
    {
        driverGrossProfitText = Instantiate(textPrefab, textContainer);
        driverGrossProfitText.fontSize = 16;
        driverGrossProfitText.rectTransform.anchoredPosition = new Vector2(5, 100);
        driverGrossProfitText.alignment = TextAlignmentOptions.Left;
        // Set the width to be the same as the parent container
        driverGrossProfitText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, driverGrossProfitText.rectTransform.sizeDelta.y);

        driverSurplusValueText = Instantiate(textPrefab, textContainer);
        driverSurplusValueText.fontSize = 16;
        driverSurplusValueText.rectTransform.anchoredPosition = new Vector2(5, 80);
        driverSurplusValueText.alignment = TextAlignmentOptions.Left;
        driverSurplusValueText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, driverSurplusValueText.rectTransform.sizeDelta.y);


        uberRevenueText = Instantiate(textPrefab, textContainer);
        uberRevenueText.fontSize = 16;
        uberRevenueText.rectTransform.anchoredPosition = new Vector2(5, 60);
        uberRevenueText.alignment = TextAlignmentOptions.Left;
        uberRevenueText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, uberRevenueText.rectTransform.sizeDelta.y);

        passengerSurplusValueText = Instantiate(textPrefab, textContainer);
        passengerSurplusValueText.fontSize = 16;
        passengerSurplusValueText.rectTransform.anchoredPosition = new Vector2(5, 40);
        passengerSurplusValueText.alignment = TextAlignmentOptions.Left;
        passengerSurplusValueText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, passengerSurplusValueText.rectTransform.sizeDelta.y);

        passengerSurplusQuartileText = Instantiate(textPrefab, textContainer);
        passengerSurplusQuartileText.fontSize = 16;
        passengerSurplusQuartileText.rectTransform.anchoredPosition = new Vector2(5, 20);
        passengerSurplusQuartileText.alignment = TextAlignmentOptions.Left;
        passengerSurplusQuartileText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, passengerSurplusQuartileText.rectTransform.sizeDelta.y);

        totalSurplusText = Instantiate(textPrefab, textContainer);
        totalSurplusText.fontSize = 16;
        totalSurplusText.rectTransform.anchoredPosition = new Vector2(5, 0);
        totalSurplusText.alignment = TextAlignmentOptions.Left;
        totalSurplusText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, totalSurplusText.rectTransform.sizeDelta.y);

        UpdateSurplusValues();

    }

}
