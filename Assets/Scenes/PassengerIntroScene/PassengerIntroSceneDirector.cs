using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        Vector3 spawnAround = new Vector3(3, 0, 6);
        System.Array.Sort(renderers, (a, b) =>
        {
            float distanceA = Vector3.Distance(a.transform.position, spawnAround);
            float distanceB = Vector3.Distance(b.transform.position, spawnAround);
            return distanceA.CompareTo(distanceB);
        });

        foreach (Renderer renderer in renderers)
        {
            yield return new WaitForSeconds(0.015f);
            StartCoroutine(SpawnGridTile(renderer, duration = 0.5f));

        }
        // while (Time.time < startTime + duration)
        // {
        //     float t = (Time.time - startTime) / duration;
        //     float scaleFactor = EaseOutCubic(t);
        //     grid.localScale = Vector3.one * scaleFactor;
        //     yield return null;
        // }
    }

    IEnumerator SpawnGridTile(Renderer tileRenderer, float duration)
    {
        tileRenderer.enabled = true;

        Material[] originalMaterials = tileRenderer.materials;
        for (int i = 0; i < tileRenderer.materials.Count(); i++)
        {
            tileRenderer.materials[i] = new Material(tileRenderer.materials[i]);
        }
        Color[] finalColors = tileRenderer.materials.Select(material => material.color).ToArray();
        Color[] startColors = finalColors.Select(color => new Color(color.r, color.g, color.b, 0)).ToArray();

        foreach (Material material in tileRenderer.materials)
        {
            material.color = new Color(material.color.r, material.color.g, material.color.b, 0);
        }


        Transform tile = tileRenderer.transform;
        tile.localScale = Vector3.zero;
        Vector3 finalPosition = tile.position;
        Vector3 startPosition = new Vector3(finalPosition.x, finalPosition.y - 5, finalPosition.z);
        yield return new WaitForSeconds(0.02f);
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseOutCubic(t);
            float transparencyFactor = EaseInCubic(t);
            tile.localScale = Vector3.one * scaleFactor;
            tile.position = Vector3.Lerp(startPosition, finalPosition, scaleFactor);
            for (int i = 0; i < tileRenderer.materials.Count(); i++)
            {
                tileRenderer.materials[i].color = Color.Lerp(startColors[i], finalColors[i], t * 1.5f);
            }
            yield return null;
        }
        tile.localScale = Vector3.one;
        tile.position = finalPosition;
        for (int i = 0; i < tileRenderer.materials.Count(); i++)
        {
            tileRenderer.materials[i].color = finalColors[i];
        }
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
