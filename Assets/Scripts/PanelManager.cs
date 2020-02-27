using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;

public class PanelManager : MonoBehaviour {

	private GameObject m_goPanel;
	private GameObject m_goPrevSelected;

    public string CurLandmarkName { get; private set; }
    private GameObject m_mainMenu, m_panels, m_instructionsPanel, m_mapPanel;
    private Text m_scoreText, m_timeText;
    private MapPanelHelper m_mpHelper;
    private bool m_gameStartInstructions;

    private void Awake()
	{
        GameObject mainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag);

        m_mainMenu = mainMenuUI.transform.Find(Strings.MainMenuName).gameObject;
        m_panels = mainMenuUI.transform.Find(Strings.PanelsName).gameObject;
        m_instructionsPanel = mainMenuUI.transform.Find(Strings.InstructionsPanelPath).gameObject;
        m_mapPanel = mainMenuUI.transform.Find(Strings.MapPanelPath).gameObject;
        m_mpHelper = m_mapPanel.GetComponent<MapPanelHelper>();
        m_scoreText = mainMenuUI.transform.Find(Strings.GameStatusScoreTextPath).GetComponent<Text>();
        m_timeText = mainMenuUI.transform.Find(Strings.GameStatusTimeTextPath).GetComponent<Text>();
    }

    private void Update()
    {
        m_timeText.text = String.Format(Strings.TimeTextFormat, (int)Time.timeSinceLevelLoad);
    }

    public void OpenPanel(GameObject goPanel)
	{
        if (m_goPanel == goPanel)
            return;

        m_goPrevSelected = EventSystem.current.currentSelectedGameObject;

        m_mainMenu.SetActive(false);
        m_panels.SetActive(true);
        goPanel.SetActive(true);

        goPanel.transform.SetAsLastSibling();

        //EventSystem.current.SetSelectedGameObject(FindFirstEnabledSelectable(goPanel));

        m_goPanel = goPanel;
	}

    // this is called when game is started
    public void OpenInstructionsPanel(bool isGameStarting)
    {
        m_gameStartInstructions = isGameStarting;

        GameSystem.Instance.PauseGame();

        OpenPanel(m_instructionsPanel);

        Text closePanelText = m_instructionsPanel.transform.Find(Strings.CloseButtonLabelPath).GetComponent<Text>();
        closePanelText.text = isGameStarting ? Strings.StartGame : Strings.Back;

        Transform hideInstructionsToggle = m_instructionsPanel.transform.Find(Strings.ButtonBarTogglePath);
        hideInstructionsToggle.GetComponent<Toggle>().isOn = (PlayerPrefs.GetInt(Strings.HideInstructionsAtStart, 0) == 1);

        // show the "don't show this again" toggle only when starting the game
        hideInstructionsToggle.gameObject.SetActive(isGameStarting);
    }

    // called when the CloseButton is clicked on the Instructions panel
    public void CloseInstructionsPanel()
	{
		if (m_goPanel == null)
			return;

        Debug.Assert(m_goPanel == m_instructionsPanel);

        if (m_gameStartInstructions) // instructions panel is being shown at the start of the game
        {
            Transform hideInstructionsToggle = m_instructionsPanel.transform.Find(Strings.ButtonBarTogglePath);
            if (hideInstructionsToggle.gameObject.activeInHierarchy)
            {
                bool hideInstructionsAtStart = hideInstructionsToggle.GetComponent<Toggle>().isOn;
                PlayerPrefs.SetInt(Strings.HideInstructionsAtStart, hideInstructionsAtStart ? 1 : 0);
            }

            // we will resume the game directly instead of going back to the main menu as is the usual case
            GameSystem.Instance.ResumeGame();

            m_gameStartInstructions = false;
        }
        else
        {
            // back to the main menu
            m_mainMenu.SetActive(true);
        }

        Text closeButtonText = m_goPanel.transform.Find(Strings.CloseButtonLabelPath).GetComponent<Text>();
        closeButtonText.text = Strings.Back;

        EventSystem.current.SetSelectedGameObject(null);

        m_goPanel.SetActive(false);
        m_panels.SetActive(false);

        m_goPanel = null;
	}

    // called from menu
    public void OpenMapPanel()
    {
        OpenMapPanel(null, false);
    }

    public void OpenMapPanel(string landmarkName, bool firstLandmarkCrossed)
    {
        if (!String.IsNullOrEmpty(CurLandmarkName)) // already showing the panel
            return;

        CurLandmarkName = landmarkName;

        OpenPanel(m_mapPanel);

        // set up map panel
        m_mpHelper.Setup(this, m_mapPanel.transform, landmarkName, firstLandmarkCrossed);
    }

    private void CloseMapPanel()
    {
        CurLandmarkName = null;

        EventSystem.current.SetSelectedGameObject(null);

        m_goPanel.SetActive(false);
        m_panels.SetActive(false);

        m_goPanel = null;
    }

    public void OnClickBack()
    {
        if (m_goPanel == null)
            return;

        CloseMapPanel();

        m_mainMenu.SetActive(true);
    }

    public void OnClickContinueGame()
    {
        if (m_goPanel == null)
            return;

        if (!m_mpHelper.CloseOrContinue()) // don't close it if the panel helper disallows it (used when game is over and we need to show results instead of closing the panel)
            return;

        CloseMapPanel();

        ContinueGame();
    }

    public void OnClickRetryGame()
    {
        if (m_goPanel == null)
            return;

        CloseMapPanel();

        RetryGame();
    }

    public void OnClickNewGame()
    {
        if (m_goPanel == null)
            return;

        CloseMapPanel();

        NewGame();
    }

    public void UpdateScore()
    {
        m_scoreText.text = String.Format(Strings.ScoreTextFormat,
            (int)Math.Round(StaticGlobals.TotalScore, MidpointRounding.AwayFromZero),
            (int)Math.Round(StaticGlobals.TotalScoreMax, MidpointRounding.AwayFromZero));
    }

    public void LoadScene(string sceneName)
    {
        GameSystem.Instance.LoadScene(sceneName);
    }

    public void ContinueGame()
    {
        GameSystem.Instance.ResumeGame();
    }

    public void RetryGame()
    {
        GameSystem.Instance.RetryGame();
    }

    public void NewGame()
    {
        GameSystem.Instance.NewGame();
    }

    public void NextLevel()
    {
        GameSystem.Instance.NextLevel();
    }

    public void QuitGame()
    {
        GameSystem.Instance.QuitGame();
    }

    // --------------- helper methods ------------------
    //private static GameObject FindFirstEnabledSelectable(GameObject gameObject)
    //{
    //    GameObject go = null;
    //    var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
    //    foreach (var selectable in selectables)
    //    {
    //        if (selectable.IsActive() && selectable.IsInteractable())
    //        {
    //            go = selectable.gameObject;
    //            break;
    //        }
    //    }

    //    return go;
    //}

    // Map panel methods
}
