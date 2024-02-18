using System;
using UnityEngine;

using Random = System.Random;

public class GridUtils : MonoBehaviour
{

    public static int blockSize = 2;
    static int numXIntersections = 4;
    static int numZIntersections = 4;

    static int numTilesX = blockSize * (numXIntersections - 1) + 1;
    static int numTilesZ = blockSize * (numZIntersections - 1) + 1;

    public static Vector3 GetRandomPosition(Random random)
    {
        bool onNorthFacingStreet = random.Next(0, 2) == 0;
        int randomIntersection = random.Next(0, onNorthFacingStreet ? numXIntersections : numZIntersections) * blockSize;

        // Get a random position on the street grid that does not include the intersections
        float randomNonIntersection;
        do
        {
            randomNonIntersection = (float)random.Next(0, onNorthFacingStreet ? numTilesX * 3 : numTilesZ * 3) / 3f;
        } while (randomNonIntersection % blockSize == 0);

        float x = onNorthFacingStreet ? randomIntersection : randomNonIntersection;
        float z = onNorthFacingStreet ? randomNonIntersection : randomIntersection;

        return new Vector3(x, 0.05f, z);
    }

    public static float GetDistance(Vector3 position1, Vector3 position2)
    {
        return Math.Abs(position1.x - position2.x) + Math.Abs(position1.z - position2.z);
    }

    public static void GenerateStreetGrid(Transform parent)
    {
        Transform intersectionPrefab = Resources.Load<Transform>("Intersection");
        Transform streetPrefab = Resources.Load<Transform>("Street");
        // Generate the street grid
        for (int x = 0; x < numTilesX; x++)
        {
            for (int z = 0; z < numTilesZ; z++)
            {
                if (x % (blockSize) == 0 && z % (blockSize) == 0)
                {
                    Transform intersection = Instantiate(intersectionPrefab, parent, false);
                    intersection.localPosition = new Vector3(x, 0, z);
                    intersection.name = "Intersection (" + x + ", " + z + ")";
                }
                else if (x % (blockSize) == 0)
                {
                    // Rotate the street 90 degrees in the y direction
                    Transform street = Instantiate(streetPrefab, parent, false);
                    street.localPosition = new Vector3(x, 0, z);
                    street.rotation = Quaternion.Euler(0, 90, 0);
                    street.name = "Street (" + x + ", " + z + ")";
                }
                else if (z % (blockSize) == 0)
                {
                    Transform street = Instantiate(streetPrefab, parent);
                    street.localPosition = new Vector3(x, 0, z);
                    street.name = "Street (" + x + ", " + z + ")";
                }
            }
        }

    }
}
