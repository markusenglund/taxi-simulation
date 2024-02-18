using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSceneDirector : MonoBehaviour
{

    [SerializeField] public Transform passengerPrefab;

    void Start()
    {
        GridUtils.GenerateStreetGrid(null);
        StartCoroutine(Scene());
    }

    IEnumerator Scene()
    {
        yield return new WaitForSeconds(1);
        Transform passenger = Instantiate(passengerPrefab, new Vector3(1.7f, 0.08f, 0.22f), Quaternion.identity);
        Vector3 lookAt = Camera.main.transform.position;
        lookAt.y = passenger.position.y;
        passenger.LookAt(lookAt);
        yield return new WaitForSeconds(1);
        // Zoom out

    }

    public void ZoomOut(float distance, float duration)
    {
        // TODO: START HERE - Implement the ZoomOut method which is supposed to gradually zoom out based on "animateValue" in the primer project
    }
}
