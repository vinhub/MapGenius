using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelCur = null; // whether / which panel is being shown right now
    private bool m_isLevelComplete = false; // whether the current level has been completed by the player
    private PlayermarkHandler m_phCur; // playmark handler correspoding to the landmark that the player just crossed

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        if (m_panelCur != null)
            return;

        m_panelCur = transform.Find(Strings.PanelPath + panelName).gameObject;

        m_phCur = PlayermarkFromLandmark(landmarkName);

        // if the playermark has already been drag-dropped, then don't show the panel. This is to prevent multiple collisions with the same landmark causing the panel to come up again and again.
        if ((m_phCur != null) && m_phCur.IsDropped)
        {
            m_panelCur = null;
            return;
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
            // TODO: Load next level
            GameSystem.Instance.LoadScene(Strings.Springfield);
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
            // highlight the current playermark and make it draggable
            if (m_phCur != null)
            {
                m_phCur.SetLock(false);
                m_phCur.SetHighlight(true);
            }
        }
    }

    private PlayermarkHandler PlayermarkFromLandmark(string landmarkName)
    {
        // PlayermarkListItem/PlayermarkText.text == landmarkname, then get its parent/Playermark/PlayermarkHandler
        Debug.Assert(m_panelCur != null);

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
        if (m_isLevelComplete)
            return;

        // lock the current playmarker and mark it as drag-dropped
        if (m_phCur != null)
        {
            m_phCur.SetHighlight(false);
            m_phCur.SetDropped(true);
            m_phCur.SetLock(true);
        }

        // check if level is complete
        m_isLevelComplete = CheckLevelComplete();
    }

    // if all playermarks have been finalized i.e. drag-dropped and locked, then the level is complete
    private bool CheckLevelComplete()
    {
        bool areAllPlayermarksFinalized = true;

        foreach (Transform tPlayermarkListItem in m_panelCur.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (!ph.IsLocked || !ph.IsDropped)
                areAllPlayermarksFinalized = false;
        }

        return areAllPlayermarksFinalized;
    }
}
