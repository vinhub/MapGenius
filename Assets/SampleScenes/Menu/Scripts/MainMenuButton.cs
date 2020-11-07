using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{
    private Toggle m_toggle;
    private AudioSource m_buttonClickAudioSource;
    private GameObject m_mainMenu;
    private RectTransform m_mainMenuRectTransform;
    private PanelManager m_mainPanelManager;

    private void Awake()
    {
        m_toggle = GetComponent<Toggle>();
        m_mainMenu = transform.parent.Find(Strings.MainMenuName).gameObject;
        m_mainMenuRectTransform = m_mainMenu.GetComponent<RectTransform>();
        m_mainPanelManager = transform.parent.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();
        m_buttonClickAudioSource = transform.parent.Find(Strings.ButtonClickAudioSourceName).GetComponent<AudioSource>();
	}

    private void OnDisable()
    {
        m_toggle.isOn = false;    
    }

    private void MenuOn()
    {
        m_buttonClickAudioSource.Play();
        GameSystem.Instance.PauseGame();
        m_mainMenu.SetActive(true);

        for (int iLevel = 0; iLevel < GameState.LevelInfos.Length; iLevel++)
        {
            LevelInfo levelInfo = GameState.LevelInfos[iLevel];
            if (!levelInfo.IsAvailable || !levelInfo.IsEnabled)
                continue;

            Transform tButton = transform.parent.Find(Strings.LevelsMenuPath + "/" + levelInfo.getName());
            Button levelButton = tButton.GetComponent<Button>();
            levelButton.interactable = true;

            TMP_Text labelText = tButton.Find(Strings.ButtonLabelPath).GetComponent<TMP_Text>();
            labelText.text = levelInfo.getName();
            labelText.color = Constants.ActiveTextColor;
        }

        m_mainMenuRectTransform.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutCubic).SetUpdate(true);
    }

    private void MenuOff(string panelName)
    {
        m_mainMenuRectTransform.DOAnchorPosY(1500f, 0.25f).SetEase(Ease.InOutCubic).SetUpdate(true).OnComplete(() => OnCompleteMenuOff(panelName));
        GameSystem.Instance.ContinueGame(false);
    }

    private void OnCompleteMenuOff(string panelName)
    {
        m_mainMenu.SetActive(false);

        switch (panelName)
        {
            case Strings.MapPanelName:
                m_mainPanelManager.OpenMapPanel();
                break;

            case Strings.InstructionsPanelName:
                m_mainPanelManager.OpenInstructionsPanel();
                break;

            case Strings.AboutPanelName:
                m_mainPanelManager.OpenAboutPanel();
                break;

            default:
                break;
        }
    }

    public void OnMenuStatusChange(string panelName)
    {
        if ((m_toggle == null) || (GameSystem.Instance == null) || GameSystem.Instance.IsGameQuitting())
            return;

        if (m_toggle.isOn && !GameSystem.Instance.IsGamePaused())
        {
            MenuOn();
        }
        else if (GameSystem.Instance.IsGamePaused())
        {
            if (m_toggle.isOn)
                m_toggle.isOn = false;

            MenuOff(panelName);
        }
    }

    public void Toggle()
    {
        m_toggle.isOn = !m_toggle.isOn;
    }
}
