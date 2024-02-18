using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSceneDirector : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Generating street grid");

        GridUtils.GenerateStreetGrid(null);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
