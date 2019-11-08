using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuHandler : MonoBehaviour
{
    private PauseMenu m_PauseMenu;
    private Toggle m_menuToggle;
    
    private void Awake()
    {
        m_PauseMenu = GetComponentInChildren<PauseMenu>();
        m_menuToggle = m_PauseMenu.GetComponent<Toggle>();
    }

    public void LoadScene(string sceneName)
	{
        GameSystem.Instance.ResumeGame();
		SceneManager.LoadScene(sceneName);
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
