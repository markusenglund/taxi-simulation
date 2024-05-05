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

public enum PassengerStatMode
{
    Fast,
    Slow
}
public class PassengerStats : MonoBehaviour
{
    [SerializeField] public Transform statTextPrefab;
    TextMeshProUGUI headingText;
    List<TextMeshProUGUI> statTexts = new List<TextMeshProUGUI>();
    PassengerEconomicParameters economicParameters;
    PassengerStatMode mode;
    void Start()
    {
        Transform passengerStatsSheet = transform.GetChild(0);
        Transform heading = passengerStatsSheet.Find("Heading");
        headingText = heading.GetComponent<TextMeshProUGUI>();
        headingText.text = $"Passenger Stats";
        headingText.color = new Color(headingText.color.r, headingText.color.g, headingText.color.b, 0);
        StartCoroutine(ScheduleActions());
    }

    private IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnCard(duration: 1f));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FadeInText(headingText, 0.5f));
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(InstantiateStats());
        StartCoroutine(SetSubstitutes());
        yield return null;
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

    public IEnumerator DespawnCard()
    {
        float duration = 1f;
        float startTime = Time.time;
        Vector3 startScale = transform.localScale;
        Vector3 finalScale = new Vector3(0f, 0f, 0f);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            text.color = new Color(text.color.r, text.color.g, text.color.b, t);
            yield return null;
        }
        text.color = new Color(text.color.r, text.color.g, text.color.b, 1);
    }

    private IEnumerator InstantiateStats()
    {
        Transform passengerStatsSheet = transform.GetChild(0);

        Stat incomeStat = new Stat()
        {
            name = "Income",
            value = $"${(Mathf.Round(economicParameters.hourlyIncome * 10) / 10f).ToString("F2")}/hr",
            barValue = economicParameters.hourlyIncome
        };
        Stat tripUtilityStat = new Stat()
        {
            name = "Trip utility",
            value = $"{economicParameters.tripUtilityScore.ToString("F2")}",
            barValue = economicParameters.tripUtilityScore * 20
        };
        Stat tripValueStat = new Stat()
        {
            name = "Trip value",
            value = $"${(Mathf.Round(economicParameters.tripUtilityValue * 10) / 10f).ToString("F2")}",
            barValue = economicParameters.tripUtilityValue / 2
        };
        Stat timeCostStat = new Stat()
        {
            name = "Cost of time",
            value = $"${(Mathf.Round(economicParameters.waitingCostPerHour * 10) / 10f).ToString("F2")}/hr",
            barValue = economicParameters.waitingCostPerHour / 2
        };

        StartCoroutine(InstantiateStat(passengerStatsSheet, incomeStat, index: 0, duration: 1));
        if (mode == PassengerStatMode.Slow)
        {
            yield return new WaitForSeconds(5f);
        }
        StartCoroutine(InstantiateStat(passengerStatsSheet, tripUtilityStat, index: 1, duration: 1));
        if (mode == PassengerStatMode.Slow)
        {
            yield return new WaitForSeconds(5f);
        }
        StartCoroutine(InstantiateStat(passengerStatsSheet, tripValueStat, index: 2, duration: 1));
        if (mode == PassengerStatMode.Slow)
        {
            yield return new WaitForSeconds(5f);
        }
        StartCoroutine(InstantiateStat(passengerStatsSheet, timeCostStat, index: 3, duration: 1));
        yield return null;
        // InstantiateStat(passengerStatsSheet, tripValueStat, 1);
        // InstantiateStat(passengerStatsSheet, timeCostStat, 2);
    }

    private IEnumerator SetSubstitutes()
    {
        Substitute bus = economicParameters.substitutes.Find(substitute => substitute.type == TripType.PublicTransport);
        Substitute walking = economicParameters.substitutes.Find(substitute => substitute.type == TripType.Walking);
        Substitute rentalCar = economicParameters.substitutes.Find(substitute => substitute.type == TripType.RentalCar);

        Transform busRow = transform.Find("PassengerStatsSheet/Table/Row2");
        busRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${bus.moneyCost.ToString("F2")}";
        busRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(bus.timeHours)} min";

        string netValueBus = bus.netValue > 0 ? $"${bus.netValue.ToString("F2")}" : $"-${Mathf.Abs(bus.netValue).ToString("F2")}";
        TextMeshProUGUI busNetValueText = busRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
        busNetValueText.text = netValueBus;
        Color netValueBusColor = bus.netValue > 0 ? Color.green : Color.red;
        busNetValueText.color = netValueBusColor;
        Transform walkingRow = transform.Find("PassengerStatsSheet/Table/Row3");
        walkingRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${walking.moneyCost.ToString("F2")}";
        walkingRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(walking.timeHours)} min";
        string netValueWalking = walking.netValue > 0 ? $"${walking.netValue.ToString("F2")}" : $"-${Mathf.Abs(walking.netValue).ToString("F2")}";
        TextMeshProUGUI walkingNetValueText = walkingRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
        walkingNetValueText.text = netValueWalking;
        Color netValueWalkingColor = walking.netValue > 0 ? Color.green : Color.red;
        walkingNetValueText.color = netValueWalkingColor;

        Transform rentalCarRow = transform.Find("PassengerStatsSheet/Table/Row4");
        rentalCarRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${rentalCar.moneyCost.ToString("F2")}";
        rentalCarRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(rentalCar.timeHours)} min";
        string netValueRentalCar = rentalCar.netValue > 0 ? $"${rentalCar.netValue.ToString("F2")}" : $"-${Mathf.Abs(rentalCar.netValue).ToString("F2")}";
        TextMeshProUGUI rentalCarNetValueText = rentalCarRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
        rentalCarNetValueText.text = netValueRentalCar;
        Color netValueRentalCarColor = rentalCar.netValue > 0 ? Color.green : Color.red;
        rentalCarNetValueText.color = netValueRentalCarColor;

        yield return null;
    }

    private IEnumerator InstantiateStat(Transform passengerStatsSheet, Stat stat, int index, float duration)
    {
        Transform statText = Instantiate(statTextPrefab, passengerStatsSheet);
        RectTransform statTextRect = statText.GetComponent<RectTransform>();
        // statText.localPosition = new Vector3(0, -4 - index * 20, 0);
        statText.localScale = Vector3.one * 0.8f;
        // statTextRect.anchoredPosition = new Vector2(0, 0);
        statTextRect.anchoredPosition = new Vector2(50, 7 - index * 20);

        Transform statName = statText.Find("StatName");
        TextMeshProUGUI statNameText = statName.GetComponent<TextMeshProUGUI>();
        statNameText.text = stat.name;
        Transform statValue = statText.Find("StatValue");
        TextMeshProUGUI statValueText = statValue.GetComponent<TextMeshProUGUI>();
        statValueText.text = stat.value;
        Transform barValue = statText.Find("Bar").Find("BarValue");
        RectTransform barValueRect = barValue.GetComponent<RectTransform>();
        barValueRect.sizeDelta = new Vector2(stat.barValue, barValueRect.sizeDelta.y);
        // statTexts.Add(statNameText)

        CanvasGroup canvasGroup = statText.GetComponent<CanvasGroup>();
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = t;
            yield return null;
        }

    }

    public static PassengerStats Create(Transform passengerStatsPrefab, Transform parent, Vector3 position, Quaternion rotation, PassengerEconomicParameters economicParameters, PassengerStatMode mode = PassengerStatMode.Fast)
    {
        Transform passengerStatsTransform = Instantiate(passengerStatsPrefab, parent);
        passengerStatsTransform.localPosition = position;
        passengerStatsTransform.localRotation = rotation;
        PassengerStats passengerStats = passengerStatsTransform.GetComponent<PassengerStats>();
        passengerStats.economicParameters = economicParameters;
        passengerStatsTransform.localScale = Vector3.zero;
        passengerStats.mode = mode;
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

