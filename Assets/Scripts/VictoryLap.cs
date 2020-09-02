using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Vehicles.Car;

public class VictoryLap : MonoBehaviour
{
    [SerializeField]
    private GameObject m_roadsideEmitterLeft = null, m_roadsideEmitterRight = null, m_nodeEmitter = null;

    [SerializeField]
    private GameObject[] m_audienceMembers = new GameObject[4];

    private GameObject m_car;
    private AudioSource m_victoryLapAudioSource;

    private float m_lastUpdateTime = 0f; // used to ensure we don't do complex calcs on every update
    private CiDyRoad m_roadLast = null;
    private int m_iOrigPointLast = -1;
    private int m_iOrigPointAheadLast = -1;
    private int m_iOrigPointAheadAudienceLast = -1;
    private CiDyNode m_nodeAheadLast = null;

    private Dictionary<CiDyRoad, List<GameObject>> m_leftEmittersDict, m_rightEmittersDict, m_leftAudienceMembersDict, m_rightAudienceMembersDict;
    private Dictionary<string, GameObject> m_nodeEmittersDict;

    // Start is called before the first frame update
    void Start()
    {
        m_leftEmittersDict = new Dictionary<CiDyRoad, List<GameObject>>();
        m_rightEmittersDict = new Dictionary<CiDyRoad, List<GameObject>>();
        m_leftAudienceMembersDict = new Dictionary<CiDyRoad, List<GameObject>>();
        m_rightAudienceMembersDict = new Dictionary<CiDyRoad, List<GameObject>>();
        m_nodeEmittersDict = new Dictionary<string, GameObject>();

        m_car = GameSystem.Instance.Car;

        GameSystem.Instance.CurDrivingMode = DrivingMode.VictoryLap;
        GameSystem.Instance.ShowInfoMessage(Strings.VictoryLapInfoMessage, 3f);

        Transform tWaypointCircuit = transform.Find(Strings.WaypointCircuit);

        // create the audience and emitters
        CreateCelebratoryElements(tWaypointCircuit);

        // place the car at the first waypoint, looking at the second
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

    // create various celebratory elements such as confetti and audience etc.
    private void CreateCelebratoryElements(Transform tWaypointCircuit)
    {
        List<CiDyRoad> roads = new List<CiDyRoad>();
        
        for (int iChild = 0; iChild < tWaypointCircuit.childCount - 1; iChild++)
        {
            Transform tWaypoint = tWaypointCircuit.GetChild(iChild);
            Transform tWaypointNext = tWaypointCircuit.GetChild(iChild + 1);
            CiDyRoad road = RoadFromPositions(tWaypoint.position, tWaypointNext.position);
            roads.Add(road);

            Transform tNode = NodeFromPosition(tWaypoint.position);
            m_nodeEmittersDict.Add(tNode.name, Instantiate(m_nodeEmitter, tNode.position, Quaternion.identity));
        }

        for (int iRoad = 0; iRoad < roads.Count; iRoad++)
        {
            CiDyRoad road = roads[iRoad];
            List<GameObject> leftEmitters = new List<GameObject>();
            List<GameObject> rightEmitters = new List<GameObject>();
            List<GameObject> leftAudienceMembers = new List<GameObject>();
            List<GameObject> rightAudienceMembers = new List<GameObject>();

            // for each origPoint
            for (int iOrigPoint = 3; iOrigPoint < road.origPoints.Length - 3; iOrigPoint++)
            {
                Vector3 origPoint = road.origPoints[iOrigPoint];

                // place emitters and audience on both sides of the road
                Vector3 leftEdge, rightEdge, roadWidthVector;
                GameObject leftEmitter, rightEmitter, leftAudienceMember, rightAudienceMember;
                
                Quaternion rotation = Quaternion.LookRotation(road.origPoints[iOrigPoint + 1] - origPoint, Vector3.up);
                roadWidthVector = rotation * Quaternion.Euler(0f, -90f, 0f) * Vector3.forward * (road.width / 2f);

                leftEdge = origPoint + roadWidthVector;
                leftEmitter = Instantiate(m_roadsideEmitterLeft, leftEdge, rotation);
                leftEmitters.Add(leftEmitter);

                rightEdge = origPoint - roadWidthVector;
                rightEmitter = Instantiate(m_roadsideEmitterRight, rightEdge, rotation);
                rightEmitters.Add(rightEmitter);

                roadWidthVector *= 1.3f;

                leftEdge = origPoint + roadWidthVector;
                leftAudienceMember = Instantiate(m_audienceMembers[UnityEngine.Random.Range(0, m_audienceMembers.Length)], leftEdge, rotation * Quaternion.Euler(0f, 90f, 0f));
                leftAudienceMembers.Add(leftAudienceMember);

                rightEdge = origPoint - roadWidthVector;
                rightAudienceMember = Instantiate(m_audienceMembers[UnityEngine.Random.Range(0, m_audienceMembers.Length)], rightEdge, rotation * Quaternion.Euler(0f, -90f, 0f));
                rightAudienceMembers.Add(rightAudienceMember);
            }

            m_leftEmittersDict.Add(road, leftEmitters);
            m_rightEmittersDict.Add(road, rightEmitters);
            m_leftAudienceMembersDict.Add(road, leftAudienceMembers);
            m_rightAudienceMembersDict.Add(road, rightAudienceMembers);
        }
    }

    private void DestroyCelebratoryElements()
    {
        DestroyDictionaryObjects(m_leftEmittersDict);
        DestroyDictionaryObjects(m_rightEmittersDict);
        DestroyDictionaryObjects(m_leftAudienceMembersDict);
        DestroyDictionaryObjects(m_rightAudienceMembersDict);
        DestroyNodeEmitters(m_nodeEmittersDict);

        m_leftEmittersDict = m_rightEmittersDict = m_leftAudienceMembersDict = m_rightAudienceMembersDict = null;
        m_nodeEmittersDict = null;
    }

    private bool DoCelebratoryElementsExist()
    {
        return m_leftEmittersDict != null;
    }

    private void DestroyDictionaryObjects(Dictionary<CiDyRoad, List<GameObject>> dict)
    {
        foreach (CiDyRoad road in dict.Keys)
        {
            foreach (GameObject go in dict[road])
            {
                Destroy(go);
            }
        }
    }

    private void DestroyNodeEmitters(Dictionary<string, GameObject> dict)
    {
        foreach (string name in dict.Keys)
        {
            Destroy(dict[name]);
        }
    }

    private Transform NodeFromPosition(Vector3 position)
    {
        foreach (Transform tNode in GameSystem.Instance.GetNodeHolder())
        {
            if (Vector3.Distance(tNode.position, position) < 0.1)
                return tNode;
        }

        return null;
    }

    private CiDyRoad RoadFromPositions(Vector3 position1, Vector3 position2)
    {
        foreach (Transform tRoad in GameSystem.Instance.GetRoadHolder())
        {
            CiDyRoad road = tRoad.GetComponent<CiDyRoad>();
            if (((Vector3.Distance(road.nodeA.position, position1) < 0.1) && (Vector3.Distance(road.nodeB.position, position2) < 0.1)) ||
                ((Vector3.Distance(road.nodeA.position, position2) < 0.1) && (Vector3.Distance(road.nodeB.position, position1) < 0.1)))
                return road;
        }

        return null;
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

        DestroyCelebratoryElements();

        PromptMessage.ShowMessage(Strings.VictoryLapEndPrompt, Strings.MoveToNextLevel, Strings.StartFreeDrive, 
            (levelUp) => { if (levelUp) GameSystem.Instance.LevelUp(); else GameSystem.Instance.StartFreeDrive(); });
    }

    private void LateUpdate()
    {
        if (Time.time - m_lastUpdateTime < 0.2f)
            return;

        m_lastUpdateTime = Time.time;

        if (!DoCelebratoryElementsExist()) // celebration over?
            return;

        // handle celebration
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;
        CiDyRoad roadAhead;
        int iOrigPointAhead;
        CiDyNode nodeAhead;

        if (!roadCur)
            return;

        // calc the node coming up ahead if we are getting close to it and place a fountain there
        nodeAhead = CalcNodeAhead();
        if ((nodeAhead != null) && (nodeAhead != m_nodeAheadLast))
        {
            // place a node emitter at the node
            NodeEmitter nodeEmitter = m_nodeEmittersDict[nodeAhead.name].GetComponent<NodeEmitter>();
            nodeEmitter.Play();
            m_nodeAheadLast = nodeAhead;
        }

        // calculate the orig point at 2 orig points ahead of the car and play the emitters and audience there
        if (CalcOrigPointAhead(2, out roadAhead, out iOrigPointAhead))
        {
            if ((iOrigPointAhead > 2) && ((roadAhead != m_roadLast) || (iOrigPointAhead != m_iOrigPointAheadLast)) && m_leftEmittersDict.ContainsKey(roadAhead))
            {
                // play emitters on both sides of the road
                GameObject leftEmitter, rightEmitter;

                leftEmitter = m_leftEmittersDict[roadAhead][iOrigPointAhead - 3];
                leftEmitter.GetComponent<RoadsideEmitter>().Play();

                rightEmitter = m_rightEmittersDict[roadAhead][iOrigPointAhead - 3];
                rightEmitter.GetComponent<RoadsideEmitter>().Play();

                GameObject leftAudienceMember, rightAudienceMember;

                leftAudienceMember = m_leftAudienceMembersDict[roadAhead][iOrigPointAhead - 3];
                leftAudienceMember.GetComponent<Audience>().Play();

                rightAudienceMember = m_rightAudienceMembersDict[roadAhead][iOrigPointAhead - 3];
                rightAudienceMember.GetComponent<Audience>().Play();

                m_iOrigPointAheadLast = iOrigPointAhead;
            }
        }

        m_roadLast = roadCur;
        m_iOrigPointLast = iOrigPointCur;
    }

    // calculate count'th orig point ahead of the current orig point
    // make sure it doesn't enter the intersection of two roads i.e. skip the end points
    // iOrigPointAhed will be negative if there is no such point.
    private bool CalcOrigPointAhead(int count, out CiDyRoad roadAhead, out int iOrigPointAhead)
    {
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;

        roadAhead = roadCur;
        iOrigPointAhead = iOrigPointCur;

        if ((m_roadLast != null) && (roadAhead == m_roadLast))
        {
            if (iOrigPointCur < m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur - count;
            }
            else if (iOrigPointCur > m_iOrigPointLast)
            {
                iOrigPointAhead = iOrigPointCur + count;
                if (iOrigPointAhead >= roadCur.origPoints.Length - 3)
                {
                    iOrigPointAhead = -1;
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
            }
            else if (iOrigPointAhead < count)
            {
                iOrigPointAhead = count;
            }
        }

        return true;
    }

    private CiDyNode CalcNodeAhead()
    {
        CiDyRoad roadCur = GameSystem.Instance.OnTrackRoad;
        int iOrigPointCur = GameSystem.Instance.OnTrackOrigPoint;

        CiDyNode nodeAhead = null;

        if ((m_roadLast != null) && (roadCur == m_roadLast))
        {
            if (iOrigPointCur < m_iOrigPointLast)
            {
                if (iOrigPointCur <= 4)
                {
                    nodeAhead = roadCur.nodeA;
                }
            }
            else if (iOrigPointCur > m_iOrigPointLast)
            {
                if (iOrigPointCur >= roadCur.origPoints.Length - 5)
                {
                    nodeAhead = roadCur.nodeB;
                }
            }
        }

        return nodeAhead;
    }
}
