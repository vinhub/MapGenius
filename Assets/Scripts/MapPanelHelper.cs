using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapPanelHelper : MonoBehaviour
{
    private Transform m_tMapPanel = null; // whether / which panel is being shown right now
    private string m_landmarkName;
    private bool m_firstLandmarkCrossed = false;

    private PlayermarkHandler m_phCur = null; // playmark handler correspoding to the landmark that the player just crossed

    private bool m_isLevelComplete = false; // whether the current level has been completed by the player
    private bool m_isScoreUpdated = false; // has score been updated after player has completed the level?

    private Text m_closePanelText; // text object for the "continue game" button
    private Transform m_tMapImage;
    private Camera m_skyCamera;

    private bool m_isMapPanelInitialized = false;
    private Text m_levelText, m_totalScoreText;
    private GameObject[] m_goLandmarks;
    private Transform m_tPlayermarkList;
    private bool m_revealedLandmarksOnMap = false;

    public void Setup(Transform tMapPanel, string landmarkName, bool firstLandmarkCrossed)
    {
        if (m_tMapPanel != null)
            return;

        m_tMapPanel = tMapPanel;
        m_landmarkName = landmarkName;
        m_firstLandmarkCrossed = firstLandmarkCrossed;

        m_isLevelComplete = m_isScoreUpdated = false;
        m_closePanelText = m_tMapPanel.Find(Strings.PanelCloseButtonPath).GetComponent<Text>();

        if (!m_isMapPanelInitialized)
        {
            m_skyCamera = GameObject.Find("Skycam").GetComponent<Camera>();
            m_tMapImage = m_tMapPanel.Find(Strings.MapImagePath);
            m_levelText = m_tMapPanel.Find(Strings.LevelTextPath).GetComponent<Text>();
            m_totalScoreText = m_tMapPanel.Find(Strings.TotalScoreTextPath).GetComponent<Text>();
            m_goLandmarks = GameObject.FindGameObjectsWithTag(Strings.LandmarkTag);
            m_tPlayermarkList = m_tMapPanel.Find(Strings.PlayermarksPath);

            m_isMapPanelInitialized = true;

            // add landmarks to the map help player know where they are (without telling them which landmark is which)
            AddLandmarksToMap();
        }

        m_phCur = PlayermarkFromLandmark(m_landmarkName);

        // display current score
        DisplayScore(false);

        if (String.IsNullOrEmpty(landmarkName))
            m_closePanelText.text = Strings.Back;
        else
            m_closePanelText.text = Strings.SaveAndContinue;

        // mark the current playermark as currently visiting
        if (m_phCur != null)
        {
            m_phCur.SetState(PlayermarkHandler.PlayermarkState.CurrentlyVisiting);

            if (firstLandmarkCrossed)
            {
                // move Start playermark to map
                StartCoroutine(MovePlayermarkToMap());
            }
        }
    }

    // called in response to player clicking the "save and continu" button on the panel. Returns true if panel can be closed. False if not (when showing results instead of closing down)
    public bool Close()
    {
        if (m_tMapPanel == null)
            return true;

        StopAllCoroutines();

        if (m_firstLandmarkCrossed)
        {
            // ensure the first landmark is properly positioned on the map in case coroutine was stopped before it ended
            GameObject goLandmark = LandmarkFromPlayermark(m_phCur);
            LandmarkHandler lh = goLandmark.GetComponent<LandmarkHandler>();
            Vector3 position = CalcPosOnMap(lh);
            m_phCur.transform.position = position;
            m_phCur.DoneMoving();

            m_firstLandmarkCrossed = false;
        }

        SavePlayermarkChanges();

        // if level is complete, we should not close the panel but show the results
        if (m_isLevelComplete)
        {
            // if the score was already shown, we can move to the next level
            if (m_isScoreUpdated)
            {
                // TODO: load next level
                GameSystem.Instance.LoadScene(Strings.Springfield);

                return true;
            }

            // Calculate and display score
            float levelScore;
            float totalScore = GameSystem.Instance.TotalScore;

            CalcScore(out levelScore, ref totalScore);

            GameSystem.Instance.SetScore(levelScore, totalScore);

            StartCoroutine(ShowLevelCompleteMessage());

            DisplayScore(true);

            m_isScoreUpdated = true;

            m_closePanelText.text = Strings.MoveToNextLevel;

            return false;
        }
        else
        {
            m_phCur = null;
            m_tMapPanel = null;
        }

        return true;
    }

    public void OnHintToggleChange(bool showLandmarksOnMap)
    {
        if (!m_isMapPanelInitialized)
            return;

        int childCount = m_tMapImage.childCount;

        for (int iChild = 0; iChild < childCount; iChild++)
        {
            Transform tChild = m_tMapImage.GetChild(iChild);
            if (tChild.name == Strings.LandmarkOnMap)
                tChild.gameObject.SetActive(showLandmarksOnMap);
        }

        // remember that landmarks were revealed
        if (showLandmarksOnMap)
        {
            m_revealedLandmarksOnMap = true;

            // disable the toggle
            Transform tToggle = m_tMapPanel.Find(Strings.ShowLandmarksTogglePath);
            tToggle.GetComponent<Toggle>().enabled = false;
            Transform tBackground = m_tMapPanel.Find(Strings.ShowLandmarksBackgroundPath);
            tBackground.GetComponent<Image>().color = new Color32(192, 192, 192, 255);
            Transform tLabel = m_tMapPanel.Find(Strings.ShowLandmarksLabelPath);
            tLabel.GetComponent<Text>().color = new Color32(192, 192, 192, 255);
        }
    }

    private IEnumerator ShowLevelCompleteMessage()
    {
        PopupMessage.ShowMessage(String.Format(Strings.LevelCompleteMessageFormat, GameSystem.Instance.LevelScore, (int)Time.fixedTime));

        yield return new WaitForSecondsRealtime(5f);

        PopupMessage.HideMessage();
    }

    private void CalcScore(out float levelScore, ref float totalScore)
    {
        levelScore = 0f;

        int numLandmarksInLevel = m_goLandmarks.Length;
        float maxLandmarkScore = (float)GameSystem.MaxLevelScore / numLandmarksInLevel;
        int maxSlopDistance = 30; // amount of slop allowed in placement of playermark

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

            // full points if the playermark is within slop distance of the landmark
            float landmarkScore = (distance <= maxSlopDistance) ? (maxLandmarkScore * ph.ScoreFactor / 100f) : 0f;

            ph.OnUpdateScore(landmarkScore);

            levelScore += landmarkScore;
        }

        totalScore += levelScore;
    }

    // Calculate the position of the landmark on the map in the map panel
    private Vector3 CalcPosOnMap(LandmarkHandler lh)
    {
        Vector3 positionIn = lh.transform.position;

        RectTransform rectTrans = m_tMapImage.GetComponentInParent<RectTransform>(); // RenderTexture holder

        Vector2 viewPos = m_skyCamera.WorldToViewportPoint(positionIn);
        Vector2 localPos = new Vector2(viewPos.x * rectTrans.sizeDelta.x, viewPos.y * rectTrans.sizeDelta.y);
        Vector3 worldPos = rectTrans.TransformPoint(localPos);
        float scalerRatio = (1 / m_tMapPanel.lossyScale.x) * 2; // Implying all x y z are the same for the lossy scale

        return new Vector3(worldPos.x - rectTrans.sizeDelta.x / scalerRatio, worldPos.y - rectTrans.sizeDelta.y / scalerRatio, 0f);
    }

    private void DisplayScore(bool isNewScore)
    {
        m_levelText.text = String.Format(Strings.LevelTextFormat, GameSystem.Instance.CurLevel);
        m_totalScoreText.text = String.Format(Strings.ScoreTextFormat, (int)Math.Round(GameSystem.Instance.TotalScore, MidpointRounding.AwayFromZero));

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
            m_totalScoreText.color = new Color(m_totalScoreText.color.r, m_totalScoreText.color.g, m_totalScoreText.color.b, 0);
            yield return new WaitForSecondsRealtime(0.5f);

            m_totalScoreText.color = new Color(m_totalScoreText.color.r, m_totalScoreText.color.g, m_totalScoreText.color.b, 1);
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
            go.SetActive(m_revealedLandmarksOnMap);
            go.name = Strings.LandmarkOnMap;
            go.transform.parent = m_tMapImage;
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(15, 15);
            go.AddComponent<Image>();
            go.transform.position = position;
        }
    }

    private PlayermarkHandler PlayermarkFromLandmark(string landmarkName)
    {
        Debug.Assert(m_isMapPanelInitialized);
        Debug.Assert(m_tMapPanel != null);

        if (String.IsNullOrEmpty(landmarkName))
            return null;

        // PlayermarkListItem/PlayermarkText.text == landmarkname, then get its parent/Playermark/PlayermarkHandler
        foreach (Transform tPlayermarkListItem in m_tPlayermarkList)
        {
            Transform tPlayermarkText = tPlayermarkListItem.Find(Strings.PlayermarkTextName);
            Text text = tPlayermarkText.GetComponent<Text>();

            if (text.text == landmarkName)
            {
                Transform tPlayermark = tPlayermarkListItem.Find(Strings.PlayermarkName);
                return tPlayermark.GetComponent<PlayermarkHandler>();
            }
        }

        return null;
    }

    private GameObject LandmarkFromPlayermark(PlayermarkHandler ph)
    {
        Debug.Assert(m_tMapPanel != null);

        if ((ph == null) || (m_goLandmarks == null))
            return null;

        // get its parent/PlayermarkText/Text
        Transform tPlayermarkText = ph.transform.parent.Find(Strings.PlayermarkTextName);
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
            m_phCur.SetScoreFactor(m_revealedLandmarksOnMap ? 50 : 100);
        }

        // check if level is complete
        m_isLevelComplete = CheckLevelComplete();

        if (m_isLevelComplete)
            m_closePanelText.text = Strings.CheckScore;
    }

    // if all playermarks have been finalized i.e. drag-dropped and locked, then the level is complete
    private bool CheckLevelComplete()
    {
        bool allPlayermarksVisited = true;

        foreach (Transform tPlayermarkListItem in m_tPlayermarkList)
        {
            Transform tPlayermark = tPlayermarkListItem.Find(Strings.PlayermarkName);
            PlayermarkHandler ph = tPlayermark.GetComponent<PlayermarkHandler>();
            if (ph.State != PlayermarkHandler.PlayermarkState.Visited)
                allPlayermarksVisited = false;
        }

        return allPlayermarksVisited;
    }
}
