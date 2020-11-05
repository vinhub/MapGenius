using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LateStart(1f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetGame()
    {
        // get into known clean state
        PlayerPrefs.SetInt(Strings.HideInstructionsAtStart, 0);
        PlayerState.SetPlayerGameLevel(null);
        PlayerState.SetPlayerDrivingMode(null);
        PlayerState.SetPlayerName(null);
        PlayerState.SetPlayerTotalScore(0);

        SceneManager.LoadScene(PlayerState.PlayerGameLevel.ToString());
    }

    IEnumerator LateStart(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        RunTests();
    }

    private void RunTests()
    {
        //RunPopupMessageTest();

        //RunInfoMessageTest();

        StartCoroutine(RunMapPanelTest());

        //RunVictoryLapTest();

        //RunLevelUpTest();
    }

    private void RunPopupMessageTest()
    {
        PopupMessage.ShowMessage(PopupMessageType.ShowHowToMark, string.Format(Strings.LandmarkCrossedMessageFormat, "Post Office"));
    }

    private void RunInfoMessageTest()
    {
        GameSystem.Instance.ShowInfoMessage("Test message", 3f);
    }

    private IEnumerator RunMapPanelTest()
    {
        Transform tMainMenuUI = GameObject.FindWithTag(Strings.MainMenuUITag).transform;
        PanelManager mainPanelManager = tMainMenuUI.Find(Strings.PanelManagerPath).GetComponent<PanelManager>();
        GameObject[] goLandmarks = GameObject.FindGameObjectsWithTag(Strings.LandmarkTag);

        for (int iLandmark = 0; iLandmark < goLandmarks.Length; iLandmark++)
        {
            goLandmarks[iLandmark].GetComponent<LandmarkHandler>().IsVisited = true;

            mainPanelManager.OpenMapPanel(goLandmarks[iLandmark].name, true);

            yield return new WaitForSecondsRealtime(2f);

            if (iLandmark < goLandmarks.Length - 1)
            {
                mainPanelManager.ClosePanelAndContinue();

                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }

    private void RunVictoryLapTest()
    {
        GameSystem.Instance.PauseGame();
        GameSystem.Instance.ContinueGame(true);
    }

    private void RunLevelUpTest()
    {
        GameSystem.Instance.LevelUp();
    }
}
