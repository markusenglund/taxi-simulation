using System.Collections;
using UnityEditor;
using UnityEngine;


public class SlowRotatingScene : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    Vector3 middlePosition = new Vector3(4.5f, -2, 4.5f);
    City city1;
    City city2;
    void Awake()
    {
        city1 = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        city2 = City.Create(cityPrefab, 0, -12, simSettings, graphSettings);
        city2.transform.position = new Vector3(0, -100, -12);
    }

    void Start()
    {
        Camera.main.transform.position = new Vector3(-2f, 4.5f, -2f);
        Camera.main.transform.LookAt(middlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Time.timeScale = 10;
        yield return new WaitForSeconds(10);
        Time.timeScale = 1;
        StartCoroutine(CameraUtils.RotateCameraAround(middlePosition, Vector3.up, -315, 4, Ease.Linear));
        yield return new WaitForSeconds(4);
        StartCoroutine(CameraUtils.MoveCamera(new Vector3(-8f, 6f, -1.5f), duration: 3, Ease.Linear));
        yield return new WaitForSeconds(2);
        StartCoroutine(SpawnCity(duration: 1));
        yield return new WaitForSeconds(10);

        EditorApplication.isPlaying = false;
    }

    IEnumerator SpawnCity(float duration)
    {
        city2.transform.position = new Vector3(0, 0, -12);
        Vector3 startScale = Vector3.one * 0.1f;
        city2.transform.localScale = startScale;
        Vector3 finalScale = Vector3.one;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            city2.transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }

        city2.transform.localScale = finalScale;
    }
}
