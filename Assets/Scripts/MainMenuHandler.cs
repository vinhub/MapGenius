using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour
{
    public GameObject m_MainPanel;
    private GameObject m_MainPanelObj = null;
    private MainPanelHandler m_MainPanelHandler = null;

    private void Awake()
    {
        if (m_MainPanelObj == null)
        {
            m_MainPanelObj = Instantiate(m_MainPanel);
            m_MainPanelHandler = m_MainPanelObj.GetComponent<MainPanelHandler>();
        }
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
        m_MainPanelHandler.ShowPanel(panelName);
    }

    public void QuitGame()
    {
        GameSystem.Instance.QuitGame();
    }
}
