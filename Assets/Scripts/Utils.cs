using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{

    public static int blockSize = 2;
    static int numXIntersections = 4;
    static int numZIntersections = 4;

    static int numTilesX = blockSize * (numXIntersections - 1) + 1;
    static int numTilesZ = blockSize * (numZIntersections - 1) + 1;

    public static Vector3 GetRandomPosition()
    {
        bool onNorthFacingStreet = UnityEngine.Random.Range(0, 2) == 0;
        int randomEveryIntersection = UnityEngine.Random.Range(0, onNorthFacingStreet ? numXIntersections : numZIntersections) * blockSize;
        int randomEach = UnityEngine.Random.Range(0, onNorthFacingStreet ? numTilesX : numTilesZ);

        int x = onNorthFacingStreet ? randomEveryIntersection : randomEach;
        int z = onNorthFacingStreet ? randomEach : randomEveryIntersection;

        return new Vector3(x, 0.05f, z);
    }

    public static void GenerateStreetGrid(Transform intersectionPrefab, Transform streetPrefab)
    {
        // Generate the street grid
        for (int x = 0; x < numTilesX; x++)
        {
            for (int z = 0; z < numTilesZ; z++)
            {
                if (x % (blockSize) == 0 && z % (blockSize) == 0)
                {
                    Transform intersection = Instantiate(intersectionPrefab, new Vector3(x, 0, z), Quaternion.identity);
                    intersection.name = "Intersection (" + x + ", " + z + ")";
                }
                else if (x % (blockSize) == 0)
                {
                    // Rotate the street 90 degrees in the y direction
                    Transform street = Instantiate(streetPrefab, new Vector3(x, 0, z), Quaternion.Euler(0, 90, 0));
                    street.name = "Street (" + x + ", " + z + ")";
                }
                else if (z % (blockSize) == 0)
                {
                    Transform street = Instantiate(streetPrefab, new Vector3(x, 0, z), Quaternion.identity);
                    street.name = "Street (" + x + ", " + z + ")";
                }
            }
        }

    }
}
