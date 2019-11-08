using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelShowingNow = null; // whether / which panel is showing right now
    private bool m_isLevelComplete = false; // whether the current level has been completed by the player

    public void ShowPanel(string panelName)
    {
        if (m_panelShowingNow != null)
            return;

        GameSystem.Instance.PauseGame();

        if (panelName == Strings.MapPanelName)
            SetUpMapPanel();

        gameObject.SetActive(true);
        m_panelShowingNow = transform.Find(Strings.PanelPath + panelName).gameObject;

        m_panelShowingNow.SetActive(true);
    }

    public void ClosePanel()
    {
        if (m_panelShowingNow == null)
            return;

        if (m_panelShowingNow.name == Strings.MapPanelName)
        {
            SavePlayermarkChanges();
        }

        m_panelShowingNow.SetActive(false);
        m_panelShowingNow = null;

        gameObject.SetActive(false);

        if (m_isLevelComplete)
        {
            // TODO: Load next level
        }
        else
        {
            GameSystem.Instance.ResumeGame();
        }
    }

    private void SetUpMapPanel()
    {
        if (m_isLevelComplete)
        {
            // TODO: show landmarks and score also

        }
        else
        {
            // TODO: highlight the playermark corresponding to the landmark that the player just crossed, if they had just done that

        }
    }

    private void SavePlayermarkChanges()
    {
        if (m_isLevelComplete)
            return;

        bool areAllPlayermarksLocked = true;

        // for each playermark that is curently unlocked, if it was dragged and dropped at least once during the time the panel was open, lock it i.e. make it undraggable
        foreach (Transform t in m_panelShowingNow.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tImage = t.Find("PlayermarkImage");
            PlayermarkHandler ph = tImage.GetComponent<PlayermarkHandler>();
            if (!ph.IsLocked)
            {
                if (ph.IsDropped)
                {
                    ph.IsLocked = true;
                }
                else
                {
                    areAllPlayermarksLocked = false;
                }
            }
        }

        if (areAllPlayermarksLocked) // if all playermarks are locked, then the level is complete
        {
            m_isLevelComplete = true;
        }
    }
}
