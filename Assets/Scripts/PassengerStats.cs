using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
    TextMeshProUGUI attributesHeadingText;
    TextMeshProUGUI optionsHeadingText;
    Transform dividingLine;
    Transform head;
    Transform uberRow;
    Transform busRow;
    Transform walkingRow;
    Transform passengerStatsSheet;
    List<TextMeshProUGUI> statTexts = new List<TextMeshProUGUI>();
    PassengerPerson person;

    RideOffer rideOffer;
    PassengerStatMode mode;
    [SerializeField] TMP_FontAsset notoEmoji;
    void Start()
    {
        passengerStatsSheet = transform.GetChild(0);
        Transform attributesHeading = passengerStatsSheet.Find("AttributesHeading");
        attributesHeadingText = attributesHeading.GetComponent<TextMeshProUGUI>();

        Transform optionsHeading = passengerStatsSheet.Find("OptionsHeading");
        optionsHeadingText = optionsHeading.GetComponent<TextMeshProUGUI>();
        if (mode == PassengerStatMode.Slow)
        {
            // Set width of PasangerStatsSheet to 100
            RectTransform passengerStatsSheetRect = passengerStatsSheet.GetComponent<RectTransform>();
            passengerStatsSheetRect.sizeDelta = new Vector2(100, passengerStatsSheetRect.sizeDelta.y);
        }

        dividingLine = passengerStatsSheet.Find("DividingLine");
        head = transform.Find("PassengerStatsSheet/Table/Head");
        uberRow = transform.Find("PassengerStatsSheet/Table/Row3");
        busRow = transform.Find("PassengerStatsSheet/Table/Row2");
        walkingRow = transform.Find("PassengerStatsSheet/Table/Row1");

        // Set alpha of everything to zero
        attributesHeadingText.color = new Color(attributesHeadingText.color.r, attributesHeadingText.color.g, attributesHeadingText.color.b, 0);
        optionsHeadingText.color = new Color(optionsHeadingText.color.r, optionsHeadingText.color.g, optionsHeadingText.color.b, 0);
        dividingLine.gameObject.SetActive(false);

        head.GetComponent<CanvasGroup>().alpha = 0;
        uberRow.GetComponent<CanvasGroup>().alpha = 0;
        busRow.GetComponent<CanvasGroup>().alpha = 0;
        walkingRow.GetComponent<CanvasGroup>().alpha = 0;
        // Set the Text component of every cell to color with alpha 0
        for (int i = 1; i < 4; i++)
        {
            uberRow.GetChild(i).Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);
            busRow.GetChild(i).Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);
            walkingRow.GetChild(i).Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);
        }
        StartCoroutine(ScheduleActions());
    }

    private IEnumerator ScheduleActions()
    {
        StartCoroutine(SpawnCard(duration: 1f));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(optionsHeadingText, mode == PassengerStatMode.Slow ? 0.5f : 0.1f));
        yield return WaitIfSlowMode(3);
        yield return StartCoroutine(SetTripOptions());
        yield return WaitIfSlowMode(9);

        yield return StartCoroutine(ExpandCard(duration: 1f));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(attributesHeadingText, mode == PassengerStatMode.Slow ? 0.5f : 0.1f));

        yield return WaitIfSlowMode(1);
        yield return StartCoroutine(InstantiateStats());
        yield return WaitIfSlowMode(19);
        // Reveal total Uber cost
        StartCoroutine(FadeInText(uberRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(1);
        // Reveal total bus cost
        StartCoroutine(FadeInText(busRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(1);
        // Reveal total walking cost
        StartCoroutine(FadeInText(walkingRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return null;
    }

    WaitForSeconds WaitIfSlowMode(float seconds)
    {
        if (mode == PassengerStatMode.Slow)
        {
            return new WaitForSeconds(seconds);
        }
        return null;
    }

    private IEnumerator SpawnCard(float duration)
    {
        Vector3 startScale = new Vector3(0, 0.002f, 0.001f);
        Vector3 finalScale = Vector3.one * 0.002f;
        float startFrameCount = Time.frameCount;
        float frameCountDuration = duration * 60;
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        transform.localScale = finalScale;
    }

    private IEnumerator ExpandCard(float duration)
    {
        float startWidth = 100;
        float finalWidth = 200;
        float startFrameCount = Time.frameCount;
        float frameCountDuration = duration * 60;
        RectTransform passengerStatsSheetRect = transform.GetChild(0).GetComponent<RectTransform>();
        // Set dividing line to active
        dividingLine.gameObject.SetActive(true);
        if (mode == PassengerStatMode.Slow)
        {
            while (Time.frameCount < startFrameCount + frameCountDuration)
            {
                float t = (Time.frameCount - startFrameCount) / frameCountDuration;
                float scaleFactor = EaseUtils.EaseInOutCubic(t);
                passengerStatsSheetRect.sizeDelta = new Vector2(Mathf.Lerp(startWidth, finalWidth, scaleFactor), passengerStatsSheetRect.sizeDelta.y);
                yield return null;
            }

        }
        passengerStatsSheetRect.sizeDelta = new Vector2(finalWidth, passengerStatsSheetRect.sizeDelta.y);
    }

    public IEnumerator DespawnCard()
    {
        float frameCountDuration = 60;
        float startFrameCount = Time.frameCount;
        Vector3 startScale = transform.localScale;
        Vector3 finalScale = new Vector3(0f, 0f, 0f);
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
    {
        float startFrameCount = Time.frameCount;
        float frameCountDuration = duration * 60;
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
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
            value = $"${(Mathf.Round(person.economicParameters.hourlyIncome * 10) / 10f).ToString("F2")}/hr",
            // Subtract 4 since it's the minimum hourlyIncome value, the barValue is an approximation of the values place in the distribution
            barValue = Mathf.Sqrt(person.economicParameters.hourlyIncome - 4) * 8
        };
        Stat timePreferenceStat = new Stat()
        {
            name = "Time sensitivity",
            value = $"{person.economicParameters.timePreference.ToString("F2")}x",
            barValue = person.economicParameters.timePreference * 45
        };
        Stat timeCostStat = new Stat()
        {
            name = "Value of time",
            value = $"${(Mathf.Round(person.economicParameters.valueOfTime * 10) / 10f).ToString("F2")}/hr",
            barValue = Mathf.Sqrt(person.economicParameters.valueOfTime) * 7
        };


        StartCoroutine(InstantiateStat(passengerStatsSheet, timeCostStat, index: 0, duration: 1));
        if (mode == PassengerStatMode.Slow)
        {
            yield return new WaitForSeconds(7);
        }
        StartCoroutine(InstantiateStat(passengerStatsSheet, incomeStat, index: 1, duration: 1));
        StartCoroutine(InstantiateStat(passengerStatsSheet, timePreferenceStat, index: 2, duration: 1));
        yield return null;
    }

    private IEnumerator SetTripOptions()
    {
        TripOption uber = person.uberTripOption;
        TripOption bus = person.economicParameters.substitutes.Find(tripOption => tripOption.type == TripType.PublicTransport);
        TripOption walking = person.economicParameters.substitutes.Find(tripOption => tripOption.type == TripType.Walking);


        if (uber != null)
        {
            uberRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${uber.moneyCost.ToString("F2")}";
            uberRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(uber.timeHours)} min";
            string uberTotalCost = $"${uber.totalCost.ToString("F2")}";
            TextMeshProUGUI uberTotalCostText = uberRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
            uberTotalCostText.text = uberTotalCost;
        }
        else
        {
            for (int i = 1; i < 4; i++)
            {
                TextMeshProUGUI uberRowText = uberRow.GetChild(i).Find("Text").GetComponent<TextMeshProUGUI>();
                uberRowText.font = notoEmoji;
                uberRowText.fontSize = 10;
                uberRowText.color = ColorScheme.red;
                uberRowText.fontStyle = FontStyles.Bold;
                uberRowText.text = "🚫";
            }
        }


        busRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${bus.moneyCost.ToString("F2")}";
        busRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(bus.timeHours)} min";

        string busTotalCost = $"${bus.totalCost.ToString("F2")}";
        TextMeshProUGUI busTotalCostText = busRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
        busTotalCostText.text = busTotalCost;

        walkingRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>().text = $"${walking.moneyCost.ToString("F2")}";
        walkingRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>().text = $"{TimeUtils.ConvertSimulationHoursToMinuteString(walking.timeHours)} min";
        string walkingTotalCost = $"${walking.totalCost.ToString("F2")}";
        TextMeshProUGUI walkingTotalCostText = walkingRow.GetChild(3).Find("Text").GetComponent<TextMeshProUGUI>();
        walkingTotalCostText.text = walkingTotalCost;

        // Fade in canvas groups for the first two columns of head and uberRow
        StartCoroutine(FadeInCanvasGroup(head.GetComponent<CanvasGroup>(), 1));
        StartCoroutine(FadeInCanvasGroup(walkingRow.GetComponent<CanvasGroup>(), 1));

        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(walkingRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(walkingRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(3);
        StartCoroutine(FadeInCanvasGroup(busRow.GetComponent<CanvasGroup>(), 1));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(busRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(busRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(3);
        StartCoroutine(FadeInCanvasGroup(uberRow.GetComponent<CanvasGroup>(), 1));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(uberRow.GetChild(1).Find("Text").GetComponent<TextMeshProUGUI>(), 1));
        yield return WaitIfSlowMode(1);
        StartCoroutine(FadeInText(uberRow.GetChild(2).Find("Text").GetComponent<TextMeshProUGUI>(), 1));


        yield return null;
    }

    IEnumerator FadeInCanvasGroup(CanvasGroup canvasGroup, float duration)
    {
        float frameCountDuration = duration * 60;
        float startFrameCount = Time.frameCount;
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            canvasGroup.alpha = t;
            yield return null;
        }
    }

    private IEnumerator InstantiateStat(Transform passengerStatsSheet, Stat stat, int index, float duration)
    {
        Transform statText = Instantiate(statTextPrefab, passengerStatsSheet);
        RectTransform statTextRect = statText.GetComponent<RectTransform>();
        // statText.localPosition = new Vector3(0, -4 - index * 20, 0);
        statText.localScale = Vector3.one * 0.8f;
        // statTextRect.anchoredPosition = new Vector2(0, 0);
        statTextRect.anchoredPosition = new Vector2(150, 2 - index * 24);

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
        float frameCountDuration = duration * 60;
        float startFrameCount = Time.frameCount;
        while (Time.frameCount < startFrameCount + frameCountDuration)
        {
            float t = (Time.frameCount - startFrameCount) / frameCountDuration;
            canvasGroup.alpha = t;
            yield return null;
        }

    }

    public static PassengerStats Create(Transform passengerStatsPrefab, Transform parent, Vector3 position, Quaternion rotation, PassengerPerson person, PassengerStatMode mode = PassengerStatMode.Fast)
    {
        Transform passengerStatsTransform = Instantiate(passengerStatsPrefab, parent);
        passengerStatsTransform.localPosition = position;
        passengerStatsTransform.localRotation = rotation;
        PassengerStats passengerStats = passengerStatsTransform.GetComponent<PassengerStats>();
        passengerStats.person = person;
        passengerStatsTransform.localScale = Vector3.zero;
        passengerStats.mode = mode;
        return passengerStats;
    }
}

