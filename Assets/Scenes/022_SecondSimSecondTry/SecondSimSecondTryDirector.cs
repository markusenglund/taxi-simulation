using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SecondSimSecondTryDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0.1f;

    City city1;
    City city2;

    Vector3 city1Position = new Vector3(0f, 0, 0f);

    Vector3 city2Position = new Vector3(0, -14, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, -9.5f);
    Vector3 cameraEndPosition = new Vector3(8.5f, 10.8f, -28f);

    void Awake()
    {
        Time.captureFramerate = 60;
        city1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        city2 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
    }

    void Start()
    {

        Camera.main.transform.position = cameraEndPosition;
        Camera.main.transform.rotation = new Quaternion(-0.287f, 0, 0, -0.955f);
        Camera.main.fieldOfView = 45f;
        StartCoroutine(Scene());
        RectTransform city1WorldCanvas = city1.transform.Find("WorldSpaceCanvas").GetComponent<RectTransform>();
        city1WorldCanvas.position = new Vector3(0, 0.8f, -1);
        city1WorldCanvas.rotation = Quaternion.Euler(67, 0, 0);
        RectTransform city2WorldCanvas = city2.transform.Find("WorldSpaceCanvas").GetComponent<RectTransform>();
        city2WorldCanvas.localPosition = new Vector3(0, 0.8f, -0.7f);
        city2WorldCanvas.rotation = Quaternion.Euler(67, 0, 0);
    }


    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        yield return null;
    }

    IEnumerator SetSimulationStart()
    {
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        yield return new WaitForSeconds(simulationStartTime);
        StartCoroutine(city1.StartSimulation());
        StartCoroutine(city2.StartSimulation());
        // FareGraph.Create(city1, city2);
        // WaitingGraph.Create(city1, city2, waitTime: 7);
    }

}
