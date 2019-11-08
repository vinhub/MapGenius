using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelShowingNow = null; // whether / which panel is showing right now
    private bool m_isLevelComplete = false; // whether the current level has been completed by the player

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        if (m_panelShowingNow != null)
            return;

        GameSystem.Instance.PauseGame();

        gameObject.SetActive(true);
        m_panelShowingNow = transform.Find(Strings.PanelPath + panelName).gameObject;

        m_panelShowingNow.SetActive(true);

        if (panelName == Strings.MapPanelName)
            SetUpMapPanel(landmarkName);
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

    private void SetUpMapPanel(string landmarkName)
    {
        if (m_isLevelComplete)
        {
            // TODO: show landmarks and score also

        }
        else
        {
            // if the panel is being shown as a result of player crossing a landmark
            if (landmarkName != null)
            {
                // highlight the playermark corresponding to the landmark
                PlayermarkHandler ph = PlayermarkFromLandmark(landmarkName);

                if (ph && !ph.IsLocked)
                {
                    ph.SetHighlight(true);
                }
            }

        }
    }

    private PlayermarkHandler PlayermarkFromLandmark(string landmarkName)
    {
        // PlayermarkListItem/PlayermarkText.text == landmarkname, then get its parent/Playermark/PlayermarkHandler
        Debug.Assert(m_panelShowingNow != null);

        foreach (Transform tPlayermarkListItem in m_panelShowingNow.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tPlayermarkText = tPlayermarkListItem.Find("PlayermarkText");
            Text text = tPlayermarkText.GetComponent<Text>();

            if (text.text == landmarkName)
            {
                Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
                return tPlayermark.GetComponent<PlayermarkHandler>();
            }
        }

        return null;
    }

    private void SavePlayermarkChanges()
    {
        if (m_isLevelComplete)
            return;

        bool areAllPlayermarksLocked = true;

        // for each playermark that is curently unlocked, if it was dragged and dropped at least once during the time the panel was open, lock it i.e. make it undraggable
        foreach (Transform tPlayermarkListItem in m_panelShowingNow.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (!ph.IsLocked)
            {
                if (ph.IsDropped)
                {
                    ph.SetLock(true);
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
