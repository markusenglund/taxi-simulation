using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverviewSceneDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
