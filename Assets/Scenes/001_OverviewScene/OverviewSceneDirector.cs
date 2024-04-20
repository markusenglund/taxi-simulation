using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;


public enum Ease
{
    Cubic,
    QuadraticIn,
    QuadraticOut,
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
        Camera.main.transform.position = new Vector3(4.5f, 500, 4.5f);
        Camera.main.transform.rotation = Quaternion.Euler(90, 0, 0);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Time.timeScale = 10;
        yield return new WaitForSeconds(20);
        Time.timeScale = 1;
        StartCoroutine(MoveCamera(toPosition: new Vector3(4.5f, 11.3f, 4.5f), duration: 1.5f, ease: Ease.Cubic));
        yield return new WaitForSeconds(2);
        StartCoroutine(RotateCameraAround(new Vector3(5.5f, 1, 5.5f), new Vector3(1, 0, 0), -70, 3));
        yield return new WaitForSeconds(10);
        EditorApplication.isPlaying = false;
    }

    IEnumerator FollowObject(Transform target, float duration)
    {
        // Camera.main.transform.position = target.position + target.up * 1f;
        // Camera.main.transform.rotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
        float startTime = Time.time;
        while (Time.time < startTime + duration && target != null)
        {
            Vector3 normalizedTargetDirection = (target.position - Camera.main.transform.position).normalized;
            Vector3 middlePosition = target.position - normalizedTargetDirection * 0.8f;
            Vector3 desiredPosition = new Vector3(middlePosition.x, 1.5f, middlePosition.z);
            Quaternion desiredRotation = Quaternion.LookRotation(target.position - Camera.main.transform.position);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, 0.1f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, desiredRotation, 0.003f);
            yield return null;
        }
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
