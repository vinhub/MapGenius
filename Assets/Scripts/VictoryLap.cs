using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Vehicles.Car;

public class VictoryLap : MonoBehaviour
{
    [SerializeField]
    private GameObject m_roadsideEmitterLeft, m_roadsideEmitterRight;

    private GameObject m_car;
    private AudioSource m_victoryLapAudioSource;

    private float m_lastUpdateTime = 0f; // used to ensure we don't do complex calcs on every update
    private Vector3 m_lastOrigPoint = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        m_car = GameSystem.Instance.Car;

        GameSystem.Instance.CurDrivingMode = DrivingMode.VictoryLap;
        GameSystem.Instance.ShowInfoMessage(Strings.VictoryLapInfoMessage, 3f);

        // place the car at the first waypoint, looking at the second
        Transform tWaypointCircuit = transform.Find(Strings.WaypointCircuit);
        Vector3 position = tWaypointCircuit.GetChild(0).position;
        Quaternion rotation = Quaternion.LookRotation(tWaypointCircuit.GetChild(1).position - tWaypointCircuit.GetChild(0).position, Vector3.up);

        m_car.transform.position = GameSystem.Instance.CarCameraRig.transform.position = position;
        m_car.transform.rotation = GameSystem.Instance.CarCameraRig.transform.rotation = rotation;

        m_victoryLapAudioSource = GetComponent<AudioSource>();

        // play victory music
        m_victoryLapAudioSource.Play();

        // disable manual driving, enable victory lap scripts to start car
        m_car.GetComponent<CarUserControl>().enabled = false;
        m_car.GetComponent<WaypointProgressTracker>().enabled = true;
        m_car.GetComponent<CarAIControl>().enabled = true;

        StartCoroutine(TerminateVictoryLap(m_victoryLapAudioSource.clip.length));
    }

    private IEnumerator TerminateVictoryLap(float duration)
    {
        yield return new WaitForSeconds(duration);

        // disable auto driving, enable manual driving
        m_car.GetComponent<CarController>().StopCar();
        GameSystem.Instance.PauseGame();
        m_car.GetComponent<CarAIControl>().enabled = false;
        m_car.GetComponent<WaypointProgressTracker>().enabled = false;
        m_car.GetComponent<CarUserControl>().enabled = true;

        PromptMessage.ShowMessage(Strings.VictoryLapEndPrompt, Strings.MoveToNextLevel, Strings.StartFreeDrive, 
            (levelUp) => { if (levelUp) GameSystem.Instance.LevelUp(); else GameSystem.Instance.StartFreeDrive(); });
    }

    private void LateUpdate()
    {
        if (Time.time - m_lastUpdateTime < 0.2)
            return;

        m_lastUpdateTime = Time.time;

        if (GameSystem.Instance.OnTrackRoad)
        {
            CiDyRoad onTrackRoad = GameSystem.Instance.OnTrackRoad;
            Vector3 origPoint = onTrackRoad.origPoints[GameSystem.Instance.OnTrackOrigPoint];

            if (origPoint != m_lastOrigPoint)
            {
                Quaternion onTrackRotation = GameSystem.Instance.OnTrackRotation;
                Vector3 leftEdge, rightEdge, roadWidthVector;
                GameObject leftEmitter, rightEmitter;

                roadWidthVector = onTrackRotation * Quaternion.Euler(0, -90, 0) * Vector3.forward * onTrackRoad.width / 2;

                leftEdge = origPoint + roadWidthVector;
                leftEmitter = Instantiate(m_roadsideEmitterLeft, leftEdge, onTrackRotation * Quaternion.Euler(-45, 0, 0));
                
                rightEdge = origPoint - roadWidthVector;
                rightEmitter = Instantiate(m_roadsideEmitterRight, rightEdge, onTrackRotation * Quaternion.Euler(-45, 0, 0));

                m_lastOrigPoint = origPoint;
            }

        }
    }
}
