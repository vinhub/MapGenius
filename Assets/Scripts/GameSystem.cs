using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class GameSystem : MonoBehaviour
{
    // our singleton instance
    private static GameSystem m_instance;
    public static GameSystem Instance { get { return m_instance; } }

    public const int MaxLevelScore = 100; // max score for a level
    public const int NumLevels = 10; // total number of levels
    public const int MaxScore = MaxLevelScore * NumLevels; // max possible score
    private const int m_numLandmarks = 5;
    private const int m_landmarksLayerIndex = 11; // TODO: remove this hardcoding
    private const int m_UILayerIndex = 5; // TODO: remove this hardcoding

    public int CurLevel { get; private set; } = 1; // current level
    public float LevelScore { get; private set; } // player's score so far for the current level
    public float TotalScore { get; private set; } = 0; // player's total score so far

    public GameObject Car; // the car being driven by the player
    private CarController m_carController;

    public GameObject LandmarkPrefab; // prefab for landmarks
    public GameObject PlayermarksListItemPrefab; // prefab for playermarks list item in the map panel

    private Transform m_tRoadHolder, m_tNodeHolder, m_tLandmarks;

    // for pausing / resuming game
    private float m_timeScaleSav = 1f;
    private bool m_paused;

    private PanelManager m_mainPanelManager;
    private Transform m_tPlayermarksList;
    private bool m_firstLandmarkCrossed = true;

    private void Awake()
    {
        m_instance = this;

        m_carController = Car.GetComponent<CarController>();
        m_tNodeHolder = GameObject.Find(Strings.NodeHolderPath).transform;
        m_tRoadHolder = GameObject.Find(Strings.RoadHolderPath).transform;
        m_tLandmarks = GameObject.Find(Strings.LandmarksPath).transform;
    }

    private void Start()
    {
        GameObject mainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag);
        m_mainPanelManager = mainMenuUI.transform.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();
        m_tPlayermarksList = mainMenuUI.transform.Find(Strings.MapPanelPath).Find(Strings.PlayermarksPath);

        InitGame();
    }

    private void InitGame()
    {
        // show game instructions at start once
        if (PlayerPrefs.GetInt(Strings.ShowInstructionsAtStart, 1) == 1)
        {
            m_mainPanelManager.OpenInstructionsPanel(true);
            PlayerPrefs.SetInt(Strings.ShowInstructionsAtStart, 0);
        }

        // init score and level
        SetScore(0, 0);

        // init landmarks
        InitLandmarks(m_numLandmarks);
    }

    private void InitLandmarks(int numLandmarks)
    {
        // select required number of roads from the road network making sure they are reasonably placed
        Transform[] tNodesSelected = new Transform[numLandmarks];

        // collect all nodes
        List<Transform> tNodes = new List<Transform>();

        foreach (Transform tNode in m_tNodeHolder)
        {
            tNodes.Add(tNode);
        }

        // sort nodes geographically
        tNodes.Sort((n1, n2) => (n1.position.x * n1.position.z).CompareTo(n2.position.x * n2.position.z));

        // select required number of nodes so they are reasonably apart from each other by selecting them from different ranges from the sorted list
        int rangeMin = 0, rangeMax;
        for (int iLandmark = 0; iLandmark < numLandmarks; iLandmark++)
        {
            rangeMax = rangeMin + (tNodes.Count / numLandmarks);
            if (rangeMax > numLandmarks - (tNodes.Count / numLandmarks))
                rangeMax = numLandmarks;

            int iNode = UnityEngine.Random.Range(rangeMin, rangeMax);
            tNodesSelected[iLandmark] = tNodes[iNode];

            rangeMin = rangeMax;
        }

        // add landmarks corresponding to the selected nodes
        for (int iLandmark = 0; iLandmark < numLandmarks; iLandmark++)
        {
            // select an end of road corresponding to the selected node, making sure they are different from each other

            // collect all roads that end in the selected node
            List<CiDyRoad> roads = new List<CiDyRoad>();
            foreach (Transform tRoad in m_tRoadHolder)
            {
                CiDyRoad road = tRoad.GetComponent<CiDyRoad>();
                string nodeA = road.gameObject.name.Substring(0, 2), nodeB = road.gameObject.name.Substring(2, 2); // TODO: Ideally this should not be based on road name
                if (road && ((nodeA == tNodesSelected[iLandmark].name) || (nodeB == tNodesSelected[iLandmark].name)))
                    roads.Add(road);
            }

            // select one of them
            int iRoadSelected = UnityEngine.Random.Range(0, roads.Count);

            // calculate a point about halfway along the road and orient it so it faces the road
            Vector3 landmarkPos = roads[iRoadSelected].origPoints[roads[iRoadSelected].origPoints.Length / 2];
            Vector3 nextPos = roads[iRoadSelected].origPoints[roads[iRoadSelected].origPoints.Length / 2 + 1];
            Quaternion rotation = Quaternion.LookRotation(nextPos - landmarkPos, Vector3.up); 

            // add a landmark to the area using the landmark prefab and place it at the above location
            GameObject goLandmark = Instantiate(LandmarkPrefab, landmarkPos, rotation);

            // set up the landmark correctly
            string landmarkName = Strings.LandmarkNames[iLandmark];
            goLandmark.name = landmarkName;
            goLandmark.transform.parent = m_tLandmarks;
            goLandmark.tag = Strings.LandmarkTag;
            goLandmark.layer = m_landmarksLayerIndex;

            // init landmark sign
            goLandmark.transform.Find(Strings.LandmarkName).GetComponent<TextMesh>().text = landmarkName;
            goLandmark.transform.Find(Strings.LandmarkName2).GetComponent<TextMesh>().text = landmarkName;

            // add playermark list item
            GameObject goPlayermarkListItem = Instantiate(PlayermarksListItemPrefab, m_tPlayermarksList);
            goPlayermarkListItem.tag = Strings.PlayermarkTag;
            goPlayermarkListItem.layer = m_UILayerIndex;
            goPlayermarkListItem.transform.Find(Strings.PlayermarkIndexEmptyPath).GetComponent<Text>().text = (iLandmark + 1).ToString();
            goPlayermarkListItem.transform.Find(Strings.PlayermarkIndexPath).GetComponent<Text>().text = (iLandmark + 1).ToString();
            goPlayermarkListItem.transform.Find(Strings.PlayermarkTextPath).GetComponent<Text>().text = landmarkName;
        }
    }

    private void OnDestroy()
    {
        m_instance = null;
    }

    private void Update()
    {
        // global game update logic goes here
    }

    private void OnGui()
    {
        // common GUI code goes here
    }

    public void NewGame()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadScene(string sceneName)
    {
        GameSystem.Instance.ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    public void PauseGame()
    {
        if (m_paused)
            return;

        m_carController.StopCar();

        m_timeScaleSav = Time.timeScale;
        Time.timeScale = 0f;

        m_paused = true;
    }

    public void ResumeGame()
    {
        if (!m_paused)
            return;

        Time.timeScale = m_timeScaleSav;
        m_paused = false;
    }

    public bool IsGamePaused()
    {
        return m_paused;
    }

    public void QuitGame()
    {
        Debug.Log("Player quit the game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }

    // called when the player crosses a landmark
    public void LandmarkCrossed(string landmarkName)
    {
        if (!String.IsNullOrEmpty(m_mainPanelManager.CurLandmarkName)) // currently processing a landmark?
            return;

        PauseGame();

        StartCoroutine(HandleLandmarkCrossed(landmarkName));
    }

    private IEnumerator HandleLandmarkCrossed(string landmarkName)
    {
        // flash a popup letting the player know they crossed the landmark
        string messageText = String.Format(Strings.LandmarkCrossedMessageFormat, landmarkName) +
            (m_firstLandmarkCrossed ? Strings.FirstLandmarkCrossedMessage : Strings.OtherLandmarkCrossedMessage);

        PopupMessage.ShowMessage(messageText);

        // let it stay for some time
        yield return new WaitForSecondsRealtime(m_firstLandmarkCrossed ? 5f : 4f);

        PopupMessage.HideMessage();

        // then show map panel
        m_mainPanelManager.OpenMapPanel(landmarkName, m_firstLandmarkCrossed);

        m_firstLandmarkCrossed = false;
    }

    public void SetScore(float levelScore, float totalScore)
    {
        LevelScore = levelScore;
        TotalScore = totalScore;

        m_mainPanelManager.UpdateScore(levelScore, totalScore);
    }
}
