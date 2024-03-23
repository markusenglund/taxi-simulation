using System.Collections;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


public class PassengersSpawningSceneDirector : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    [SerializeField] public Transform passengerPrefab;
    public Random passengerSpawnRandom;

    City city;
    void Awake()
    {
        city = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
    }

    void Start()
    {
        passengerSpawnRandom = new Random(1);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Camera.main.transform.position = new Vector3(-1f, 0.7f, 3f);
        Camera.main.transform.rotation = Quaternion.Euler(10, 90, 0);
        StartCoroutine(CameraUtils.MoveCamera(toPosition: new Vector3(6f, 1, 3f), duration: 6, ease: Ease.Linear));
        yield return new WaitForSeconds(0.3f);
        PassengerBase.CreateRaw(passengerPrefab, new Vector3(2, 0.08f, 2.77f), Quaternion.identity, spawnDuration: 1, passengerSpawnRandom, simSettings);
        yield return new WaitForSeconds(0.7f);
        PassengerBase.CreateRaw(passengerPrefab, new Vector3(5, 0.08f, 3.23f), Quaternion.LookRotation(new Vector3(0, 0, -1)), spawnDuration: 1, passengerSpawnRandom, simSettings);
        PassengerBase.CreateRaw(passengerPrefab, new Vector3(7f, 0.08f, 3.23f), Quaternion.LookRotation(new Vector3(0, 0, -1)), spawnDuration: 1, passengerSpawnRandom, simSettings);
        StartCoroutine(CameraUtils.RotateCamera(Quaternion.Euler(30, 90, 0), duration: 5, ease: Ease.QuadraticIn));
        yield return new WaitForSeconds(1.5f);
        PassengerBase.CreateRaw(passengerPrefab, new Vector3(6.23f, 0.08f, 2.5f), Quaternion.LookRotation(new Vector3(-1, 0, 0f)), spawnDuration: 1, passengerSpawnRandom, simSettings);
        yield return new WaitForSeconds(1);
        PassengerBase.CreateRaw(passengerPrefab, new Vector3(9.23f, 0.08f, 2.77f), Quaternion.LookRotation(new Vector3(1, 0, 0f)), spawnDuration: 1, passengerSpawnRandom, simSettings);
        yield return new WaitForSeconds(5);
        EditorApplication.isPlaying = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
