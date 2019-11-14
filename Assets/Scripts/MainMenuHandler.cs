using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    private PauseMenu m_pauseMenu;
    private Toggle m_menuToggle;
    
    private void Start()
    {
        m_pauseMenu = GetComponentInChildren<PauseMenu>();
        m_menuToggle = m_pauseMenu.GetComponent<Toggle>();
    }

    public void LoadScene(string sceneName)
	{
        GameSystem.Instance.LoadScene(sceneName);
	}

    public void LoadURL(string url)
	{
		Application.OpenURL(url);
	}

    public void ShowPanel(string panelName)
    {
        m_menuToggle.isOn = false;

        GameSystem.Instance.ShowPanel(panelName);
    }

    public void QuitGame()
    {
        GameSystem.Instance.QuitGame();
    }
}
