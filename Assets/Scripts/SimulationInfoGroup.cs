using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class SimultionInfoGroup : MonoBehaviour
{
    City city;
    TMP_Text ridesCompletedValueText;
    TMP_Text averageFareValueText;
    TMP_Text averageDistanceValueText;

    void Start()
    {
        city = GameObject.Find("City(Clone)").GetComponent<City>();

        ridesCompletedValueText = transform.Find("Group1").Find("RidesCompletedValueText").GetComponent<TMP_Text>();
        averageFareValueText = transform.Find("Group2").Find("AverageFareValueText").GetComponent<TMP_Text>();
        averageDistanceValueText = transform.Find("Group3").Find("AverageDistanceValueText").GetComponent<TMP_Text>();
    }
    void Update()
    {
        List<Trip> trips = city.GetTrips();
        List<Trip> completedTrips = trips.Where(trip => trip.state == TripState.Completed).ToList();

        int numCompletedTrips = completedTrips.Count;
        float totalFare = completedTrips.Sum(trip => trip.tripCreatedData.fare.total);
        float totalDistance = completedTrips.Sum(trip => trip.tripCreatedData.tripDistance);

        ridesCompletedValueText.text = numCompletedTrips.ToString();
        if (numCompletedTrips > 0)
        {
            float averageFare = totalFare / numCompletedTrips;
            averageFareValueText.text = $"${averageFare:0.00}";

            float averageDistance = totalDistance / numCompletedTrips;
            averageDistanceValueText.text = $"{averageDistance:0.0} km";
        }
        else
        {
            averageFareValueText.text = "";
            averageDistanceValueText.text = "";
        }

    }
}
