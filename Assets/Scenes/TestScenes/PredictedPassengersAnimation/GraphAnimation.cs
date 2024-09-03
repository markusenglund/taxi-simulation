using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GraphAnimation : MonoBehaviour
{
    LineRenderer lineRenderer;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        StartCoroutine(AnimateLine());
    }

    IEnumerator AnimateLine()
    {
        yield return new WaitForSeconds(1);
        float duration = 2f;
        float startTime = Time.time;
        float[] startYPositions = new float[lineRenderer.positionCount];
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 position = lineRenderer.GetPosition(i);
            startYPositions[i] = position.y;
        }
        while (startTime + duration + 2 > Time.time)
        {
            float t = (Time.time - startTime) / duration;
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                Vector3 position = lineRenderer.GetPosition(i);
                float scaleFactor = EaseUtils.EaseInOutCubic(t);
                position.y = Mathf.Lerp(startYPositions[i], 433f, scaleFactor);
                lineRenderer.SetPosition(i, position);
            }
            yield return null;

        }

        yield return new WaitForSeconds(2);
        EditorApplication.isPlaying = false;

        // Gradually move the line to the final position

    }

    // Update is called once per frame
    void Update()
    {

    }
}
