﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Vehicles.Car;

public class GameSystem : MonoBehaviour
{
    // our singleton instance
    private static GameSystem m_instance;
    public static GameSystem Instance { get { return m_instance; } }

    public const int MaxLevelScore = 100; // max score for a level
    public const int NumLevels = 10; // total number of levels
    public const int MaxScore = MaxLevelScore * NumLevels; // max possible score

    public int CurLevel { get; private set; } = 1; // current level
    public int LevelScore { get; private set; } // player's score so far for the current level
    public int TotalScore { get; private set; } = 0; // player's total score so far

    public GameObject Car; // the car being driven by the player
    private CarController m_carController;

    // for pausing / resuming game
    private float m_timeScaleSav = 1f;
    private float m_volumeSav = 1f;
    private bool m_paused;

    // main UI panel management
    public GameObject MainPanelUI;

    private GameObject m_goMainPanelUI = null;
    private MainPanelHandler m_mainPanelHandler = null;

    void Awake()
    {
        m_instance = this;

        if (m_goMainPanelUI == null)
        {
            m_goMainPanelUI = Instantiate(this.MainPanelUI);
            m_mainPanelHandler = m_goMainPanelUI.GetComponent<MainPanelHandler>();
        }

        m_carController = Car.GetComponent<CarController>();
    }

    void OnDestroy()
    {
        m_instance = null;
    }

    void Update()
    {
        // global game update logic goes here
    }

    void OnGui()
    {
        // common GUI code goes here
    }

    internal void LoadScene(string sceneName)
    {
        GameSystem.Instance.ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void PauseGame()
    {
        if (m_paused)
            return;

        m_timeScaleSav = Time.timeScale;
        Time.timeScale = 0f;

        m_volumeSav = AudioListener.volume;
        AudioListener.volume = 0f;

        m_paused = true;
    }

    public void ResumeGame()
    {
        if (!m_paused)
            return;

        Time.timeScale = m_timeScaleSav;
        AudioListener.volume = m_volumeSav;
        m_paused = false;
    }

    public bool IsGamePaused()
    {
        return m_paused;
    }

    public void QuitGame()
    {
        Debug.Log("Player quit the game.");
        Application.Quit();
    }

    public void LandmarkCrossed(string landmarkName)
    {
        // when a landmark is crossed, show map panel
        ShowPanel(Strings.MapPanelName, landmarkName);
    }

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        m_mainPanelHandler.ShowPanel(panelName, landmarkName);
    }

    internal void SetScore(int levelScore, int totalScore)
    {
        LevelScore = levelScore;
        TotalScore = totalScore;
    }
}
