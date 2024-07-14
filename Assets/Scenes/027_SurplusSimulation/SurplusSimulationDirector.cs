using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class SurplusSimulationDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 4;

    List<City> staticCities = new List<City>();
    List<City> surgeCities = new List<City>();

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(27f, 7f, 4.5f);

    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        // staticCity1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        // surgeCity1 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
        for (int i = 0; i < 20; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            Vector3 cityPositionOffset = i == 0 ? Vector3.zero : new Vector3(0, 0, i * 12);
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
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * -7f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        StartCoroutine(Scene());
    }



    IEnumerator Scene()
    {
        StartCoroutine(SetSimulationStart());
        Quaternion originalRotation = Camera.main.transform.rotation;
        Vector3 newPosition = Camera.main.transform.position + new Vector3(0, 0, 200);
        StartCoroutine(CameraUtils.MoveCamera(newPosition, 55, Ease.Quadratic));
        Quaternion newRotation = Quaternion.Euler(15, 0, 0);
        yield return new WaitForSeconds(2);
        yield return null;
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