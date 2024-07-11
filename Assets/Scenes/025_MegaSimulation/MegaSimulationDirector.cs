using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        for (int i = 0; i < 2; i++)
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
    }

    private void InstantiateTimeSensitivityBucketGraph()
    {
        float[] timeSensitivityQuartileThresholds = new float[] { 1.428f, 2f, 2.801f, float.PositiveInfinity };
        GetBucketGraphValues getBucketedTimeSensitivityValues = (City[] cities) =>
        {
            PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).Where(p => p.state != PassengerState.Idling && p.state != PassengerState.BeforeSpawn).ToArray();
            List<PassengerPerson> firstQuartile = new List<PassengerPerson>();
            List<PassengerPerson> secondQuartile = new List<PassengerPerson>();
            List<PassengerPerson> thirdQuartile = new List<PassengerPerson>();
            List<PassengerPerson> fourthQuartile = new List<PassengerPerson>();
            foreach (PassengerPerson passenger in passengers)
            {
                float timeSensitivity = passenger.economicParameters.timePreference;
                if (timeSensitivity < timeSensitivityQuartileThresholds[0])
                {
                    firstQuartile.Add(passenger);
                }
                else if (timeSensitivity < timeSensitivityQuartileThresholds[1])
                {
                    secondQuartile.Add(passenger);
                }
                else if (timeSensitivity < timeSensitivityQuartileThresholds[2])
                {
                    thirdQuartile.Add(passenger);
                }
                else
                {
                    fourthQuartile.Add(passenger);
                }
            }

            float percentageWhoGotAnUberFirstQuartile = firstQuartile.Count == 0 ? 0 : (float)firstQuartile.Count(p => p.tripTypeChosen == TripType.Uber) / firstQuartile.Count;
            float percentageWhoGotAnUberSecondQuartile = secondQuartile.Count == 0 ? 0 : (float)secondQuartile.Count(p => p.tripTypeChosen == TripType.Uber) / secondQuartile.Count;
            float percentageWhoGotAnUberThirdQuartile = thirdQuartile.Count == 0 ? 0 : (float)thirdQuartile.Count(p => p.tripTypeChosen == TripType.Uber) / thirdQuartile.Count;
            float percentageWhoGotAnUberFourthQuartile = fourthQuartile.Count == 0 ? 0 : (float)fourthQuartile.Count(p => p.tripTypeChosen == TripType.Uber) / fourthQuartile.Count;

            return (percentageWhoGotAnUberFirstQuartile, percentageWhoGotAnUberSecondQuartile, percentageWhoGotAnUberThirdQuartile, percentageWhoGotAnUberFourthQuartile);
        };

        FormatBucketGraphValue formatValue = (float value) =>
        {
            return (value * 100).ToString("F0") + "%";
        };
        BucketGraph.Create(staticCities.ToArray(), new Vector3(700, 800), "Time Sensitivity fixed", getBucketedTimeSensitivityValues, formatValue, ColorScheme.blue);
        BucketGraph.Create(surgeCities.ToArray(), new Vector3(2000, 800), "Time Sensitivity surge", getBucketedTimeSensitivityValues, formatValue, ColorScheme.surgeRed);

    }
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