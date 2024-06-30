using System.Collections;
using UnityEngine;
using Random = System.Random;
using System.Collections.Generic;
using System.Linq;

public class SecondSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    City city1;
    City city2;

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(10.5f, 11f, -6f);

    bool hasSavedPassengerData = false;

    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        city1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        city2 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);

    }



    void Start()
    {

        Camera.main.transform.position = cameraPosition;
        Camera.main.transform.LookAt(middlePosition);
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        StartCoroutine(Scene());
        InstatiateTimeSensitivityInfoBox();


    }


    void InstatiateTimeSensitivityInfoBox()
    {
        GetValue GetAverageTimeSensitivityOfPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoTookUber = passengers.Where(p => p.tripTypeChosen == TripType.Uber).ToArray();
            if (passengersWhoTookUber.Length == 0)
            {
                return 0;
            }
            float averageTimeSensitivity = passengersWhoTookUber.Average(p => p.economicParameters.timePreference);
            return averageTimeSensitivity;
        };

        FormatValue FormatTimeSensitivity = value => value.ToString("0.00") + "x";
        InfoBox timeSensitivityStatic = InfoBox.Create(city1, new Vector3(200, -200), "Time sensitivity", GetAverageTimeSensitivityOfPassengers, FormatTimeSensitivity);

        Debug.Log("Time sensitivity info box created");
    }

    IEnumerator Scene()
    {
        // PredictedSupplyDemandGraph.Create(city, PassengerSpawnGraphMode.Regular);
        // PassengerTripTypeGraph.Create(city);
        // StartCoroutine(simulationInfoGroup.FadeInSchedule());
        FareGraph.Create(city1, city2);
        WaitingGraph.Create(city1, city2);
        StartCoroutine(city1.StartSimulation());
        StartCoroutine(city2.StartSimulation());
        yield return null;
    }

    void Update()
    {
        Passenger[] passengers = city2.GetPassengers();
        if (city1.simulationEnded && !hasSavedPassengerData)
        {
            List<PassengerPerson> persons = new List<PassengerPerson>();
            foreach (Passenger p in passengers)
            {
                persons.Add(p.person);
            }
            Debug.Log($"Saving passenger data from {persons.Count} passengers");
            SaveData.SaveObject(surgePriceSettings.randomSeed + "_020", persons);
            hasSavedPassengerData = true;
        }
    }
}
