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
        Transform heading = passengerStatsSheet.Find("Heading");
        heading.GetComponent<TextMeshProUGUI>().text = $"Passenger {passenger.id} Stats";
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
        StartCoroutine(ScheduleActions());
    }

    private IEnumerator ScheduleActions()
    {
        yield return StartCoroutine(SpawnCard(duration: 1f));
    }

    private IEnumerator SpawnCard(float duration)
    {
        Vector3 startScale = new Vector3(0.0005f, 0f, 0.001f);
        Vector3 finalScale = Vector3.one * 0.001f;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        transform.localScale = finalScale;
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

    public static PassengerStats Create(Transform passengerStatsPrefab, Vector3 position, Quaternion rotation, PassengerBase passenger)
    {
        Transform passengerStatsTransform = Instantiate(passengerStatsPrefab, position, rotation);
        PassengerStats passengerStats = passengerStatsTransform.GetComponent<PassengerStats>();
        passengerStats.passenger = passenger;
        passengerStatsTransform.localScale = Vector3.zero;
        return passengerStats;
    }

    float EaseInOutCubic(float t)
    {
        float t2;
        if (t <= 0.5f)
        {
            t2 = Mathf.Pow(t * 2, 3) / 2;
        }
        else
        {
            t2 = (2 - Mathf.Pow((1 - t) * 2, 3)) / 2;
        }
        return t2;
    }
}

