using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;


public class SlowRotatingScene : MonoBehaviour
{
    [SerializeField] private Transform cityPrefab;
    [SerializeField] public SimulationSettings simSettings;
    [SerializeField] public GraphSettings graphSettings;
    Vector3 city1MiddlePosition = new Vector3(4.5f, -2, 4.5f);
    Vector3 middlePosition = new Vector3(10.5f, -2, 4.5f);
    City city1;
    City city2;
    void Awake()
    {
        city1 = City.Create(cityPrefab, 0, 0, simSettings, graphSettings);
        city2 = City.Create(cityPrefab, 12, 0, simSettings, graphSettings);
        city2.transform.position = new Vector3(12, -100, 0);
    }

    void Start()
    {
        Camera.main.transform.position = new Vector3(-2f, 4.5f, -2f);
        Camera.main.transform.LookAt(city1MiddlePosition);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        Time.timeScale = 10;
        yield return new WaitForSeconds(10);
        Time.timeScale = 1;
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

    // IEnumerator SpawnCity(float duration)
    // {
    //     Transform streetGridTransform = city2.transform.Find("StreetGrid");
    //     // Get the scale of all children, instantiate as empty array
    //     Transform[] children = new Transform[city2.transform.childCount];
    //     Vector3[] finalScales = new Vector3[city2.transform.childCount];
    //     for (int i = 0; i < city2.transform.childCount; i++)
    //     {
    //         children[i] = city2.transform.GetChild(i);
    //         finalScales[i] = children[i].localScale;
    //         children[i].localScale = Vector3.zero;
    //     }
    //     city2.transform.position = new Vector3(0, 0, -12);
    //     Vector3 startScale = Vector3.zero;
    //     // city2.transform.localScale = startScale;

    //     Vector3 finalScale = Vector3.one;
    //     float startTime = Time.time;

    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         float scaleFactor = EaseUtils.EaseInOutCubic(t);
    //         // city2.transform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
    //         streetGridTransform.localScale = Vector3.Lerp(startScale, finalScale, scaleFactor);
    //         yield return null;
    //     }

    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         float scaleFactor = EaseUtils.EaseInOutCubic(t);
    //         for (int i = 0; i < children.Count(); i++)
    //         {
    //             children[i].localScale = Vector3.Lerp(startScale, finalScales[i], scaleFactor);
    //         }
    //         yield return null;
    //     }
    //     // Reset start scales
    //     // for (int i = 0; i < finalScales.Count; i++)
    //     // {
    //     //     city2.transform.GetChild(i).localScale = finalScales[i];
    //     // }
    //     for (int i = 0; i < children.Count(); i++)
    //     {
    //         children[i].localScale = finalScales[i];
    //     }
    //     // foreach (Transform child in city2.transform)
    //     // {
    //     //     if (child != streetGridTransform)
    //     //     {
    //     //         child.localScale = Vector3.one;

    //     //     }
    //     // }
    //     city2.transform.localScale = finalScale;
    // }


    // IEnumerator SpawnCity(float duration)
    // {
    //     float startTime = Time.time;

    //     // Get all materials of all children of city2
    //     Material[] city2Materials = city2.GetComponentsInChildren<MeshRenderer>().SelectMany(mr => mr.materials).ToArray();
    //     for (int i = 0; i < city2Materials.Count(); i++)
    //     {
    //         city2Materials[i] = new Material(city2Materials[i]);
    //         SetupMaterialWithBlendMode(city2Materials[i], BlendMode.Transparent);
    //     }

    //     Color[] finalColors = city2Materials.Select(material => material.color).ToArray();
    //     Color[] startColors = finalColors.Select(color => new Color(color.r, color.g, color.b, 0)).ToArray();

    //     city2.transform.position = new Vector3(0, 0, -12);


    //     while (Time.time < startTime + duration)
    //     {
    //         float t = (Time.time - startTime) / duration;
    //         for (int i = 0; i < city2Materials.Count(); i++)
    //         {
    //             city2Materials[i].color = Color.Lerp(startColors[i], finalColors[i], t);
    //         }
    //         yield return null;
    //     }

    //     for (int i = 0; i < city2Materials.Count(); i++)
    //     {
    //         city2Materials[i].color = finalColors[i];
    //         SetupMaterialWithBlendMode(city2Materials[i], BlendMode.Opaque);

    //     }
    // }

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
