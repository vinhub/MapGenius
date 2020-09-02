﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeEmitter : MonoBehaviour
{
    private ParticleSystem[] m_pSystems;
    private AudioSource m_audioSource;

    // Start is called before the first frame update
    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_pSystems = transform.GetComponentsInChildren<ParticleSystem>();
    }

    public void Play()
    {
        foreach(ParticleSystem ps in m_pSystems)
        {
            ps.Play();
        }

        // play victory music
        m_audioSource.Play();
    }
}