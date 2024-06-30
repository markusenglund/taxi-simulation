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
        InstatiateTimeSensitivityInfoBoxes();
        InstantiateIncomeInfoBoxes();
        InstantiateTimeOfBestSubstituteInfoBoxes();


    }


    void InstantiateTimeOfBestSubstituteInfoBoxes()
    {
        GetValue GetTimeOfBestSubstitute = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoTookUber = passengers.Where(p => p.tripTypeChosen == TripType.Uber).ToArray();
            if (passengersWhoTookUber.Length == 0)
            {
                return 0;
            }
            float timeOfBestSubstitute = StatisticsUtils.CalculateMedian(passengersWhoTookUber.Select(p => p.economicParameters.GetBestSubstitute().timeHours).ToList());
            return timeOfBestSubstitute;
        };

        FormatValue FormatTimeOfBestSubstitute = value => (value * 60).ToString("0") + " min";

        GetValue GetTimeOfBestSubstituteOfNonPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoDidNotTakeUber = passengers.Where(p => p.tripTypeChosen == TripType.Walking || p.tripTypeChosen == TripType.PublicTransport).ToArray();
            if (passengersWhoDidNotTakeUber.Length == 0)
            {
                return 0;
            }
            float timeOfBestSubstitute = StatisticsUtils.CalculateMedian(passengersWhoDidNotTakeUber.Select(p => p.economicParameters.GetBestSubstitute().timeHours).ToList());
            return timeOfBestSubstitute;
        };

        InfoBox timeOfBestSubstituteStatic = InfoBox.Create(city1, new Vector3(800, 1000), "Time of best substitute (Uber)", GetTimeOfBestSubstitute, FormatTimeOfBestSubstitute, ColorScheme.blue);
        InfoBox timeOfBestSubstituteSubstituteStatic = InfoBox.Create(city1, new Vector3(1300, 1000), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.blue);

        InfoBox timeOfBestSubstituteSurge = InfoBox.Create(city2, new Vector3(800, 700), "Time of best substitute (Uber)", GetTimeOfBestSubstitute, FormatTimeOfBestSubstitute, ColorScheme.orange);
        InfoBox timeOfBestSubstituteSubstituteSurge = InfoBox.Create(city2, new Vector3(1300, 700), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.orange);
    }

    void InstantiateIncomeInfoBoxes()
    {
        GetValue GetMedianIncomeOfPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoTookUber = passengers.Where(p => p.tripTypeChosen == TripType.Uber).ToArray();
            if (passengersWhoTookUber.Length == 0)
            {
                return 0;
            }
            float medianIncome = StatisticsUtils.CalculateMedian(passengersWhoTookUber.Select(p => p.economicParameters.hourlyIncome).ToList());
            return medianIncome;
        };

        FormatValue FormatIncome = value => "$" + value.ToString("0.00");

        GetValue GetMedianIncomeOfNonPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoDidNotTakeUber = passengers.Where(p => p.tripTypeChosen == TripType.Walking || p.tripTypeChosen == TripType.PublicTransport).ToArray();
            if (passengersWhoDidNotTakeUber.Length == 0)
            {
                return 0;
            }
            float medianIncome = StatisticsUtils.CalculateMedian(passengersWhoDidNotTakeUber.Select(p => p.economicParameters.hourlyIncome).ToList());
            return medianIncome;
        };

        InfoBox incomeStatic = InfoBox.Create(city1, new Vector3(-200, 1000), "Income (Uber)", GetMedianIncomeOfPassengers, FormatIncome, ColorScheme.blue);
        InfoBox incomeSubstituteStatic = InfoBox.Create(city1, new Vector3(300, 1000), "Income (Substitute)", GetMedianIncomeOfNonPassengers, FormatIncome, ColorScheme.blue);

        InfoBox incomeSurge = InfoBox.Create(city2, new Vector3(-200, 700), "Income (Uber)", GetMedianIncomeOfPassengers, FormatIncome, ColorScheme.orange);
        InfoBox incomeSubstituteSurge = InfoBox.Create(city2, new Vector3(300, 700), "Income (Substitute)", GetMedianIncomeOfNonPassengers, FormatIncome, ColorScheme.orange);
    }
    void InstatiateTimeSensitivityInfoBoxes()
    {
        GetValue GetMedianTimeSensitivityOfPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoTookUber = passengers.Where(p => p.tripTypeChosen == TripType.Uber).ToArray();
            if (passengersWhoTookUber.Length == 0)
            {
                return 0;
            }
            float medianTimeSensitivity = StatisticsUtils.CalculateMedian(passengersWhoTookUber.Select(p => p.economicParameters.timePreference).ToList());
            return medianTimeSensitivity;
        };

        FormatValue FormatTimeSensitivity = value => value.ToString("0.00") + "x";

        GetValue GetMedianTimeSensitivityOfNonPassengers = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoDidNotTakeUber = passengers.Where(p => p.tripTypeChosen == TripType.Walking || p.tripTypeChosen == TripType.PublicTransport).ToArray();
            if (passengersWhoDidNotTakeUber.Length == 0)
            {
                return 0;
            }
            float medianTimeSensitivity = StatisticsUtils.CalculateMedian(passengersWhoDidNotTakeUber.Select(p => p.economicParameters.timePreference).ToList());
            return medianTimeSensitivity;
        };

        InfoBox timeSensitivityStatic = InfoBox.Create(city1, new Vector3(-1200, 1000), "Time sensitivity (Uber)", GetMedianTimeSensitivityOfPassengers, FormatTimeSensitivity, ColorScheme.blue);
        InfoBox timeSensitivitySubstituteStatic = InfoBox.Create(city1, new Vector3(-700, 1000), "Time sensitivity (Substitute)", GetMedianTimeSensitivityOfNonPassengers, FormatTimeSensitivity, ColorScheme.blue);

        InfoBox timeSensitivitySurge = InfoBox.Create(city2, new Vector3(-1200, 700), "Time sensitivity (Uber)", GetMedianTimeSensitivityOfPassengers, FormatTimeSensitivity, ColorScheme.orange);
        InfoBox timeSensitivitySubstituteSurge = InfoBox.Create(city2, new Vector3(-700, 700), "Time sensitivity (Substitute)", GetMedianTimeSensitivityOfNonPassengers, FormatTimeSensitivity, ColorScheme.orange);
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
