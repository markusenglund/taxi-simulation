using System.Collections;
using UnityEngine;
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
    Vector3 cameraPosition = new Vector3(10.5f, 15f, -10f);

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
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * 3f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        Camera.main.fieldOfView = 45f;
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        StartCoroutine(Scene());
        // InstatiateTimeSensitivityInfoBoxes();
        // InstantiateIncomeInfoBoxes();
        // InstantiateTimeOfBestSubstituteInfoBoxes();
        // InstantiateMaxTimeSavingInfoBoxes();
        // InstantiatePassengerNumberInfoBoxes();
        // InstantiateNoOfferPassengerNumberInfoBoxes();
        // InstantiateBucketInfoBoxes();


    }

    void InstantiateNoOfferPassengerNumberInfoBoxes()
    {
        GetValue GetNumPassengersWhoWereNotOfferedARide = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            int numPassengersWhoDidNotGetRides = passengers.Count(passenger => passenger.state == PassengerState.NoRideOffer);
            return numPassengersWhoDidNotGetRides;
        };
        FormatValue formatValue = value => value.ToString();

        InfoBox numPassengersWhoDidNotGetRidesStatic = InfoBox.Create(city1, new Vector3(1500, 1000), "No offer static", GetNumPassengersWhoWereNotOfferedARide, formatValue, ColorScheme.blue);
        InfoBox numPassengersWhoDidNotGetRidesSurge = InfoBox.Create(city2, new Vector3(1500, 750), "No offer surge", GetNumPassengersWhoWereNotOfferedARide, formatValue, ColorScheme.orange);
    }


    void InstantiatePassengerNumberInfoBoxes()
    {
        GetValue GetNumPassengersWhoGotRides = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            int numPassengersWhoGotRides = passengers.Count(passenger => passenger.tripTypeChosen == TripType.Uber);
            return numPassengersWhoGotRides;
        };
        FormatValue formatValue = value => value.ToString();

        InfoBox numPassengersWhoGotRidesStatic = InfoBox.Create(city1, new Vector3(1100, 1000), "Passengers static", GetNumPassengersWhoGotRides, formatValue, ColorScheme.blue);
        InfoBox numPassengersWhoGotRidesSurge = InfoBox.Create(city2, new Vector3(1100, 750), "Passengers surge", GetNumPassengersWhoGotRides, formatValue, ColorScheme.orange);
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

        InfoBox timeOfBestSubstituteStatic = InfoBox.Create(city1, new Vector3(300, 1000), "Time of best substitute (Uber)", GetTimeOfBestSubstitute, FormatTimeOfBestSubstitute, ColorScheme.blue);
        // InfoBox timeOfBestSubstituteSubstituteStatic = InfoBox.Create(city1, new Vector3(600, 1000), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.blue);

        InfoBox timeOfBestSubstituteSurge = InfoBox.Create(city2, new Vector3(300, 750), "Time of best substitute (Uber)", GetTimeOfBestSubstitute, FormatTimeOfBestSubstitute, ColorScheme.orange);
        // InfoBox timeOfBestSubstituteSubstituteSurge = InfoBox.Create(city2, new Vector3(600, 750), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.orange);
    }

    void InstantiateMaxTimeSavingInfoBoxes()
    {
        GetValue GetMaxTimeSavingsOfUberOverBestSubstitute = city =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople();
            PassengerPerson[] passengersWhoTookUber = passengers.Where(p => p.tripTypeChosen == TripType.Uber).ToArray();
            if (passengersWhoTookUber.Length == 0)
            {
                return 0;
            }
            float medianTimeSavingsOfUber = StatisticsUtils.CalculateMedian(passengersWhoTookUber.Select(p => p.economicParameters.GetBestSubstitute().maxTimeSavedByUber).ToList());
            return medianTimeSavingsOfUber;
        };

        FormatValue FormatTimeSavings = value => (value * 60).ToString("0") + " min";

        InfoBox timeOfBestSubstituteStatic = InfoBox.Create(city1, new Vector3(700, 1000), "Max time savings", GetMaxTimeSavingsOfUberOverBestSubstitute, FormatTimeSavings, ColorScheme.blue);
        // InfoBox timeOfBestSubstituteSubstituteStatic = InfoBox.Create(city1, new Vector3(700, 1000), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.blue);

        InfoBox timeOfBestSubstituteSurge = InfoBox.Create(city2, new Vector3(700, 750), "Max time savings", GetMaxTimeSavingsOfUberOverBestSubstitute, FormatTimeSavings, ColorScheme.orange);
        // InfoBox timeOfBestSubstituteSubstituteSurge = InfoBox.Create(city2, new Vector3(700, 750), "Time of best substitute (Substitute)", GetTimeOfBestSubstituteOfNonPassengers, FormatTimeOfBestSubstitute, ColorScheme.orange);
    }

    public delegate float GetBucket(City city, int quartile);


    void InstantiateBucketInfoBoxes()
    {
        float[] timeSensitivityQuartileThresholds = new float[] { 1.428f, 2f, 2.801f, float.PositiveInfinity };
        GetBucket GetQuartileOfPassengersByTimeSensitivity = (City city, int quartile) =>
        {
            PassengerPerson[] passengers = city.GetPassengerPeople().Where(p => p.state != PassengerState.Idling && p.state != PassengerState.BeforeSpawn).ToArray();
            PassengerPerson[] quartilePassengers = passengers.Where(p =>
            {
                float timeSensitivity = p.economicParameters.timePreference;
                if (timeSensitivity < timeSensitivityQuartileThresholds[quartile])
                {
                    if (quartile == 0)
                    {
                        return true;
                    }
                    if (timeSensitivity >= timeSensitivityQuartileThresholds[quartile - 1])
                    {
                        return true;
                    }
                }
                return false;
            }).ToArray();
            float percentageWhoGotAnUber = quartilePassengers.Length == 0 ? 0 : (float)quartilePassengers.Count(p => p.tripTypeChosen == TripType.Uber) / quartilePassengers.Length;
            // Debug.Log($"Bottom quartile: {quartilePassengers.Length}, {percentageWhoGotAnUber}");
            return percentageWhoGotAnUber;
        };

        GetValue GetBottomQuartile = city => GetQuartileOfPassengersByTimeSensitivity(city, 0);
        GetValue GetSecondQuartile = city => GetQuartileOfPassengersByTimeSensitivity(city, 1);
        GetValue GetThirdQuartile = city => GetQuartileOfPassengersByTimeSensitivity(city, 2);
        GetValue GetTopQuartile = city => GetQuartileOfPassengersByTimeSensitivity(city, 3);

        FormatValue FormatPercentage = value => (value * 100).ToString("0") + "%";
        InfoBox bottomQuartileStatic = InfoBox.Create(city1, new Vector3(-500, 1000), "Bottom quartile time sense", GetBottomQuartile, FormatPercentage, ColorScheme.blue);
        InfoBox bottomQuartileSurge = InfoBox.Create(city2, new Vector3(-500, 750), "Bottom quartile time sense", GetBottomQuartile, FormatPercentage, ColorScheme.orange);
        InfoBox secondQuartileStatic = InfoBox.Create(city1, new Vector3(-100, 1000), "Second quartile time sense", GetSecondQuartile, FormatPercentage, ColorScheme.blue);
        InfoBox secondQuartileSurge = InfoBox.Create(city2, new Vector3(-100, 750), "Second quartile time sense", GetSecondQuartile, FormatPercentage, ColorScheme.orange);
        InfoBox thirdQuartileStatic = InfoBox.Create(city1, new Vector3(300, 1000), "Third quartile time sense", GetThirdQuartile, FormatPercentage, ColorScheme.blue);
        InfoBox thirdQuartileSurge = InfoBox.Create(city2, new Vector3(300, 750), "Third quartile time sense", GetThirdQuartile, FormatPercentage, ColorScheme.orange);
        InfoBox topQuartileStatic = InfoBox.Create(city1, new Vector3(700, 1000), "Top quartile time sense", GetTopQuartile, FormatPercentage, ColorScheme.blue);
        InfoBox topQuartileSurge = InfoBox.Create(city2, new Vector3(700, 750), "Top quartile time sense", GetTopQuartile, FormatPercentage, ColorScheme.orange);
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

        InfoBox incomeStatic = InfoBox.Create(city1, new Vector3(-500, 1000), "Income (Uber)", GetMedianIncomeOfPassengers, FormatIncome, ColorScheme.blue);
        InfoBox incomeSubstituteStatic = InfoBox.Create(city1, new Vector3(-100, 1000), "Income (Substitute)", GetMedianIncomeOfNonPassengers, FormatIncome, ColorScheme.blue);

        InfoBox incomeSurge = InfoBox.Create(city2, new Vector3(-500, 750), "Income (Uber)", GetMedianIncomeOfPassengers, FormatIncome, ColorScheme.orange);
        InfoBox incomeSubstituteSurge = InfoBox.Create(city2, new Vector3(-100, 750), "Income (Substitute)", GetMedianIncomeOfNonPassengers, FormatIncome, ColorScheme.orange);
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

        InfoBox timeSensitivityStatic = InfoBox.Create(city1, new Vector3(-1300, 1000), "Time sensitivity (Uber)", GetMedianTimeSensitivityOfPassengers, FormatTimeSensitivity, ColorScheme.blue);
        InfoBox timeSensitivitySubstituteStatic = InfoBox.Create(city1, new Vector3(-900, 1000), "Time sensitivity (Substitute)", GetMedianTimeSensitivityOfNonPassengers, FormatTimeSensitivity, ColorScheme.blue);

        InfoBox timeSensitivitySurge = InfoBox.Create(city2, new Vector3(-1300, 750), "Time sensitivity (Uber)", GetMedianTimeSensitivityOfPassengers, FormatTimeSensitivity, ColorScheme.orange);
        InfoBox timeSensitivitySubstituteSurge = InfoBox.Create(city2, new Vector3(-900, 750), "Time sensitivity (Substitute)", GetMedianTimeSensitivityOfNonPassengers, FormatTimeSensitivity, ColorScheme.orange);
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
