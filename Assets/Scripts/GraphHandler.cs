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

    public IEnumerator CreateGraph(CiDyGraph graph)
    {
        yield return graph.ClearGraph();

        // load previously created terrain

        // create graph
        yield return CreateRoadNetwork(graph);

        // adjust graph for terrain
        
    }

    private IEnumerator CreateRoadNetwork(CiDyGraph graph)
    { 
        CiDyNode node1 = graph.NewMasterNode(new Vector3(0, 0, 0), 1f);
        CiDyNode node2 = graph.NewMasterNode(new Vector3(10, 0, 0), 1f);
        CiDyNode node3 = graph.NewMasterNode(new Vector3(0, 0, 10), 1f);
        CiDyNode node4 = graph.NewMasterNode(new Vector3(10, 0, 10), 1f);

        graph.ConnectNodes(node1, node2, 12f, 6, 8);
        graph.ConnectNodes(node1, node3, 12f, 6, 8);
        graph.ConnectNodes(node2, node4, 12f, 6, 8);
        graph.ConnectNodes(node3, node4, 12f, 6, 8);

        yield return null;
    }
}
