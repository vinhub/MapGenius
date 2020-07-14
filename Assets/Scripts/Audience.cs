using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audience : MonoBehaviour
{
    private string[] animationNames = { "idle", "applause", "applause2", "celebration", "celebration2", "celebration3" };

    // Use this for initialization
    void Start()
    {
        Animation[] audienceAnimations = gameObject.GetComponentsInChildren<Animation>();
        foreach (Animation anim in audienceAnimations)
        {
            string animationName = animationNames[Random.Range(1, 6)];

            anim.wrapMode = WrapMode.Loop;
            anim.GetComponent<Animation>().CrossFade(animationName);
            anim[animationName].time = Random.Range(0f, 3f);
        }
    }
}