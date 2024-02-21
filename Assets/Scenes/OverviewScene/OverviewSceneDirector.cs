using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;


public enum Ease
{
    Cubic,
    QuadraticIn,
    Linear
}

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

    void Start()
    {
        Camera.main.transform.position = new Vector3(3, 500, 3);
        Camera.main.transform.rotation = Quaternion.Euler(90, 0, 0);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        StartCoroutine(MoveCamera(toPosition: new Vector3(3, 8, 3), duration: 1.5f, ease: Ease.Cubic));
        yield return new WaitForSeconds(2);
        StartCoroutine(RotateCameraAround(new Vector3(3, 1, 3), new Vector3(1, 0, 0), -70, 3));
        yield return new WaitForSeconds(4);
        Camera.main.transform.position = new Vector3(-0.5f, 1, -0.5f);
        Camera.main.transform.rotation = Quaternion.Euler(8, 45, 0);
        StartCoroutine(MoveCamera(toPosition: new Vector3(3, 1, 3), duration: 6, ease: Ease.Linear));
        yield return new WaitForSeconds(2);

        StartCoroutine(RotateCamera(Quaternion.Euler(30, 45, 0), duration: 4, ease: Ease.QuadraticIn));

        yield return new WaitForSeconds(4);
        EditorApplication.isPlaying = false;
    }

    IEnumerator MoveCamera(Vector3 toPosition, float duration, Ease ease)
    {
        float startTime = Time.time;
        Vector3 startPosition = Camera.main.transform.position;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.Cubic)
            {
                t = EaseInOutCubic(t);
            }
            else if (ease == Ease.Linear)
            {
            }
            Camera.main.transform.position = Vector3.Lerp(startPosition, toPosition, t);
            yield return null;
        }
        Camera.main.transform.position = toPosition;
    }


    IEnumerator RotateCameraAround(Vector3 point, Vector3 axis, float angle, float duration)
    {
        float startTime = Time.time;
        float prevT = 0;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseInOutCubic(t);
            Camera.main.transform.RotateAround(point, axis, angle * (t - prevT));
            prevT = t;
            yield return null;
        }
    }

    IEnumerator RotateCamera(Quaternion toRotation, float duration, Ease ease)
    {
        float startTime = Time.time;
        Quaternion startRotation = Camera.main.transform.rotation;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.QuadraticIn)
            {
                t = EaseInQuadratic(t);
            }
            else if (ease == Ease.Cubic)
            {
                t = EaseInOutCubic(t);
            }
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, toRotation, t);
            yield return null;
        }
        Camera.main.transform.rotation = toRotation;
    }


    float EaseInQuadratic(float t)
    {
        return t * t;
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
}
