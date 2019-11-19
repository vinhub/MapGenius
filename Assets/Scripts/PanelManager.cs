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
    private GameObject m_mapPanel;
    private MapPanelHelper m_mpHelper;
    private GameObject m_fader;

    private void Awake()
	{
        GameObject mainMenuUI = GameObject.FindWithTag("MainMenuUI");

        m_mapPanel = mainMenuUI.transform.Find("Map").gameObject;
        m_mpHelper = m_mapPanel.GetComponent<MapPanelHelper>();
        m_fader = mainMenuUI.transform.Find("Fader").gameObject;
    }

    public void OpenPanel(GameObject goPanel)
	{
        if (m_goPanel == goPanel)
            return;

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

	public void CloseCurrentPanel()
	{
		if (m_goPanel == null)
			return;

        if (m_landmarkCrossed != null) // map panel was invoked in response to landmark crossing
        {
            m_mpHelper.Close();
            m_fader.SetActive(false);
            GameSystem.Instance.ResumeGame();
            m_landmarkCrossed = null;
        }

        EventSystem.current.SetSelectedGameObject(m_goPrevSelected);

        m_goPanel.SetActive(false);

		m_goPanel = null;
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
