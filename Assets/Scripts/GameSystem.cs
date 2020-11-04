using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Car;

public class GameSystem : MonoBehaviour
{
    // our singleton instance
    private static GameSystem m_instance;
    public static GameSystem Instance { get { return m_instance; } }

    private const int m_numLandmarks = 5;
    private const int m_landmarksLayerIndex = 11; // TODO: remove this hardcoding
    private const int m_UILayerIndex = 5; // TODO: remove this hardcoding

    public GameObject Car; // the car being driven by the player
    public GameObject CarCameraRig; // the car camera rig
    private CarController m_carController;
    public GameObject LandmarkPrefab; // prefab for landmarks
    public GameObject PlayermarksListItemPrefab; // prefab for playermarks list item in the map panel
    public TMP_Text LicensePlateText; // user's name will be displayed here

    // car status
    private TMP_Text m_carSpeedText, m_carRevsText;
    private float m_carStuckTime = 0f;
    private int m_carMoveAttempts = 0;

    // info message
    private GameObject m_infoMessage;
    private TMP_Text m_infoMessageText;
    private AudioSource m_infoMessageAudioSource;

    private Transform m_tRoadHolder, m_tNodeHolder, m_tLandmarks;
    private GameObject m_graph;

    // for pausing / resuming game
    private float m_timeScaleSav = 1f;
    private bool m_isGamePaused;
    private bool m_isGameQuitting = false;

    private MainMenuButton m_mainMenuButton;
    private PanelManager m_mainPanelManager;
    private Transform m_tPlayermarksList;
    private bool m_firstLandmarkCrossed = true; // true only until the first landmark is crossed (used for showing first marker)

    // current car location
    public Quaternion OnTrackRotation { get; private set; } = Quaternion.identity;
    public CiDyRoad OnTrackRoad { get; private set; } = null;
    public int OnTrackOrigPoint { get; private set; } = 0;
    public int OnTrackOrigPointAhead { get; private set; } = 0;
    private Vector3 m_closestPointOnTrack;

    private float m_lastUpdateTime = 0f; // used to ensure we don't do complex calcs on every update

    public GameObject VictoryLap;

    private void Awake()
    {
        m_instance = this;

        DOTween.useSmoothDeltaTime = true;

        // read playerprefs
        PlayerState.InitPlayerName();
        PlayerState.InitPlayerGameLevel();
        PlayerState.InitPlayerTotalScore();
    }

    private void Start()
    {
        m_carController = Car.GetComponent<CarController>();

        Transform tMainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag).transform;
        m_mainPanelManager = tMainMenuUI.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();
        m_tPlayermarksList = tMainMenuUI.Find(Strings.MapPanelPath).Find(Strings.PlayermarksPath);
        m_mainMenuButton = tMainMenuUI.Find(Strings.OpenMenuButtonPath).GetComponent<MainMenuButton>();

        m_carSpeedText = tMainMenuUI.Find(Strings.StatusBarSpeedTextPath).GetComponent<TMP_Text>();
        m_carRevsText = tMainMenuUI.Find(Strings.StatusBarRevsTextPath).GetComponent<TMP_Text>();

        Transform tInfoMessage = tMainMenuUI.Find(Strings.InfoMessagePath);
        m_infoMessage = tInfoMessage.gameObject;
        m_infoMessageText = tInfoMessage.Find(Strings.InfoMessageTextPath).GetComponent<TMP_Text>();
        m_infoMessageAudioSource = m_infoMessageText.GetComponent<AudioSource>();

        GameState.CurDrivingMode = DrivingMode.Normal;

        InitGame();
    }

    private void InitGame()
    {
        SetLicensePlate();

        // show game instructions at the start of the game unless user has asked to hide them
        if (GameState.IsGameStarting && (PlayerPrefs.GetInt(Strings.HideInstructionsAtStart, 0) == 0))
        {
            m_mainPanelManager.OpenInstructionsPanel();
        }
        else
        {
            PopupMessage.ShowMessage(PopupMessageType.LevelStarting, String.Format(Strings.LevelStartingMessageFormat, LevelInfo.getLevelInfo(PlayerState.PlayerGameLevel).getName()), 1f);
        }

        m_graph = GameObject.Find(Strings.GraphPath);

        // now that we have a graph, we can gather some frequently needed references
        m_tNodeHolder = m_graph.transform.Find(Strings.NodeHolderPath).transform;
        m_tRoadHolder = m_graph.transform.Find(Strings.RoadHolderPath).transform;
        m_tLandmarks = GameObject.Find(Strings.LandmarksPath).transform;

        if (GameState.RetryingGame)
        {
            // use the init game state we had remembered
            LoadInitGameState();

            GameState.RetryingGame = false;
        }
        else
        {
            // init landmarks, car initial pos etc.
            InitGameState();

            // remember them so they can be reused in case user wants to retry
            RememberInitGameState();
        }

        // place car some distance from the first landmark
        Car.transform.position = CarCameraRig.transform.position = OnTrackRoad.origPoints[OnTrackOrigPoint];
        Car.transform.rotation = CarCameraRig.transform.rotation = OnTrackRotation;
    }

    public void SetLicensePlate()
    {
        LicensePlateText.text = String.IsNullOrWhiteSpace(PlayerState.PlayerName) ? Strings.GameName : PlayerState.PlayerName;
    }

    private void InitGameState()
    {
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
            landmarkPos.y -= 0.7f; // lower it slightly so the landmark does not appear to be floating on sloping roads
            Vector3 nextPos = road.origPoints[road.origPoints.Length / 2 + 1];
            nextPos.y = landmarkPos.y; // make the direction horizontal so the landmark stands up vertical
            Quaternion rotation = Quaternion.LookRotation(landmarkPos - nextPos, Vector3.up);

            // add a landmark to the area using the landmark prefab and place it at the above location
            CreateLandmark(iLandmark, landmarkPos, rotation);

            // use the start landmark to calculate starting car position
            if (iLandmark == iLandmarkStart)
            {
                CalcStartingCarLocation(road);
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
        goPlayermarkListItem.transform.Find(Strings.PlayermarkIndexEmptyPath).GetComponent<TMP_Text>().text = (iLandmark + 1).ToString();
        goPlayermarkListItem.transform.Find(Strings.PlayermarkIndexPath).GetComponent<TMP_Text>().text = (iLandmark + 1).ToString();
        goPlayermarkListItem.transform.Find(Strings.PlayermarkTextPath).GetComponent<TMP_Text>().text = landmarkName;
    }

    private void CalcStartingCarLocation(CiDyRoad road)
    {
        // use a position about 1/3 of the way along the road as the car position, looking along the road
        OnTrackRoad = road;
        OnTrackOrigPoint = road.origPoints.Length / 4;
        Debug.Assert(road.origPoints.Length >= 4);
        OnTrackRotation = Quaternion.LookRotation(road.origPoints[OnTrackOrigPoint + 1] - road.origPoints[OnTrackOrigPoint], Vector3.up);
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
        m_isGameQuitting = true;
        m_instance = null;
    }

    private void Update()
    {
        HandleHotkeys();
    }

    private void LateUpdate()
    {
        if (Time.time - m_lastUpdateTime < 0.2)
            return;

        m_lastUpdateTime = Time.time;

        bool fCarIsOnTrack = UpdateCarStatus();

        // detect if car is stuck and show option to get it unstuck
        if (IsCarStuck())
            ShowInfoMessage(Strings.GetBackOnTrackMessage, 3f);
    }

    private void HandleHotkeys()
    {
        bool fShowMap = Input.GetKeyUp(KeyCode.M);
        bool fFreeDrive = Input.GetKeyUp(KeyCode.F);
        bool fEscape = Input.GetKeyUp(KeyCode.Escape);
        bool fBackOnTrack = false;
        bool fRunTests = false;

        if (Input.GetKeyUp(KeyCode.T))
        {
            if (Debug.isDebugBuild && Input.GetKey(KeyCode.LeftControl))
                fRunTests = true;
            else
                fBackOnTrack = true;
        }

        if (fBackOnTrack && !IsGamePaused())
            GetBackOnTrack();
        else if (fShowMap && !IsGamePaused())
            m_mainPanelManager.OpenMapPanel();
        else if (fFreeDrive)
            StartFreeDrive();
        else if (fEscape)
        {
            if (m_mainPanelManager.IsPanelOpen())
                m_mainPanelManager.ClosePanelAndContinue();
            else
                m_mainMenuButton.Toggle();
        }
        else if (fRunTests)
        {
            Resources.FindObjectsOfTypeAll<Test>()[0].gameObject.SetActive(true);
        }
    }

    private void OnGui()
    {
        // common GUI code goes here
    }

    public void NewGame()
    {
        ContinueGame(false);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LevelUp()
    {
        switch (PlayerState.PlayerGameLevel)
        {
            case GameLevel.Downtown:
                // enable Smalltown level
                LevelInfo levelInfo = LevelInfo.getLevelInfo(GameLevel.Smalltown);
                levelInfo.IsEnabled = true;

                GoToLevel(GameLevel.Smalltown);
                break;

            case GameLevel.Smalltown:
                GoToLevel(GameLevel.Oldtown);
                break;

            case GameLevel.Oldtown:
                GoToLevel(GameLevel.FutureTown);
                break;

            case GameLevel.FutureTown:
                GameOver();
                break;
        }
    }

    public void GoToLevel(GameLevel gameLevel)
    {
        switch (gameLevel)
        {
            case GameLevel.Downtown:
                PlayerState.SetPlayerGameLevel(GameLevel.Downtown);
                break;

            case GameLevel.Smalltown:
                PlayerState.SetPlayerGameLevel(GameLevel.Smalltown);
                break;

            case GameLevel.Oldtown:
                // TODO: Implement
                GameOver();
                return;

            case GameLevel.FutureTown:
                // TODO: Implement
                GameOver();
                return;
        }

        ContinueGame(false);

        LevelInfo levelInfo = LevelInfo.getLevelInfo(gameLevel);

        SceneManager.LoadScene(levelInfo.getName());
    }

    // retry the same level without changing anything
    public void RetryGame()
    {
        ContinueGame(false);

        GameState.RetryingGame = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PauseGame()
    {
        if (m_isGamePaused)
            return;

        m_isGamePaused = true;

        m_carController.StopCar();

        PauseAllAudio();

        m_timeScaleSav = Time.timeScale;

        Time.timeScale = 0.0005f; // we want to set it to 0, but doing so causes a jerk.

        // DOTween.To(() => { return Time.timeScale; }, (s) => { Time.timeScale = s; }, 0f, 0.2f);
    }

    public void ContinueGame(bool fVictoryLap)
    {
        if (!m_isGamePaused)
            return;

        if (fVictoryLap)
        {
            StartCoroutine(DoVictoryLap());
            return;
        }

        Time.timeScale = m_timeScaleSav;

        m_carController.StopCar();

        ResumePausedAudio();

        m_isGamePaused = false;
    }

    public bool IsGamePaused()
    {
        return m_isGamePaused;
    }

    public bool IsGameQuitting()
    {
        return m_isGameQuitting;
    }

    public void QuitGame()
    {
        //Debug.Log("Player quit the game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
    }

    public void GameOver()
    {
        m_carController.Move(0, 0, -1, 1);
        m_carController.StopCar();
        PopupMessage.ShowMessage(PopupMessageType.GameOver, Strings.GameOverMessage);
        StartCoroutine(GameOverAfterDelay());
    }

    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(5f);

        PopupMessage.HideMessage();
        QuitGame();
    }

    private List<AudioSource> m_pausedAudioSources = new List<AudioSource>(); 
    private void PauseAllAudio()
    {
         AudioSource[] allAudioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
        foreach (AudioSource audio in allAudioSources)
        {
            if (!audio.isPlaying)
                continue;

            audio.Pause();
            m_pausedAudioSources.Add(audio);
        }
    }


    private void ResumePausedAudio()
    {
        foreach (AudioSource audio in m_pausedAudioSources)
        {
            if (audio != null)
                audio.Play();
        }

        m_pausedAudioSources.Clear();
    }


    // called when the player crosses a landmark
    public void LandmarkCrossed(string landmarkName)
    {
        if ((GameState.CurDrivingMode != DrivingMode.Normal) || !String.IsNullOrEmpty(m_mainPanelManager.CurLandmarkName)) // in free drive mode or victory lap mode or already processing a landmark?
            return;

        PauseGame();

        StartCoroutine(HandleLandmarkCrossed(landmarkName));
    }

    private IEnumerator HandleLandmarkCrossed(string landmarkName)
    {
        // flash a popup letting the player know they crossed the landmark
        PopupMessageType type = m_firstLandmarkCrossed ? PopupMessageType.ShowHowToMark : PopupMessageType.NormalLandmarkCrossed;
        string message = m_firstLandmarkCrossed ? String.Format(Strings.ShowHowToMarkMessageFormat, landmarkName) : String.Format(Strings.LandmarkCrossedMessageFormat, landmarkName);

        PopupMessage.ShowMessage(type, message);

        // let it stay for some time
        yield return new WaitForSecondsRealtime(m_firstLandmarkCrossed ? 5f : 4f);

        PopupMessage.HideMessage();

        // then show map panel
        m_mainPanelManager.OpenMapPanel(landmarkName, m_firstLandmarkCrossed);

        m_firstLandmarkCrossed = false;
    }

    private void RememberInitGameState()
    {
        if (GameState.InitStateExists)
            ClearGameState();

        GameState.InitLandmarks = new List<LandmarkParams>();

        foreach(Transform tLandmark in m_tLandmarks)
        {
            GameObject goLandmark = tLandmark.gameObject;

            LandmarkParams sl = new LandmarkParams();
            sl.name = goLandmark.name;
            sl.pos = tLandmark.position;
            sl.rotation = tLandmark.rotation;

            GameState.InitLandmarks.Add(sl);
        }

        GameState.InitRoadOnTrack = OnTrackRoad;
        GameState.InitOrigPointIndexOnTrack = OnTrackOrigPoint;
        GameState.InitRotationOnTrack = OnTrackRotation;
    }

    private void LoadInitGameState()
    {
        if (!GameState.InitStateExists)
            return;

        for (int iLandmark = 0; iLandmark < GameState.InitLandmarks.Count; iLandmark++)
        {
            CreateLandmark(iLandmark, GameState.InitLandmarks[iLandmark].pos, GameState.InitLandmarks[iLandmark].rotation);
        }

        OnTrackRoad = GameState.InitRoadOnTrack;
        OnTrackOrigPoint = GameState.InitOrigPointIndexOnTrack;
        OnTrackRotation = GameState.InitRotationOnTrack;
    }

    private void ClearGameState()
    {
        GameState.InitLandmarks.Clear();
        GameState.InitLandmarks = null;

        GameState.InitRoadOnTrack = null;
        GameState.InitOrigPointIndexOnTrack = -1;
        GameState.InitRotationOnTrack = Quaternion.identity;
    }

    private bool UpdateCarStatus()
    {
        m_carSpeedText.text = String.Format(Strings.CarSpeedStatusFormat, m_carController.CurrentSpeed);
        m_carRevsText.text = String.Format(Strings.CarRevsStatusFormat, Mathf.RoundToInt(m_carController.Revs * 1000f));

        // determine road and origPoint closest to the car
        return UpdateOnTrackInfo();
    }

    // we need to remember the origPoint closest to the car at all times. That way, when we go offroad and car gets stuck, the player can hit
    // the GetUnstuck hotkey and we can bring them back to the last origPoint they were at before they left the road.
    // returns true iff car is still on track
    private bool UpdateOnTrackInfo()
    {
        if (OnTrackRoad == null)
            return true;

        // If the car is off track, we should not update the roadCur and iOrigPointCur.
        // This is an optimization as well as because when the player wants to bring the car back on track, we want to bring them to where they left the road.
        // We don't want to bring them to the closest origPoint at that time (which could be in unfamiliar territory because they may have wandered off quite a bit.)
        if (IsCarOffTrack())
            return false;

        // check distance from the current origPoint
        CiDyRoad roadNext;
        int iOrigPointNext;
        float dist = CarDistanceFromOrigPoint(OnTrackRoad, OnTrackOrigPoint);

        float distNext = dist;
        roadNext = OnTrackRoad;
        iOrigPointNext = OnTrackOrigPoint;

        // do
        // check distance from the next origPoint
        // if we have run out of origPoints on this road, go to the first origPoint on the adjacent road.
        // while (distance from next origPoint is smaller than current origPoint)
        // go in the other direction and do the same.
        // if both distances are bigger than original then that means car has gone off road and m_roadCur and m_iOrigPoint remain unchanged
        // otherwise, the road and the origPoint get updated
        do
        {
            dist = distNext;
            OnTrackRoad = roadNext;
            OnTrackOrigPoint = iOrigPointNext;
            OnTrackRotation = Car.transform.rotation;

            ++iOrigPointNext;

            if (iOrigPointNext >= roadNext.origPoints.Length)
            {
                // ran out of the road, find next road
                CalcNextRoad(dist, roadNext.nodeB, ref roadNext, ref iOrigPointNext);
                if (iOrigPointNext < roadNext.origPoints.Length) // found next road
                {
                    OnTrackRoad = roadNext;
                    OnTrackOrigPoint = iOrigPointNext;
                }

                break;
            }

            distNext = CarDistanceFromOrigPoint(roadNext, iOrigPointNext);
        } while (distNext < dist);

        // go in reverse direction and do the same thing
        distNext = dist;
        roadNext = OnTrackRoad;
        iOrigPointNext = OnTrackOrigPoint;

        do
        {
            dist = distNext;
            OnTrackRoad = roadNext;
            OnTrackOrigPoint = iOrigPointNext;
            OnTrackRotation = Car.transform.rotation;

            --iOrigPointNext;

            if (iOrigPointNext < 0)
            {
                // ran out of the road, find prev road
                CalcNextRoad(dist, roadNext.nodeA, ref roadNext, ref iOrigPointNext);
                if (iOrigPointNext >= 0) // found prev road
                {
                    OnTrackRoad = roadNext;
                    OnTrackOrigPoint = iOrigPointNext;
                }

                break;
            }

            distNext = CarDistanceFromOrigPoint(roadNext, iOrigPointNext);
        } while (distNext < dist);

        // update closest point on track
        Collider roadCollider = OnTrackRoad.GetComponent<Collider>();
        m_closestPointOnTrack = roadCollider.ClosestPointOnBounds(Car.transform.position);

        //ShowDebugInfo("road: " + m_roadOnTrack.name + ", orig: " + m_iOrigPointOnTrack + ", dist: " + dist.ToString("F3"));

        return true;
    }

    private void CalcNextRoad(float dist, CiDyNode node, ref CiDyRoad roadNext, ref int iOrigPointNext)
    {
        float distNext;
        string roadInName = roadNext.name;

        foreach (CiDyRoad road in node.connectedRoads)
        {
            if ((road == null) || (road.origPoints == null) || (road.origPoints.Length < 3))
                continue;

            if (road.name == roadInName)
                continue;

            float dist1 = CarDistanceFromOrigPoint(road, 1);
            float dist2 = CarDistanceFromOrigPoint(road, road.origPoints.Length - 2);
            int iOrigPoint;
            if (dist1 < dist2)
            {
                distNext = dist1;
                iOrigPoint = 1;
            }
            else
            {
                distNext = dist2;
                iOrigPoint = road.origPoints.Length - 2;
            }

            if (distNext <= dist)
            {
                roadNext = road;
                iOrigPointNext = iOrigPoint;
                dist = distNext;
                continue;
            }
        }
    }

    private float CarDistanceFromOrigPoint(CiDyRoad road, int iOrigPoint)
    {
        Debug.Assert(road != null);
        return Vector3.Distance(Car.transform.position, road.origPoints[iOrigPoint]);
    }

    private bool IsCarOffTrack()
    {
        Vector3 carPosition = Car.transform.position;
        Collider roadCollider = OnTrackRoad.GetComponent<Collider>();
        m_closestPointOnTrack = roadCollider.ClosestPointOnBounds(carPosition);

        if (Vector3.Distance(m_closestPointOnTrack, carPosition) < OnTrackRoad.width / 2f) // this means it is inside the collider or close to it, so we'll say it's on track
            return false;

        // check if it is on a different road
        foreach (Transform tRoad in m_tRoadHolder)
        {
            if (tRoad == OnTrackRoad.transform)
                continue;

            roadCollider = tRoad.GetComponent<Collider>();
            m_closestPointOnTrack = roadCollider.ClosestPointOnBounds(carPosition);

            if (Vector3.Distance(m_closestPointOnTrack, carPosition) < OnTrackRoad.width / 2f) // this means it is inside the collider or close to it, so it is on or close to this road
            {
                OnTrackRoad = tRoad.GetComponent<CiDyRoad>();
                OnTrackOrigPoint = OnTrackRoad.origPoints.Length / 2; // just place it at half the length for now, it will get adjusted to the closest point
                return false;
            }
        }

        return true;
    }

    private bool IsCarStuck()
    {
        // if car isn't moving, check if it may be stuck.
        if ((m_carController.CurrentSpeed < 0.1f) && (m_carController.AccelInput > 0f))
        {
            if (m_carStuckTime < 0f)
                m_carStuckTime = Time.time;

            m_carMoveAttempts++;

            // If player has made many attempts to get unstuck and it's stuck for a while then report it as stuck.
            return (m_carMoveAttempts > 25) && (Time.time - m_carStuckTime >= 0.1f);
        }
        else
        {
            m_carStuckTime = -1f;
            m_carMoveAttempts = 0;
        }

        return false;
    }

    public void GetBackOnTrack()
    {
        m_carController.StopCar();
        m_carController.transform.position = OnTrackRoad.origPoints[OnTrackOrigPoint];
        m_carController.transform.rotation = OnTrackRotation;
    }

    // allow player to freely drive without worrying about landmarks etc.
    public void StartFreeDrive()
    {
        GameState.CurDrivingMode = DrivingMode.Free;
        ShowInfoMessage(Strings.FreeDriveMessage, 3f);
        ContinueGame(false);
    }

    public IEnumerator DoVictoryLap()
    {
        Debug.Assert(m_isGamePaused);

        m_carController.Move(0, 0, -1, 1);
        m_carController.StopCar();

        PopupMessage.ShowMessage(PopupMessageType.VictoryLapStarting, Strings.VictoryLapStartingMessage);

        yield return new WaitForSecondsRealtime(6f);

        PopupMessage.HideMessage();

        VictoryLap.SetActive(true);

        ContinueGame(false);
    }

    public void ShowInfoMessage(string message, float duration)
    {
        if (!m_infoMessage || m_infoMessage.activeSelf)
            return;

        m_infoMessage.SetActive(true);

        m_infoMessageAudioSource.Play();

        m_infoMessageText.text = message;
        StartCoroutine(HideInfoMessage(duration));
    }

    public IEnumerator HideInfoMessage(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        m_infoMessageText.text = null;
        m_infoMessage.SetActive(false);
    }

    private void ShowDebugInfo(string info)
    {
        Debug.Log(info);
        ShowInfoMessage(info, 3f);
    }

    public Transform GetNodeHolder()
    {
        return m_tNodeHolder;
    }

    public Transform GetRoadHolder()
    {
        return m_tRoadHolder;
    }
}