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

    float simulationStartTime = 5;

    List<City> staticCities = new List<City>();
    List<City> surgeCities = new List<City>();

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(30f, 7f, 4.5f);

    BucketGraph bucketGraph;
    DriverUberGraph driverUberGraph;


    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        // staticCity1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        // surgeCity1 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
        for (int i = 0; i < 40; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            Vector3 cityPositionOffset = i == 0 ? Vector3.zero : new Vector3(0, 0, i * 13);
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
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * -8f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        StartCoroutine(Scene());
        InstantiateIncomeGraph();
        InstantiateSurplusBucketGraph();
        StartCoroutine(InspectData());
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

    private SimStatistic GetSurplusMinusFareBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
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

        float aggregateSurplus = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.valueSurplus + passenger.trip.tripCreatedData.fare.total) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedValueSurplus + passenger.trip.tripCreatedData.fare.total);
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

    private SimStatistic GetTotalFaresPaidBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
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

        float aggregateSurplus = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.tripCreatedData.fare.total) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedData.fare.total);
        // float aggregateExpectedSurplus = passengersWhoAreWaitingOrInTransit
        int sampleSize = passengersInQuartileWhoCompletedJourney.Count + passengersInQuartileWhoAreWaitingOrInTransit.Count;

        return new SimStatistic
        {
            value = aggregateSurplus,
            sampleSize = sampleSize
        };
    }

    private SimStatistic GetTotalTimeCostBucketInfo(City[] cities, int quartile, GetPassengerValue getValue, float[] quartileThresholds)
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

        float aggregateTimeCost = passengersInQuartileWhoCompletedJourney.Sum(passenger => passenger.trip.droppedOffPassengerData.totalTimeCost) + passengersInQuartileWhoAreWaitingOrInTransit.Sum(passenger => passenger.trip.tripCreatedPassengerData.expectedTripTimeCost + passenger.trip.tripCreatedPassengerData.expectedWaitingCost);
        // float aggregateExpectedSurplus = passengersWhoAreWaitingOrInTransit
        int sampleSize = passengersInQuartileWhoCompletedJourney.Count + passengersInQuartileWhoAreWaitingOrInTransit.Count;

        return new SimStatistic
        {
            value = aggregateTimeCost,
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
            if (value > 10000)
            {
                return "$" + (value / 1000).ToString("F0") + "k";
            }
            if (value > 1000)
            {
                return "$" + (value / 1000).ToString("F1") + "k";
            }
            return "$" + value.ToString("F0");
        };
        // string[] labels = new string[] { "< $12.72", "$12.72 - $20", "$20 - $33.36", "> $33.36" };
        string[] labels = new string[] { "Poorest 25%", "25-50%", "50-75%", "Richest 25%" };
        bucketGraph = BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(1200, 500), "Passenger surplus\nby income level", "Total surplus ($)", getBucketedSurplusValues, formatValue, labels, 30000);
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
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(1900, 1700), "Surplus/income by income", "Surplus utils", getBucketedSurplusValues, formatValue, labels, 1000);
    }

    void InstantiateSurplusMinusFareByIncomeBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedSurplusMinusFareValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetSurplusMinusFareBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
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
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(600, 1700), "Surplus minus fare by income", "Surplus utils", getBucketedSurplusMinusFareValues, formatValue, labels, 100000);
    }

    void InstantiateTotalFaresPaidByIncomeBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedFaresPaidValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetTotalFaresPaidBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
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
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(600, 800), "Total fares paid by income", "Fares paid $", getBucketedFaresPaidValues, formatValue, labels, 10000);
    }

    void InstantiateTotalTimeCostByIncomeBucketGraph()
    {
        float[] hourlyIncomeQuartileThresholds = new float[] { 12.72f, 20f, 33.36f, float.PositiveInfinity };
        GetPassengerValue getHourlyIncome = (PassengerPerson passenger) => passenger.economicParameters.hourlyIncome;
        GetBucketGraphValues getBucketedTimeCostValues = (City[] cities) =>
        {
            SimStatistic[] bucketInfos = new SimStatistic[4];
            for (int i = 0; i < 4; i++)
            {
                bucketInfos[i] = GetTotalTimeCostBucketInfo(cities, i, getHourlyIncome, hourlyIncomeQuartileThresholds);
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
        BucketGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(1900, 800), "Total time cost by income", "Money $", getBucketedTimeCostValues, formatValue, labels, 40000);
    }


    void InstantiateIncomeGraph()
    {
        GetStatistic GetUberIncome = cities =>
        {
            PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

            PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
            // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
            PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();

            PassengerPerson[] allPassengers = passengersWhoCompletedJourney.Concat(passengersWhoAreWaitingOrInTransit).ToArray();
            float uberIncome = allPassengers.Sum(passenger => passenger.trip.tripCreatedData.fare.uberCut);

            int sampleSize = allPassengers.Length;

            return new SimStatistic
            {
                value = uberIncome,
                sampleSize = sampleSize
            };
        };

        GetStatistic GetDriverIncome = cities =>
        {
            PassengerPerson[] passengers = cities.SelectMany(city => city.GetPassengerPeople()).ToArray();

            PassengerPerson[] passengersWhoCompletedJourney = passengers.Where(passenger => passenger.state == PassengerState.DroppedOff).ToArray();
            // Don't count passengers who are queued to get a ride (trip state = DriverAssigned)
            PassengerPerson[] passengersWhoAreWaitingOrInTransit = passengers.Where(passenger => passenger.state == PassengerState.AssignedToTrip && (passenger.trip.state == TripState.DriverEnRoute || passenger.trip.state == TripState.DriverWaiting || passenger.trip.state == TripState.OnTrip)).ToArray();

            PassengerPerson[] allPassengers = passengersWhoCompletedJourney.Concat(passengersWhoAreWaitingOrInTransit).ToArray();
            float driverIncome = allPassengers.Sum(passenger =>
            {
                float driverMarginalCostPerKm = staticPriceSettings.driverMarginalCostPerKm;
                float marginalDriverCost = (passenger.trip.tripCreatedData.tripDistance + passenger.trip.driverDispatchedData.enRouteDistance) * driverMarginalCostPerKm;
                return passenger.trip.tripCreatedData.fare.driverCut - marginalDriverCost;
            });

            int sampleSize = allPassengers.Length;

            return new SimStatistic
            {
                value = driverIncome,
                sampleSize = sampleSize
            };
        };

        FormatValue formatIncome = value =>
        {
            if (value > 1000)
            {
                return "$" + (value / 1000).ToString("F1") + "k";
            }
            return "$" + value.ToString("F0");
        };

        driverUberGraph = DriverUberGraph.Create(staticCities.ToArray(), surgeCities.ToArray(), new Vector3(2620, 500), "Producer surplus", GetUberIncome, GetDriverIncome, formatIncome);
    }

    IEnumerator InspectData()
    {
        while (true)
        {
            yield return new WaitForFrames(5 * 60);
            DataInspection.ShowSurplusBreakdown(staticCities.ToArray(), surgeCities.ToArray());
            // City[] allCities = staticCities.Concat(surgeCities).ToArray();
            // DataInspection.GetAverageWaitingTimeByNumAssignedTrips(allCities);
        }
    }

    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        Vector3 newPosition = Camera.main.transform.position + new Vector3(0, 0, 70);
        StartCoroutine(CameraUtils.MoveCamera(newPosition, 120, Ease.Quadratic));
        yield return new WaitForSeconds(TimeUtils.ConvertSimulationHoursDurationToRealSeconds(1.5f) + simulationStartTime);
        Time.timeScale = 0;
        yield return new WaitForFrames(60 * 3);
        StartCoroutine(bucketGraph.scaleGraph(2.3f, new Vector2(1900, 1080)));
        yield return new WaitForFrames(60 * 2);
        StartCoroutine(bucketGraph.FadeInDeltaLabels(duration: 1));
        StartCoroutine(driverUberGraph.FadeInDeltaLabels(duration: 1));
        yield return new WaitForFrames(60 * 9);
        StartCoroutine(bucketGraph.scaleGraph(1, new Vector2(1200, 500)));
        yield return new WaitForFrames(60 * 20);
        Time.timeScale = 1;
        yield return new WaitForFrames(Mathf.FloorToInt(60f * TimeUtils.ConvertSimulationHoursDurationToRealSeconds(2.5f)));
        yield return new WaitForFrames(50 * 60);
        UnityEditor.EditorApplication.isPlaying = false;

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