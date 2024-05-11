using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings sim1Settings;
    [SerializeField] public SimulationSettings sim2Settings;
    [SerializeField] public GraphSettings sim1graphSettings;
    [SerializeField] public GraphSettings sim2graphSettings;

    City city1;
    City city2;

    void Awake()
    {
        Instance = this;
        city1 = City.Create(cityPrefab, 0, 0, sim1Settings, sim1graphSettings);
        city2 = City.Create(cityPrefab, 12, 0, sim2Settings, sim2graphSettings);
        StartCoroutine(city1.StartSimulation());
        StartCoroutine(city2.StartSimulation());
    }
}
