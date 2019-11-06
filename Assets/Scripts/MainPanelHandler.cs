using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    GameObject panelShowingNow = null;

    public void ShowPanel(string panelName)
    {
        if (panelShowingNow != null)
            return;

        GameSystem.Instance.PauseGame();

        panelShowingNow = transform.Find("PanelParent/Panels/" + panelName).gameObject;

        panelShowingNow.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelShowingNow == null)
            return;

        panelShowingNow.SetActive(false);
        panelShowingNow = null;

        GameSystem.Instance.ResumeGame();
    }
}
