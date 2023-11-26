using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] public Transform taxiPrefab;
    [SerializeField] public Transform intersectionPrefab;
    [SerializeField] public Transform streetPrefab;
    [SerializeField] public Transform passengerPrefab;

    void Awake()
    {

        GenerateStreetGrid();
        PassengerBehavior.Create(passengerPrefab, 0, 0);
        PassengerBehavior.Create(passengerPrefab, 3, 0);
        PassengerBehavior.Create(passengerPrefab, 4, 3);

        TaxiBehavior.Create(taxiPrefab, 0, 0);
        TaxiBehavior.Create(taxiPrefab, 4, 0);
        TaxiBehavior.Create(taxiPrefab, 4, 11);
        TaxiBehavior.Create(taxiPrefab, 8, 7);
        TaxiBehavior.Create(taxiPrefab, 0, 3);
        TaxiBehavior.Create(taxiPrefab, 4, 4);
        TaxiBehavior.Create(taxiPrefab, 4, 5);
        TaxiBehavior.Create(taxiPrefab, 8, 8);
        TaxiBehavior.Create(taxiPrefab, 0, 9);
        TaxiBehavior.Create(taxiPrefab, 4, 10);
        TaxiBehavior.Create(taxiPrefab, 4, 10);
        TaxiBehavior.Create(taxiPrefab, 8, 0);
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

    }
}
