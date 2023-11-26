using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static Vector3 GetRandomPosition()
    {
        bool onNorthFacingStreet = UnityEngine.Random.Range(0, 2) == 0;
        int randomEveryFourth = UnityEngine.Random.Range(0, 4) * 4;
        int randomEach = UnityEngine.Random.Range(0, 13);

        int x = onNorthFacingStreet ? randomEveryFourth : randomEach;
        int z = onNorthFacingStreet ? randomEach : randomEveryFourth;

        return new Vector3(x, 0.05f, z);
    }
}
