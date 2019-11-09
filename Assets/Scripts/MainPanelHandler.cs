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

        // if the landmark has already been locked, then don't show the panel
        if ((m_phCur != null) && m_phCur.IsLocked)
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
            // highlight the current playermark
            if (m_phCur != null)
                m_phCur.SetHighlight(true);
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

        // lock the current playmarker
        if (m_phCur != null)
            m_phCur.SetLock(true);

        // check if level is complete
        m_isLevelComplete = CheckLevelComplete();
    }

    // if all playmarkers are locked, then level is complete
    private bool CheckLevelComplete()
    {
        bool areAllPlayermarksLocked = true;

        foreach (Transform tPlayermarkListItem in m_panelCur.transform.Find("PlayermarksPanel/Playermarks"))
        {
            Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (!ph.IsLocked)
                areAllPlayermarksLocked = false;
        }

        return areAllPlayermarksLocked;
    }
}
