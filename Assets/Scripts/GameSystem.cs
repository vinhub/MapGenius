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

    public float LevelScore { get; private set; } // player's score so far for the current level
    public float TotalScore { get; private set; } = 0; // player's total score so far

    public GameObject Car; // the car being driven by the player
    public GameObject CarCameraRig; // the car camera rig
    private CarController m_carController;
    public GameObject LandmarkPrefab; // prefab for landmarks
    public GameObject PlayermarksListItemPrefab; // prefab for playermarks list item in the map panel

    private Transform m_tRoadHolder, m_tNodeHolder, m_tLandmarks;
    private GameObject m_graph;

    // for pausing / resuming game
    private float m_timeScaleSav = 1f;
    private bool m_paused;

    private PanelManager m_mainPanelManager;
    private Transform m_tPlayermarksList;
    private bool m_firstLandmarkCrossed = true;

    // starting loation for the car
    Vector3 m_carPosStart;
    Quaternion m_carRotationStart;

    private void Awake()
    {
        m_instance = this;
    }

    private void Start()
    {
        m_carController = Car.GetComponent<CarController>();

        GameObject mainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag);
        m_mainPanelManager = mainMenuUI.transform.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();
        m_tPlayermarksList = mainMenuUI.transform.Find(Strings.MapPanelPath).Find(Strings.PlayermarksPath);

        InitGame();
    }

    private void InitGame()
    {
        // show game instructions at start unless user has asked to hide them
        if (PlayerPrefs.GetInt(Strings.HideInstructionsAtStart, 0) == 0)
        {
            m_mainPanelManager.OpenInstructionsPanel(true);
        }

        CiDyGraph[] graphs = Resources.FindObjectsOfTypeAll<CiDyGraph>();

        m_graph = Array.Find<CiDyGraph>(graphs, g => g.name == StaticGlobals.CurGameLevel.ToString()).gameObject;
        m_graph.SetActive(true);
 
        // now that we have a graph, we can gather some frequently needed references
        m_tNodeHolder = m_graph.transform.Find(Strings.NodeHolderPath).transform;
        m_tRoadHolder = m_graph.transform.Find(Strings.RoadHolderPath).transform;
        m_tLandmarks = GameObject.Find(Strings.LandmarksPath).transform;

        if (StaticGlobals.SavedInitStateExists)
        {
            ReInitGameState();
        }
        else
        {
            // init landmarks
            InitGameState();
        }

        // place car some distance from the first landmark
        Car.transform.position = CarCameraRig.transform.position = m_carPosStart;
        Car.transform.rotation = CarCameraRig.transform.rotation = m_carRotationStart;

        // init score and level
        SetScore(0, 0);
    }

    private void InitGameState()
    {
        m_carPosStart = Vector3.forward;
        m_carRotationStart = Quaternion.identity;

        // select required number of roads from the road network making sure they are geographically distributed 
        CiDyRoad[] roadsSelected = new CiDyRoad[m_numLandmarks];

        // collect all roads
        List<CiDyRoad> roads = new List<CiDyRoad>();

        foreach (Transform tRoad in m_tRoadHolder)
        {
            roads.Add(tRoad.GetComponent<CiDyRoad>());
        }

        // determine the dimensions of the road network (not the whole map)
        float roadNetworkMinX, roadNetworkMinZ, roadNetworkWidth, roadNetworkHeight;

        CalcRoadNetworkDimensions(out roadNetworkMinX, out roadNetworkMinZ, out roadNetworkWidth, out roadNetworkHeight);

        // sort roads so they are spread out along the map
        int numSectionsX = 3, numSectionsZ = 3;
        roads.Sort((r1, r2) => 
            (CalcPosWeight(r1.origPoints[0], roadNetworkMinX, roadNetworkMinZ, roadNetworkWidth, roadNetworkHeight, numSectionsX, numSectionsZ).CompareTo(CalcPosWeight(r2.origPoints[0], roadNetworkMinX, roadNetworkMinZ, roadNetworkWidth, roadNetworkHeight, numSectionsX, numSectionsZ))));

        // select required number of roads so they are reasonably apart from each other by selecting them from different ranges from the sorted list
        int rangeMin = 0, rangeMax;
        for (int iLandmark = 0; iLandmark < m_numLandmarks; iLandmark++)
        {
            if (iLandmark == m_numLandmarks - 1)
                rangeMax = roads.Count;
            else 
                rangeMax = rangeMin + (roads.Count / m_numLandmarks);

            // select a road within this range, making sure we haven't selected it already
            int iRoad = rangeMin, cTries = 10;
            while (cTries-- > 0)
            {
                iRoad = UnityEngine.Random.Range(rangeMin, rangeMax);
                if (Array.FindIndex(roadsSelected, 0, iLandmark, road => road == roads[iRoad]) < 0)
                    break;
            }

            roadsSelected[iLandmark] = roads[iRoad];

            rangeMin = rangeMax;
        }

        int iLandmarkStart = UnityEngine.Random.Range(0, m_numLandmarks); // select one of the landmarks to be the first one

        // add landmarks corresponding to the selected roads
        for (int iLandmark = 0; iLandmark < m_numLandmarks; iLandmark++)
        {
            // get the selected road
            CiDyRoad road = roadsSelected[iLandmark];

            // calculate a point about halfway along the road and orient it so it faces the road
            Vector3 landmarkPos = road.origPoints[road.origPoints.Length / 2];
            landmarkPos.y -= 0.5f; // lower it slightly so the landmark does not appear to be floating on sloping roads
            Vector3 nextPos = road.origPoints[road.origPoints.Length / 2 + 1];
            nextPos.y = landmarkPos.y; // make the direction horizontal so the landmark stands up vertical
            Quaternion rotation = Quaternion.LookRotation(landmarkPos - nextPos, Vector3.up);

            // add a landmark to the area using the landmark prefab and place it at the above location
            CreateLandmark(iLandmark, landmarkPos, rotation);

            // for the start landmark, use a position about 1/3 of the way along the road as the car position, looking along the road
            if (iLandmark == iLandmarkStart)
            {
                CalcCarPos(road);
            }
        }
    }

    private void CreateLandmark(int iLandmark, Vector3 landmarkPos, Quaternion rotation)
    {
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

    private void CalcCarPos(CiDyRoad road)
    {
        m_carPosStart = road.origPoints[road.origPoints.Length / 3];
        m_carRotationStart = Quaternion.LookRotation(road.origPoints[road.origPoints.Length / 3 + 1] - m_carPosStart, Vector3.up);
    }

    // divide the whole map into numSectionsX x numSectionsZ sections, calc which section the point belongs to, calc its weight based on that
    private float CalcPosWeight(Vector3 pos, float roadNetworkMinX, float roadNetworkMinZ, float roadNetworkWidth, float roadNetworkHeight, int numSectionsX, int numSectionsZ)
    {
        float weightX, weightZ;

        weightX = (pos.x - roadNetworkMinX) / (roadNetworkWidth / numSectionsX);
        weightZ = (pos.z - roadNetworkMinZ) / (roadNetworkHeight / numSectionsZ);

        return weightX * numSectionsX + weightZ;
    }

    // calculate dimensions of the road network by taking the min and max positions of the nodes
    private void CalcRoadNetworkDimensions(out float roadNetworkMinX, out float roadNetworkMinZ, out float roadNetworkWidth, out float roadNetworkHeight)
    {
        float nodeXMin = float.MaxValue, nodeXMax = float.MinValue, nodeZMin = float.MaxValue, nodeZMax = float.MinValue;

        foreach (Transform tNode in m_tNodeHolder)
        {
            nodeXMin = Mathf.Min(tNode.position.x, nodeXMin);
            nodeXMax = Mathf.Max(tNode.position.x, nodeXMax);
            nodeZMin = Mathf.Min(tNode.position.z, nodeZMin);
            nodeZMax = Mathf.Max(tNode.position.z, nodeZMax);
        }

        roadNetworkMinX = nodeXMin;
        roadNetworkMinZ = nodeZMin;
        roadNetworkWidth = nodeXMax - nodeXMin;
        roadNetworkHeight = nodeZMax - nodeZMin;
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

    public void NewLevel()
    {
        ResumeGame();

        switch (StaticGlobals.CurGameLevel)
        {
            case GameLevel.Downtown:
                StaticGlobals.CurGameLevel = GameLevel.Smalltown;
                break;

            case GameLevel.Smalltown:
                StaticGlobals.CurGameLevel = GameLevel.OldTown;
                break;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // retry the same level without changing anything
    public void RetryGame()
    {
        ResumeGame();
        SaveGameInitState();

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

    private void SaveGameInitState()
    {
        if (StaticGlobals.SavedLandmarks != null)
            StaticGlobals.SavedLandmarks.Clear();

        StaticGlobals.SavedLandmarks = new List<SavedLandmark>();

        foreach(Transform tLandmark in m_tLandmarks)
        {
            GameObject goLandmark = tLandmark.gameObject;

            SavedLandmark sl = new SavedLandmark();
            sl.name = goLandmark.name;
            sl.pos = tLandmark.position;
            sl.rotation = tLandmark.rotation;

            StaticGlobals.SavedLandmarks.Add(sl);
        }

        StaticGlobals.SavedCarPosStart = m_carPosStart;
        StaticGlobals.SavedCarRotationStart = m_carRotationStart;

        StaticGlobals.SavedInitStateExists = true;
    }

    private void ReInitGameState()
    {
        for (int iLandmark = 0; iLandmark < StaticGlobals.SavedLandmarks.Count; iLandmark++)
        {
            CreateLandmark(iLandmark, StaticGlobals.SavedLandmarks[iLandmark].pos, StaticGlobals.SavedLandmarks[iLandmark].rotation);
        }

        m_carPosStart = StaticGlobals.SavedCarPosStart;
        m_carRotationStart = StaticGlobals.SavedCarRotationStart;

        StaticGlobals.SavedLandmarks.Clear();
        StaticGlobals.SavedLandmarks = null;
        StaticGlobals.SavedInitStateExists = false;
    }


}
