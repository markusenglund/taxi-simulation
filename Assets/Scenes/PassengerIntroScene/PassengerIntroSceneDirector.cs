using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Random = System.Random;

public class PassengerIntroSceneDirector : MonoBehaviour
{
    [SerializeField] public Transform spawnAnimationPrefab;
    [SerializeField] public Transform passengerPrefab;
    [SerializeField] public SimulationSettings simSettings;

    Transform passenger;
    Vector3 passengerPosition = new Vector3(1.7f, 0.08f, 0);
    // Vector3 closeUpCameraPosition = new Vector3(1.7f, 0.4f, -0.2f);
    public Random passengerSpawnRandom;




    // Start is called before the first frame update
    void Start()
    {
        passengerSpawnRandom = new Random(1);

        Camera.main.transform.position = new Vector3(3, 3, -3);
        Camera.main.transform.rotation = Quaternion.Euler(35, 0, 0);
        StartCoroutine(Scene());

    }

    IEnumerator Scene()
    {
        StartCoroutine(SpawnGrid());
        yield return new WaitForSeconds(1);
        PassengerBase passenger = PassengerBase.Create(passengerPrefab, passengerPosition, 1.5f, passengerSpawnRandom, simSettings);
        Vector3 closeUpCameraPosition = new Vector3(passenger.transform.position.x, 0.2f, passenger.transform.position.z - 0.2f);
        StartCoroutine(MoveCamera(closeUpCameraPosition, 1));
    }

    IEnumerator MoveCamera(Vector3 finalPosition, float duration)
    {
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;
        Quaternion finalRotation = Quaternion.Euler(15, 0, 0);
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseInOutCubic(t);
            Camera.main.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
            Camera.main.transform.rotation = Quaternion.Lerp(startRotation, finalRotation, t);
            yield return null;
        }
        Camera.main.transform.position = finalPosition;
    }

    IEnumerator SpawnGrid()
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
            StartCoroutine(SpawnGridTile(renderer, duration: 0.5f));

        }
    }

    IEnumerator SpawnGridTile(Renderer tileRenderer, float duration)
    {
        tileRenderer.enabled = true;

        Material[] originalMaterials = tileRenderer.materials;
        for (int i = 0; i < tileRenderer.materials.Count(); i++)
        {
            tileRenderer.materials[i] = new Material(tileRenderer.materials[i]);
            SetupMaterialWithBlendMode(tileRenderer.materials[i], BlendMode.Transparent);
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
            SetupMaterialWithBlendMode(tileRenderer.materials[i], BlendMode.Opaque);

        }
        yield return null;
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

    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,        // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
    }

    // https://forum.unity.com/threads/standard-material-shader-ignoring-setfloat-property-_mode.344557/
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


}
