using System.Collections;
using UnityEditor;
using UnityEngine;


public class SlowRotatingScene : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    Vector3 middlePosition = new Vector3(4.5f, -2, 4.5f);
    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
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
        yield return new WaitForSeconds(20);
        Time.timeScale = 1;
        StartCoroutine(CameraUtils.RotateCameraAround(middlePosition, Vector3.up, 360, 30, Ease.Linear));
        yield return new WaitForSeconds(30);

        EditorApplication.isPlaying = false;
    }
}
