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

        gameObject.SetActive(true);
        panelShowingNow = transform.Find("PanelParent/Panels/" + panelName).gameObject;

        panelShowingNow.SetActive(true);
    }

    public void HidePanel()
    {
        if (panelShowingNow == null)
            return;

        panelShowingNow.SetActive(false);
        panelShowingNow = null;

        gameObject.SetActive(false);

        GameSystem.Instance.ResumeGame();
    }
}
