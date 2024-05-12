using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class SimulationInfoGroup : MonoBehaviour
{
    City city;
    TMP_Text numPassengersWhoGotRidesValueText;
    TMP_Text numPassengersWhoDidNotGetRidesValueText;
    TMP_Text ridesCompletedValueText;

    void Start()
    {
        city = GameObject.Find("City(Clone)").GetComponent<City>();
        numPassengersWhoGotRidesValueText = transform.Find("Group1").Find("ValueText").GetComponent<TMP_Text>();
        numPassengersWhoDidNotGetRidesValueText = transform.Find("Group2").Find("ValueText").GetComponent<TMP_Text>();
        ridesCompletedValueText = transform.Find("Group3").Find("ValueText").GetComponent<TMP_Text>();
        StartCoroutine(FadeInSchedule());
    }

    IEnumerator FadeInSchedule()
    {
        yield return new WaitForSeconds(4f);
        CanvasGroup group1CanvasGroup = transform.Find("Group1").GetComponent<CanvasGroup>();
        CanvasGroup group2CanvasGroup = transform.Find("Group2").GetComponent<CanvasGroup>();
        CanvasGroup group3CanvasGroup = transform.Find("Group3").GetComponent<CanvasGroup>();
        StartCoroutine(FadeInGroup(group1CanvasGroup, 1f));
        yield return new WaitForSeconds(0.4f);
        StartCoroutine(FadeInGroup(group2CanvasGroup, 1f));
        yield return new WaitForSeconds(0.4f);
        StartCoroutine(FadeInGroup(group3CanvasGroup, 1f));
    }

    IEnumerator FadeInGroup(CanvasGroup group, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float fadeInPercentage = EaseUtils.EaseInCubic(t);
            group.alpha = Mathf.Lerp(0, 1, fadeInPercentage);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        group.alpha = 1;
        yield return null;
    }
    void Update()
    {
        List<Trip> trips = city.GetTrips();
        List<Trip> completedTrips = trips.Where(trip => trip.state == TripState.Completed).ToList();

        int numCompletedTrips = completedTrips.Count;
        float totalFare = completedTrips.Sum(trip => trip.tripCreatedData.fare.total);
        float totalDistance = completedTrips.Sum(trip => trip.tripCreatedData.tripDistance);

        ridesCompletedValueText.text = numCompletedTrips.ToString();

        PassengerPerson[] passengers = city.GetPassengerPeople();
        int numPassengersWhoGotRides = passengers.Count(passenger => passenger.tripTypeChosen == TripType.Uber);
        int numPassengerWhoDidNotGetRides = passengers.Count(passenger => passenger.tripTypeChosen == TripType.Walking || passenger.tripTypeChosen == TripType.PublicTransport);

        numPassengersWhoGotRidesValueText.text = numPassengersWhoGotRides.ToString();
        numPassengersWhoDidNotGetRidesValueText.text = numPassengerWhoDidNotGetRides.ToString();

        // if (numCompletedTrips > 0)
        // {
        //     float averageFare = totalFare / numCompletedTrips;
        //     averageFareValueText.text = $"${averageFare:0.00}";

        //     float averageDistance = totalDistance / numCompletedTrips;
        //     averageDistanceValueText.text = $"{averageDistance:0.0} km";
        // }
        // else
        // {
        //     averageFareValueText.text = "";
        //     averageDistanceValueText.text = "";
        // }

    }
}
