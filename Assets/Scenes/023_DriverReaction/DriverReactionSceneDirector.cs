using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class DriverReactionDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        Time.captureFramerate = 60;

    }

    void Start()
    {
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return null; // Wait for the city to run the Start method before generating passenger
        GameObject driver = GameObject.Find("TaxiWithEmotingDriver");
        Animator driverAnimator = driver.transform.Find("blender-character-v5@Standing Greeting").GetComponent<Animator>();
        // driverAnimator.SetTrigger("GestureLeft");
        Time.timeScale = 0.4f;
        StartCoroutine(CameraUtils.RotateCameraAroundMovingObject(driver.transform, distance: 0.37f, Vector3.up, 20, 10, Ease.Quadratic));
        driverAnimator.SetTrigger("BreathingIdle");
        driverAnimator.SetTrigger("IdleVariation2");
        Camera.main.transform.position = new Vector3(3.05f, 0.25f, 1.7f);
        Camera.main.transform.rotation = Quaternion.Euler(14f, 1.82f, 0);
        // Camera.main.transform.SetParent(driver.transform);
        // Camera.main.transform.localPosition = new Vector3(0.2f, 1.4f, 2f);
        // Camera.main.transform.localRotation = Quaternion.Euler(30, 180, 0);
        // yield return StartCoroutine(FollowObject(driver.transform, duration: 5));
        yield return new WaitForSeconds(2f);
        Time.timeScale = 0f;
        yield return new WaitForSeconds(1f);
        yield return new WaitForSeconds(4);
        EditorApplication.isPlaying = false;
    }

    // IEnumerator FollowObject(Transform target, float duration)
    // {
    //     Camera.main.transform.position = target.position + target.forward * 1f;
    //     Camera.main.transform.rotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
    //     float startTime = Time.time;
    //     while (Time.time < startTime + duration && target != null)
    //     {
    //         Vector3 normalizedTargetDirection = (target.position - Camera.main.transform.position).normalized;
    //         Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
    //         Vector3 desiredPosition = new Vector3(middlePosition.x, 1.5f, middlePosition.z);
    //         Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
    //         Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.1f);
    //         Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
    //         yield return null;
    //     }
    // }

    DriverPerson CreateGenericDriverPerson()
    {
        return new DriverPerson()
        {
            opportunityCostProfile = DriverPool.normalDriverProfile,
            baseOpportunityCostPerHour = 10,
            preferredSessionLength = 4,
            interval = new SessionInterval()
            {
                startTime = 0,
                endTime = 4
            }
        };
    }
}
