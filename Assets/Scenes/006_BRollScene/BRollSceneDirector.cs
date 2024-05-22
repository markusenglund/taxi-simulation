using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;


public class BRollDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 cameraStartPosition = new Vector3(3f, 1.8f, 0f);
    Quaternion cameraStartRotation = Quaternion.Euler(90, 90, 0);
    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        StartCoroutine(city.StartSimulation());

        Time.captureFramerate = 60;
    }

    void Start()
    {
        Camera.main.transform.position = cameraStartPosition;
        Camera.main.transform.rotation = cameraStartRotation;
        Time.timeScale = 0.8f;
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        StartCoroutine(CameraUtils.MoveCamera(cameraStartPosition + new Vector3(0, 0, 9), duration: 25, Ease.Linear));
        yield return new WaitForSeconds(25);

        EditorApplication.isPlaying = false;
    }
}
