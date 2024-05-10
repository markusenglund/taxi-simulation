using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


public class LineUpDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cityMiddlePosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;

    Random random = new Random();

    List<Vector3> lineUpPositions = new List<Vector3>
    { };

    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        GameObject.Find("Canvas").GetComponent<CanvasGroup>().alpha = 0;
    }

    void Start()
    {
        Camera.main.transform.position = new Vector3(4.5f, 1f, -16f);
        Camera.main.transform.LookAt(cityMiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return new WaitForSeconds(0.5f);
        Passenger[] passengers = city.SpawnSavedPassengers();
        StartCoroutine(FadeInCanvas());
        StartCoroutine(MovePassengersToLineUp(passengers));
        yield return new WaitForSeconds(3f);
        StartCoroutine(TriggerIdleVariations(passengers));
    }

    IEnumerator FadeInCanvas()
    {
        CanvasGroup canvasGroup = GameObject.Find("Canvas").GetComponent<CanvasGroup>();
        float duration = 1.5f;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float alpha = EaseUtils.EaseInCubic(t);
            canvasGroup.alpha = alpha;
            yield return null;
        }
    }

    IEnumerator MovePassengersToLineUp(Passenger[] passengers)
    {
        float duration = 1.5f;
        foreach (Passenger passenger in passengers)
        {
            StartCoroutine(MovePassengerToLineUp(passenger, duration));
            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator TriggerIdleVariations(Passenger[] passengers)
    {
        while (true)
        {
            // Pick a random passenger
            Passenger randomPassenger1 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator = randomPassenger1.GetComponentInChildren<Animator>();
            passengerAnimator.SetTrigger("IdleVariation1");
            yield return new WaitForSeconds(3f);
            Passenger randomPassenger2 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator2 = randomPassenger2.GetComponentInChildren<Animator>();
            passengerAnimator2.SetTrigger("IdleVariation2");
            yield return new WaitForSeconds(2f);

            Passenger randomPassenger3 = passengers[random.Next(passengers.Length)];
            Animator passengerAnimator3 = randomPassenger3.GetComponentInChildren<Animator>();
            passengerAnimator3.SetTrigger("IdleVariation1");
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator MovePassengerToLineUp(Passenger passenger, float duration)
    {
        Animator passengerAnimator = passenger.GetComponentInChildren<Animator>();
        Vector3 startPosition = passenger.transform.position;
        float linePosition = ConvertUtilityScoreToLinePosition(passenger.person.economicParameters.tripUtilityScore);
        Vector3 endPosition = new Vector3(linePosition, 0, -6.7f);
        // Check if there's a lineUpPosition within 0.3f of the endPosition
        bool isEndPositionFree = false;
        while (!isEndPositionFree)
        {
            isEndPositionFree = true;
            foreach (Vector3 existingLineUpPosition in lineUpPositions)
            {
                if (Vector3.Distance(existingLineUpPosition, endPosition) < 0.35f)
                {
                    endPosition = endPosition + new Vector3(0, 0, 0.2f);
                    isEndPositionFree = false;
                }
            }
        }
        lineUpPositions.Add(endPosition);
        float startTime = Time.time;
        passengerAnimator.SetTrigger("SlowJump");
        Quaternion startRotation = passenger.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(0, 180 - 30 + 60f * (float)random.NextDouble(), 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float positionFactor = EaseUtils.EaseInOutQuadratic(t);
            // Make the vertical position an upside down parabola
            // float verticalPosition = EaseUtils.EaseInOutCubic(verticalPositionBase);
            // float positionFactor = EaseUtils.EaseInOutCubic(t);
            float verticalPositionBase = 1 - 4 * (positionFactor - 0.5f) * (positionFactor - 0.5f);
            passenger.transform.position = Vector3.Lerp(startPosition, endPosition, positionFactor) + new Vector3(0, verticalPositionBase * 4, 0);
            passenger.transform.rotation = Quaternion.Lerp(startRotation, finalRotation, t);
            yield return null;
        }
        // passenger.transform.position = endPosition;
    }


    private float ConvertUtilityScoreToLinePosition(float utilityScore)
    {
        float linePositionStart = -5f;
        float linePositionStep = 3f;
        return utilityScore * linePositionStep + linePositionStart;
    }

}
