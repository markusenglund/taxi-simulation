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
            else if (ease == Ease.Linear)
            {
            }
            Camera.main.transform.position = Vector3.Lerp(startPosition, toPosition, t);
            yield return null;
        }
        Camera.main.transform.position = toPosition;
    }

    public static IEnumerator RotateCameraAround(Vector3 point, Vector3 axis, float angle, float duration)
    {
        float startTime = Time.time;
        float prevT = 0;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseUtils.EaseInOutCubic(t);
            Camera.main.transform.RotateAround(point, axis, angle * (t - prevT));
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
