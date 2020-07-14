﻿using System;
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
    private GameObject[] m_audienceMembers = new GameObject[4];

    private GameObject m_car;
    private AudioSource m_victoryLapAudioSource;

    private float m_lastUpdateTime = 0f; // used to ensure we don't do complex calcs on every update
    private CiDyRoad m_roadLast = null;
    private int m_iOrigPointLast = -1;
    private int m_iOrigPointAheadEmitterLast = -1;
    private int m_iOrigPointAheadAudienceLast = -1;
    private CiDyNode m_nodeAheadLast = null;

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
        CiDyRoad roadAheadEmitter, roadAheadAudience;
        int iOrigPointAheadEmitter, iOrigPointAheadAudience;
        CiDyNode nodeAhead;
        Quaternion rotationAheadEmitter, rotationAheadAudience;

        if (!roadCur)
            return;

        if (!m_roadLast)
        {
            m_roadLast = roadCur;
            m_iOrigPointLast = iOrigPointCur;
        }

        // calc the node coming up ahead if we are getting close to it and place a fountain there
        nodeAhead = CalcNodeAhead();
        if ((nodeAhead != null) && (nodeAhead != m_nodeAheadLast))
        {
            // place a node emitter at the node
            GameObject nodeEmitter = Instantiate(m_nodeEmitter, nodeAhead.position, Quaternion.identity);
            Destroy(nodeEmitter, 3f);
            m_nodeAheadLast = nodeAhead;
        }

        // calculate the orig point at 2 orig points ahead of the car and place the confetti there
        if (CalcOrigPointAhead(2, out roadAheadEmitter, out iOrigPointAheadEmitter, out rotationAheadEmitter))
        {
            if ((iOrigPointAheadEmitter >= 0) && ((roadAheadEmitter != m_roadLast) || (iOrigPointAheadEmitter != m_iOrigPointAheadEmitterLast)))
            {
                // place emitters on both sides of the road
                Vector3 origPointAhead = roadAheadEmitter.origPoints[iOrigPointAheadEmitter];
                Vector3 leftEdge, rightEdge, roadWidthVector;
                GameObject leftEmitter, rightEmitter;

                roadWidthVector = rotationAheadEmitter * Quaternion.Euler(0f, -90f, 0f) * Vector3.forward * roadAheadEmitter.width / 2f;

                leftEdge = origPointAhead + roadWidthVector;
                leftEmitter = Instantiate(m_roadsideEmitterLeft, leftEdge, rotationAheadEmitter);

                rightEdge = origPointAhead - roadWidthVector;
                rightEmitter = Instantiate(m_roadsideEmitterRight, rightEdge, rotationAheadEmitter);

                Destroy(leftEmitter, 2f);
                Destroy(rightEmitter, 2f);

                m_iOrigPointAheadEmitterLast = iOrigPointAheadEmitter;
            }
        }

        // calculate the orig point at 3 orig points ahead of the car and place the audience there
        if (CalcOrigPointAhead(5, out roadAheadAudience, out iOrigPointAheadAudience, out rotationAheadAudience))
        {
            if ((iOrigPointAheadAudience >= 0) && ((roadAheadAudience != m_roadLast) || (iOrigPointAheadAudience != m_iOrigPointAheadAudienceLast)))
            {
                // place audience on both sides of the road
                Vector3 origPointAhead = roadAheadAudience.origPoints[iOrigPointAheadAudience];
                Vector3 leftEdge, rightEdge, roadWidthVector;
                GameObject audienceMemberLeft, audienceMemberRight;

                roadWidthVector = rotationAheadAudience * Quaternion.Euler(0f, -90f, 0f) * Vector3.forward * (1.3f + roadAheadAudience.width / 2f);

                leftEdge = origPointAhead + roadWidthVector;
                audienceMemberLeft = Instantiate(m_audienceMembers[UnityEngine.Random.Range(0, m_audienceMembers.Length)], leftEdge, rotationAheadAudience * Quaternion.Euler(0f, 90f, 0f));

                rightEdge = origPointAhead - roadWidthVector;
                audienceMemberRight = Instantiate(m_audienceMembers[UnityEngine.Random.Range(0, m_audienceMembers.Length)], rightEdge, rotationAheadAudience * Quaternion.Euler(0f, -90f, 0f));

                Destroy(audienceMemberLeft, 5f);
                Destroy(audienceMemberRight, 5f);

                m_iOrigPointAheadAudienceLast = iOrigPointAheadAudience;
            }
        }

        m_roadLast = roadCur;
        m_iOrigPointLast = iOrigPointCur;
    }

    // calculate couth'th orig point ahead of the current orig point
    // make sure it doesn't enter the intersection of two roads i.e. skip the end points
    // iOrigPointAhed will be negative if there is no such point.
    private bool CalcOrigPointAhead(int count, out CiDyRoad roadAhead, out int iOrigPointAhead, out Quaternion rotationAhead)
    {
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;
        int iOrigPointAheadNext;

        roadAhead = roadCur;
        iOrigPointAhead = iOrigPointCur;
        rotationAhead = Quaternion.identity;

        if (roadAhead == m_roadLast)
        {
            if (iOrigPointCur < m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur - count;
                iOrigPointAheadNext = iOrigPointAhead - 1;
            }
            else if (iOrigPointCur > m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur + count;
                iOrigPointAheadNext = iOrigPointAhead + 1;
                if (iOrigPointAhead >= roadCur.origPoints.Length - 3)
                {
                    iOrigPointAhead = iOrigPointAheadNext = -1;
                }
            }
            else
                return false; // if the last and current origPoints are the same, then there's nothing to do for now.
        }
        else
        {
            if (iOrigPointAhead > roadAhead.origPoints.Length - count - 1)
            {
                iOrigPointAhead = roadAhead.origPoints.Length - count - 1;
                iOrigPointAheadNext = iOrigPointAhead - 1;
            }
            else if (iOrigPointAhead < count)
            {
                iOrigPointAhead = count;
                iOrigPointAheadNext = iOrigPointAhead + 1;
            }
            else
                iOrigPointAheadNext = iOrigPointAhead + 1;
        }

        if ((iOrigPointAhead >= 0) && (iOrigPointAheadNext >= 0))
            rotationAhead = Quaternion.LookRotation(roadAhead.origPoints[iOrigPointAheadNext] - roadAhead.origPoints[iOrigPointAhead], Vector3.up);

        return true;
    }

    private CiDyNode CalcNodeAhead()
    {
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;

        CiDyNode nodeAhead = null;

        if (roadCur == m_roadLast)
        {
            if (iOrigPointCur < m_iOrigPointLast)
            {
                if (iOrigPointCur <= 3)
                {
                    nodeAhead = roadCur.nodeA;
                }
            }
            else if (iOrigPointCur > m_iOrigPointLast)
            {
                if (iOrigPointCur >= roadCur.origPoints.Length - 4)
                {
                    nodeAhead = roadCur.nodeB;
                }
            }
        }

        return nodeAhead;
    }
}
