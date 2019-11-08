using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    // our singleton instance
    private static GameSystem m_instance;
    public static GameSystem Instance { get { return m_instance; } }

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
            m_goMainPanelUI = Instantiate(MainPanelUI);
            m_mainPanelHandler = m_goMainPanelUI.GetComponent<MainPanelHandler>();
        }

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
        ShowPanel(Strings.MapPanelName);
    }

    public void ShowPanel(string panelName)
    {
        m_mainPanelHandler.ShowPanel(panelName);
    }
}
