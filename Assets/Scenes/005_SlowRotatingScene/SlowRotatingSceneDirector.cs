using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;


public class SlowRotatingScene : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings staticSimSettings;
    [SerializeField] public SimulationSettings surgeSimSettings;
    [SerializeField] public GraphSettings graphSettings;
    Vector3 city1MiddlePosition = new Vector3(4.5f, -2, 4.5f);
    Vector3 middlePosition = new Vector3(10.5f, -2, 4.5f);
    City city1;
    City city2;
    void Awake()
    {
        city1 = City.Create(cityPrefab, 0, 0, staticSimSettings, graphSettings);
        StartCoroutine(city1.StartSimulation());

        city2 = City.Create(cityPrefab, 12, 0, surgeSimSettings, graphSettings);
        StartCoroutine(city2.StartSimulation());

        city2.transform.position = new Vector3(12, -100, 0);
        Time.captureFramerate = 60;

    }

    void Start()
    {
        Camera.main.transform.position = new Vector3(-2f, 4.5f, -2f);
        Camera.main.transform.LookAt(city1MiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        // Time.timeScale = 10;
        // yield return new WaitForSeconds(10);
        // Time.timeScale = 1;
        StartCoroutine(CameraUtils.RotateCameraAround(city1MiddlePosition, Vector3.up, -405, 25, Ease.Linear));
        yield return new WaitForSeconds(25);
        // StartCoroutine(CameraUtils.MoveCamera(new Vector3(-8f, 6f, -1.5f), duration: 3, Ease.Cubic));
        // StartCoroutine(CameraUtils.RotateCameraAround(middlePosition, Vector3.up, -90, 3, Ease.Linear));
        Vector3 finalPosition = new Vector3(10.5f, 11f, -6f);
        StartCoroutine(CameraUtils.MoveAndRotateCameraLocal(finalPosition, Quaternion.LookRotation(middlePosition - finalPosition), duration: 4, Ease.QuadraticOut));
        yield return new WaitForSeconds(1);
        StartCoroutine(SpawnCity(duration: 2f));
        yield return new WaitForSeconds(10);

        EditorApplication.isPlaying = false;
    }

    IEnumerator SpawnCity(float duration)
    {
        float startTime = Time.time;
        Vector3 startScale = Vector3.zero;
        Vector3 finalScale = Vector3.one;
        city2.transform.position = new Vector3(12, 0, 0);
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseInOutCubic(t);
            city2.transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
            yield return null;
        }
        city2.transform.localScale = finalScale;
    }

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,        // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
    }


    public void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Opaque:
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;
            case BlendMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 2450;
                break;
            case BlendMode.Fade:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
            case BlendMode.Transparent:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                break;
        }
    }
}
