using System;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelCur = null; // whether / which panel is being shown right now
    private PlayermarkHandler m_phCur; // playmark handler correspoding to the landmark that the player just crossed

    private bool m_isLevelComplete = false; // whether the current level has been completed by the player
    private bool m_isScoreUpdated = false; // has score been updated after player has completed the level?

    private Text m_continueGameText; // text object for the "continue game" button

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        if (m_panelCur != null)
            return;

        m_isLevelComplete = m_isScoreUpdated = false;

        m_panelCur = transform.Find(Strings.PanelPath + panelName).gameObject;
        m_continueGameText = transform.Find(Strings.ContinueGameTextPath).GetComponent<Text>();

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
        else
            m_continueGameText.text = "Continue Game";
    }

    // called in response to player clicking the "continue game" button on the panel
    public void ContinueFromPanel()
    {
        if (m_panelCur == null)
            return;

        if (m_panelCur.name == Strings.MapPanelName)
        {
            SavePlayermarkChanges();
        }

        // if level is complete, we should not close the panel but show the results
        if (m_isLevelComplete)
        {
            // if the score was already shown, we can move to the next level
            if (m_isScoreUpdated)
            {
                // TODO: load next level
                GameSystem.Instance.LoadScene(Strings.Springfield);

                return;
            }

            // show the landmarks and score
            DisplayLandmarks();

            // Calculate and display score
            int levelScore;
            int totalScore = GameSystem.Instance.TotalScore;

            CalcScore(out levelScore, ref totalScore);

            GameSystem.Instance.SetScore(levelScore, totalScore);

            DisplayScore();

            m_isScoreUpdated = true;
        }
        else
        {
            m_panelCur.SetActive(false);
            m_panelCur = null;

            gameObject.SetActive(false);

            GameSystem.Instance.ResumeGame();
        }
    }

    private void CalcScore(out int levelScore, ref int totalScore)
    {
        levelScore = 0;

        int numLandmarksInLevel = 1;
        float maxLandmarkScore = GameSystem.MaxLevelScore / numLandmarksInLevel;
        float maxMapDistance = 100;

        // calculate and add up the score for each landmark
        GameObject[] landmarks = GameObject.FindGameObjectsWithTag("Landmark");
        foreach (GameObject goLandmark in landmarks)
        {
            LandmarkHandler lh = goLandmark.GetComponent<LandmarkHandler>();

            // find corresponding playermark
            PlayermarkHandler ph = PlayermarkFromLandmark(lh.name);

            // calculate position of landmark in the map
            Vector3 landmarkPos = CalcPosOnMap(lh);

            // claculate position of playermark in the map
            Transform tPlayermark = ph.transform;

            // calculae distance between them
            double distance = Vector3.Distance(landmarkPos, tPlayermark.position);

            if (distance > maxMapDistance)
                distance = maxMapDistance;

            // interpolate between the distance and the max distance possible in the map to get the score for this landmark
            int landmarkScore = (int)Math.Round(Mathf.Lerp(0, maxLandmarkScore, 1f - (float)(distance / maxMapDistance)));

            levelScore += landmarkScore;
        }

        totalScore += levelScore;
    }

    // Calculate the position of the landmark on the map in the map panel
    private Vector3 CalcPosOnMap(LandmarkHandler lh)
    {
        Vector3 positionIn = lh.transform.position;

        Transform tMapImage = m_panelCur.transform.Find("MapBackground/MapImage");
        RectTransform rectTrans = tMapImage.GetComponentInParent<RectTransform>(); //RenderTexture holder

        Camera skyCamera = GameObject.Find("Skycam").GetComponent<Camera>();

        Vector2 viewPos = skyCamera.WorldToViewportPoint(positionIn);
        Vector2 localPos = new Vector2(viewPos.x * rectTrans.sizeDelta.x, viewPos.y * rectTrans.sizeDelta.y);
        Vector3 worldPos = rectTrans.TransformPoint(localPos);
        float scalerRatio = (1 / this.transform.lossyScale.x) * 2; // Implying all x y z are the same for the lossy scale

        return new Vector3(worldPos.x - rectTrans.sizeDelta.x / scalerRatio, worldPos.y - rectTrans.sizeDelta.y / scalerRatio, 0f);
    }

    private void DisplayScore()
    {
        Text levelText = m_panelCur.transform.Find("PlayermarksPanel/Score/LevelText").GetComponent<Text>();
        levelText.text = "Level: " + GameSystem.Instance.CurLevel + ".";

        Text levelScoreText = m_panelCur.transform.Find("PlayermarksPanel/Score/LevelScoreText").GetComponent<Text>();
        levelScoreText.text = "Level Score: " + GameSystem.Instance.LevelScore + " / " + GameSystem.MaxLevelScore + ".";

        Text totalScoreText = m_panelCur.transform.Find("PlayermarksPanel/Score/TotalScoreText").GetComponent<Text>();
        totalScoreText.text = "Total Score: " + GameSystem.Instance.TotalScore + " / " + GameSystem.MaxScore + ".";
    }

    // Show landmarks so player can see their mistakes
    private void DisplayLandmarks()
    {
        GameObject[] landmarks = GameObject.FindGameObjectsWithTag("Landmark");
        foreach (GameObject goLandmark in landmarks)
        {
            LandmarkHandler lh = goLandmark.GetComponent<LandmarkHandler>();
            Vector3 position = CalcPosOnMap(lh);

            GameObject go = new GameObject();
            go.transform.parent = m_panelCur.transform.Find("MapBackground/MapImage");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
            go.AddComponent<Image>();
            go.transform.position = position;
        }
    }

    private void SetUpMapPanel()
    {
        if (m_phCur != null)
        {
            m_phCur.SetState(PlayermarkHandler.PlayermarkState.CurrentlyVisiting);
        }

        // display current score
        DisplayScore();

        // display landmarks to help player know where they are (without telling them which landmark is which)
        DisplayLandmarks();

        m_continueGameText.text = "Save and Continue Game";
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
