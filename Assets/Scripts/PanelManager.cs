using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
using TMPro;
using System.Linq;

public class PanelManager : MonoBehaviour {

	private GameObject m_goPanel;
	private GameObject m_goPrevSelected;

    public string CurLandmarkName { get; private set; }
    private GameObject m_instructionsPanel, m_aboutPanel, m_mapPanel;
    private TMP_Text m_scoreText, m_timeText;
    private MapPanelHelper m_mpHelper;
    private AudioSource m_buttonClickAudioSource;

    // intructions panel specific items
    private int m_iInstructionStep = 0; // instructions panel consists of multiple steps

    private void Awake()
	{
        GameObject mainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag);

        m_instructionsPanel = mainMenuUI.transform.Find(Strings.InstructionsPanelPath).gameObject;
        m_aboutPanel = mainMenuUI.transform.Find(Strings.AboutPanelPath).gameObject;
        m_mapPanel = mainMenuUI.transform.Find(Strings.MapPanelPath).gameObject;
        m_mpHelper = m_mapPanel.GetComponent<MapPanelHelper>();
        m_scoreText = mainMenuUI.transform.Find(Strings.StatusBarScoreTextPath).GetComponent<TMP_Text>();
        m_timeText = mainMenuUI.transform.Find(Strings.StatusBarTimeTextPath).GetComponent<TMP_Text>();
        m_buttonClickAudioSource = transform.parent.Find(Strings.ButtonClickAudioSourceName).GetComponent<AudioSource>();
    }

    private void Update()
    {
        m_timeText.text = String.Format(Strings.TimeTextFormat, (int)Time.timeSinceLevelLoad);
    }

    public void OpenPanel(GameObject goPanel)
	{
        if (m_goPanel == goPanel)
            return;

        GameSystem.Instance.PauseGame();

        m_goPrevSelected = EventSystem.current.currentSelectedGameObject;

        goPanel.SetActive(true);

        goPanel.transform.SetAsLastSibling();

        m_goPanel = goPanel;

        m_goPanel.GetComponent<RectTransform>().DOAnchorPosY(0f, 0.6f, false).SetEase(Ease.InOutCubic).SetUpdate(true);
    }

    public void ClosePanelAndContinue()
    {
        ClosePanel(true);

        ContinueGame(false);
    }

    public bool ClosePanel(bool fCheckOkToClose)
    {
        if (m_goPanel == m_instructionsPanel)
            CloseInstructionsPanel();
        else
        {
            if (!CloseMapPanel(fCheckOkToClose))
                return false;
        }

        EventSystem.current.SetSelectedGameObject(null);

        m_goPanel.GetComponent<RectTransform>().DOAnchorPosY(1600f, 0.25f, false).SetEase(Ease.InOutCubic).SetUpdate(true).OnComplete(() => { m_goPanel.SetActive(false); m_goPanel = null; });

        return true;
    }

    // instructions for playing the game
    public void OpenInstructionsPanel()
    {
        OpenPanel(m_instructionsPanel);

        SetupInstructionsPanel();

        Transform hideInstructionsToggle = m_instructionsPanel.transform.Find(Strings.ButtonBarTogglePath);
        hideInstructionsToggle.GetComponent<Toggle>().isOn = (PlayerPrefs.GetInt(Strings.HideInstructionsAtStart, 0) == 1);

        // show the "don't show this again" toggle only when starting the game
        hideInstructionsToggle.gameObject.SetActive(GameState.IsGameStarting);
    }

    private void SetupInstructionsPanel()
    {
        Transform tInstructionSteps = m_instructionsPanel.transform.Find(Strings.InstructionSteps);
        bool isLastInstruction = (m_iInstructionStep == tInstructionSteps.childCount - 1);

        // show current step
        for (int iInstructionStep = 0; iInstructionStep < tInstructionSteps.childCount; iInstructionStep++)
        {
            Transform instructionStep = tInstructionSteps.GetChild(iInstructionStep);
            instructionStep.gameObject.SetActive(iInstructionStep == m_iInstructionStep);
        }

        Transform tCloseButton = m_instructionsPanel.transform.Find(Strings.CloseButtonPath);

        TMP_Text closePanelText = tCloseButton.Find(Strings.ButtonLabelPath).GetComponent<TMP_Text>();

        closePanelText.text = isLastInstruction ? (GameState.IsGameStarting ? Strings.StartGame : Strings.ContinueGame) : Strings.Next;

        Button closeButton = tCloseButton.GetComponent<Button>();
        
        closeButton.onClick.RemoveAllListeners();

        if (isLastInstruction)
            closeButton.onClick.AddListener(ClosePanelAndContinue);
        else
            closeButton.onClick.AddListener(OnClickNextInstruction);

        closeButton.onClick.AddListener(m_buttonClickAudioSource.Play);
    }

    public void OnClickNextInstruction()
    {
        m_iInstructionStep++;
        SetupInstructionsPanel();
    }

    // called when the CloseButton is clicked on the Instructions panel
    private void CloseInstructionsPanel()
	{
		if (m_goPanel == null)
			return;

        Debug.Assert(m_goPanel == m_instructionsPanel);

        if (GameState.IsGameStarting) // instructions panel is being shown at the start of the game
        {
            Transform hideInstructionsToggle = m_instructionsPanel.transform.Find(Strings.ButtonBarTogglePath);
            if (hideInstructionsToggle.gameObject.activeInHierarchy)
            {
                bool hideInstructionsAtStart = hideInstructionsToggle.GetComponent<Toggle>().isOn;
                PlayerPrefs.SetInt(Strings.HideInstructionsAtStart, hideInstructionsAtStart ? 1 : 0);
            }

            GameSystem.Instance.ShowInfoMessage(Strings.StartingInstructionsMessage, 5f);

            GameState.IsGameStarting = false;
        }

        m_iInstructionStep = 0;
    }

    public void OpenAboutPanel()
    {
        OpenPanel(m_aboutPanel);
    }

    // called from menu
    public void OpenMapPanel()
    {
        OpenMapPanel(null, false);
    }

    public void OpenMapPanel(string landmarkName, bool firstLandmarkCrossed)
    {
        if (!String.IsNullOrEmpty(CurLandmarkName)) // already showing the panel for a landmark
            return;

        CurLandmarkName = landmarkName;

        OpenPanel(m_mapPanel);

        // set up map panel
        m_mpHelper.Setup(this, m_mapPanel.transform, landmarkName, firstLandmarkCrossed);
    }

    private bool CloseMapPanel(bool fCheckOkToClose)
    {
        if (m_goPanel == null)
            return false;

        if (!m_mpHelper.CloseOrContinue(fCheckOkToClose)) // don't close it if the panel helper disallows it (used when game is over and we need to show results instead of closing the panel)
            return false;

        CurLandmarkName = null;

        return true;
    }

    public bool IsPanelOpen()
    {
        return m_goPanel != null;
    }

    public void OnClickContinueGame()
    {
        if (!ClosePanel(true))
            return;

        ContinueGame(false);
    }

    public void OnClickRetryGame()
    {
        if (!ClosePanel(false))
            return;

        RetryGame();
    }

    public void OnClickVictoryLap()
    {
        if (!ClosePanel(false))
            return;

        ContinueGame(true);
    }

    public void OnClickNewGame()
    {
        if (!ClosePanel(false))
            return;

        NewGame();
    }

    public void DisplayScore()
    {
        m_scoreText.text = String.Format(Strings.ScoreTextFormat, (int)Math.Round(PlayerState.TotalScore, MidpointRounding.AwayFromZero));
    }

    public void ContinueGame(bool fVictoryLap)
    {
        GameSystem.Instance.ContinueGame(fVictoryLap);
    }

    public void RetryGame()
    {
        GameSystem.Instance.RetryGame();
    }

    public void NewGame()
    {
        GameSystem.Instance.NewGame();
    }

    public void GoToLevel(string gameLevelName)
    {
        GameLevel gameLevel = GameState.LevelInfos.First<LevelInfo>(li => { return li.getName() == gameLevelName; }).GameLevel;
        GameSystem.Instance.GoToLevel(gameLevel);
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
