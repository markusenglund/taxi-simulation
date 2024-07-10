using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MegaSimulationDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticPriceSettings;
    [SerializeField] public SimulationSettings surgePriceSettings;
    [SerializeField] public GraphSettings graphSettings;

    float simulationStartTime = 0;

    List<City> cities = new List<City>();

    Vector3 city1Position = new Vector3(0f, 0, 0f);
    Vector3 city2Position = new Vector3(12f, 0, 0f);
    Vector3 middlePosition = new Vector3(6 + 4.5f, 0, 4.5f);
    Vector3 cameraPosition = new Vector3(10.5f, 10f, -10f);

    bool hasSavedPassengerData = false;

    // A set of passenger IDs that have already spawned a PassengerStats object
    void Awake()
    {
        Time.captureFramerate = 60;
        // staticCity1 = City.Create(cityPrefab, city1Position.x, city1Position.y, staticPriceSettings, graphSettings);
        // surgeCity1 = City.Create(cityPrefab, city2Position.x, city2Position.y, surgePriceSettings, graphSettings);
        for (int i = 0; i < 2; i++)
        {
            SimulationSettings staticPriceSettingsClone = Instantiate(staticPriceSettings);
            staticPriceSettingsClone.randomSeed = i;
            City staticCity = City.Create(cityPrefab, city1Position.x, city1Position.y + 12 * i, staticPriceSettingsClone, graphSettings);
            SimulationSettings surgePriceSettingsClone = Instantiate(surgePriceSettings);
            surgePriceSettingsClone.randomSeed = i;
            City surgeCity = City.Create(cityPrefab, city2Position.x, city2Position.y + 12 * i, surgePriceSettingsClone, graphSettings);
            cities.Add(staticCity);
            cities.Add(surgeCity);
        }
    }




    void Start()
    {

        Camera.main.transform.position = cameraPosition;
        Vector3 cameraLookAtPosition = middlePosition + Vector3.up * 2f;
        Camera.main.transform.LookAt(cameraLookAtPosition);
        Camera.main.fieldOfView = 45f;
        TimeUtils.SetSimulationStartTime(simulationStartTime);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        foreach (City city in cities)
        {
            StartCoroutine(city.StartSimulation());
        }
        yield return null;
    }
}
