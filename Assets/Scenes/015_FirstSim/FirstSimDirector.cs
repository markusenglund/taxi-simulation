using System.Collections;
using UnityEngine;
using Random = System.Random;


public class FirstSimDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;

    Vector3 rotateAroundPosition = new Vector3(4.5f, -3.5f, 4.5f);
    City city;
    public Random driverSpawnRandom;

    void Awake()
    {
        city = City.Create(cityPrefab, -4.5f, 0, simSettings, graphSettings);
    }

    void Start()
    {
        driverSpawnRandom = new Random(simSettings.randomSeed);

        Camera.main.transform.position = new Vector3(4.5f, 11f, 0);
        Camera.main.transform.LookAt(rotateAroundPosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        PredictedSupplyDemandGraph.Create(city);
        StartCoroutine(CameraUtils.RotateCameraAround(rotateAroundPosition + new Vector3(-4.5f, 0, 0f), Vector3.up, -360, 80, Ease.Linear));
        yield return null;
    }
}
