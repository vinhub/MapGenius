using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;

public class PanelManager : MonoBehaviour {

	private GameObject m_goPanel;
	private GameObject m_goPrevSelected;

    private string m_landmarkCrossed;
    private GameObject m_mainMenu, m_instructionsPanel, m_mapPanel;
    private Text m_scoreText;
    private MapPanelHelper m_mpHelper;
    private GameObject m_fader;
    private bool m_gameStartInstructions;

    private void Awake()
	{
        GameObject mainMenuUI = GameObject.FindWithTag("MainMenuUI");

        m_mainMenu = mainMenuUI.transform.Find("MainMenu").gameObject;
        m_instructionsPanel = mainMenuUI.transform.Find("Instructions").gameObject;
        m_instructionsPanel = mainMenuUI.transform.Find("Instructions").gameObject;
        m_mapPanel = mainMenuUI.transform.Find("Map").gameObject;
        m_mpHelper = m_mapPanel.GetComponent<MapPanelHelper>();
        m_fader = mainMenuUI.transform.Find("Fader").gameObject;
        m_scoreText = mainMenuUI.transform.Find("OpenMenuButton/ScoreText").GetComponent<Text>();
    }

    public void OpenPanel(GameObject goPanel)
	{
        if (m_goPanel == goPanel)
            return;

        m_mainMenu.SetActive(false);
        goPanel.SetActive(true);

        GameObject goCurSelected = EventSystem.current.currentSelectedGameObject;

        goPanel.transform.SetAsLastSibling();

        CloseCurrentPanel();

        m_goPrevSelected = goCurSelected;

        m_goPanel = goPanel;

        GameObject go = FindFirstEnabledSelectable(goPanel);

        EventSystem.current.SetSelectedGameObject(go);
	}

    public void OpenMapPanel(string landmarkName)
    {
        if (m_landmarkCrossed != null) // already showing the panel
            return;

        m_landmarkCrossed = landmarkName;

        GameSystem.Instance.PauseGame();

        m_fader.SetActive(true);
        OpenPanel(m_mapPanel);

        // set up map panel
        m_mpHelper.Setup(m_mapPanel.transform, landmarkName);
    }

    // this is called when game is started
    public void OpenInstructionsPanel()
    {
        m_gameStartInstructions = true;

        GameSystem.Instance.PauseGame();

        m_fader.SetActive(true);
        OpenPanel(m_instructionsPanel);

        Text closePanelText = m_instructionsPanel.transform.Find(Strings.PanelCloseButtonPath).GetComponent<Text>();
        closePanelText.text = Strings.StartGame;
    }

    public void CloseCurrentPanel()
	{
		if (m_goPanel == null)
			return;

        if (m_gameStartInstructions) // instructions panel is being shown at the start of the game
        {
            m_fader.SetActive(false);
            GameSystem.Instance.ResumeGame();

            m_gameStartInstructions = false;
        }
        else if (m_landmarkCrossed != null) // map panel was invoked in response to landmark crossing
        {
            if (!m_mpHelper.Close()) // don't close it if the panel helper disallows it (used when game is over and we need to show results instead of closing the panel)
                return;

            m_fader.SetActive(false);
            GameSystem.Instance.ResumeGame();

            m_landmarkCrossed = null;
        }
        else
        {
            m_mainMenu.SetActive(true);
        }

        Text closePanelText = m_goPanel.transform.Find(Strings.PanelCloseButtonPath).GetComponent<Text>();
        closePanelText.text = Strings.Back;

        EventSystem.current.SetSelectedGameObject(m_goPrevSelected);

        m_goPanel.SetActive(false);

		m_goPanel = null;
	}

    public void UpdateScore(int levelScore, int totalScore)
    {
        m_scoreText.text = String.Format(Strings.ScoreTextFormat, totalScore);
    }

    public void LoadScene(string sceneName)
    {
        GameSystem.Instance.LoadScene(sceneName);
    }

    public void ContinueGame()
    {
        GameSystem.Instance.ResumeGame();
    }

    public void NewGame()
    {
        GameSystem.Instance.NewGame();
    }

    public void QuitGame()
    {
        GameSystem.Instance.QuitGame();
    }

    // --------------- helper methods ------------------
    private static GameObject FindFirstEnabledSelectable(GameObject gameObject)
    {
        GameObject go = null;
        var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable.IsActive() && selectable.IsInteractable())
            {
                go = selectable.gameObject;
                break;
            }
        }

        return go;
    }

    // Map panel methods
}
