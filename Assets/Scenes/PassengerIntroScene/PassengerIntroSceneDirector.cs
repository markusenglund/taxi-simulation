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

    IEnumerator SpawnGrid(float duration)
    {
        Transform grid = GridUtils.GenerateStreetGrid(null);
        grid.localScale = Vector3.zero;

        Vector3 finalPosition = grid.position;
        grid.position = finalPosition - Vector3.up * 2;

        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseOutCubic(t);
            grid.localScale = Vector3.one * scaleFactor;
            grid.position = Vector3.Lerp(grid.position, finalPosition, t);
            yield return null;
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


}
