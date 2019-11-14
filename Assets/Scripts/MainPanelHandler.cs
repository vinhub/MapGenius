using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainPanelHandler : MonoBehaviour
{
    private GameObject m_panelCur = null; // whether / which panel is being shown right now
    private PlayermarkHandler m_phCur = null; // playmark handler correspoding to the landmark that the player just crossed

    private bool m_isLevelComplete = false; // whether the current level has been completed by the player
    private bool m_isScoreUpdated = false; // has score been updated after player has completed the level?

    private Text m_continueGameText; // text object for the "continue game" button
    private Transform m_tMapImage;
    private Camera m_skyCamera;

    private bool m_isMapPanelInitialized = false;
    private Text m_levelText, m_levelScoreText, m_totalScoreText;
    private GameObject[] m_goLandmarks;
    private Transform m_tPlayermarkList;
    private string m_landmarkName;

    public void ShowPanel(string panelName, string landmarkName = null)
    {
        if (m_panelCur != null)
            return;

        m_isLevelComplete = m_isScoreUpdated = false;

        m_landmarkName = landmarkName;
        m_panelCur = transform.Find(Strings.PanelPath + panelName).gameObject;
        m_continueGameText = transform.Find(Strings.ContinueGameTextPath).GetComponent<Text>();

        // if map panel is being invoked as a result of the player crossing a landmark but it has already been visited, then don't show the panel
        if (m_isMapPanelInitialized && (panelName == Strings.MapPanelName) && (landmarkName != null))
        {
            m_phCur = PlayermarkFromLandmark(landmarkName);

            if (m_phCur.State != PlayermarkHandler.PlayermarkState.Unvisited)
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

    private void SetUpMapPanel()
    {
        if (!m_isMapPanelInitialized)
        {
            m_skyCamera = GameObject.Find("Skycam").GetComponent<Camera>();
            m_tMapImage = this.transform.Find(Strings.MapImagePath);
            m_levelText = m_panelCur.transform.Find("PlayermarksPanel/Score/LevelText").GetComponent<Text>();
            m_levelScoreText = m_panelCur.transform.Find("PlayermarksPanel/Score/LevelScoreText").GetComponent<Text>();
            m_totalScoreText = m_panelCur.transform.Find("PlayermarksPanel/Score/TotalScoreText").GetComponent<Text>();
            m_goLandmarks = GameObject.FindGameObjectsWithTag("Landmark");
            m_tPlayermarkList = m_panelCur.transform.Find("PlayermarksPanel/Playermarks");

            m_isMapPanelInitialized = true;

            // add landmarks to the map help player know where they are (without telling them which landmark is which)
            AddLandmarksToMap();

            m_phCur = PlayermarkFromLandmark(m_landmarkName);
        }

        // display current score
        DisplayScore(false);

        m_continueGameText.text = "Save and Continue Game";

        // mark the current playermark as currently visiting
        if (m_phCur != null)
        {
            m_phCur.SetState(PlayermarkHandler.PlayermarkState.CurrentlyVisiting);

            if (m_phCur.tag == Strings.StartTag)
            {
                // move Start playermark to map
                StartCoroutine(MovePlayermarkToMap());
            }
        }
    }

    // called in response to player clicking the "continue game" button on the panel
    public void ContinueFromPanel()
    {
        if (m_panelCur == null)
            return;

        StopAllCoroutines();

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

            // Calculate and display score
            int levelScore;
            int totalScore = GameSystem.Instance.TotalScore;

            CalcScore(out levelScore, ref totalScore);

            GameSystem.Instance.SetScore(levelScore, totalScore);

            DisplayScore(true);

            m_isScoreUpdated = true;
        }
        else
        {
            StopAllCoroutines();

            m_phCur = null;
            m_panelCur.SetActive(false);
            m_panelCur = null;

            gameObject.SetActive(false);

            GameSystem.Instance.ResumeGame();
        }
    }

    private void CalcScore(out int levelScore, ref int totalScore)
    {
        levelScore = 0;

        int numLandmarksInLevel = m_goLandmarks.Length;
        float maxLandmarkScore = GameSystem.MaxLevelScore / numLandmarksInLevel;
        float maxMapDistance = 100;

        // calculate and add up the score for each landmark
        foreach (GameObject goLandmark in m_goLandmarks)
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

        RectTransform rectTrans = m_tMapImage.GetComponentInParent<RectTransform>(); //RenderTexture holder

        Vector2 viewPos = m_skyCamera.WorldToViewportPoint(positionIn);
        Vector2 localPos = new Vector2(viewPos.x * rectTrans.sizeDelta.x, viewPos.y * rectTrans.sizeDelta.y);
        Vector3 worldPos = rectTrans.TransformPoint(localPos);
        float scalerRatio = (1 / this.transform.lossyScale.x) * 2; // Implying all x y z are the same for the lossy scale

        return new Vector3(worldPos.x - rectTrans.sizeDelta.x / scalerRatio, worldPos.y - rectTrans.sizeDelta.y / scalerRatio, 0f);
    }

    private void DisplayScore(bool isNewScore)
    {
        m_levelText.text = "Level: " + GameSystem.Instance.CurLevel + ".";
        m_levelScoreText.text = "Level Score: " + GameSystem.Instance.LevelScore + " / " + GameSystem.MaxLevelScore + ".";
        m_totalScoreText.text = "Total Score: " + GameSystem.Instance.TotalScore + " / " + GameSystem.MaxScore + ".";

        if (isNewScore)
        {
            // if the score is new, blink it
            StartCoroutine(Blink(1000));
        }
    }

    private IEnumerator Blink(int blinkCount)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            m_levelScoreText.color = new Color(m_levelScoreText.color.r, m_levelScoreText.color.g, m_levelScoreText.color.b, 0);
            m_totalScoreText.color = new Color(m_levelScoreText.color.r, m_levelScoreText.color.g, m_levelScoreText.color.b, 0);
            yield return new WaitForSecondsRealtime(0.5f);

            m_levelScoreText.color = new Color(m_levelScoreText.color.r, m_levelScoreText.color.g, m_levelScoreText.color.b, 1);
            m_totalScoreText.color = new Color(m_levelScoreText.color.r, m_levelScoreText.color.g, m_levelScoreText.color.b, 1);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    // add landmarks to the map help player know where they are (without telling them which landmark is which)
    private void AddLandmarksToMap()
    {
        foreach (GameObject goLandmark in m_goLandmarks)
        {
            LandmarkHandler lh = goLandmark.GetComponent<LandmarkHandler>();
            Vector3 position = CalcPosOnMap(lh);

            GameObject go = new GameObject();
            go.transform.parent = m_tMapImage;
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(15, 15);
            go.AddComponent<Image>();
            go.transform.position = position;
        }
    }

    private PlayermarkHandler PlayermarkFromLandmark(string landmarkName)
    {
        Debug.Assert(m_isMapPanelInitialized);
        Debug.Assert(m_panelCur != null);

        if (landmarkName == null)
            return null;

        // PlayermarkListItem/PlayermarkText.text == landmarkname, then get its parent/Playermark/PlayermarkHandler
        foreach (Transform tPlayermarkListItem in m_tPlayermarkList)
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

    private GameObject LandmarkFromPlayermark(PlayermarkHandler ph)
    {
        Debug.Assert(m_panelCur != null);

        if ((ph == null) || (m_goLandmarks == null))
            return null;

        // get its parent/PlayermarkText/Text
        Transform tPlayermarkText = ph.transform.parent.Find("PlayermarkText");
        Text text = tPlayermarkText.GetComponent<Text>();

        // Text.text == goLandmark.name
        foreach (GameObject goLandmark in m_goLandmarks)
        {
            if (text.text == goLandmark.name)
            {
                return goLandmark;
            }
        }

        return null;
    }

    // move the current playermark from the playermark panel to the corresponding landmark on the map
    private IEnumerator MovePlayermarkToMap()
    {
        yield return new WaitForSecondsRealtime(1f); // need to delay calcs until things have settled down. Plus, it looks better this way.

        GameObject goLandmark = LandmarkFromPlayermark(m_phCur);
        LandmarkHandler lh = goLandmark.GetComponent<LandmarkHandler>();

        Vector3 position = CalcPosOnMap(lh);

        m_phCur.Move(position);
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

        foreach (Transform tPlayermarkListItem in m_tPlayermarkList)
        {
            Transform tPlayermark = tPlayermarkListItem.Find("Playermark");
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (ph.State != PlayermarkHandler.PlayermarkState.Visited)
                allPlayermarksVisited = false;
        }

        return allPlayermarksVisited;
    }
}
