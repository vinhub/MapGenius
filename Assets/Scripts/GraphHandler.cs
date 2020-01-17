using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphHandler : MonoBehaviour
{
    // our singleton instance
    private static GraphHandler m_instance;
    public static GraphHandler Instance { get { return m_instance; } }

    private void Awake()
    {
        m_instance = this;
    }

    public void CreateGraph(CiDyGraph graph)
    {
        //StartCoroutine(CreateGraphCoroutine(graph));
    }

    private IEnumerator CreateGraphCoroutine(CiDyGraph graph)
    {
        yield return StartCoroutine(ClearGraph(graph));

        yield return StartCoroutine(_CreateGraph(graph));
    }

    private IEnumerator ClearGraph(CiDyGraph graph)
    {
        yield return graph.ClearGraph();
    }

    private IEnumerator _CreateGraph(CiDyGraph graph)
    { 
        CiDyNode node1 = graph.NewMasterNode(new Vector3(0, 0, 0), 1f);
        CiDyNode node2 = graph.NewMasterNode(new Vector3(10, 0, 0), 1f);
        CiDyNode node3 = graph.NewMasterNode(new Vector3(0, 0, 10), 1f);
        CiDyNode node4 = graph.NewMasterNode(new Vector3(10, 0, 10), 1f);

        graph.ConnectNodes(node1, node2, 12f, 6, 8);
        graph.ConnectNodes(node1, node3, 12f, 6, 8);
        graph.ConnectNodes(node2, node4, 12f, 6, 8);
        graph.ConnectNodes(node3, node4, 12f, 6, 8);

        yield break;
    }
}
