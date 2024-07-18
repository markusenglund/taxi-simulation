using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public delegate float GetPassengerValue(PassengerPerson passenger);


public class MegaSimulationDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 4;

    List<City> staticCities = new List<City>();
    List<City> surgeCities = new List<City>();

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(10.5f, 15f, -10f);

    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        for (int i = 0; i < 40; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            Vector3 cityPositionOffset = i == 0 ? Vector3.zero : new Vector3(-1000, 0, i * 12);
            Vector3 staticCityPosition = city1Position + cityPositionOffset;
            City staticCity = City.Create(cityPrefab, staticCityPosition.x, staticCityPosition.z, staticPriceSettingsClone, graphSettings);
            SimulationSettings surgePriceSettingsClone = Instantiate(surgePriceSettings);
            surgePriceSettingsClone.randomSeed = i;
            Vector3 surgeCityPosition = city2Position + cityPositionOffset;
            City surgeCity = City.Create(cityPrefab, surgeCityPosition.x, surgeCityPosition.z + 12 * i, surgePriceSettingsClone, graphSettings);
            staticCities.Add(staticCity);
            surgeCities.Add(surgeCity);
        }
    }




    void Start()
    {

        Camera.main.transform.position = cameraPosition;
        Quaternion cameraRotation = Quaternion.Euler(45, 5, 3);
        Camera.main.transform.rotation = cameraRotation;
        Camera.main.fieldOfView = 45f;
        StartCoroutine(Scene());

        // InstantiateMaxTimeSavingsBucketGraph();
    }




    private SimStatistic GetBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
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
        return new SimStatistic { value = percentageWhoGotAnUber, sampleSize = sampleSize };
    }

    private void InstantiateTimeSensitivityBucketGraph()
    {
        float[] timeSensitivityQuartileThresholds = new float[] { 0.714f, 1f, 1.401f, float.PositiveInfinity };
        GetPassengerValue getTimeSensitivity = (PassengerPerson passenger) => passenger.economicParameters.timePreference;
        GetBucketGraphValues getBucketedTimeSensitivityValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
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
        string[] labels = new string[] { "< 1.43x", "1.43 - 2x", "2 - 2.80x", "> 2.80x" };
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(1300, 1100), "Time Sensitivity", "% served by Uber", getBucketedTimeSensitivityValues, formatValue, labels, 1);
    }

    private void InstantiateHourlyIncomeBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedHourlyIncomeValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
            }
            return bucketInfos;
        };
        FormatBucketGraphValue formatValue = (float value) =>
        {
            return (value * 100).ToString("F0") + "%";
        };
        string[] labels = new string[] { "< $12.72", "$12.72 - $20", "$20 - $33.36", "> $33.36" };
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(2600, 1100), "Hourly Income", "% served by Uber", getBucketedHourlyIncomeValues, formatValue, labels, 1);
    }


    private void InstantiateTimeSensitivityBarGraph()
    {
        GetVerticalBarValue getTopTimeSensitivityPassengersShareOfRides = (City[] cities) =>
        {
            PassengerPerson[] passengersWhoGotAnUber = cities.SelectMany(city => city.GetPassengerPeople()).Where(p =>
            {
                bool passengerDidNotGetAnUber = p.tripTypeChosen != TripType.Uber;
                if (passengerDidNotGetAnUber)
                {
                    return false;
                }
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
            float top10PercentThreshold = 1.9f;
            PassengerPerson[] topTimeSensitivityPassengers = passengersWhoGotAnUber.Where(p =>
            {
                float value = p.economicParameters.timePreference;
                return value >= top10PercentThreshold;
            }).ToArray();

            return new SimStatistic { value = (float)topTimeSensitivityPassengers.Count() / passengersWhoGotAnUber.Count(), sampleSize = passengersWhoGotAnUber.Count() };
        };

        FormatValue formatValue = (float value) =>
        {
            return (value * 100).ToString("F0") + "%";
        };
        string[] labels = new string[] { "< 1.43x", "1.43 - 2x", "2 - 2.80x", "> 2.80x" };
        VerticalBarGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(3100, 1300), "Share of Uber rides taken by the\ntop 10% most time sensitive agents", getTopTimeSensitivityPassengersShareOfRides, formatValue);
    }

    // private void InstantiateMaxTimeSavingsBucketGraph()
    // {
    //     // Just a guess, no distribution to go off of
    //     float[] maxTimeSavingsQuartileThresholds = new float[] { 29f / 60f, 47f / 60f, 65f / 60f, float.PositiveInfinity };
    //     GetPassengerValue getMaxTimeSavings = (PassengerPerson passenger) => passenger.economicParameters.GetBestSubstitute().maxTimeSavedByUber;
    //     GetBucketGraphValues getBucketedMaxTimeSavingsValues = (City[] cities) =>
    //     {
    //         SimStatistic[] bucketInfos = new SimStatistic[4];
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
        StartCoroutine(SetSimulationStart());
        Quaternion newRotation = Quaternion.Euler(15, 10, 0);
        StartCoroutine(CameraUtils.RotateCamera(newRotation, 8, Ease.Cubic));
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(SpawnCities());
        Vector3 newPosition = Camera.main.transform.position + new Vector3(0, 0, 270);
        StartCoroutine(CameraUtils.MoveCamera(newPosition, 80, Ease.Quadratic));
        yield return new WaitForSeconds(6.5f);
        StartCoroutine(CameraUtils.RotateCamera(Quaternion.Euler(15, 10, 0), 3, Ease.Quadratic));
        yield return new WaitForSeconds(1.5f);
        InstantiateTimeSensitivityBarGraph();

        yield return null;
    }

    IEnumerator SetSimulationStart()
    {
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        yield return new WaitForSeconds(simulationStartTime);
        foreach (City city in staticCities)
        {
            StartCoroutine(city.StartSimulation());
        }

        foreach (City city in surgeCities)
        {
            StartCoroutine(city.StartSimulation());
        }
    }

    IEnumerator SpawnCities()
    {
        for (int i = 1; i < staticCities.Count; i++)
        {
            float waitTime = i == 1 ? 0.5f : 0.15f;
            City staticCity = staticCities[i];
            float z = i * 13;
            StartCoroutine(SpawnCity(staticCity, 2f, new Vector3(0, 0, z)));
            yield return new WaitForSeconds(waitTime);
            City surgeCity = surgeCities[i];
            StartCoroutine(SpawnCity(surgeCity, 2f, new Vector3(12, 0, z)));
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator SpawnCity(City city, float duration, Vector3 position)
    {
        float startTime = Time.time;
        Vector3 startScale = Vector3.zero;
        Vector3 finalScale = Vector3.one;
        city.transform.position = position;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            city.transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        city.transform.localScale = finalScale;
    }
}