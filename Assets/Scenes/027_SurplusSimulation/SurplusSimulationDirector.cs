using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class SimStatistic
{
    public float value;
    public int sampleSize;
}

public delegate SimStatistic GetStatistic(City[] cities);


public class SurplusSimulationDirector : MonoBehaviour
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
    Vector3 cameraPosition = new Vector3(27f, 7f, 4.5f);


    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        // staticCity1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        // surgeCity1 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
        for (int i = 40; i < 80; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            Vector3 cityPositionOffset = i == 0 ? Vector3.zero : new Vector3(0, 0, i * 12);
            Vector3 staticCityPosition = city1Position + cityPositionOffset;
            City staticCity = City.Create(cityPrefab, staticCityPosition.x, staticCityPosition.z, staticPriceSettingsClone, graphSettings);
            SimulationSettings surgePriceSettingsClone = Instantiate(surgePriceSettings);
            surgePriceSettingsClone.randomSeed = i;
            Vector3 surgeCityPosition = city2Position + cityPositionOffset;
            City surgeCity = City.Create(cityPrefab, surgeCityPosition.x, surgeCityPosition.z, surgePriceSettingsClone, graphSettings);
            staticCities.Add(staticCity);
            surgeCities.Add(surgeCity);
        }
    }




    void Start()
    {

        Camera.main.transform.position = cameraPosition;
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * -7f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        StartCoroutine(Scene());
        InstantiateSurplusBucketGraph();
        InstantiateSurplusDividedByIncomeBucketGraph();
    }

    private SimStatistic GetSurplusBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

        PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
        // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
        PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();


        List<PassengerPerson> passengersInQuartileWhoCompletedJourney = new List<PassengerPerson>();
        List<PassengerPerson> passengersInQuartileWhoAreWaitingOrInTransit = new List<PassengerPerson>();
        if (quartile == 0)
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
        }
        else
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
        }

        float aggregateSurplus = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedValueSurplus);
        // float aggregateExpectedSurplus = passengersWhoAreWaitingOrInTransit
        int sampleSize = passengersInQuartileWhoCompletedJourney.Count + passengersInQuartileWhoAreWaitingOrInTransit.Count;

        return new SimStatistic
        {
            value = aggregateSurplus,
            sampleSize = sampleSize
        };
    }


    private SimStatistic GetSurplusDividedByIncomeBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
    {
        PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

        PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
        // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
        PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();


        List<PassengerPerson> passengersInQuartileWhoCompletedJourney = new List<PassengerPerson>();
        List<PassengerPerson> passengersInQuartileWhoAreWaitingOrInTransit = new List<PassengerPerson>();
        if (quartile == 0)
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) < quartileThresholds[quartile]).ToList();
        }
        else
        {
            passengersInQuartileWhoCompletedJourney = passengersWhoCompletedJourney.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
            passengersInQuartileWhoAreWaitingOrInTransit = passengersWhoAreWaitingOrInTransit.Where(passenger => getValue(passenger) >= quartileThresholds[quartile - 1] && getValue(passenger) < quartileThresholds[quartile]).ToList();
        }

        float aggregateSurplus = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus / passenger.economicParameters.hourlyIncome) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedValueSurplus / passenger.economicParameters.hourlyIncome);

        int sampleSize = passengersInQuartileWhoCompletedJourney.Count + passengersInQuartileWhoAreWaitingOrInTransit.Count;

        return new SimStatistic
        {
            value = aggregateSurplus,
            sampleSize = sampleSize
        };
    }
    void InstantiateSurplusBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedSurplusValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetSurplusBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
            }
            return bucketInfos;
        };
        FormatBucketGraphValue formatValue = (float value) =>
        {
            if (value > 1000)
            {
                return "$" + (value / 1000).ToString("F1") + "k";
            }
            return "$" + value.ToString("F0");
        };
        string[] labels = new string[] { "< $12.72", "$12.72 - $20", "$20 - $33.36", "> $33.36" };
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(2600, 1400), "Surplus by income", "Total surplus $", getBucketedSurplusValues, formatValue, labels, 50000);
    }


    void InstantiateSurplusDividedByIncomeBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedSurplusValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetSurplusDividedByIncomeBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
            }
            return bucketInfos;
        };
        FormatBucketGraphValue formatValue = (float value) =>
        {
            return value.ToString("F0");
        };
        string[] labels = new string[] { "< $12.72", "$12.72 - $20", "$20 - $33.36", "> $33.36" };
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(1200, 1400), "Surplus/income by income", "Surplus utils", getBucketedSurplusValues, formatValue, labels, 1000);
    }

    // void InstantiatePassengerSurplusInfoBoxes()
    // {
    //     GetStatistic GetRichestAggregateSurplus = cities =>
    //     {
    //         PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

    //         PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
    //         // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
    //         PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();

    //         float richestQuartileIncomeThreshold = 33.36f;

    //         PassengerPerson[] richestPassengers = passengersWhoCompletedJourney.Where(passenger => passenger.economicParameters.hourlyIncome > richestQuartileIncomeThreshold).ToArray();

    //         float aggregateSurplus = richestPassengers.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus);

    //         int sampleSize = richestPassengers.Length;

    //         return new SimStatistic
    //         {
    //             value = aggregateSurplus,
    //             sampleSize = sampleSize
    //         };
    //     };

    //     GetStatistic GetPoorestAggregateSurplus = cities =>
    //     {
    //         PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

    //         PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
    //         // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
    //         PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();

    //         float poorestQuartileIncomeThreshold = 12.72f;

    //         PassengerPerson[] poorestPassengers = passengersWhoCompletedJourney.Where(passenger => passenger.economicParameters.hourlyIncome < poorestQuartileIncomeThreshold).ToArray();

    //         float aggregateSurplus = poorestPassengers.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus);

    //         int sampleSize = poorestPassengers.Length;

    //         return new SimStatistic
    //         {
    //             value = aggregateSurplus,
    //             sampleSize = sampleSize
    //         };
    //     };

    //     FormatValue formatSurplus = value => $"${value:0}";

    //     SurplusGraph surplusGraph = SurplusGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(700, 1200), "Aggregate Surplus", GetRichestAggregateSurplus, GetPoorestAggregateSurplus, formatSurplus);
    // }



    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        Vector3 newPosition = Camera.main.transform.position + new Vector3(0, 0, 200);
        StartCoroutine(CameraUtils.MoveCamera(newPosition, 55, Ease.Quadratic));

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
}