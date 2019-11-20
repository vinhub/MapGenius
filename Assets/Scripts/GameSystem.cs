using System;
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

    private PanelManager m_mainPanelManager;

    private void Awake()
    {
        m_instance = this;

        GameObject mainMenuUI = GameObject.FindWithTag("MainMenuUI");
        m_mainPanelManager = mainMenuUI.transform.Find("PanelManager").GetComponent<PanelManager>();

        m_carController = Car.GetComponent<CarController>();
    }

    private void Start()
    {
        m_mainPanelManager.OpenInstructionsPanel();
    }

    private void OnDestroy()
    {
        m_instance = null;
    }

    private void Update()
    {
        // global game update logic goes here
    }

    private void OnGui()
    {
        // common GUI code goes here
    }

    public void NewGame()
    {
        GameSystem.Instance.ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadScene(string sceneName)
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

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }

    public void LandmarkCrossed(string landmarkName)
    {
        // when a landmark is crossed, show map panel
        m_mainPanelManager.OpenMapPanel(landmarkName);
    }

    public void SetScore(int levelScore, int totalScore)
    {
        LevelScore = levelScore;
        TotalScore = totalScore;
    }
}
