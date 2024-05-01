using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ResultsInfoBox : MonoBehaviour
{

    private RectTransform textContainer;

    [SerializeField] private TMP_Text textPrefab;
    const float timeInterval = 5f / 60f;

    TMP_Text driverGrossProfitText;
    TMP_Text driverSurplusValueText;
    TMP_Text uberRevenueText;

    TMP_Text passengerSurplusValueText;
    TMP_Text passengerSurplusQuartileText;
    TMP_Text totalSurplusText;

    TMP_Text tripText;
    TMP_Text waitingTimeText;

    TMP_Text fareText;

    City city;


    public static ResultsInfoBox Create(Transform prefab, Vector3 screenPos, City city)
    {
        Transform canvas = GameObject.Find("Canvas").transform;
        Transform transform = Instantiate(prefab, canvas);

        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = screenPos;
        ResultsInfoBox resultsInfoBox = transform.GetComponent<ResultsInfoBox>();
        resultsInfoBox.city = city;
        return resultsInfoBox;
    }

    void Start()
    {
        textContainer = transform.Find("TextContainer").GetComponent<RectTransform>();
        Invoke("InstantiateText", 0.1f);
        StartCoroutine(UpdateValues());
    }

    IEnumerator UpdateValues()
    {
        while (true)
        {
            float intervalRealSeconds = TimeUtils.ConvertSimulationHoursTimeToRealSeconds(timeInterval);
            yield return new WaitForSeconds(intervalRealSeconds);
            UpdateSurplusValues();
            UpdateTripValues();
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
        (float grossProfitLastHour, float surplusValueLastHour, float totalDriverGrossProfit, float totalDriverSurplusValue, float totalUberRevenue) = city.driverPool.CalculateAverageGrossProfitInInterval(city.simulationSettings.simulationLengthHours);

        driverGrossProfitText.text = $"Avg hourly gross profit: <color={GetTextColor(grossProfitLastHour)}><b>${grossProfitLastHour:0.00}</b></color>, total: <color={GetTextColor(totalDriverGrossProfit)}><b>${totalDriverGrossProfit:0.00}</b></color>";
        driverSurplusValueText.text = $"Avg hourly surplus: <color={GetTextColor(surplusValueLastHour)}><b>${surplusValueLastHour:0.00}</b></color>, total: <color={GetTextColor(totalDriverSurplusValue)}><b>${totalDriverSurplusValue:0.00}</b></color>";

        uberRevenueText.text = $"Total Uber revenue: <color={GetTextColor(totalUberRevenue)}><b>${totalUberRevenue:0.00}</b></color>";

        (float totalRiderUtilitySurplus, float totalRiderUtilitySurplusPerCapita, int numRiders, float[] quartiledUtilitySurplusPerCapita, int[] quartiledPopulation) = city.CalculatePassengerUtilitySurplusData();

        passengerSurplusValueText.text = $"Rider surplus per ride: <color={GetTextColor(totalRiderUtilitySurplusPerCapita)}><b>${totalRiderUtilitySurplusPerCapita:0.00}</b></color>, total: <color={GetTextColor(totalRiderUtilitySurplus)}><b>${totalRiderUtilitySurplus:0.00}</b></color>";

        passengerSurplusQuartileText.text = $"Quartiles: <color={GetTextColor(quartiledUtilitySurplusPerCapita[0])}><b>${quartiledUtilitySurplusPerCapita[0]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[1])}><b>${quartiledUtilitySurplusPerCapita[1]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[2])}><b>${quartiledUtilitySurplusPerCapita[2]:0.00}</b></color>, <color={GetTextColor(quartiledUtilitySurplusPerCapita[3])}><b>${quartiledUtilitySurplusPerCapita[3]:0.00}</b></color>";

        float totalSurplusValue = totalDriverSurplusValue + totalUberRevenue + totalRiderUtilitySurplus;
        totalSurplusText.text = $"Total market surplus: <color={GetTextColor(totalSurplusValue)}><b>${totalSurplusValue:0.00}</b></color>";
    }

    void UpdateTripValues()
    {
        List<Trip> trips = city.GetTrips();
        List<Trip> completedTrips = trips.Where(trip => trip.state == TripState.Completed).ToList();
        int numCompletedTrips = completedTrips.Count();

        List<Trip> startedOrCompletedTrips = trips.Where(trip => trip.state == TripState.Completed || trip.state == TripState.OnTrip).ToList();

        float totalWaitingTime = 0;
        foreach (Trip trip in startedOrCompletedTrips)
        {
            totalWaitingTime += trip.pickedUpData.waitingTime;
        }

        float averageWaitingTime = totalWaitingTime / startedOrCompletedTrips.Count;

        float totalTransactionVolume = 0;
        foreach (Trip trip in trips)
        {
            totalTransactionVolume += trip.tripCreatedData.fare.total;
        }
        float averageFare = totalTransactionVolume / trips.Count;

        tripText.text = $"Completed trips: {numCompletedTrips}";
        waitingTimeText.text = $"Avg waiting time: <b>{TimeUtils.ConvertSimulationHoursToTimeString(averageWaitingTime)}</b>, total: <b>{TimeUtils.ConvertSimulationHoursToTimeString(totalWaitingTime)}</b>";
        fareText.text = $"Avg fare: <b>${averageFare:0.00}</b>, total: <b>${totalTransactionVolume:0.00}</b>";
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

        tripText = Instantiate(textPrefab, textContainer);
        tripText.fontSize = 16;
        tripText.rectTransform.anchoredPosition = new Vector2(5, -20);
        tripText.alignment = TextAlignmentOptions.Left;
        tripText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, tripText.rectTransform.sizeDelta.y);

        waitingTimeText = Instantiate(textPrefab, textContainer);
        waitingTimeText.fontSize = 16;
        waitingTimeText.rectTransform.anchoredPosition = new Vector2(5, -40);
        waitingTimeText.alignment = TextAlignmentOptions.Left;
        waitingTimeText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, waitingTimeText.rectTransform.sizeDelta.y);

        fareText = Instantiate(textPrefab, textContainer);
        fareText.fontSize = 16;
        fareText.rectTransform.anchoredPosition = new Vector2(5, -60);
        fareText.alignment = TextAlignmentOptions.Left;
        fareText.rectTransform.sizeDelta = new Vector2(textContainer.rect.width, fareText.rectTransform.sizeDelta.y);

        // UpdateSurplusValues();
        // UpdateTripValues();

    }

}
