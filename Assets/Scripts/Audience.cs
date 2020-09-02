using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audience : MonoBehaviour
{
    private string[] animationNames = { "idle", "applause", "applause2", "celebration", "celebration2", "celebration3" };

    public void Play()
    {
        Animation anim = gameObject.GetComponent<Animation>();

        anim.wrapMode = WrapMode.Loop;
        //m_anim[animationNames[1]].time = Random.Range(0f, 3f);
        anim.Play(animationNames[1]);
    }
}