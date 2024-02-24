using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassengerIntroSceneDirector : MonoBehaviour
{
    [SerializeField] public Transform passengerPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.transform.position = new Vector3(3, 3, -3);
        Camera.main.transform.rotation = Quaternion.Euler(35, 0, 0);
        Debug.Log("Hello from PassengerIntroSceneDirector!");
        StartCoroutine(Scene());

    }

    IEnumerator Scene()
    {
        StartCoroutine(SpawnGrid(duration: 1));
        yield return new WaitForSeconds(1);
    }

    // IEnumerator SpawnGrid(float duration)
    // {
    //     Transform grid = GridUtils.GenerateStreetGrid(null);
    //     grid.localScale = Vector3.zero;

    //     float startTime = Time.time;
    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         float scaleFactor = EaseOutCubic(t);
    //         grid.localScale = Vector3.one * scaleFactor;
    //         yield return null;
    //     }
    // }

    IEnumerator SpawnGrid(float duration)
    {
        Transform grid = GridUtils.GenerateStreetGrid(null);
        Renderer[] renderers = grid.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        float startTime = Time.time;

        // Sort renderers by distance to Vector3(3,0,3)
        System.Array.Sort(renderers, (a, b) =>
        {
            float distanceA = Vector3.Distance(a.transform.position, new Vector3(3, 0, 3));
            float distanceB = Vector3.Distance(b.transform.position, new Vector3(3, 0, 3));
            return distanceA.CompareTo(distanceB);
        });

        foreach (Renderer renderer in renderers)
        {
            yield return new WaitForSeconds(0.02f);
            renderer.enabled = true;
            StartCoroutine(SpawnGridTile(renderer.transform, duration = 0.5f));

        }
        // while (Time.time < startTime + duration)
        // {
        //     float t = (Time.time - startTime) / duration;
        //     float scaleFactor = EaseOutCubic(t);
        //     grid.localScale = Vector3.one * scaleFactor;
        //     yield return null;
        // }
    }

    IEnumerator SpawnGridTile(Transform tile, float duration)
    {
        tile.localScale = Vector3.zero;
        Vector3 finalPosition = tile.position;
        Vector3 startPosition = new Vector3(finalPosition.x, finalPosition.y - 5, finalPosition.z);
        yield return new WaitForSeconds(0.02f);
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseOutCubic(t);
            tile.localScale = Vector3.one * scaleFactor;
            tile.position = Vector3.Lerp(startPosition, finalPosition, scaleFactor);
            yield return null;
        }
        tile.localScale = Vector3.one;
        tile.position = finalPosition;
    }

    float EaseInOutCubic(float t)
    {
        float t2;
        if (t <= 0.5f)
        {
            t2 = Mathf.Pow(t * 2, 3) / 2;
        }
        else
        {
            t2 = (2 - Mathf.Pow((1 - t) * 2, 3)) / 2;
        }
        return t2;
    }

    float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }

    float EaseInCubic(float t)
    {
        return Mathf.Pow(t, 3);
    }


}
