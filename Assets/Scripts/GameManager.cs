using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings sim1Settings;
    [SerializeField] public SimulationSettings sim2Settings;

    City city1;
    City city2;

    void Awake()
    {
        Instance = this;
        city1 = City.Create(cityPrefab, 0, 0, sim1Settings);
        city2 = City.Create(cityPrefab, 8, 0, sim2Settings);
    }
}
