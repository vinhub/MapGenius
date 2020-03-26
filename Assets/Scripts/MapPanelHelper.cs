using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapPanelHelper : MonoBehaviour
{
    private PanelManager m_panelManager;
    private Transform m_tMapPanel = null; // whether / which panel is being shown right now
    private string m_landmarkName;
    private bool m_firstLandmarkCrossed = false;

    private PlayermarkHandler m_phCur = null; // playmark handler correspoding to the landmark that the player just crossed

    private bool m_isLevelComplete = false; // whether the current level has been completed by the player

    private Transform m_tActionButton1, m_tActionButton2; // action buttons
    private Text m_actionButton1Text, m_actionButton2Text; // text for the action buttons
    private Transform m_tMapImage;
    private Camera m_skyCamera;

    private bool m_isMapPanelInitialized = false;
    private Text m_levelText, m_totalScoreText;
    private GameObject[] m_goLandmarks;
    private Transform m_tPlayermarkList;
    private bool m_revealedLandmarksOnMap = false;

    public void Setup(PanelManager panelManager, Transform tMapPanel, string landmarkName, bool firstLandmarkCrossed, bool continueGameOnClose)
    {
        if (m_tMapPanel != null)
            return;

        m_panelManager = panelManager;
        m_tMapPanel = tMapPanel;
        m_landmarkName = landmarkName;
        m_firstLandmarkCrossed = firstLandmarkCrossed;

        m_isLevelComplete = false;
        m_tActionButton1 = m_tMapPanel.Find(Strings.ActionButton1Path);
        m_tActionButton2 = m_tMapPanel.Find(Strings.ActionButton2Path);
        m_actionButton1Text = m_tMapPanel.Find(Strings.ActionButton1LabelPath).GetComponent<Text>();
        m_actionButton2Text = m_tMapPanel.Find(Strings.ActionButton2LabelPath).GetComponent<Text>();

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

            if (StaticGlobals.CurGameLevel <= GameLevel.Smalltown)
            {
                // for lower levels, always show hint
                Transform tHint = m_tMapPanel.Find(Strings.HintPath);
                tHint.gameObject.SetActive(false);

                ToggleHint(true);
            }
        }

        m_phCur = PlayermarkFromLandmark(m_landmarkName);

        // display current score
        DisplayScore(false);

        m_tActionButton1.gameObject.SetActive(false);

        if (continueGameOnClose) // show "continue game" button
        {
            m_actionButton2Text.text = Strings.ContinueGame;
            m_tActionButton2.GetComponent<Button>().onClick.RemoveAllListeners();
            m_tActionButton2.GetComponent<Button>().onClick.AddListener(m_panelManager.OnClickContinueGame);
        }
        else // show "back" button
        {
            m_actionButton2Text.text = Strings.Back;
            m_tActionButton2.GetComponent<Button>().onClick.RemoveAllListeners();
            m_tActionButton2.GetComponent<Button>().onClick.AddListener(m_panelManager.OnClickBack);
        }

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

    // Returns true if panel can be closed. False if not (when showing results instead of closing down)
    public bool CloseOrContinue(bool fCheckOkToClose)
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
        if (fCheckOkToClose && m_isLevelComplete)
        {
            // Calculate and display score
            float levelScore = CalcLevelScore();

            GameSystem.Instance.SetScore(levelScore);

            StartCoroutine(ShowLevelCompleteMessage());

            DisplayScore(true);

            m_tActionButton1.gameObject.SetActive(true);
            if (levelScore == StaticGlobals.MaxLevelScore) // max score achieved
            {
                m_actionButton1Text.text = Strings.VictoryLap;
                m_tActionButton1.GetComponent<Button>().onClick.RemoveAllListeners();
                m_tActionButton1.GetComponent<Button>().onClick.AddListener(m_panelManager.OnClickVictoryLap);
            }
            else
            {
                m_actionButton1Text.text = Strings.RetryGame;
                m_tActionButton1.GetComponent<Button>().onClick.RemoveAllListeners();
                m_tActionButton1.GetComponent<Button>().onClick.AddListener(m_panelManager.OnClickRetryGame);
            }


            m_actionButton2Text.text = Strings.NewGame;
            m_tActionButton2.GetComponent<Button>().onClick.RemoveAllListeners();
            m_tActionButton2.GetComponent<Button>().onClick.AddListener(m_panelManager.OnClickNewGame);

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
        ToggleHint(showLandmarksOnMap);
    }

    public void ToggleHint(bool showLandmarksOnMap)
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
        PopupMessage.ShowMessage(String.Format(
            (GameSystem.Instance.LevelScore == StaticGlobals.MaxLevelScore) ? Strings.GoodLevelCompleteMessageFormat : Strings.BadLevelCompleteMessageFormat,
            GameSystem.Instance.LevelScore, StaticGlobals.MaxLevelScore, (int)Time.fixedTime));

        yield return new WaitForSecondsRealtime(5f);

        PopupMessage.HideMessage();
    }

    private float CalcLevelScore()
    {
        float levelScore = 0f;

        int numLandmarksInLevel = m_goLandmarks.Length;
        float maxLandmarkScore = (float)StaticGlobals.MaxLevelScore / numLandmarksInLevel;
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

        return levelScore;
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
        m_levelText.text = String.Format(Strings.LevelTextFormat, StaticGlobals.CurGameLevel);
        m_totalScoreText.text = String.Format(Strings.ScoreTextFormat,
            (int)Math.Round(StaticGlobals.TotalScore, MidpointRounding.AwayFromZero),
            (int)Math.Round(StaticGlobals.TotalScoreMax, MidpointRounding.AwayFromZero));

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
            m_phCur.SetScoreFactor(((StaticGlobals.CurGameLevel > GameLevel.Smalltown) && m_revealedLandmarksOnMap) ? 50f : 100f);
        }

        // check if level is complete
        m_isLevelComplete = CheckLevelComplete();

        if (m_isLevelComplete)
            m_actionButton1Text.text = Strings.CheckScore;
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
