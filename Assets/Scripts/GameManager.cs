using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private Transform cityPrefab;

    City city1;
    City city2;

    void Awake()
    {
        Instance = this;
        city1 = City.Create(cityPrefab, 0, 0);
        city2 = City.Create(cityPrefab, 8, 0);
    }
}
