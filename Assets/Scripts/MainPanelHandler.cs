using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelCur = null; // whether / which panel is being shown right now
    private PlayermarkHandler m_phCur; // playmark handler correspoding to the landmark that the player just crossed

    private bool m_isLevelComplete = false; // whether the current level has been completed by the player
    private bool m_isScoreUpdated = false; // has score been updated after player has completed the level?

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        if (m_panelCur != null)
            return;

        m_panelCur = transform.Find(Strings.PanelPath + panelName).gameObject;

        if (panelName == Strings.MapPanelName)
        {
            m_phCur = PlayermarkFromLandmark(landmarkName);

            // if the playermark has already been visited, then don't show the panel
            if ((m_phCur != null) && (m_phCur.State != PlayermarkHandler.PlayermarkState.Unvisited))
            {
                m_panelCur = null;
                return;
            }
        }

        GameSystem.Instance.PauseGame();

        gameObject.SetActive(true);
        m_panelCur.SetActive(true);

        if (panelName == Strings.MapPanelName)
            SetUpMapPanel();
    }

    public void ClosePanel()
    {
        if (m_panelCur == null)
            return;

        if (m_panelCur.name == Strings.MapPanelName)
        {
            SavePlayermarkChanges();
        }

        m_panelCur.SetActive(false);
        m_panelCur = null;

        gameObject.SetActive(false);

        if (m_isLevelComplete)
        {
            if (m_isScoreUpdated)
            {
                // TODO: load next level
                GameSystem.Instance.LoadScene(Strings.Springfield);

                return;
            }

            // TODO: calculate and show score

            // TODO: Show landmarks with labels so player can see their mistakes

            // TODO: change close button to next level button

            m_isScoreUpdated = true;
        }
        else
        {
            GameSystem.Instance.ResumeGame();
        }
    }

    private void SetUpMapPanel()
    {
        if (m_phCur != null)
        {
            m_phCur.SetState(PlayermarkHandler.PlayermarkState.CurrentlyVisiting);
        }
    }

    private PlayermarkHandler PlayermarkFromLandmark(string landmarkName)
    {
        Debug.Assert(m_panelCur != null);

        if (landmarkName == null)
            return null;

        // PlayermarkListItem/PlayermarkText.text == landmarkname, then get its parent/Playermark/PlayermarkHandler
        foreach (Transform tPlayermarkListItem in m_panelCur.transform.Find("PlayermarksPanel/Playermarks"))
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
        // lock the current playmarker and mark it as drag-dropped
        if (m_phCur != null)
        {
            m_phCur.SetState(PlayermarkHandler.PlayermarkState.Visited);
        }

        // check if level is complete
        m_isLevelComplete = CheckLevelComplete();
    }

    // if all playermarks have been finalized i.e. drag-dropped and locked, then the level is complete
    private bool CheckLevelComplete()
    {
        bool allPlayermarksVisited = true;

        foreach (Transform tPlayermarkListItem in m_panelCur.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (ph.State != PlayermarkHandler.PlayermarkState.Visited)
                allPlayermarksVisited = false;
        }

        return allPlayermarksVisited;
    }
}
