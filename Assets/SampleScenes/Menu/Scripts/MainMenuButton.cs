using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{
    private Toggle m_toggle;
    private AudioSource m_buttonClickAudioSource;
    private GameObject m_mainMenu;
    private PanelManager m_mainPanelManager;

    private void Awake()
    {
        m_toggle = GetComponent<Toggle>();
        m_mainMenu = transform.parent.Find(Strings.MainMenuName).gameObject;
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
        m_mainMenu.transform.Find("Window").DOLocalMoveY(0f, 0.6f).SetUpdate(true);
    }

    private void MenuOff()
    {
        m_mainMenu.transform.Find("Window").DOLocalMoveY(1500f, 0.6f).SetUpdate(true);
        m_mainMenu.SetActive(false);
        GameSystem.Instance.ResumeGame(false);
    }
    
    public void OnMenuStatusChange()
    {
        if ((m_toggle == null) || (GameSystem.Instance == null))
            return;

        if (m_toggle.isOn && !GameSystem.Instance.IsGamePaused())
        {
            MenuOn();
        }
        else if (GameSystem.Instance.IsGamePaused())
        {
            if (m_toggle.isOn)
                m_toggle.isOn = false;

            MenuOff();
        }
    }

    public void Toggle()
    {
        m_toggle.isOn = !m_toggle.isOn;
    }
}
