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
    [SerializeField] public Transform passengerStatsPrefab;
    [SerializeField] public SimulationSettings simSettings;

    Vector3 passengerPosition = new Vector3(2.5f - 4.5f, 0.08f, -4.3f);
    // Vector3 closeUpCameraPosition = new Vector3(1.7f, 0.4f, -0.2f);
    Animator passengerAnimator;
    Transform grid;
    public Random passengerSpawnRandom;


    void Awake()
    {
        Time.captureFramerate = 60;
    }

    // Start is called before the first frame update
    void Start()
    {
        passengerSpawnRandom = new Random(1);

        Camera.main.transform.position = new Vector3(4.5f, 4, -4);
        Camera.main.transform.rotation = Quaternion.Euler(35, 0, 0);
        StartCoroutine(Scene());

    }

    IEnumerator Scene()
    {
        StartCoroutine(SpawnCity());
        yield return new WaitForSeconds(1.5f);
        Vector3 closeUpCameraPosition = new Vector3(passengerPosition.x, 0.2f, passengerPosition.z - 0.22f) + new Vector3(4.5f, 0, 4.5f);

        StartCoroutine(MoveCamera(closeUpCameraPosition, Quaternion.Euler(15, 0, 0), 2.5f));
        yield return new WaitForSeconds(1f);
        PassengerPerson person = new PassengerPerson(passengerPosition, simSettings, passengerSpawnRandom);
        float hourlyIncome = 45.70f;
        float timePreference = 3.10f;
        float waitingCostPerHour = hourlyIncome * timePreference;
        person.economicParameters = new PassengerEconomicParameters
        {
            hourlyIncome = hourlyIncome,
            timePreference = timePreference,
            waitingCostPerHour = waitingCostPerHour,
            substitutes = person.GenerateSubstitutes(waitingCostPerHour, hourlyIncome)
        };
        float timeHours = 24f / 60f;
        float timeCost = timeHours * waitingCostPerHour;
        float moneyCost = 12f;
        person.uberTripOption = new TripOption
        {
            type = TripType.Uber,
            timeHours = timeHours,
            timeCost = timeCost,
            moneyCost = moneyCost,
            totalCost = moneyCost + timeCost,
        };
        float spawnDuration = 1.2f;
        Passenger passenger = Passenger.Create(person, passengerPrefab, grid, simSettings, null, mode: PassengerMode.Inactive, spawnDuration);
        passenger.transform.rotation = Quaternion.Euler(0, 180, 0);
        passengerAnimator = passenger.GetComponentInChildren<Animator>();
        yield return new WaitForSeconds(0.8f);
        passengerAnimator.SetTrigger("Wave");
        DriverPerson driverPerson = CreateGenericDriverPerson();
        Transform taxiPrefab = Resources.Load<Transform>("Taxi2");
        Driver driver = Driver.Create(driverPerson, taxiPrefab, grid, 1.5f, -4f, simSettings, null, DriverMode.Inactive);
        driver.transform.rotation = Quaternion.Euler(0, 180, 0);
        yield return new WaitForSeconds(6f);
        StartCoroutine(MoveCamera(closeUpCameraPosition, Quaternion.Euler(11, 20, 0), 5));

        yield return new WaitForSeconds(2.5f);
        StartCoroutine(SpawnPassengerStats(passenger));
        yield return new WaitForSeconds(3f);
        passengerAnimator.SetTrigger("LookAtPhone");
        yield return new WaitForSeconds(34);
        passengerAnimator.SetTrigger("GestureLeft");
        StartCoroutine(RotateCameraAround(passenger.transform.position, new Vector3(1, 1, 0), -10, duration: 10));
        yield return new WaitForSeconds(23);
        // Pan to Uber
        EditorApplication.isPlaying = false;
    }

    IEnumerator MoveCamera(Vector3 finalPosition, Quaternion finalRotation, float duration)
    {
        Vector3 startPosition = Camera.main.transform.position;
        Quaternion startRotation = Camera.main.transform.rotation;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            t = EaseUtils.EaseInOutCubic(t);
            Camera.main.transform.position = Vector3.Lerp(startPosition, finalPosition, t);
            Camera.main.transform.rotation = Quaternion.Lerp(startRotation, finalRotation, t);
            yield return null;
        }
        Camera.main.transform.position = finalPosition;
    }

    IEnumerator RotateCameraAround(Vector3 point, Vector3 axis, float angle, float duration)
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

    IEnumerator SpawnCity()
    {
        grid = GridUtils.GenerateStreetGrid(null);
        Transform tiles = grid.Find("Tiles");
        Renderer[] tileRenderers = tiles.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in tileRenderers)
        {
            renderer.enabled = false;
        }

        Transform buildingBlocks = grid.Find("BuildingBlocks");
        Renderer[] buildingBlockRenderers = buildingBlocks.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in buildingBlockRenderers)
        {
            renderer.enabled = false;
        }
        float startTime = Time.time;

        Vector3 spawnAround = new Vector3(4.5f, 0, 9);
        System.Array.Sort(tileRenderers, (a, b) =>
        {
            float distanceA = Vector3.Distance(a.transform.position, spawnAround);
            float distanceB = Vector3.Distance(b.transform.position, spawnAround);
            return distanceA.CompareTo(distanceB);
        });
        System.Array.Sort(buildingBlockRenderers, (a, b) =>
        {
            float distanceA = Vector3.Distance(a.transform.position, spawnAround);
            float distanceB = Vector3.Distance(b.transform.position, spawnAround);
            return distanceA.CompareTo(distanceB);
        });

        StartCoroutine(SpawnGridTiles(tileRenderers));
        yield return new WaitForSeconds(0.8f);


        int buildingChunkSize = 3;
        for (int i = 0; i < buildingBlockRenderers.Count(); i += buildingChunkSize)
        {
            yield return new WaitForSeconds(0.02f);
            for (int j = 0; j < buildingChunkSize; j++)
            {
                if (i + j < buildingBlockRenderers.Count())
                {
                    StartCoroutine(SpawnBuilding(buildingBlockRenderers[i + j], duration: 1, spawnPositionOffset: Vector3.up * -1));
                }
            }

        }
    }

    IEnumerator SpawnGridTiles(Renderer[] tileRenderers)
    {
        int tileChunkSize = 3;
        for (int i = 0; i < tileRenderers.Count(); i += tileChunkSize)
        {
            yield return new WaitForSeconds(0.02f);
            for (int j = 0; j < tileChunkSize; j++)
            {
                if (i + j < tileRenderers.Count())
                {
                    StartCoroutine(SpawnGridTile(tileRenderers[i + j], duration: 0.5f, spawnPositionOffset: Vector3.up * -5));
                }
            }

        }
    }


    IEnumerator SpawnGridTile(Renderer tileRenderer, float duration, Vector3 spawnPositionOffset)
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
        Vector3 originalScale = tile.localScale;
        tile.localScale = Vector3.zero;
        Vector3 finalPosition = tile.position;
        Vector3 startPosition = finalPosition + spawnPositionOffset;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseOutCubic(t);
            float transparencyFactor = EaseUtils.EaseInCubic(t);
            tile.localScale = originalScale * scaleFactor;
            tile.position = Vector3.Lerp(startPosition, finalPosition, scaleFactor);
            for (int i = 0; i < tileRenderer.materials.Count(); i++)
            {
                tileRenderer.materials[i].color = Color.Lerp(startColors[i], finalColors[i], t * 1.5f);
            }
            yield return null;
        }
        tile.localScale = originalScale;
        tile.position = finalPosition;
        for (int i = 0; i < tileRenderer.materials.Count(); i++)
        {
            tileRenderer.materials[i].color = finalColors[i];
            SetupMaterialWithBlendMode(tileRenderer.materials[i], BlendMode.Opaque);

        }
        yield return null;
    }

    IEnumerator SpawnBuilding(Renderer tileRenderer, float duration, Vector3 spawnPositionOffset)
    {
        tileRenderer.enabled = true;

        Transform tile = tileRenderer.transform;
        Vector3 originalScale = tile.localScale;
        tile.localScale = Vector3.zero;
        Vector3 finalPosition = tile.position;
        Vector3 startPosition = finalPosition + spawnPositionOffset;
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float scaleFactor = EaseUtils.EaseOutCubic(t);
            tile.localScale = originalScale * scaleFactor;
            tile.position = Vector3.Lerp(startPosition, finalPosition, scaleFactor);

            yield return null;
        }
        tile.localScale = originalScale;
        tile.position = finalPosition;
        yield return null;
    }


    IEnumerator SpawnPassengerStats(Passenger passenger)
    {
        Vector3 position = new Vector3(-0.24f, 0.19f, 0.08f);
        Quaternion rotation = Quaternion.Euler(0, 20, 0);
        PassengerStats.Create(passengerStatsPrefab, passenger.transform, position, rotation, passenger.person, mode: PassengerStatMode.Slow);
        yield return null;
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

    DriverPerson CreateGenericDriverPerson()
    {
        return new DriverPerson()
        {
            opportunityCostProfile = DriverPool.normalDriverProfile,
            baseOpportunityCostPerHour = 10,
            preferredSessionLength = 4,
            interval = new SessionInterval()
            {
                startTime = 0,
                endTime = 4
            }
        };
    }

}
