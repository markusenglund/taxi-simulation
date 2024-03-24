using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUtils : MonoBehaviour
{
    public static IEnumerator MoveCamera(Vector3 toPosition, float duration, Ease ease)
    {
        float startTime = Time.time;
        Vector3 startPosition = Camera.main.transform.position;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.Cubic)
            {
                t = EaseUtils.EaseInOutCubic(t);
            }
            else if (ease == Ease.QuadraticOut)
            {
                t = EaseUtils.EaseOutQuadratic(t);
            }
            else if (ease == Ease.Linear)
            {
            }
            Camera.main.transform.position = Vector3.Lerp(startPosition, toPosition, t);
            yield return null;
        }
        Camera.main.transform.position = toPosition;
    }

    public static IEnumerator MoveCameraLocal(Vector3 toPosition, float duration, Ease ease)
    {
        float startTime = Time.time;
        Vector3 startPosition = Camera.main.transform.localPosition;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.Cubic)
            {
                t = EaseUtils.EaseInOutCubic(t);
            }
            else if (ease == Ease.Linear)
            {
            }
            Camera.main.transform.localPosition = Vector3.Lerp(startPosition, toPosition, t);
            yield return null;
        }
        Camera.main.transform.localPosition = toPosition;
    }

    public static IEnumerator MoveAndRotateCameraLocal(Vector3 finalPosition, Quaternion finalRotation, float duration, Ease ease = Ease.Cubic)
    {
        Vector3 startPosition = Camera.main.transform.localPosition;
        Quaternion startRotation = Camera.main.transform.localRotation;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.Cubic)
            {
                t = EaseUtils.EaseInOutCubic(t);
            }
            else if (ease == Ease.QuadraticOut)
            {
                t = EaseUtils.EaseOutQuadratic(t);
            }
            Camera.main.transform.localPosition = Vector3.Lerp(startPosition, finalPosition, t);
            Camera.main.transform.localRotation = Quaternion.Lerp(startRotation, finalRotation, t);
            yield return null;
        }
        Camera.main.transform.localPosition = finalPosition;
    }

    public static IEnumerator RotateCameraAround(Vector3 point, Vector3 axis, float angle, float duration, Ease ease)
    {
        float startTime = Time.time;
        float prevT = 0;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.Cubic)
            {
                t = EaseUtils.EaseInOutCubic(t);
            }
            else if (ease == Ease.Linear)
            {
            }
            Camera.main.transform.RotateAround(point, axis, angle * (t - prevT));
            prevT = t;
            yield return null;
        }
    }

    public static IEnumerator RotateCameraAroundMovingObject(Transform target, float distance, Vector3 axis, float angle, float duration)
    {
        float startTime = Time.time;
        float prevT = 0;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseUtils.EaseInOutCubic(t);
            Camera.main.transform.position = target.position + (Camera.main.transform.position - target.position).normalized * distance;
            Camera.main.transform.RotateAround(target.position, axis, angle * (t - prevT));
            prevT = t;
            yield return null;
        }
    }

    public static IEnumerator RotateCamera(Quaternion toRotation, float duration, Ease ease)
    {
        float startTime = Time.time;
        Quaternion startRotation = Camera.main.transform.rotation;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            if (ease == Ease.QuadraticIn)
            {
                t = EaseUtils.EaseInQuadratic(t);
            }
            else if (ease == Ease.Cubic)
            {
                t = EaseUtils.EaseInOutCubic(t);
            }
            Camera.main.transform.rotation = Quaternion.Slerp(startRotation, toRotation, t);
            yield return null;
        }
        Camera.main.transform.rotation = toRotation;
    }
}
