using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject panelShowingNow = null;

    public void ShowPanel(string panelName)
    {
        if (panelShowingNow != null)
            return;

        GameSystem.Instance.PauseGame();

        if (panelName == Strings.MapPanelName)
            SetUpMapPanel();

        gameObject.SetActive(true);
        panelShowingNow = transform.Find(Strings.PanelPath + panelName).gameObject;

        panelShowingNow.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelShowingNow == null)
            return;

        if (panelShowingNow.name == Strings.MapPanelName)
        {
            SaveMapPanel();
        }

        panelShowingNow.SetActive(false);
        panelShowingNow = null;

        gameObject.SetActive(false);

        GameSystem.Instance.ResumeGame();
    }

    private void SetUpMapPanel()
    {
        // TODO: highlight the playermark corresponding to the landmark that the player just crossed
    }

    private void SaveMapPanel()
    {
        // TODO: for each playermark, if it was dragged on to the map, lock it i.e. make it undraggable

    }

}
