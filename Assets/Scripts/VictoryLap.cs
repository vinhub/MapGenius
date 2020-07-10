using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Vehicles.Car;

public class VictoryLap : MonoBehaviour
{
    [SerializeField]
    private GameObject m_roadsideEmitterLeft, m_roadsideEmitterRight, m_nodeEmitter;

    [SerializeField]
    private GameObject[] m_audienceMembers = new GameObject[6];

    private GameObject m_car;
    private AudioSource m_victoryLapAudioSource;

    private float m_lastUpdateTime = 0f; // used to ensure we don't do complex calcs on every update
    private CiDyRoad m_roadLast = null;
    private int m_iOrigPointLast = -1;
    private int m_iOrigPointAheadLast = -1;

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
        if (Time.time - m_lastUpdateTime < 0.2f)
            return;

        m_lastUpdateTime = Time.time;

        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;
        CiDyNode nodeAhead = null;
        CiDyRoad roadAhead;
        int iOrigPointAhead;

        if (!roadCur)
            return;

        if (!m_roadLast)
        {
            m_roadLast = roadCur;
            m_iOrigPointLast = iOrigPointCur;
        }

        // update "ahead" orig point: which is a little ahead of the car
        // make sure it doesn't enter the intersection of two roads i.e. skip the end points
        if (!CalcOrigPointAhead(2, out roadAhead, out iOrigPointAhead))
            return;

        if (nodeAhead != null)
        {
            GameObject nodeEmitter = Instantiate(m_nodeEmitter, nodeAhead.position, Quaternion.identity);
            Destroy(nodeEmitter, 3f);
        }
        else if (((roadCur != m_roadLast) || (iOrigPointAhead != m_iOrigPointAheadLast)))
        {
            Vector3 origPointAhead = roadCur.origPoints[iOrigPointAhead];
            Quaternion onTrackRotation = GameSystem.Instance.OnTrackRotation;
            Vector3 leftEdge, rightEdge, roadWidthVector;
            GameObject leftEmitter, rightEmitter, audienceMember1, audienceMember2;

            roadWidthVector = onTrackRotation * Quaternion.Euler(0f, -90f, 0f) * Vector3.forward * roadCur.width / 2f;

            leftEdge = origPointAhead + roadWidthVector;
            leftEmitter = Instantiate(m_roadsideEmitterLeft, leftEdge, onTrackRotation);
            audienceMember1 = Instantiate(m_audienceMembers[0], leftEdge, onTrackRotation);
                
            rightEdge = origPointAhead - roadWidthVector;
            rightEmitter = Instantiate(m_roadsideEmitterRight, rightEdge, onTrackRotation);
            audienceMember2 = Instantiate(m_audienceMembers[1], rightEdge, onTrackRotation);

            Destroy(leftEmitter, 2f);
            Destroy(rightEmitter, 2f);
            Destroy(audienceMember1, 2f);
            Destroy(audienceMember2, 2f);
        }

        m_roadLast = roadCur;
        m_iOrigPointAheadLast = iOrigPointAhead;
        m_iOrigPointLast = iOrigPointCur;
    }

    // calculate couth'th orig point ahead of the current orig point
    private bool CalcOrigPointAhead(int count, out CiDyRoad roadAhead, out int iOrigPointAhead)
    {
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;
        CiDyNode nodeAhead = null;

        roadAhead = roadCur;
        iOrigPointAhead = iOrigPointCur;
        if (roadCur == m_roadLast)
        {
            if (iOrigPointCur < m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur - count;
                if (iOrigPointAhead <= count)
                {
                    iOrigPointAhead = -1;
                    nodeAhead = roadCur.nodeA;
                }
            }
            else if (iOrigPointCur > m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur + count;
                if (iOrigPointAhead >= roadCur.origPoints.Length - count - 1)
                {
                    iOrigPointAhead = -1;
                    nodeAhead = roadCur.nodeB;
                }
            }
            else
                return false; // if the last and current origPoints are the same, then there's nothing to do for now.
        }
        else
        {
            iOrigPointAhead = Math.Max(Math.Min(iOrigPointCur, roadCur.origPoints.Length - count - 1), count);
        }

        return true;
    }
}
