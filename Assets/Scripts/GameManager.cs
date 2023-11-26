using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform intersectionPrefab;
    [SerializeField] public Transform streetPrefab;
    [SerializeField] public Transform passengerPrefab;

    private float passengerSpawnRate = 0.01f;


    void Awake()
    {

        GenerateStreetGrid();

        // Create 16 passengers in random places
        for (int i = 0; i < 16; i++)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            Transform passenger = PassengerBehavior.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }


        // Create 16 taxis in random places
        for (int i = 0; i < 16; i++)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            Transform taxi = TaxiBehavior.Create(taxiPrefab, randomPosition.x, randomPosition.z);
        }
    }

    void GenerateStreetGrid()
    {
        // Generate the street grid

        int numStreetTilesBetweenIntersections = 3;
        int numXIntersections = 4;
        int numZIntersections = 4;

        int numTilesX = numStreetTilesBetweenIntersections * (numXIntersections - 1) + numXIntersections;
        int numTilesZ = numStreetTilesBetweenIntersections * (numZIntersections - 1) + numZIntersections;

        for (int x = 0; x < numTilesX; x++)
        {
            for (int z = 0; z < numTilesZ; z++)
            {
                if (x % (numStreetTilesBetweenIntersections + 1) == 0 && z % (numStreetTilesBetweenIntersections + 1) == 0)
                {
                    Transform intersection = Instantiate(intersectionPrefab, new Vector3(x, 0, z), Quaternion.identity);
                    intersection.name = "Intersection (" + x + ", " + z + ")";
                }
                else if (x % (numStreetTilesBetweenIntersections + 1) == 0)
                {
                    // Rotate the street 90 degrees in the y direction
                    Transform street = Instantiate(streetPrefab, new Vector3(x, 0, z), Quaternion.Euler(0, 90, 0));
                    street.name = "Street (" + x + ", " + z + ")";
                }
                else if (z % (numStreetTilesBetweenIntersections + 1) == 0)
                {
                    Transform street = Instantiate(streetPrefab, new Vector3(x, 0, z), Quaternion.identity);
                    street.name = "Street (" + x + ", " + z + ")";
                }
            }
        }

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Spawn a passenger
        if (Random.Range(0f, 1f) < passengerSpawnRate)
        {
            Vector3 randomPosition = Utils.GetRandomPosition();
            Transform passenger = PassengerBehavior.Create(passengerPrefab, randomPosition.x, randomPosition.z);
        }
    }
}
