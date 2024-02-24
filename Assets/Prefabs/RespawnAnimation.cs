using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnAnimation : MonoBehaviour
{

    public float pause = 1;
    public AnimationCurve fadeIn;

    ParticleSystem ps;
    // float timer = 0;
    int shaderProperty;

    void Start()
    {
        shaderProperty = Shader.PropertyToID("_cutoff");
        ps = GetComponentInChildren<ParticleSystem>();

        ps.Play();

    }

    // void Update()
    // {
    //     if (timer < spawnEffectTime + pause)
    //     {
    //         timer += Time.deltaTime;
    //     }
    //     else
    //     {
    //         ps.Play();
    //         timer = 0;
    //     }


    // }
}
