using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;

public class PanelManager : MonoBehaviour {

	private GameObject m_goPanel;
	private GameObject m_goPrevSelected;

	public void OnEnable()
	{
	}

	public void OpenPanel(GameObject goPanel)
	{
		goPanel.SetActive(true);
		GameObject goCurSelected = EventSystem.current.currentSelectedGameObject;

		goPanel.transform.SetAsLastSibling();

		CloseCurrent();

		m_goPrevSelected = goCurSelected;

		m_goPanel = goPanel;

		GameObject go = FindFirstEnabledSelectable(goPanel);

		SetSelected(go);
	}

	static GameObject FindFirstEnabledSelectable(GameObject gameObject)
	{
		GameObject go = null;
		var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
		foreach (var selectable in selectables) {
			if (selectable.IsActive () && selectable.IsInteractable()) {
				go = selectable.gameObject;
				break;
			}
		}
		return go;
	}

	public void CloseCurrent()
	{
		if (m_goPanel == null)
			return;

		SetSelected(m_goPrevSelected);

        m_goPanel.SetActive(false);

		m_goPanel = null;
	}

	private void SetSelected(GameObject go)
	{
		EventSystem.current.SetSelectedGameObject(go);
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
}
