using System;
using UnityEngine;
using System.Collections.Generic;

using Random = System.Random;

public class GridUtils : MonoBehaviour
{

  public static int blockSize = 3;
  static int numXIntersections = 4;
  static int numZIntersections = 4;

  public static float curbHeight = 0.075f;

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
      randomNonIntersection = (float)random.Next(0, onNorthFacingStreet ? (numTilesX * 3 - 1) : numTilesZ * 3 - 1) / 3f;
    } while (randomNonIntersection % blockSize == 0);

    float x = onNorthFacingStreet ? randomIntersection : randomNonIntersection;
    float z = onNorthFacingStreet ? randomNonIntersection : randomIntersection;

    return new Vector3(x, 0.05f, z);
  }

  public static float GetDistance(Vector3 position1, Vector3 position2)
  {
    Queue<Vector3> waypoints = GetWaypoints(position1, position2);
    // Add up the distances between each waypoint
    float distance = 0;
    Vector3 previousWaypoint = position1;
    while (waypoints.Count > 0)
    {
      Vector3 currentWaypoint = waypoints.Dequeue();
      distance += Vector3.Distance(previousWaypoint, currentWaypoint);
      previousWaypoint = currentWaypoint;
    }

    return distance;
  }

  public static Queue<Vector3> GetWaypoints(Vector3 pos1, Vector3 pos2)
  {
    Queue<Vector3> waypoints = new Queue<Vector3>();
    float y = pos1.y;
    Vector3 taxiDirection = pos2 - pos1;
    if ((pos1.x % GridUtils.blockSize == 0 && taxiDirection.x == 0) || (pos1.z % GridUtils.blockSize == 0 && taxiDirection.z == 0))
    {
      waypoints.Enqueue(pos2);
      return waypoints;
    }
    if (pos1.x % blockSize != 0)
    {
      float bestFirstIntersectionX = pos1.x > pos2.x ? Mathf.Ceil(pos2.x / blockSize) * blockSize : Mathf.Floor(pos2.x / blockSize) * blockSize;
      waypoints.Enqueue(new Vector3(bestFirstIntersectionX, y, pos1.z));
      if (pos2.x % blockSize != 0)
      {
        float bestSecondIntersectionZ = pos1.z > pos2.z ? Mathf.Ceil(pos2.z / blockSize) * blockSize : Mathf.Floor(pos2.z / blockSize) * blockSize;
        waypoints.Enqueue(new Vector3(bestFirstIntersectionX, y, bestSecondIntersectionZ));
      }
    }
    else
    {
      float bestFirstIntersectionZ = pos1.z > pos2.z ? Mathf.Ceil(pos2.z / blockSize) * blockSize : Mathf.Floor(pos2.z / blockSize) * blockSize;
      waypoints.Enqueue(new Vector3(pos1.x, y, bestFirstIntersectionZ));
      if (pos2.z % blockSize != 0)
      {
        float bestSecondIntersectionX = pos1.x > pos2.x ? Mathf.Ceil(pos2.x / blockSize) * blockSize : Mathf.Floor(pos2.x / blockSize) * blockSize;
        waypoints.Enqueue(new Vector3(bestSecondIntersectionX, y, bestFirstIntersectionZ));
      }
    }
    waypoints.Enqueue(pos2);
    return waypoints;
  }

  public static Transform GenerateStreetGrid(Transform parent)
  {
    // Create an empty game object to hold the street grid
    Transform grid = new GameObject("StreetGrid").transform;
    grid.parent = parent;
    grid.localPosition = new Vector3(4.5f, 0, 4.5f);
    Transform intersectionPrefab = Resources.Load<Transform>("Intersection");
    Transform streetPrefab = Resources.Load<Transform>("Street");
    Transform grassTilePrefab = Resources.Load<Transform>("GrassTile");
    Transform buildingBlockPrefab = Resources.Load<Transform>("BuildingBlock");
    Transform centerBuildingBlockPrefab = Resources.Load<Transform>("CenterBuildingBlock");
    Transform intersection3WayPrefab = Resources.Load<Transform>("Intersection3Way");
    Transform streetCurvePrefab = Resources.Load<Transform>("StreetCurve");

    Transform tiles = new GameObject("Tiles").transform;
    Transform buildingBlocks = new GameObject("BuildingBlocks").transform;
    tiles.parent = grid;
    tiles.localPosition = Vector3.zero;
    buildingBlocks.parent = grid;
    buildingBlocks.localPosition = Vector3.zero;

    // Generate the street grid
    for (int x = 0; x < numTilesX; x++)
    {
      for (int z = 0; z < numTilesZ; z++)
      {
        if (x % (blockSize) == 0 && z % (blockSize) == 0)
        {
          bool isCorner = (x == 0 && z == 0) || (x == 0 && z == numTilesZ - 1) || (x == numTilesX - 1 && z == 0) || (x == numTilesX - 1 && z == numTilesZ - 1);
          bool isXEdge = x == 0 || x == numTilesX - 1;
          bool isZEdge = z == 0 || z == numTilesZ - 1;
          bool is3Way = (isXEdge && !isZEdge) || (!isXEdge && isZEdge);
          if (isCorner)
          {
            Transform curveStreet = Instantiate(streetCurvePrefab, tiles, false);
            curveStreet.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
            curveStreet.name = "StreetCurve (" + x + ", " + z + ")";
            if (x == 0 && z == numTilesZ - 1)
            {
              curveStreet.rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (x == 0 && z == 0)
            {
              curveStreet.rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (x == numTilesX - 1 && z == numTilesZ - 1)
            {
              curveStreet.rotation = Quaternion.Euler(0, 270, 0);
            }
          }
          else if (is3Way)
          {
            Transform intersection = Instantiate(intersection3WayPrefab, tiles, false);
            intersection.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
            intersection.name = "Intersection3Way (" + x + ", " + z + ")";
            if (x == 0)
            {
              intersection.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (x == numTilesX - 1)
            {
              intersection.rotation = Quaternion.Euler(0, 180, 0);
            }
            else if (z == 0)
            {
              intersection.rotation = Quaternion.Euler(0, 270, 0);
            }
            else if (z == numTilesZ - 1)
            {
              intersection.rotation = Quaternion.Euler(0, 90, 0);
            }
          }
          else
          {
            Transform intersection = Instantiate(intersectionPrefab, tiles, false);
            intersection.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
            intersection.name = "Intersection (" + x + ", " + z + ")";
          }

          if (x < numTilesX - 1 && z < numTilesZ - 1)

          {
            if (x == 3 && z == 3)
            {
              Transform centerBuildingBlock = Instantiate(centerBuildingBlockPrefab, buildingBlocks, false);
              centerBuildingBlock.localPosition = new Vector3(x - blockSize, 0, z - blockSize);
            }
            else
            {
              // Create a building block at the intersection (except for the center intersection)
              Transform buildingBlock = Instantiate(buildingBlockPrefab, buildingBlocks, false);
              buildingBlock.localPosition = new Vector3(x - blockSize, 0, z - blockSize);
              buildingBlock.name = "BuildingBlock (" + x + ", " + z + ")";
              if (x / blockSize == 1 || z / blockSize == 1)
              {
                buildingBlock.localScale = new Vector3(-1, 1, 1);
              }
              buildingBlock.rotation = Quaternion.Euler(0, 90 * (x / blockSize + z / blockSize), 0);
            }
          }
        }
        else if (x % (blockSize) == 0)
        {
          // Rotate the street 90 degrees in the y direction
          Transform street = Instantiate(streetPrefab, tiles, false);
          street.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
          street.rotation = Quaternion.Euler(0, 90, 0);
          street.name = "Street (" + x + ", " + z + ")";
        }
        else if (z % (blockSize) == 0)
        {
          Transform street = Instantiate(streetPrefab, tiles);
          street.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
          street.name = "Street (" + x + ", " + z + ")";
        }
        else
        {
          Transform grassTile = Instantiate(grassTilePrefab, tiles);
          grassTile.localPosition = new Vector3(x - 4.5f, 0, z - 4.5f);
          grassTile.name = "GrassTile (" + x + ", " + z + ")";
        }
      }
    }


    return grid;

  }
}
