using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BucketInfo
{
    public float percentageWhoGotAnUber;
    public int sampleSize;
}

public class MegaSimulationDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    List<City> staticCities = new List<City>();
    List<City> surgeCities = new List<City>();

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(10.5f, 10f, -10f);

    bool hasSavedPassengerData = false;

    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        // staticCity1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        // surgeCity1 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
        for (int i = 0; i < 20; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            City staticCity = City.Create(cityPrefab, city1Position.x, city1Position.y + 12 * i, staticPriceSettingsClone, graphSettings);
            SimulationSettings surgePriceSettingsClone = Instantiate(surgePriceSettings);
            surgePriceSettingsClone.randomSeed = i;
            City surgeCity = City.Create(cityPrefab, city2Position.x, city2Position.y + 12 * i, surgePriceSettingsClone, graphSettings);
            staticCities.Add(staticCity);
            surgeCities.Add(surgeCity);
        }
    }




    void Start()
    {

        Camera.main.transform.position = cameraPosition;
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * 2f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        Camera.main.fieldOfView = 45f;
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        StartCoroutine(Scene());
        InstantiateTimeSensitivityBucketGraph();
        // InstantiateHourlyIncomeBucketGraph();
        // InstantiateMaxTimeSavingsBucketGraph();
    }



    public delegate float GetPassengerValue(PassengerPerson passenger);

    private BucketInfo GetBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).Where(p =>
        {
            bool passengerHasNotRequestedTripYet = p.state == PassengerState.Idling || p.state == PassengerState.BeforeSpawn;
            if (passengerHasNotRequestedTripYet)
            {
                return false;
            }

            // If the passenger's driver is assigned but not yet driving to the passenger, we don't want to count them to prevent bias in favor of having a large queue of waiting passengers
            bool passengerIsQueued = p.trip != null && (p.trip.state == TripState.Queued || p.trip.state == TripState.DriverAssigned);
            if (passengerIsQueued)
            {
                return false;
            }
            return true;
        }).ToArray();
        List<PassengerPerson> quartileList = new List<PassengerPerson>();
        foreach (PassengerPerson passenger in passengers)
        {
            float value = getValue(passenger);
            if (value < quartileThresholds[quartile])
            {
                if (quartile == 0)
                {
                    quartileList.Add(passenger);
                }
                else if (value >= quartileThresholds[quartile - 1])
                {
                    quartileList.Add(passenger);
                }
            }
        }
        int sampleSize = quartileList.Count;
        float percentageWhoGotAnUber = sampleSize == 0 ? 0 : (float)quartileList.Count(p => p.tripTypeChosen == TripType.Uber) / sampleSize;
        return new BucketInfo { percentageWhoGotAnUber = percentageWhoGotAnUber, sampleSize = sampleSize };
    }

    private void InstantiateTimeSensitivityBucketGraph()
    {
        float[] timeSensitivityQuartileThresholds = new float[] { 1.428f, 2f, 2.801f, float.PositiveInfinity };
        GetPassengerValue getTimeSensitivity = (PassengerPerson passenger) => passenger.economicParameters.timePreference;
        GetBucketGraphValues getBucketedTimeSensitivityValues = (City[] cities) =>
        {
            BucketInfo[] bucketInfos = new BucketInfo[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetBucketInfo(cities, i, getTimeSensitivity, timeSensitivityQuartileThresholds);
            }
            return bucketInfos;
        };

        FormatBucketGraphValue formatValue = (float value) =>
        {
            return (value * 100).ToString("F0") + "%";
        };
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(700, 800), "Time Sensitivity", getBucketedTimeSensitivityValues, formatValue, ColorScheme.blue, ColorScheme.surgeRed);
        // BucketGraph.Create(staticCities.ToArray(), new Vector3(700, 800), "Time Sensitivity fixed", getBucketedTimeSensitivityValues, formatValue, ColorScheme.blue);
        // BucketGraph.Create(surgeCities.ToArray(), new Vector3(700, 1600), "Time Sensitivity surge", getBucketedTimeSensitivityValues, formatValue, ColorScheme.surgeRed);

    }

    // private void InstantiateHourlyIncomeBucketGraph()
    // {
    //     float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
    //     GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
    //     GetBucketGraphValues getBucketedHourlyIncomeValues = (City[] cities) =>
    //     {
    //         BucketInfo[] bucketInfos = new BucketInfo[4];
    //         for (int i = 0; i < 4; i++)
    //         {
    //             bucketInfos[i] = GetBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
    //         }
    //         return bucketInfos;
    //     };
    //     FormatBucketGraphValue formatValue = (float value) =>
    //     {
    //         return (value * 100).ToString("F0") + "%";
    //     };
    //     BucketGraph.Create(staticCities.ToArray(), new Vector3(2000, 800), "Hourly Income fixed", getBucketedHourlyIncomeValues, formatValue, ColorScheme.blue);
    //     BucketGraph.Create(surgeCities.ToArray(), new Vector3(2000, 1600), "Hourly Income surge", getBucketedHourlyIncomeValues, formatValue, ColorScheme.surgeRed);
    // }

    // private void InstantiateMaxTimeSavingsBucketGraph()
    // {
    //     // Just a guess, no distribution to go off of
    //     float[] maxTimeSavingsQuartileThresholds = new float[] { 29f / 60f, 47f / 60f, 65f / 60f, float.PositiveInfinity };
    //     GetPassengerValue getMaxTimeSavings = (PassengerPerson passenger) => passenger.economicParameters.GetBestSubstitute().maxTimeSavedByUber;
    //     GetBucketGraphValues getBucketedMaxTimeSavingsValues = (City[] cities) =>
    //     {
    //         BucketInfo[] bucketInfos = new BucketInfo[4];
    //         for (int i = 0; i < 4; i++)
    //         {
    //             bucketInfos[i] = GetBucketInfo(cities, i, getMaxTimeSavings, maxTimeSavingsQuartileThresholds);
    //         }
    //         return bucketInfos;
    //     };
    //     FormatBucketGraphValue formatValue = (float value) =>
    //     {
    //         return (value * 100).ToString("F0") + "%";
    //     };
    //     BucketGraph.Create(staticCities.ToArray(), new Vector3(3300, 800), "Max Time Savings fixed", getBucketedMaxTimeSavingsValues, formatValue, ColorScheme.blue);
    //     BucketGraph.Create(surgeCities.ToArray(), new Vector3(3300, 1600), "Max Time Savings surge", getBucketedMaxTimeSavingsValues, formatValue, ColorScheme.surgeRed);
    // }

    IEnumerator Scene()
    {
        foreach (City city in staticCities)
        {
            StartCoroutine(city.StartSimulation());
        }

        foreach (City city in surgeCities)
        {
            StartCoroutine(city.StartSimulation());
        }
        yield return null;
    }
}