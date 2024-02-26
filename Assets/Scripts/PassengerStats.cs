using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class Stat
{
    public string name;
    public string value;
    public float barValue;
}
public class PassengerStats : MonoBehaviour
{
    [SerializeField] public Transform statTextPrefab;
    public PassengerBase passenger;
    void Start()
    {
        Transform passengerStatsSheet = transform.GetChild(0);
        Stat incomeStat = new Stat()
        {
            name = "Income",
            value = $"${(Mathf.Round(passenger.passengerEconomicParameters.hourlyIncome * 10) / 10f).ToString("F2")}/hr",
            barValue = passenger.passengerEconomicParameters.hourlyIncome
        };
        Stat tripValueStat = new Stat()
        {
            name = "Trip value",
            value = $"${(Mathf.Round(passenger.passengerEconomicParameters.tripUtilityValue * 10) / 10f).ToString("F2")}",
            barValue = passenger.passengerEconomicParameters.tripUtilityValue / 2
        };
        Stat timeCostStat = new Stat()
        {
            name = "Cost of time",
            value = $"${(Mathf.Round(passenger.passengerEconomicParameters.waitingCostPerHour * 10) / 10f).ToString("F2")}/hr",
            barValue = passenger.passengerEconomicParameters.waitingCostPerHour / 2
        };

        InstantiateStat(passengerStatsSheet, incomeStat, 0);
        InstantiateStat(passengerStatsSheet, tripValueStat, 1);
        InstantiateStat(passengerStatsSheet, timeCostStat, 2);

    }

    private void InstantiateStat(Transform passengerStatsSheet, Stat stat, int index)
    {
        Transform statText1 = Instantiate(statTextPrefab, passengerStatsSheet, true);
        statText1.localPosition = new Vector3(0, -4 - index * 20, 0);
        Transform statName = statText1.Find("StatName");
        TextMeshProUGUI statNameText = statName.GetComponent<TextMeshProUGUI>();
        statNameText.text = stat.name;
        Transform statValue = statText1.Find("StatValue");
        TextMeshProUGUI statValueText = statValue.GetComponent<TextMeshProUGUI>();
        statValueText.text = stat.value;
        Transform barValue = statText1.Find("Bar").Find("BarValue");
        RectTransform barValueRect = barValue.GetComponent<RectTransform>();
        barValueRect.sizeDelta = new Vector2(stat.barValue, 1.5f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static PassengerStats Create(Transform passengerStatsPrefab, Vector3 position, Quaternion rotation, PassengerBase passenger)
    {
        Transform passengerStatsTransform = Instantiate(passengerStatsPrefab, position, rotation);
        PassengerStats passengerStats = passengerStatsTransform.GetComponent<PassengerStats>();
        passengerStats.passenger = passenger;
        return passengerStats;
    }
}
