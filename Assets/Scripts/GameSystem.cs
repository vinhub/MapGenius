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
    public float LevelScore { get; private set; } // player's score so far for the current level
    public float TotalScore { get; private set; } = 0; // player's total score so far

    public GameObject Car; // the car being driven by the player
    private CarController m_carController;

    // for pausing / resuming game
    private float m_timeScaleSav = 1f;
    private bool m_paused;

    private PanelManager m_mainPanelManager;
    private bool m_firstLandmarkCrossed = true;

    private void Awake()
    {
        m_instance = this;

        m_carController = Car.GetComponent<CarController>();
    }

    private void Start()
    {
        GameObject mainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag);
        m_mainPanelManager = mainMenuUI.transform.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();

        // show game instructions at start once
        if (PlayerPrefs.GetInt(Strings.ShowInstructionsAtStart, 1) == 1)
        {
            m_mainPanelManager.OpenInstructionsPanel(true);
            PlayerPrefs.SetInt(Strings.ShowInstructionsAtStart, 0);
        }

        SetScore(0, 0);
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
        ResumeGame();
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

        m_carController.StopCar();

        m_timeScaleSav = Time.timeScale;
        Time.timeScale = 0f;

        m_paused = true;
    }

    public void ResumeGame()
    {
        if (!m_paused)
            return;

        Time.timeScale = m_timeScaleSav;
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

    // called when the player crosses a landmark
    public void LandmarkCrossed(string landmarkName)
    {
        if (!String.IsNullOrEmpty(m_mainPanelManager.CurLandmarkName)) // currently processing a landmark?
            return;

        PauseGame();

        StartCoroutine(HandleLandmarkCrossed(landmarkName));
    }

    private IEnumerator HandleLandmarkCrossed(string landmarkName)
    {
        // flash a popup letting the player know they crossed the landmark
        string messageText = String.Format(Strings.LandmarkCrossedMessageFormat, landmarkName) +
            (m_firstLandmarkCrossed ? Strings.FirstLandmarkCrossedMessage : Strings.OtherLandmarkCrossedMessage);

        PopupMessage.ShowMessage(messageText);

        // let it stay for some time
        yield return new WaitForSecondsRealtime(m_firstLandmarkCrossed ? 5f : 4f);

        PopupMessage.HideMessage();

        // then show map panel
        m_mainPanelManager.OpenMapPanel(landmarkName, m_firstLandmarkCrossed);

        m_firstLandmarkCrossed = false;
    }

    public void SetScore(float levelScore, float totalScore)
    {
        LevelScore = levelScore;
        TotalScore = totalScore;

        m_mainPanelManager.UpdateScore(levelScore, totalScore);
    }
}
