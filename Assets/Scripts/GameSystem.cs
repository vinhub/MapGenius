﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    private static GameSystem m_Instance;
    public static GameSystem Instance { get { return m_Instance; } }

    private float m_TimeScaleRef = 1f;
    private float m_VolumeRef = 1f;
    private bool m_Paused;

    void Awake()
    {
        m_Instance = this;
    }

    void OnDestroy()
    {
        m_Instance = null;
    }

    void Update()
    {
        // global game update logic goes here
    }

    void OnGui()
    {
        // common GUI code goes here
    }

    public void PauseGame()
    {
        if (m_Paused)
            return;

        m_TimeScaleRef = Time.timeScale;
        Time.timeScale = 0f;

        m_VolumeRef = AudioListener.volume;
        AudioListener.volume = 0f;

        m_Paused = true;
    }

    public void ResumeGame()
    {
        if (!m_Paused)
            return;

        Time.timeScale = m_TimeScaleRef;
        AudioListener.volume = m_VolumeRef;
        m_Paused = false;
    }

    public bool IsGamePaused()
    {
        return m_Paused;
    }

    public void QuitGame()
    {
        Debug.Log("Player quit the game.");
        Application.Quit();
    }

}