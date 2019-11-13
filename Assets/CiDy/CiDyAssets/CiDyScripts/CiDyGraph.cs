using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using UnityEditor;
/*using BuildR2;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;*/


[System.Serializable]
//This is Required in the Scene Space for Graph Creation and Manipulation
public class CiDyGraph : MonoBehaviour {

    //Static Instance Reference
    public static CiDyGraph instance;
    //private CiDyPopulationManager populationInstance;
    //Tagging system
    public string terrainTag = "Terrain";
    public string cellTag = "Cell";
    public string roadTag = "Road";
    public string nodeTag = "Node";
    //LayerMasks
    public LayerMask roadMask;
    public LayerMask roadMask2;
    public LayerMask cellMask;

    //float heightSpacing = 0.1f;
    public bool drawCycles = false;
    public bool drawEdges = false;
    public bool drawFilaments = false;
    public GameObject nodePrefab;
    //Designer
    //CiDyDesigner designer;
    [HideInInspector]
    public List<CiDyEdge> graphEdges = new List<CiDyEdge>(0);
    //Corrosponding roads
    public List<GameObject> roads = new List<GameObject>(0);
    //Corrosponding User Defined Road in Process.
    [HideInInspector]
    public List<Vector3> userDefinedRoadPnts = new List<Vector3>(0);
    [HideInInspector]
    public List<Vector3> userDefinedRoad = new List<Vector3>(0);

    List<List<CiDyNode>> cycles = new List<List<CiDyNode>>(0);
    //CurNodes GameObject Representation.
    readonly GameObject curGameObject;
    //Master Graph(Users Graph)
    //[System.NonSerialized]
    [HideInInspector]
    [SerializeField]
    public List<CiDyNode> masterGraph = new List<CiDyNode>(0);
    //A Temp Node List.
    //[System.NonSerialized]
    //private List<CiDyNode> subNodes = new List<CiDyNode> (0);
    //Cells
    public List<CiDyCell> cells = new List<CiDyCell>(0);
    //Raycast Variable
    RaycastHit hit;
    //Node Naming Count
    public int nodeCount;//How many master Nodes we have :)
    public int nodeCount2;
    //How close nodes are allowed to be.
    public float nodeSpacing = 50;
    public float intersectionAngleLimit = 6f;
    public GameObject cellsHolder;
    public GameObject boundaryHolder;
    public GameObject roadHolder;
    public GameObject secondaryRdHolder;
    public GameObject nodeHolder;
    //User Building Prefabs
    public GameObject[] downtownPrefabs = new GameObject[0];
    public GameObject[] industrialPrefabs = new GameObject[0];
    public GameObject[] residentialPrefabs = new GameObject[0];
    //Foundation Material
    public Material foundationMaterial;
    //Terrain Info
    [HideInInspector]
    [SerializeField]
    public float[] originalHeights;

    //Cells Build Type
    //Cell Type Enum
    public enum BuildingType {
        Downtown,
        Industrial,
        Residential,
    }
    public BuildingType buildingType = BuildingType.Downtown;
    //Defined Materials.
    public Material intersectionMaterial;//Intersection Material defined by user in Editor for Road Creation.
    public Material roadMaterial;//Intersection Material defined by user in Editor for Intersection Creation.

    //Bools for Node and Cell Collider Visualizing
    [HideInInspector]
    public bool activeCells;
    public bool activeNodes;

    void Awake() {
        InitilizeGraph();
    }

    //Multi-Threading Queue Logic
    List<Action> functionsToRunInMainThread = new List<Action>(0);
    bool hasRan = false;
    //Used to Update on Main Thread after Multi- Threads have completed
    void Update()
    {
        // Update() always runs in the main thread

        while (functionsToRunInMainThread.Count > 0)
        {
            if (!hasRan)
            {
                hasRan = true;
            }
            // Grab the first/oldest function in the list
            Action someFunc = functionsToRunInMainThread[0];
            functionsToRunInMainThread.RemoveAt(0);

            // Now run it
            someFunc();
            #if UNITY_EDITOR
                EditorApplication.update += Update;
            #endif
        }
        if (hasRan && functionsToRunInMainThread.Count == 0)
        {
            Debug.Log("Completed All Thread Queue Tasks!");
            hasRan = false;
        }
    }

    public void QueueMainThreadFunction(Action someFunction)
    {
        // We need to make sure that someFunction is running from the
        // main thread

        //someFunction(); // This isn't okay, if we're in a child thread

        functionsToRunInMainThread.Add(someFunction);
    }
    //End of Multi Threading Queue Functions
    /// <summary>
    /// Example Queue Setup
    /// </summary>
    ///  // Now we need to modify a Unity gameobject
    /*Action aFunction = () => {
        Debug.Log("The results of the child thread are being applied to a Unity GameObject safely.");
        this.transform.position = new Vector3(1, 1, 1);   // NOT ALLOWED FROM A CHILD THREAD
    };

    // NOTE: We still aren't allowed to call this from a child thread
    //aFunction();

    QueueMainThreadFunction(aFunction );
    #if UNITY_EDITOR
                EditorApplication.update += Update;
            #endif
    */

    public void InitilizeGraph() {
        if (instance == null) {
            instance = this;
        }
        /*if(allSplines == null)
            allSplines = new List<CiDySpline>();*/
        //Debug.Log ("InitilizeGraph");
        if (masterGraph == null) {
            masterGraph = new List<CiDyNode>(0);
        }
        //Set Road Searching Mask
        roadMask = 1 << LayerMask.NameToLayer(terrainTag);
        roadMask2 = 1 << LayerMask.NameToLayer(roadTag);
        cellMask = 1 << LayerMask.NameToLayer(cellTag);

        if (roadHolder == null) {
            CreateGraphHolders();
        }
        //Fill downTownPrefabs
        if (downtownPrefabs.Length == 0) {
            //No User defined Clutter objects. Self Fill
            downtownPrefabs = Resources.LoadAll("CiDyResources/DownTownPrefabs", typeof(GameObject)).Cast<GameObject>().ToArray();
        }
        //Fill Industiral Prefabs
        if (industrialPrefabs.Length == 0) {
            //No User defined Clutter objects. Self Fill
            industrialPrefabs = Resources.LoadAll("CiDyResources/IndustrialPrefabs", typeof(GameObject)).Cast<GameObject>().ToArray();
        }
        //Fill Residential Prefabs
        if (residentialPrefabs.Length == 0) {
            //No User defined Clutter objects. Self Fill
            residentialPrefabs = Resources.LoadAll("CiDyResources/ResidentialPrefabs", typeof(GameObject)).Cast<GameObject>().ToArray();
        }
        //Grab Foundation Material
        if (foundationMaterial == null) {
            foundationMaterial = Resources.Load("CiDyResources/FoundationMaterial", typeof(Material)) as Material;
        }
        //Grab Intersection Material
        if (intersectionMaterial == null)
        {
            intersectionMaterial = (Material)Resources.Load("CiDyResources/Intersection", typeof(Material));
        }
        //Grab Road Material
        if (roadMaterial == null)
        {
            roadMaterial = (Material)Resources.Load("CiDyResources/Road", typeof(Material));
        }
        //Save the Initial TerrainData
        terrain = Terrain.activeTerrain;
        if (terrain)
        {
            if (terrainTrees == null)
            {
                TreeInstance[] trees = terrain.terrainData.treeInstances;
                terrainTrees = new SerializedTreeInstance[trees.Length];
                for (int i = 0; i < terrainTrees.Length; i++) {
                    terrainTrees[i] = new SerializedTreeInstance(trees[i].color, trees[i].heightScale, trees[i].lightmapColor, trees[i].position, trees[i].prototypeIndex, trees[i].rotation, trees[i].widthScale);
                }
                //Debug.Log("Trees: " + terrainTrees.Length);
            }
            if (terrainGrass == null)
            {
                //Get All Layers
                int[] layers = terrain.terrainData.GetSupportedLayers(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
                terrainGrass = new TerrainDetail[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    var tmpGrass = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, layers[i]);
                    terrainGrass[i] = new TerrainDetail(tmpGrass, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
                    //Debug.Log("Grabbed Grass: " + terrainGrass.Length());
                }
            }
            //Debug.Log("Trees: " + terrainTrees.Length);
            //Debug.Log("Grass: " + terrainGrass.Length());
        }
        //Check Terrain Heights
        if (originalHeights == null) {
            originalHeights = new float[0];
        }
        if (terrain)
        {
            hmWidth = terrain.terrainData.heightmapWidth;
            hmHeight = terrain.terrainData.heightmapHeight;
            //Debug.Log("HeightMapWidth " + hmWidth + " HeightMapHeight " + hmHeight + " TerrainDataSize XYZ: " + terrain.terrainData.size + " TerrainWorldPos " + terrain.transform.position);
            float[,] tmpHeights = terrain.terrainData.GetHeights(0, 0, hmWidth, hmHeight);
            originalHeights = new float[hmWidth * hmHeight];
            for (int i = 0; i < hmWidth; i++)
            {
                for (int j = 0; j < hmHeight; j++)
                {
                    originalHeights[j * hmWidth + i] = tmpHeights[i, j];
                }
            }
        }
    }

    public void CreateGraphHolders() {
        //Create Cells Holder
        cellsHolder = new GameObject("CellsHolder");
        cellsHolder.transform.parent = transform;
        cellsHolder.transform.position = transform.position;
        //Boundary Cell Holder
        boundaryHolder = new GameObject("BoundaryCellsHolder");
        boundaryHolder.transform.parent = transform;
        boundaryHolder.transform.position = transform.position;
        //Create RoadHolder
        roadHolder = new GameObject("RoadHolder");
        roadHolder.transform.parent = transform;
        roadHolder.transform.position = transform.position;
        //Create secondaryRd Holder
        secondaryRdHolder = new GameObject("SecondaryRoads");
        secondaryRdHolder.transform.parent = transform;
        secondaryRdHolder.transform.position = transform.position;
        //Create node Holder
        nodeHolder = new GameObject("NodeHolder");
        nodeHolder.transform.parent = transform;
        nodeHolder.transform.position = transform.position;
        //Find Node Prefab in Resources.
        nodePrefab = Resources.Load("CiDyResources/NodePrefab", typeof(GameObject)) as GameObject;
    }

    void OnDrawGizmos() {
        //Scene View VISUAL REFERENCE ONLY
        if (drawEdges) {
            if (graphEdges.Count > 0) {
                for (int i = 0; i < graphEdges.Count; i++) {
                    CiDyEdge tmpEdge = graphEdges[i];
                    Debug.DrawLine(tmpEdge.v1.position, tmpEdge.v2.position, Color.green);
                }
            }
            if (drawFilaments) {
                if (filaments.Count > 0) {
                    for (int i = 0; i < filaments.Count; i++) {
                        List<CiDyNode> filament = filaments[i];
                        for (int j = 0; j < filament.Count - 1; j++) {
                            CiDyNode v1 = filament[j];
                            CiDyNode v2 = filament[j + 1];
                            //This filament needs to be showing in white.
                            Debug.DrawLine(v1.position, v2.position, Color.white);
                        }
                    }
                }
                /*if(filamentTrees.Count > 0){
					for(int i = 0;i<filamentTrees.Count;i++){
						List<List<CiDyNode>> tree = filamentTrees[i];
						//Debug.Log("Tree"+(i+1));
						for(int h=0;h<tree.Count;h++){
							List<CiDyNode> subTree = tree[h];
							//Debug.Log("SubTree "+(h+1));
							for(int k = 0;k<subTree.Count-1;k++){
								CiDyNode v1 = subTree[k];
								CiDyNode v2 = null;
								if(k != subTree.Count-1){
									v2 = subTree[k+1];
								} else {
									v2 = subTree[0];
								}
								Debug.DrawLine(v1.position,v2.position, Color.grey);
							}
						}
					}
				}*/
                if (cycleFilaments.Count > 0) {
                    for (int i = 0; i < cycleFilaments.Count; i++) {
                        List<CiDyNode> filament = cycleFilaments[i];
                        for (int j = 0; j < filament.Count - 1; j++) {
                            Debug.DrawLine(filament[j].position, filament[j + 1].position, Color.red);
                        }
                    }
                }
            }

            //Sub Division
            //Draw regions.
            if (regions.Count > 0) {
                for (int i = 0; i < regions.Count; i++) {
                    //Grab list and draw its edges.
                    List<List<CiDyNode>> curEdges = regions[i];
                    for (int j = 0; j < curEdges.Count; j++) {
                        List<CiDyNode> curEdges2 = curEdges[j];
                        for (int h = 0; h < curEdges2.Count; h++) {
                            CiDyNode curNode = curEdges2[h];
                            CiDyNode nxtNode = curNode.succNode;
                            Debug.DrawLine(curNode.position, nxtNode.position, Color.blue);
                        }
                    }
                }
                //Draw on Top of blues
                //Draw greenSpace
                /*if(greenSpace.Count > 0){
					for(int i = 0;i<greenSpace.Count;i++){
						//Grab list and draw its edges.
						List<CiDyNode> curEdges = greenSpace[i];
						for(int j = 0;j<curEdges.Count;j++){
							CiDyNode curNode = curEdges[j];
							CiDyNode nxtNode = curNode.succNode;
							Debug.DrawLine(curNode.position, nxtNode.position, Color.green);
						}
					}
				}*/
            }
            //Draw Filaments on top so we know :)
            /*if(filaments.Count > 0){
				for(int i = 0;i<filaments.Count;i++){
					List<CiDyNode> newFilament = filaments[i];
					for(int j = 0;j<newFilament.Count-1;j++){
						//Debug.Log("Drawn From "+newFilament[j].name+" to "+newFilament[j+1].name);
						//Draw line through edges
						if(j==newFilament.Count-2){
							Debug.DrawLine(newFilament[j].position, newFilament[j+1].position,Color.red);
							j++;
							//Last Filament
							Debug.DrawLine(newFilament[j].position, newFilament[j].position+Vector3.up*50, Color.red);
						}
					}
				}
			}*/
        }

        if (cycles.Count > 0 && drawCycles) {
            for (int i = 0; i < cycles.Count; i++) {
                //Grab Cycle 
                List<CiDyNode> newCycle = cycles[i];
                //Iterate through new cycle drawing connections
                for (int j = 0; j < newCycle.Count - 1; j++) {
                    Debug.DrawLine(newCycle[j].position, newCycle[j + 1].position, Color.blue);
                }
                //yield return new WaitForSeconds(0f);
            }
            //Debug.Log("Total Cycles "+cycles.Count);
        }

        if (sphereSpots.Count > 0) {
            for (int i = 0; i < sphereSpots.Count; i++) {
                Gizmos.DrawSphere(sphereSpots[i], 0.5f);
            }
        }

    }

    //Visual Spheres
    public List<Vector3> sphereSpots = new List<Vector3>(0);
    void ClearSphereSpots() {
        sphereSpots = new List<Vector3>(0);
    }

    /*public void GrabDesigner(CiDyDesigner newDesigner){
		//Updating our holder info
		designer = newDesigner;
	}*/

    //Now that we have the Boundary Cells.
    List<List<Vector3>> boundaryLines;
    //this will run a MCB on this graph and return the amount of cycles found if any
    public List<List<CiDyNode>> FindCycles(ref List<List<Vector3>> boundaryCell){
		//Debug.Log ("Find Cycles MCB");
		filaments = new List<List<CiDyNode>> ();
		//cycleEdges = new List<List<CiDyNode>> ();
		//Debug.Log ("Before Call");
		cycles = CiDyMCB.ExtractCells(masterGraph, ref filaments);
        //Debug.Log("Cycles: " + cycles.Count + " Filaments: " + filaments.Count);
        /*List<List<CiDyNode>> tmpList;

        if (cycles.Count == 0 && roads.Count > 0) {
            //Special Case of only Filaments
            //Debug.Log("Special Case");
            tmpList = CiDyMCB.GetBoundaryCells(masterGraph, true);
        }
        else
        {
            tmpList = CiDyMCB.GetBoundaryCells(masterGraph, false);
        }*/
        /*//Now that we have the Boundary Cells.
        boundaryLines = new List<List<Vector3>>(0);
        boundaryCell = new List<List<Vector3>>(0);
            //Lets Grab the Roads in there Sequences.
            for (int i = 0; i < tmpList.Count; i++)
            {
                for (int j = 0; j < tmpList[i].Count; j++)
                {
                    CiDyNode curNode = tmpList[i][j];
                    CiDyNode nxtNode;
                    if (j == tmpList[i].Count - 1)
                    {
                        if (curNode.adjacentNodes.Count > 1)
                        {
                            //We are at end
                            nxtNode = tmpList[i][0];
                        }
                        else {
                            //Filament
                            nxtNode = tmpList[i][j-1];
                        }
                    }
                    else
                    {
                        //In Beginning or Middle
                        nxtNode = tmpList[i][j + 1];
                    }
                    CiDyEdge testEdge = new CiDyEdge(curNode, nxtNode);
                    //Now determine the curRoad for these Nodes.
                    CiDyRoad newRoad = ReturnRoadFromEdge(testEdge);
                    if (newRoad == null)
                    {
                        Debug.Log("Null");
                        continue;
                    }
                    //Debug.Log("After");
                    //Temporary List.
                    List<Vector3> roadPoints = new List<Vector3>();
                    //Now Add this Roads Interior Vertices to the List.
                    if (newRoad != null)
                    {
                        //Which Node of This Road is closest to this CycleNode
                        float nodeDist = Vector3.Distance(newRoad.nodeA.position, curNode.position);
                        float nodeDist2 = Vector3.Distance(newRoad.nodeB.position, curNode.position);
                        if (nodeDist < nodeDist2)
                        {

                            //NodeA is Closer
                            //Since NodeB Is Closer we only need to Grab the Odds and Reverse there List.
                            CiDyUtils.GrabOddVs(newRoad, ref roadPoints);
                        }
                        else
                        {

                            //NodeB is Closer
                            CiDyUtils.GrabEvenVs(newRoad, ref roadPoints);
                            roadPoints.Reverse();
                        }
                    }
                    boundaryCell.Add(roadPoints);
                    boundaryLines.Add(roadPoints);
                }
            }*/

        return cycles;
	}

    /*
    private void OnDrawGizmosSelected()
    {
        //Draw Gizmos Selected
        if(boundaryLines != null && boundaryLines.Count > 0) {
            Gizmos.color = Color.yellow;
            //Draw Lines.
            for (int i = 0; i < boundaryLines.Count; i++) {
                for(int j = 0; j < boundaryLines[i].Count-1; j++)
                {
                    if (j % 2 == 0)
                    {
                        //Draw a Line
                        Gizmos.DrawLine(boundaryLines[i][j], boundaryLines[i][j + 1]);

                        Vector3 fwd = (boundaryLines[i][j + 1] - boundaryLines[i][j]).normalized;
                        //Calculate Right Direction
                        Vector3 right = Vector3.Cross(fwd, Vector3.up).normalized;
                        Gizmos.DrawLine(boundaryLines[i][j], boundaryLines[i][j] + (-right * 32));

                        Vector3 prevNode;
                        Vector3 curNode = boundaryLines[i][j];
                        Vector3 nxtNode;

                        if (j == 0)
                        {
                            //We are at the begining, so the prev node points back to the end.
                            prevNode = boundaryLines[i][boundaryLines[i].Count - 1];
                            //Next node = Future
                            nxtNode = boundaryLines[i][j + 1];
                        }
                        else if (j == boundaryLines[i].Count - 1)
                        {
                            //At End
                            //Nxt Node points to Begining.
                            nxtNode = boundaryLines[i][0];
                            //Prev node = before
                            prevNode = boundaryLines[i][j - 1];
                        }
                        else
                        {
                            //Prev node is before
                            prevNode = boundaryLines[i][j - 1];
                            //Next node = Future
                            nxtNode = boundaryLines[i][j + 1];
                        }
                        prevNode.y = 0;
                        curNode.y = 0;
                        nxtNode.y = 0;
                        //Get Angle Bisector
                        Vector3 bisector = CiDyUtils.AngleBisector(prevNode, curNode, nxtNode);
                        //Now project Point of Reference.
                        Vector3 refPoint = curNode + (bisector * 1000f);
                        Gizmos.DrawLine(curNode, refPoint);
                        //Is refPoint left of direction Line or Right?
                        //int angle = CiDyUtils.AngleDirection(fwd, (refPoint - curNode), Vector3.up);
                    }
                }
            }
        }
    }
    */

    public CiDyNode FindMasterNode(CiDyNode cloneNode){
        Debug.Log("Match Node: "+cloneNode.name);
		for(int i=0;i<masterGraph.Count;i++){
			if(masterGraph[i].name == cloneNode.name){
				//Found Orig Node
				return masterGraph[i];
			}
		}
		return null;
	}

    //Overload
    public CiDyNode FindMasterNode(Vector3 clonePosition)
    {
        for (int i = 0; i < masterGraph.Count; i++)
        {
            if (masterGraph[i].position.Equals(clonePosition))
            {
                //Debug.Log("Mathced Node: "+masterGraph[i].name);
                //Found Orig Node
                return masterGraph[i];
            }
        }
        return null;
    }

    private List<List<CiDyNode>> filaments = new List<List<CiDyNode>>(0);
	private List<List<CiDyNode>> cycleFilaments = new List<List<CiDyNode>> (0);
	//private List<List<List<CiDyNode>>> filamentTrees = new List<List<List<CiDyNode>>>();
	private readonly List<List<CiDyNode>> curCycleEdges = new List<List<CiDyNode>>(0);
	//private List<List<CiDyNode>> cycleEdges = new List<List<CiDyNode>>();

	/*public List<List<CiDyNode>> FoundFilaments(List<List<CiDyNode>> newFilaments, List<List<CiDyNode>> cycleEdges){
		//Debug.Log ("FoundFilaments: "+newFilaments.Count+" CycleEdges: "+cycleEdges.Count);
		//filamentTrees = new List<List<List<CiDyNode>>> ();
		List<List<CiDyNode>> subFilaments = new List<List<CiDyNode>> ();
		//List<List<CiDyNode>> groupedFilaments = new List<List<CiDyNode>> ();
		curCycleEdges = new List<List<CiDyNode>>(cycleEdges);
		int dir = 0;
		cycleFilaments = new List<List<CiDyNode>> ();
		//Find true CidyNode from clones data.
		for(int i = 0;i<newFilaments.Count;i++){
			List<CiDyNode> filament = newFilaments[i];
			for(int j =0 ;j<filament.Count;j++){
				//Update clone Node with true node
				filament[j] = FindMasterNode(filament[j]);
			}
		}
		filaments = new List<List<CiDyNode>>(newFilaments);
		//Now we want to sort these filaments into two categories(Inside a Cell/Outside a cell) Save the Inside Cell only(for now)
		//Find the endpoint of the filaments that is connected to a cell.(cycle edges)
		for(int i = 0;i<filaments.Count;i++){
			List<CiDyNode> filament = filaments[i];
			//Debug.Log("FilamentCount "+filament.Count);
			CiDyNode lastNode = filament[filament.Count-1];
			if(lastNode.adjacentNodes.Count < 3){
				//This filament cannot be apart of a cycle.
				//Debug.Log("Cannot be apart of a cycle");
				continue;
			}
			//Debug.Log("LastNode "+lastNode.name);
			CiDyNode fwdNode = filament[filament.Count-2];
			//Debug.Log("Forward Name "+fwdNode.name);
			if(fwdNode == null){
				Debug.LogError("Error");
			}
			//Debug.Log("testing Last Node "+lastNode.name+" cnt= "+lastNode.adjacentNodes.Count);
			//Test last node in list if its apart of a cell.
			//Grab next Forward direction node.
			//Now find the counterclockwise most node to this direction in our adjacent Nodes list ignoring the fwdNode.
			//CiDyNode clockWise = GetClockWiseMost(lastNode,fwdNode);
			CiDyNode clockWise = null;
			CiDyNode counterWise = null;
			if(!GetCycleEdges(lastNode,fwdNode,ref clockWise, ref counterWise)){
				//This means that we did not find two acceptable cycle edges.
				subFilaments.Add(filament);
				continue;
			}
			//Debug.Log("Returned Both ClockWise: "+clockWise.name+" CounterClockwise: "+counterWise.name);
			//Debug.Log("Sub-Filament Add");
			subFilaments.Add(filament);
			//Now the New filament must be added to filaments list.
			//Then Clear the Grouped filaments list.
			//groupedFilaments = new List<List<CiDyNode>>();
			subFilaments = new List<List<CiDyNode>>();
			//Debug.Log("Root Node "+lastNode.name+" Grouped Fil Cleared. Sub-Filaments Cleared");
			//if both are apart of a cycle then we are inside a cycle.
			if(clockWise != null && counterWise != null){
				//Debug.Log("ClockWise "+clockWise.name);
				//Debug.Log("CounterClockWise "+counterWise.name);
				CiDyEdge clockEdge = new CiDyEdge(lastNode,clockWise);
				CiDyEdge counterEdge = new CiDyEdge(lastNode,counterWise);
				//Debug.Log("ClockEdge = "+clockEdge.name);
				//Debug.Log("CounterEdge = "+counterEdge.name);
				if(clockWise.name == counterWise.name){
					//Debug.Log("Cannot Continue Returned Same Directions");
					continue;
				}
				List<CiDyNode> newCycle = new List<CiDyNode>();
				List<CiDyNode> origCycle = new List<CiDyNode>();
				if(SameCycle(ref dir, lastNode, clockWise,counterWise, cycleEdges, filament, ref newCycle, ref origCycle)){
					//Debug.Log("Root Node of Filament ");
					//Debug.Log("New Cycle "+newCycle.Count+" OrigCycle "+origCycle.Count+" Cycles "+cycles.Count);
					cycleFilaments.Add(filament);
					//FindCycleIndex(ref origCycle, ref newCycle, ref cycles);
					//Debug.Log("OrigCycle pnt = "+cycles.IndexOf(origCycle));
				}
			}
		}
		//Debug.Log ("Updated Filaments " + filamentTrees.Count+" In cycles "+cycleFilaments.Count);
		//The Cycle Filament Trees are Known
		//Split them into as many needed sub points.
		//Debug.Log ("Cycles Returned? " + cycles.Count);
		if(cycles.Count > 0){
			//Debug.Log("New Cycle Count "+cycles[0].Count);
		}
		return cycles;
	}*/

	//Find Cycle Index Iterate through the list and find the desired list.
	void FindCycleIndex(ref List<CiDyNode> origCycle, ref List<CiDyNode> newCycle, ref List<List<CiDyNode>> oldCycles){
		//Debug.Log("Finding Cycle Index!!!!!! OrigCycle: "+origCycle.Count+" NewCycle "+newCycle.Count+" OldCycles "+oldCycles.Count);
		bool found = false;
		for(int i = 0;i<oldCycles.Count;i++){
			List<CiDyNode> testCycle = oldCycles[i];
			//Debug.Log("TestCycle Cnt "+testCycle.Count);
			//To find the Orig we only need to have them in there proper order. 
			for(int n = 0;n<testCycle.Count;n++){
				CiDyNode node = testCycle[n];
				//Debug.Log(node.name);
				//Find the first three nodes in order.
				if(node.name == origCycle[0].name){
					//Debug.Log("Found First Node "+node.name);
					//We found the First node. Test if the nxt two match as well.
					n++;
					if(testCycle[n].name == origCycle[1].name){
						//Debug.Log("Found Second Node "+testCycle[n].name);
						//Second Confirmed. Final Test
						n++;
						if(testCycle[n].name == origCycle[2].name){
							//Debug.Log("Found Third "+testCycle[n].name+" Before "+oldCycles[i].Count);
							//Replace with New?
							oldCycles[i] = new List<CiDyNode>(newCycle);
							//Debug.Log("Updated Cnt On Cycles "+oldCycles[i].Count);
							found = true;
							break;
						}
					}
				}
			}
			if(found){
				break;
			}
		}
	}

	bool IsCycleEdge(CiDyEdge testEdge, List<List<CiDyNode>> cycleEdges){
		//Debug.Log ("TestCycleEdge "+testEdge.name);
		/*for(int n=0;n<cycleEdges.Count;n++){
			List<CiDyNode> cycle = cycleEdges[n];
			for(int k =0 ;k<cycle.Count;k++){
				CiDyNode v1 = cycle[k];
				CiDyNode v2 = null;
				if(k==cycle.Count-1){
					v2 = cycle[0];
				} else {
					v2 = cycle[k+1];
				}
				CiDyEdge newEdge = new CiDyEdge(v1,v2);
				Debug.Log("Cycle Edge "+newEdge.name);
			}
		}*/
		//Test against cycle edges
		for(int i =0;i<cycleEdges.Count;i++){
			List<CiDyNode> cycle = cycleEdges[i];
			for(int j = 0;j<cycle.Count;j++){
				CiDyNode v1 = cycle[j];
				CiDyNode v2 = null;
				if(j == cycle.Count-1){
					//At end
					v2 = cycle[0];
				} else {
					v2 = cycle[j+1];
				}
				CiDyEdge newEdge = new CiDyEdge(v1,v2);
				//Debug.Log(newEdge.name);
				if(testEdge.name == newEdge.name){
					//Debug.Log("Is Cycle Edge "+newEdge.name);
					return true;
				}
			}
		}
		//Debug.Log ("TestingEdge "+testEdge.name+" No Cycle Edge");
		return false;
	}

	//This function will test if these edges are apart of the same cycle.
	bool SameCycle(ref int dir, CiDyNode lastNode, CiDyNode clockWise, CiDyNode counterWise, List<List<CiDyNode>> cycleEdges, List<CiDyNode> filament, ref List<CiDyNode> finalCycle, ref List<CiDyNode> origCycle){
		//Determine if cycle connection is flowing from counter to center to clockwise.
		//Set Dir 0 = not set, 1 = counter is first, 2 = clock is first
		//Iterate through the cycles
		for(int i = 0;i<cycleEdges.Count;i++){
			List<CiDyNode> cycle = cycleEdges[i];
			//Set bools
			bool foundLast = false;
			bool foundClock = false;
			bool foundCounter = false;
			//Debug.Log("New Cycle "+cycle.Count);
			for(int j = 0;j<cycle.Count;j++){
				//Iterate and find last Node
				CiDyNode testNode = cycle[j];
				if(testNode.name == lastNode.name){
					//Found last node
					if(!foundLast){
						foundLast = true;
						CiDyNode newNode = null;
						if(j == cycle.Count-1){
							newNode = cycle[0];
						} else {
							newNode = cycle[j+1];
						}
						if(newNode.name == counterWise.name){
							//ClockWise->LastNode->CounterClockWise is next direction
							dir = 1;//LastNode to counter
							//Debug.Log("ClockWise->LastNode->CounterClockWise");
						} else if(newNode.name == clockWise.name){
							//CounterClockWise->LastNode->ClockWise is next direction
							dir = 2;//LastNode to clockwise
							//This is the Proper Direction for an Internal Filament
							//return true;
							//Debug.Log("CounterClockWise->LastNode->ClockWise");
						}
					}
				}
				if(testNode.name == counterWise.name){
					foundCounter = true;
				}
				if(testNode.name == clockWise.name){
					foundClock = true;
				}
			}
			//If both are true then these edges are apart of the same cycle
			if(foundClock && foundCounter && dir == 2){
				//Debug.Log("Returned True Same Cycle "+cycle.Count+" Filament count "+filament.Count);
				//(((Add Filament Nodes to Cycle now by going through the list finding best left. :))))
				origCycle = new List<CiDyNode>(cycle);
				//AddFilamentToCycle(cycle,filament);
				finalCycle = new List<CiDyNode>(cycle);
				return true;
			}
		}
		//Debug.Log ("Returned False");
		return false;
	}

	void ShowCycle(List<CiDyNode> cycle){
		//Debug.Log ("Show Cycle");
		for(int h = 0;h<cycle.Count;h++){
			//Debug.Log(cycle[h].name);
		}
	}

	//This function will Add the filament to the cycle by using a Left Adjacent Find Algorithm we we need the Start of the Filament. Root node.
	void AddFilamentToCycle(List<CiDyNode> cycle, List<CiDyNode> filament){
		//Debug.Log ("Add Filament To Cycle Fil.Cnt: " + filament.Count + " Cycle Cnt: " + cycle.Count);
		//Flip for the Root node at 0.
		filament.Reverse ();
		CiDyNode rootNode = FindMasterNode (filament [0]);
		CiDyNode curNode = null;
		//Find root node in cycle and begin entering the filament cycle nodes in the proper sequence. ;)
		for(int n = 0;n<cycle.Count;n++){
			//Find root node.
			if(cycle[n].name == rootNode.name){
				int cycleLoc = n+1;//This means if we call insert at cycleLoc it will push the current One and add a new one in this place.
				//Check for single connection scenario
				if(filament.Count == 2){
					//does the fwd node have more than 1 connection?
					if(filament[1].adjacentNodes.Count<=1){
						//This is a single Connection scenario.
						cycle.Insert(cycleLoc,filament[1]);
						cycleLoc++;
						cycle.Insert(cycleLoc,rootNode);
						cycleLoc++;
						//Debug.Log("Single Connection Inserted");
						//Debug.Log("Inserted "+filament[1].name);
						//Debug.Log("Inserted "+rootNode.name);
						break;
					}
				}
				//Yay we found the root node. run the Left finding algorithm starting at the root node and ending at the root Node. :).
				curNode = FindMasterNode(filament[1]);
				cycle.Insert(cycleLoc,curNode);
				cycleLoc++;
				//Debug.Log("Inserted "+curNode.name);
				//ShowCycle(cycle);
				CiDyNode nxtNode = GetLeftMost(rootNode,curNode);
				bool process = true;
				//Debug.Log("Start Process CurNode: "+curNode.name);
				while(process){
					if(nxtNode != null){
						//Debug.Log("Have Nxt Node :) "+nxtNode.name);
						if(nxtNode.name != rootNode.name){
							//Debug.Log("Nxt Node != rootNode");
							//Scenario where No outlet
							if(nxtNode.adjacentNodes.Count<=1){
								//Debug.Log("Nxt Node Dead End");
								//Dead End/Flip test
								cycle.Insert(cycleLoc,nxtNode);
								cycleLoc++;
								cycle.Insert(cycleLoc,curNode);
								cycleLoc++;
								//Debug.Log("Inserted "+nxtNode.name);
								//Debug.Log("Inserted "+curNode.name);
								nxtNode = GetLeftMost(nxtNode,curNode);
							} else {
								//Debug.Log("Nxt Node has Potentials");
								//Sceario with a potential nxtNode
								CiDyNode tmpNode = curNode;
								curNode = nxtNode;
								cycle.Insert(cycleLoc,curNode);
								cycleLoc++;
								//Debug.Log("Inserted "+curNode.name);
								//Run test using nxtNode & curNode
								nxtNode = GetLeftMost(tmpNode,curNode);
							}
						} else {
							//We are at the end of the iteration.:)
							process = false;
							cycle.Insert(cycleLoc,rootNode);
							cycleLoc++;
							//Debug.Log("End Process Inserted "+rootNode.name);
							return;
						}
					} else {
						//Debug.Log("Do Not Have NxtNode? :(");
						//Nothing returned
						if(curNode.adjacentNodes.Count<=1){
							//Dead End. :) Add curNode and re-test
							cycle.Insert(cycleLoc,curNode);
							cycleLoc++;
							nxtNode = GetLeftMost(nxtNode,curNode);
							//Debug.Log("Inserted "+curNode.name);
						} else {
							Debug.LogError("Cur Node Adj Count > 1 But we still have no nxtNode???");
							process = false;
							break;
						}
					}
				}
			}
		}
	}

	//For Filament Detection
	CiDyNode GetLeftMost(CiDyNode srcNode,CiDyNode fwdNode){
		//Debug.Log ("SRCNode "+srcNode.name+" ADJ "+srcNode.adjacentNodes.Count+" FWDNode "+fwdNode.name+" ADJ "+fwdNode.adjacentNodes.Count);
		//CiDyNode startNode = new CiDyNode ("", Vector3.zero, 0);
		//CiDyNode startNode = ScriptableObject.CreateInstance<CiDyNode>().Init("",Vector3.zero,0);
		CiDyNode finalNode = null;
		//List<Vector3> points = new List<Vector3> (0);
		//Debug.Log ("running "+srcNode.name+" "+nxtNode.name);
		float currentDirection = Mathf.Infinity;
		int bestNode = -1;
		
		// the vector that we want to measure an angle from
		Vector3 referenceForward = (fwdNode.position-srcNode.position);// some vector that is not Vector3.Debug.Log (referenceForward);
		//referenceForward = nxtNode.position+referenceForward;
		//points.Add (referenceForward);
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
		//referenceRight = srcNode.position+referenceRight*100;
		//points.Add (referenceRight);
		// the vector of interest
		//Debug.Log ("NxtNode ADJ Cnt "+nxtNode.adjacentNodes.Count);
		if(fwdNode.adjacentNodes.Count != 1){
			//Itearate through adjacent Nodes
			for(int i = 0;i<fwdNode.adjacentNodes.Count;i++){
				CiDyNode tmpNode = fwdNode.adjacentNodes[i];
				//Debug.Log(nxtNode.name+ " Adj Node "+tmpNode.name+" Current Place "+i);
				//If the curNode we are checking is not equal to the node we came from (SRC)
				if(tmpNode.name != srcNode.name){
					//Debug.Log(tmpNode.name);
					//Grab new Direction
					Vector3 newDirection = (tmpNode.position-fwdNode.position);// some vector that we're interested in
					//newDirection = srcNode.position+newDirection*100;
					//points.Add(newDirection);
					//Debug.Log("Added "+newDirection);
					// Get the angle in degrees between 0 and 180
					float angle = Vector3.Angle(newDirection, referenceForward);
					//Debug.Log(angle);
					// Determine if the degree value should be negative.  Here, a positive value
					// from the dot product means that our vector is on the right of the reference vector   
					// whereas a negative value means we're on the left.
					float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
					//float finalAngle = angle*sign;
					float finalAngle = angle;
					//print ("Final Angle for "+tmpNode.name+" = "+finalAngle);
					//Catch scenario when adjacent node is directly behind us returning as a positive and make it a negative.
					/*if(finalAngle == 180){
						finalAngle = -180;
					}*/
					if(sign < 0){
						finalAngle = (360-finalAngle);
					}
					//Debug.Log("CounterClockWise "+tmpNode.name+" Angle = "+finalAngle);
					//finalAngle = Mathf.Round(finalAngle * 100f) / 100f;
					//finalAngle = (finalAngle<= 0) ? 360 + finalAngle : finalAngle;
					//Debug.Log(tmpNode.name+" "+finalAngle);
					//ClockWise Most (Highest/Positive)
					if(currentDirection > finalAngle){
						//The New angle is higher update CurrentDirection
						bestNode = i;
						currentDirection = finalAngle;
						//Debug.Log("BestNode "+nxtNode.adjacentNodes[bestNode].name+" FinalAngle "+currentDirection);
					}
				}
			}
			//Did we find a new node?
			if(bestNode != -1){
				//We have selected a Node
				finalNode = fwdNode.adjacentNodes[bestNode];
				//points.Add(finalNode.position);
				//Debug.Log("CounterClockwise FinalNode "+finalNode.name+" Found For "+srcNode.name);
				//Debug.Log("Left Most = "+finalNode.name);
				return finalNode;
			}
		}
		Debug.Log ("No Counter ClockWise Found for " + fwdNode.name);
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}

	//This will Get Both The Clockwise and Counterclockwise Cycle Edges
	bool GetCycleEdges(CiDyNode srcNode,CiDyNode fwdNode, ref CiDyNode clockWise, ref CiDyNode counterWise){
		//Debug.Log ("GetCycleEdges SrcNode: "+srcNode.name+" FwdNode: "+fwdNode.name);
		// the vector that we want to measure an angle from
		Vector3 referenceForward = (fwdNode.position-srcNode.position);// some vector that is not Vector3.up
		
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);

		List<CiDyNode> sortedAngles = new List<CiDyNode> ();
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<srcNode.adjacentNodes.Count;i++){
			CiDyNode tmpNode = srcNode.adjacentNodes[i];
			if(tmpNode.name == fwdNode.name){
				continue;
			}
			//Grab new Direction
			Vector3 newDirection = (tmpNode.position - srcNode.position).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, referenceForward);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			float finalAngle = angle;//sign * angle;
			//print ("Final Angle for "+tmpNode.name+" = "+finalAngle);
			//Catch scenario when adjacent node is directly behind us returning as a positive and make it a negative.
			/*if(finalAngle == 180){
				finalAngle = -180;
			}*/
			if(sign < 0){
				finalAngle = (360-finalAngle);
			}
			tmpNode.Angle = finalAngle;
			sortedAngles.Add(tmpNode);
			//Debug.Log("Clockwise "+tmpNode.name+" Angle = "+finalAngle);
		}
		//Now sort the Nodes List from lowest to highest angle
		sortedAngles = sortedAngles.OrderBy(x => x.Angle).ToList();
		//Debug.Log ("Lowest: " + sortedAngles [0].name + " Highest: " + sortedAngles [sortedAngles.Count - 1].name);
		//Test Highest to Lowest and find first cycle Edge
		for(int i = sortedAngles.Count-1;i>0;i--){
			CiDyEdge testEdge2 = new CiDyEdge(srcNode, sortedAngles[i]);
			//("Testing for Hightest cycle Edge "+testEdge2.name);
			if(IsCycleEdge(testEdge2,curCycleEdges)){
				//This is a Cycle Edge
				//Make sure that the clockwise is not the same
				//if(sortedAngles[i].name != clockWise.name){
					//This is a different Answer :)
					counterWise = sortedAngles[i];
					break;
				//}
			}
		}
		//Test Lowest to Highest and find first cycle edge
		for(int i = 0;i<sortedAngles.Count;i++){
			//Test lowest to highest until a cycle edge is returned
			CiDyEdge testEdge = new CiDyEdge(srcNode,sortedAngles[i]);
			//Debug.Log("Testing for Lowest Cycle edge "+testEdge.name);
			if(IsCycleEdge(testEdge,curCycleEdges)){
				//This is a cycle Edge.
				if(sortedAngles[i].name != counterWise.name){
					clockWise = sortedAngles[i];
					break;
				}
			}
		}
		//Determine if we have both
		if(clockWise !=null && counterWise !=null){
			return true;
		}
		//If we are here then we didnt find an answer for both.
		return false;
	}

	//For Filament Cycle Determination
	public CiDyNode GetClockWiseMost(CiDyNode srcNode,CiDyNode fwdNode){
		//Debug.Log ("");
		CiDyNode finalNode = null;
		float currentDirection = Mathf.Infinity;
		int bestNode = -1;
		// the vector that we want to measure an angle from
		Vector3 referenceForward = (fwdNode.position-srcNode.position);// some vector that is not Vector3.up
		
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
		
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<srcNode.adjacentNodes.Count;i++){
			CiDyNode tmpNode = srcNode.adjacentNodes[i];
			if(tmpNode.name == fwdNode.name){
				continue;
			}
			//Grab new Direction
			Vector3 newDirection = (tmpNode.position - srcNode.position).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, referenceForward);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			float finalAngle = angle;//sign * angle;
			//print ("Final Angle for "+tmpNode.name+" = "+finalAngle);
			//Catch scenario when adjacent node is directly behind us returning as a positive and make it a negative.
			/*if(finalAngle == 180){
				finalAngle = -180;
			}*/
			if(sign < 0){
				finalAngle = (360-finalAngle);
			}
			//Debug.Log("Clockwise "+tmpNode.name+" Angle = "+finalAngle);
			//Debug.Log("Angle for Clockwise Test "+tmpNode.name+" is "+finalAngle+" From "+srcNode.name);
			//ClockWise Most (Lowest)
			if(currentDirection > finalAngle){
				//Is this edge apart of a cycle edge?
				CiDyEdge newEdge = new CiDyEdge(srcNode,tmpNode);
				if(IsCycleEdge(newEdge,curCycleEdges)){
					//The New angle is higher update CurrentDirection
					bestNode = i;
					currentDirection = finalAngle;
					//print ("Best Edge = "+newEdge.name);
				}
			}
		}
		//Did we find a new node?
		if(bestNode != -1){
			//We have selected a Node
			finalNode = srcNode.adjacentNodes[bestNode];
			//Debug.Log("Found for "+srcNode.name+" ClockWise "+finalNode.name);
			return finalNode;
		}
		//Debug.Log("No Clockwise found for "+srcNode.name);//potentially sub-filament.
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}

	//For Filament Detection
	CiDyNode GetCounterClockWiseMost(CiDyNode srcNode,CiDyNode fwdNode){
		//Debug.Log ("Get Counter SRCNode: " + srcNode.name + " FwdNode: " + fwdNode.name);
		//startNode = new CiDyNode ("", Vector3.zero, 0);
		//CiDyNode startNode = ScriptableObject.CreateInstance<CiDyNode>().Init("", Vector3.zero, 0);
		CiDyNode finalNode = null;
		//List<Vector3> points = new List<Vector3> (0);
		//Debug.Log ("running "+srcNode.name+" "+nxtNode.name);
		float currentDirection = 0;
		int bestNode = -1;
		
		// the vector that we want to measure an angle from
		Vector3 referenceForward = (fwdNode.position-srcNode.position);// some vector that is not Vector3.Debug.Log (referenceForward);
		//referenceForward = nxtNode.position+referenceForward;
		//points.Add (referenceForward);
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
		//referenceRight = srcNode.position+referenceRight*100;
		//points.Add (referenceRight);
		// the vector of interest
		//Debug.Log ("NxtNode ADJ Cnt "+nxtNode.adjacentNodes.Count);
		if(srcNode.adjacentNodes.Count != 1){
			//Itearate through adjacent Nodes
			for(int i = 0;i<srcNode.adjacentNodes.Count;i++){
				CiDyNode tmpNode = srcNode.adjacentNodes[i];
				//Debug.Log(nxtNode.name+ " Adj Node "+tmpNode.name+" Current Place "+i);
				//If the curNode we are checking is not equal to the node we came from (SRC)
				if(tmpNode.name != fwdNode.name){
					//Debug.Log(tmpNode.name);
					//Grab new Direction
					Vector3 newDirection = (tmpNode.position - srcNode.position);// some vector that we're interested in
					//newDirection = srcNode.position+newDirection*100;
					//points.Add(newDirection);
					//Debug.Log("Added "+newDirection);
					// Get the angle in degrees between 0 and 180
					float angle = Vector3.Angle(newDirection, referenceForward);
					//Debug.Log(angle);
					// Determine if the degree value should be negative.  Here, a positive value
					// from the dot product means that our vector is on the right of the reference vector   
					// whereas a negative value means we're on the left.
					float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
					//float finalAngle = angle*sign;
					float finalAngle = angle;
					//print ("Final Angle for "+tmpNode.name+" = "+finalAngle);
					//Catch scenario when adjacent node is directly behind us returning as a positive and make it a negative.
					/*if(finalAngle == 180){
						finalAngle = -180;
					}*/
					if(sign < 0){
						finalAngle = (360-finalAngle);
					}
					Debug.Log("CounterClockWise "+tmpNode.name+" Angle = "+finalAngle);
					//finalAngle = Mathf.Round(finalAngle * 100f) / 100f;
					//finalAngle = (finalAngle<= 0) ? 360 + finalAngle : finalAngle;
					//Debug.Log(tmpNode.name+" "+finalAngle);
					//ClockWise Most (Highest/Positive)
					if(currentDirection < finalAngle){
						CiDyEdge newEdge = new CiDyEdge(srcNode,tmpNode);
						if(IsCycleEdge(newEdge,curCycleEdges)){
							//The New angle is higher update CurrentDirection
							bestNode = i;
							currentDirection = finalAngle;
							//Debug.Log("BestNode "+nxtNode.adjacentNodes[bestNode].name+" FinalAngle "+currentDirection);
						}
					}
				}
			}
			//Did we find a new node?
			if(bestNode != -1){
				//We have selected a Node
				finalNode = srcNode.adjacentNodes[bestNode];
				//points.Add(finalNode.position);
				//Debug.Log("CounterClockwise FinalNode "+finalNode.name+" Found For "+srcNode.name);
				return finalNode;
			}
		}
		Debug.Log ("No Counter ClockWise Found for " + srcNode.name);
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}

	public List<CiDyNode> ReturnCell(int CellInt, ref List<List<CiDyNode>> filaments){
		//Debug.Log ("Return Cell:" + CellInt);
		//runtimeFilaments[CellInt];
		return runtimeCycles [CellInt];
	}

	//Adds good edges to the Graph for Testing later.
	public bool AddEdge(CiDyEdge newEdge){
		//CiDyEdge duplicateEdge = (graphEdges.Find(x=> x.name == newEdge.name));
        if (!DuplicateEdge(newEdge)) {
            //Debug.Log("AddedEdge " + newEdge.name);
            //This edge doesnt exist yet. :)
            //Connect Nodes in Edge. :)
            //No Intersections with Graph Edges. We Can Create a New Edge. :)
            newEdge.ConnectNodes();
			graphEdges.Add(newEdge);
            //We have a New Road Added to the Graph. Update the TerrainDetails as Well.
            return true;
		}
        return false;
	}

	//Removes edge from Graph Edges if it exist in there.
	public void RemoveEdge(CiDyEdge oldEdge){
		//Debug.Log ("Remove Edge: " + oldEdge.name+" GraphEdgeCnt: "+graphEdges.Count);
		for(int i = 0;i<graphEdges.Count;i++){
			if(graphEdges[i].name == oldEdge.name){
				//Tell the Nodes intersections to remove there proper ends as well.
				graphEdges.RemoveAt(i);
				//Debug.Log("Removed");
				break;
			}
		}
	}

	//This function will handle Mesh Road adding
	public void AddRoad(GameObject newRoad){
		//Debug.Log ("Added Road "+newRoad.name);
		//The roads names are matched to there Edges in the Graph for removal later.
		roads.Add (newRoad);
		newRoad.transform.parent = roadHolder.transform;
	}

    //string[] stringSeparators = new string[] {"V"};
    //string[] result;
    /*string source = "[stop]ONE[stop][stop]TWO[stop][stop][stop]THREE[stop][stop]";
	string[] stringSeparators = new string[] {"[stop]"};
	string[] result;
	
	// ...
	result = source.Split(stringSeparators, StringSplitOptions.None);
	
	foreach (string s in result)
	{
		Console.Write("'{0}' ", String.IsNullOrEmpty(s) ? "<>" : s);
	}*/

    public CiDyRoad ReturnRoadFromEdge(CiDyEdge edge) {
        for (int i = 0; i < roads.Count; i++)
        {
            if (roads[i].name == edge.name)
            {
                return roads[i].GetComponent<CiDyRoad>();
            }
        }

        //We didnt find it.
        return null;
    }

	//This will remove the edge from the roads list. on its edge/road Name
	public void RemoveRoad(string oldRoad){
		//Debug.Log ("Called Remove Road "+oldRoad);
		for(int i = 0; i<roads.Count;i++){
			if(roads[i].name == oldRoad){
				DestroyImmediate(roads[i]);
				roads.RemoveAt(i);
				break;
			}
		}
	}

	//This Function wil remove a Road and all of its graph connections
	public void DestroyRoad(string oldRoad){
		//Debug.Log ("Destroy Road Called " + oldRoad);
		//Remove Road GameObject
		RemoveRoad (oldRoad);
		//Break Node Connections to eachother.
		//Find Edge
		for(int i = 0;i<graphEdges.Count;i++){
			CiDyEdge edge = graphEdges[i];
			if(edge.name == oldRoad){
				//This is the Edge that we want to disconnect the nodes on.
				edge.SeperateEdge();
				//Remove Edge
				graphEdges.RemoveAt(i);
				//End For loop there is only one. :)
				break;
			}
		}
	}

	//Compares if the vectors are the same or so close they might as well be.
	public bool SameVector3s(Vector3 v1, Vector3 v2){
		if(Mathf.Approximately(v1.x,v2.x)){
			//Same X Value
			if(Mathf.Approximately(v1.y,v2.y)){
				//Same Y Value
				if(Mathf.Approximately(v1.z,v2.z)){
					//Same Z Value
					//They have the same X,Y,Z axis floats they are the Same Vector3
					return true;
				} else {
					//Not the Same Z axis they are different
					return false;
				}
			} else {
				//Not the Same Y Axis they are Different
				return false;
			}
		}
		//Not the Same X Axis they are Different
		return false;
	}

	//Test if Vector3 is not too close to any exsiting Nodes in the Graph
	public bool NodePlacement(Vector3 hitPoint){
		//Iterate through Master Graph and Test each node Position distance to hitPoint.
		for(int i = 0;i<masterGraph.Count;i++){
			//Update current Node for Testing
			CiDyNode graphNode = masterGraph[i];
			//Grab dist
			float dist = Vector3.Distance(graphNode.position, hitPoint);
			if(dist <= nodeSpacing){
				//This node is too close to an exsiting node.
				Debug.LogWarning("Node "+graphNode.name+" "+graphNode.position+" Is too Close "+hitPoint);
				return false;
			}
		}
		//No nodes are too close to this point.
		return true;
	}

	//OverLoad Test for Moving a Specific Node to a new position
	public bool NodePlacement(ref CiDyNode moveNode,Vector3 hitPoint){
		//Run Angle Test on this Node if it has more than one adjacency Node in its list
		if(moveNode.adjacentNodes.Count >= 2){
			//This move could potential create invalid intersection angle between adjacent node edges. Run Node Adjacency Angle Test.
			if(NodeAngleTest(ref moveNode, hitPoint)){
				Debug.LogWarning("Node move Exceeds Intersection Angle limit");
				return false;
			}
		}
		//Iterate through Master Graph and Test each node Position distance to hitPoint.
		for(int i = 0;i<masterGraph.Count;i++){
			//Update current Node for Testing
			CiDyNode graphNode = masterGraph[i];
			//Do not test our moving Node as we want to be able to move within its node spacing just not any other nodes space.
			if(graphNode.name == moveNode.name){
				continue;
			}
			//Grab dist
			float dist = Vector3.Distance(graphNode.position, hitPoint);
			if(dist <= nodeSpacing){
				//This node is too close to an exsiting node.
				Debug.LogWarning("Node Is too Close to "+graphNode.name+" Dist: "+dist);
				return false;
			}
		}
		//No nodes are too close to this point.
		return true;
	}

	//This function will test if any adjacent nodes angles are exceeding Graph Interesection Angle limits.
	public bool NodeAngleTest(ref CiDyNode testNode, CiDyNode newNode){
		//We need to test the new nodes direction to all our current node directions for angle limits.
		for(int i = 0;i<testNode.adjacentNodes.Count;i++){
			CiDyNode adjNode = testNode.adjacentNodes[i];
			//Create curDirection
			Vector3 dir1 = (adjNode.position-testNode.position).normalized;
			//Create newNode desired direction
			Vector3 dir2 = (newNode.position-testNode.position).normalized;
			//Compare angle of exsiting direction to desired added direction
			float angle = Vector3.Angle(dir1,dir2);
			if(angle<intersectionAngleLimit){
				//This nodes adjacents are exceeding the graph interserction set limit.
				return true;
			}
		}
		//Now test if newNodes angles also need to be tested.
		if(newNode.adjacentNodes.Count >=1){
			//We need to test this nodes adjacent Nodes directions to the testNodes direction
			for(int i = 0;i<newNode.adjacentNodes.Count;i++){
				CiDyNode adjNode = newNode.adjacentNodes[i];
				Vector3 dir1 = (adjNode.position- newNode.position).normalized;
				Vector3 dir2 = (testNode.position - newNode.position).normalized;
				float angle = Vector3.Angle(dir1,dir2);
				if(angle<intersectionAngleLimit){
					//Failed Test
					return true;
				}
			}
		}
		//Passed Angle test.
		return false;
	}

	//This function will test if any adjacent nodes angles are exceeding Graph Interesection Angle limits.
	public bool NodeAngleTest(ref CiDyNode testNode, Vector3 newPos){
		//We need to test if any angles are too steep. All nodes adjacency list are clockwise pre sorted as nodes are added. simply iterate in pairs.
		//looping the last node pair.
		int lastOne = testNode.adjacentNodes.Count-1;
		if(lastOne > 0){
			for(int i = 0;i<testNode.adjacentNodes.Count;i++){
				CiDyNode node1 = testNode.adjacentNodes[i];
				if(node1.adjacentNodes.Count > 1){
					//Test this nodes possible angles as well.
					if(AngleTest(node1,testNode,newPos)){
						//Failed Test
						return true;
					}
				}
				CiDyNode node2;//Create referenced node the set data based on where we are in the iteration
				if(i == lastOne){
					node2 = testNode.adjacentNodes[0];
				} else {
					node2 = testNode.adjacentNodes[i+1];
				}
				//test paired angles.
				Vector3 dir1 = (node1.position-newPos).normalized;
				Vector3 dir2 = (node2.position-newPos).normalized;
				float angle = Vector3.Angle(dir1,dir2);
				//Debug.Log(angle);
				//If angle is less than intersectionAngleLimit then this is an invalid node setup.
				if(angle<intersectionAngleLimit){
					//This nodes adjacents are exceeding the graph interserction set limit.
					return true;
				}
			}
		}
		if(lastOne == 0){
			//Test node Does not have enough adj nodes for Angle Failure but we need to Make sure that the AdjNode doesnt have a failure either.
			//Test adjacent Nodes angles and skip over adj-testNode angle as if it is moved.
			if(testNode.adjacentNodes[0].adjacentNodes.Count > 1){
				//Possible Angle Test Fail
				if(AngleTest(testNode.adjacentNodes[0],testNode, newPos)){
					//This is a Failed Angle Test
					return true;
				}
			}
		}
		//Passed Angle test.
		return false;
	}

	bool AngleTest(CiDyNode testNode, CiDyNode oldNode, Vector3 newPos){
		//We need to see if the New direction will exceed angle limits with exisiting angles ignoring oldNode angle as it will be moved.
		for(int i =0 ;i<testNode.adjacentNodes.Count;i++){
			CiDyNode node1 = testNode.adjacentNodes[i];
			if(node1.name != oldNode.name){
				//test this direction against newPostion direction
				Vector3 dir1 = (node1.position-testNode.position).normalized;
				Vector3 dir2 = (newPos-testNode.position).normalized;
				float angle = Vector3.Angle(dir1,dir2);
				if(angle<intersectionAngleLimit){
					//this exceeds graph angle limit
					return true;
				}
			}
		}
		//Passed test
		return false;
	}

	//Test if desired Edge will intersect with any exsiting edges in the Graph.
	public bool EdgeIntersection(CiDyEdge newEdge){
		//Debug.Log ("Edge Intersection: " + newEdge.name);
		Vector3 intersection = new Vector3(0,0,0);
		Vector3 nA = newEdge.v1.position;
		Vector3 nB = newEdge.v2.position;
		//Iterate through the Graphs current Edges and test for Intersection between the Graph Edge and the New Edge
		for(int i =0;i<graphEdges.Count;i++){
			CiDyEdge graphEdge = graphEdges[i];
			Vector3 a = graphEdge.v1.position;
			Vector4 b = graphEdge.v2.position;
			//Check to See if any endPoints are Shared between the Edges
			if(SameVector3s(a, nA) || SameVector3s(a, nB) || SameVector3s(b, nA) || SameVector3s(b, nB)){
				//Debug.Log("Shared End Points");
				//These will return a false positive do not test them.
				continue;
			} else if(CiDyUtils.LineIntersection(graphEdge.v1.position,graphEdge.v2.position, newEdge.v1.position,newEdge.v2.position, ref intersection)){
				//Debug.Log("Found intersection With an Edge: "+graphEdge.name);
				//These edges have an Intersection.
				//These edges do intersect
				return true;
			}
		}
		//No intersection detected
		return false;
	}

	//Test if desired Edge will intersect with any exsiting edges in the Graph.(Overload version using reference Edge List)
	public bool EdgeIntersection(CiDyEdge newEdge, List<CiDyEdge> tmpEdges){
		Vector3 intersection = new Vector3(0,0,0);
		//Iterate through the Graphs current Edges and test for Intersection between the Graph Edge and the New Edge
		for(int i =0;i<tmpEdges.Count;i++){
			CiDyEdge graphEdge = tmpEdges[i];
			//Check to See if any endPoints are Shared between the Edges
			if(SameVector3s(graphEdge.v1.position, newEdge.v1.position) || SameVector3s(graphEdge.v1.position, newEdge.v2.position) || SameVector3s(graphEdge.v2.position, newEdge.v1.position) || SameVector3s(graphEdge.v2.position, newEdge.v2.position)){
				//Debug.Log("Shared End Points");
				//These will return a false positive do not test them.
				continue;
			} else if(CiDyUtils.LineIntersection(graphEdge.v1.position,graphEdge.v2.position, newEdge.v1.position,newEdge.v2.position, ref intersection)){
				//These edges have an Intersection.
				//These edges do intersect
				//Debug.Log("Intersection Edge Detected Graph edge = "+graphEdge.name+" & Test Edge = "+newEdge.name);
				return true;
			}
		}
		//No intersection detected
		return false;
	}

	public bool DuplicateEdge(CiDyEdge testEdge){
		//Iterate through graph edges and check name comparison for a duplicate
		for(int i = 0;i<graphEdges.Count;i++){
			CiDyEdge graphEdge = graphEdges[i];
			if(graphEdge.name == testEdge.name){
				//This is a dupliate
				return true;
			}
		}
		//No duplicate found
		return false;
	}

	public void ChangeNodeScale(float newScale){
		for(int i = 0;i<masterGraph.Count;i++){
			if(masterGraph[i] !=null){
				masterGraph[i].UpdateGraphicScale(newScale);
			}
		}
	}

	//This will iterate through the nodes and disable there Graphics/Collision Mesh
	public void DisableNodeGraphics(){
		//Debug.Log ("Disable Node Graphics");
		for(int i = 0;i<masterGraph.Count;i++){
			masterGraph[i].DisableGraphic();
		}
        //Update Node State
        activeNodes = false;
	}
	//This will iterate through the nodes and Enable there Graphics/Collision Mesh
	public void EnableNodeGraphics(){
		//Debug.Log ("Enable Node Graphics");
		for(int i = 0;i<masterGraph.Count;i++){
			masterGraph[i].EnableGraphic();
		}
        //Update Node
        activeNodes = true;
	}
	//This will iterate through the cells and disable there Graphics/Collision Mesh
	public void DisableCellGraphics(){
        CiDyCell[] actualCellContainers = cellsHolder.GetComponentsInChildren<CiDyCell>();
        if (actualCellContainers.Length != cells.Count)
        {
            //Find Re grab them.
            cells = actualCellContainers.ToList();
        }
        for (int i = 0;i<cells.Count;i++){
			cells[i].DisableGraphic();
		}
        //Update Bool
        activeCells = false;
	}
	//This will iterate through the cells and Enable there Graphics/Collision Mesh
	public void EnableCellGraphics(){
        CiDyCell[] actualCellContainers = cellsHolder.GetComponentsInChildren<CiDyCell>();
        if (actualCellContainers.Length != cells.Count) {
            //Find Re grab them.
            cells = actualCellContainers.ToList();
        }
        for (int i = 0;i<cells.Count;i++){
			cells[i].EnableGraphic();
		}
        //Update Bool
        activeCells = true;
	}

    public bool ReturnTerrainPos(ref Vector3 proposedPos)
    {
        bool isOnTerrain = true;
        //Get Position Relative to Terrain Height if Applicable
        if (terrain != null)
        {
            //We have an active Terrain for this CiDy.
            //Get Stored Heights
            float[,] terrHeights = ReturnHeights();
            //Grab Area around Point in Terrain for Height Samples
            int mWidth = terrain.terrainData.heightmapWidth;
            int mHeight = terrain.terrainData.heightmapHeight;
            // we set an offset so that all the raising terrain is under this game object
            int xDetail = Mathf.RoundToInt((terrain.terrainData.heightmapWidth / terrain.terrainData.size.x) * 2);
            int zDetail = Mathf.RoundToInt((terrain.terrainData.heightmapHeight / terrain.terrainData.size.z) * 2);
            Vector3 tSize = terrain.terrainData.size;
            Vector3 terrPos = terrain.transform.position;
            Vector3 mapScale = terrain.terrainData.heightmapScale;
            //Iterate through Node Points
            //for (int i = 0; i < masterGraph.Count; i++)
            //{
            Vector3 nodePos = proposedPos;
            //Translate World Space to Terraian Space
            Vector3 coord = GetNormalizedPositionRelativeToTerrain(nodePos, terrPos,tSize);
            // get the position of the terrain heightmap where this Game Object is
            int posXInTerrain = (int)(coord.x * mWidth);
            int posYInTerrain = (int)(coord.z * mHeight);
            int offsetX = posXInTerrain - (xDetail / 2);
            int offsetZ = posYInTerrain - (zDetail / 2);

            //Make sure Offset is in Bounds
            if (offsetX < 0)
            {
                isOnTerrain = false;
                offsetX = 0;
                //Debug.Log("Fixed Offset X");
            }
            else
            {
                //Handle Positive Fix
                if ((offsetX + xDetail) > mWidth)
                {
                    isOnTerrain = false;
                    int diff = (mWidth - offsetX);
                    offsetX -= (mWidth - diff);
                    //Debug.Log("Diff: " + diff + " Width: " + mWidth);
                }
            }
            if (offsetZ < 0)
            {
                isOnTerrain = false;
                offsetZ = 0;
                //Debug.Log("Fixed Offset Z");
            }
            else
            {
                if ((offsetZ + zDetail) > mHeight)
                {
                    isOnTerrain = false;
                    int diff = (mHeight - offsetZ);
                    offsetZ -= (mHeight - diff);
                    //Debug.Log("Diff: " + diff + " Height: " + mHeight);
                }
            }
            // get the grass map of the terrain under this game object
            float[,] map = GetHeightsFromArray(offsetX, offsetZ, xDetail, zDetail, terrHeights);
            //Iterate through Map Points and Grab Closest Points Terrain Height Sample.
            for (int x = 0; x < xDetail; x++)
            {
                for (int y = 0; y < xDetail; y++)
                {
                    //Its original Height
                    float oHeight = map[x, y];
                    //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                    //float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * (offsetX + y));
                    //float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * (offsetZ + x));
                    float pY = (oHeight * tSize.y) + (terrPos.y / mapScale.y);//((tSize.y/oHeight) + terrPos.y + mapScale.y);
                    //Vector3 p0 = new Vector3(pX, pY, pZ);
                    //masterGraph[i].MoveNode(new Vector3(nodePos.x, pY + 0.1f, nodePos.z));
                    proposedPos = new Vector3(nodePos.x, pY + 0.1f, nodePos.z);
                }
            }
        }

        return isOnTerrain;
    }
	//Need Position for newNode and Desired Scale of Transform)
	public CiDyNode NewMasterNode(Vector3 position, float scale){
		//Debug.Log ("New Master Node"+position+" scale "+scale);
		//Is This place in the Graph Occupied?
		GameObject worldRep = (GameObject)Instantiate(nodePrefab, position, Quaternion.identity);
		worldRep.name = ("V"+nodeCount);
        //CiDyNode newNode = new CiDyNode("V"+nodeCount, position, this, nodeCount,worldRep);
        CiDyNode newNode = ScriptableObject.CreateInstance<CiDyNode>();
        newNode.Init(worldRep.name,position,this,nodeCount,worldRep);
		//Debug.Log("NewNode: "+newNode.name+" MasterGraph[int]: "+newNode.nodeNumber);
		//subNodes.Add (newNode);
		masterGraph.Add (newNode);
		newNode.nodeObject.transform.parent = nodeHolder.transform;
		newNode.UpdateGraphicScale (scale);
		//Set Nodes Tag on its Sphere collider.
		newNode.graphicTransform.gameObject.tag = nodeTag;
		newNode.graphicTransform.gameObject.layer = LayerMask.NameToLayer(nodeTag);
		//Debug.Log ("Graph is Adding Master Node "+newNode.name+" ndCount: "+nodeCount);
		this.nodeCount++;
		/*Undo.IncrementCurrentGroup();
		Undo.RegisterCreatedObjectUndo (worldRep, "Create Node");*/
		return newNode;
	}

    //Overload
    //Need Position for newNode and Desired Scale of Transform)
    public CiDyNode NewMasterNode(Vector3 position, float scale, CiDyNode.Hierarchy newHierarchy)
    {
        //Debug.Log ("New Master Node"+position+" scale "+scale);
        //Is This place in the Graph Occupied?
        GameObject worldRep = (GameObject)Instantiate(nodePrefab, position, Quaternion.identity);
        worldRep.name = ("V" + nodeCount);
        //CiDyNode newNode = new CiDyNode("V"+nodeCount, position, this, nodeCount,worldRep);
        CiDyNode newNode = ScriptableObject.CreateInstance<CiDyNode>();
        newNode.Init(worldRep.name, position, this, nodeCount, worldRep, newHierarchy);
        //Debug.Log("NewNode: "+newNode.name+" MasterGraph[int]: "+newNode.nodeNumber);
        //subNodes.Add (newNode);
        masterGraph.Add(newNode);
        newNode.nodeObject.transform.parent = nodeHolder.transform;
        newNode.UpdateGraphicScale(scale);
        //Set Nodes Tag on its Sphere collider.
        newNode.graphicTransform.gameObject.tag = nodeTag;
        newNode.graphicTransform.gameObject.layer = LayerMask.NameToLayer(nodeTag);
        //Debug.Log ("Graph is Adding Master Node "+newNode.name+" ndCount: "+nodeCount);
        this.nodeCount++;
        /*Undo.IncrementCurrentGroup();
		Undo.RegisterCreatedObjectUndo (worldRep, "Create Node");*/
        return newNode;
    }

    public CiDyNode NewNode(string newName,Vector3 position, int nodeCount2, GameObject worldRep){
		Debug.Log ("New Node " + newName + " pos: " + position + " nodeCount: " + nodeCount2 + " WorldRep: " + worldRep.name);
        //CiDyNode newNode = new CiDyNode(newName,position, this, nodeCount2, worldRep);
        CiDyNode newNode = ScriptableObject.CreateInstance<CiDyNode>();
        newNode.Init(newName,position,this,nodeCount2,worldRep);
		//subNodes.Add (newNode);
		//nodeCount2++;
		//masterGraph.Add (newNode);
		return newNode;
	}

	public CiDyNode NewNode(string newName,Vector3 position, int newCount){
        //Debug.Log ("New Node " + newName + " pos: " + position + " nodeCount: " + nodeCount2);
        //CiDyNode newNode = new CiDyNode(newName,position, this, nodeCount2);
        CiDyNode newNode = ScriptableObject.CreateInstance<CiDyNode>();
        newNode.Init(newName,position,this,newCount);
		//subNodes.Add (newNode);
		//nodeCount2++;
		//masterGraph.Add (newNode);
		return newNode;
	}

	//Returns true if a connection between the nodes is established. NODE A MUST Already Apart of the Graph.
	public bool ConnectNodes(CiDyNode nodeA, CiDyNode nodeB, float roadWidth, int roadSegmentLength, int flattenAmount){
		//Debug.Log ("Connect Nodes " + nodeA.name + "-" + nodeB.name + " roadWidth = " + roadWidth+" FlattenAmount: "+flattenAmount);
		//Create tmp Edge for Intersection Testing.
		CiDyEdge tmpEdge = new CiDyEdge(nodeA, nodeB);
		//Does this Edge Already Exist?
		if(DuplicateEdge(tmpEdge)){
			//Debug.Log("Duplicate Edge!");
			return false;
		}
		//Simple Distance Test from point to point. If less than roadWidth this road cannot exist as it is too close
		float dist = Vector3.Distance (nodeA.position, nodeB.position);
		if(dist <= (roadWidth*2)){
			Debug.Log("Nodes are closer than roadWidth will Allow.");
			return false;
		}
		//Connect these nodes with an edge if it wont interfere with other nodes or edges.
		if(nodeA.adjacentNodes.Count>=1){
			//Perform angle Test for new node
			//Debug.Log("fired Test");
			if(NodeAngleTest(ref nodeA,nodeB)){
			    Debug.Log("Angle Test Failed, VA: "+nodeA.name+" VB: "+nodeB.name);
				return false;
			}
		}
		//Passed Angle Tests
		//Debug.Log("Now Test Edge Intersection");
		//Test graph for acceptable new Edge
		if(EdgeIntersection(tmpEdge)){
            Debug.Log("Edge Intersects another Edge");
			return false;
		}
		//We Can Make this Connection :)
		AddEdge(tmpEdge);//Add Edge to Graph.
		//Create the Road Points so the Nodes can Calculate there Intersections.
		//Plot Path from A-B.
		//List<Vector3> newPath = new List<Vector3> ();//PlotPath(nodeA,nodeB);
        Vector3[] newPath = new Vector3[4];//Plot Path(nodeA,nodeB);
		Vector3 cent = ((nodeA.position + nodeB.position))/2;
        /*Vector3 fwd=(nodeB.position-nodeA.position).normalized;
		Vector3 offset = nodeA.position + fwd * roadWidth/2;
		Vector3 offset2 = nodeB.position + -fwd * roadWidth/2;*/
        newPath[0] = nodeA.position;//newPath.Add (nodeA.position);
		newPath[1] = (nodeA.position+cent)/ 2;//newPath.Add ((nodeA.position+cent)/2);
        newPath[2] = (cent+nodeB.position) / 2;//newPath.Add ((cent+nodeB.position)/2);
        newPath[3] = nodeB.position;//newPath.Add (nodeB.position);
		//Create the GameObject that the CiDyRoadComponent Will be on.
		GameObject newObject = new GameObject (tmpEdge.name);
		/*Undo.IncrementCurrentGroup ();
		Undo.RegisterCreatedObjectUndo (newObject, "Create Road");*/
		newObject.transform.parent = roadHolder.transform;
		//Set to Road Layer and Tag
		newObject.tag = roadTag;
		newObject.layer = LayerMask.NameToLayer(roadTag);
		//Create CiDyRoad and Set its basic information(BSpline Path and RoadWidth.
		CiDyRoad newRoad = newObject.AddComponent<CiDyRoad> ();
		newRoad.InitilizeRoad(newPath,roadWidth,roadSegmentLength, flattenAmount,nodeA,nodeB, newObject, this, false);
        //Set Static Game Object
        newObject.isStatic = true;
		//Add Road to Graph.
		AddRoad (newObject);
        //If we are here then we have made a Connection :)
        //Debug.Log ("Made Connection");
        return true;
	}

    //When User is Creating A Bezier Road in the Editor.
    public void AddUserRoadPoints(Vector3 newPoint, int segmentLength) {
        userDefinedRoadPnts.Add(newPoint);
        //Update Bezier output.
        userDefinedRoad = CiDyUtils.CreateBezier(userDefinedRoadPnts, segmentLength);
    }

    public void ClearUserDefinedRoadPoints() {
        if (userDefinedRoadPnts.Count > 0) {
            userDefinedRoadPnts.Clear();
            userDefinedRoad.Clear();
        }
    }

    //First and Last Knot will be turned into a Node in the MasterGraph
    public void CreateRoadFromKnots(CiDyNode nodeA, Vector3[] knots, float roadWidth, float nodeScale, int roadSegmentLength, int flattenAmount){
		Debug.Log ("Creating Knots");
		CiDyNode nodeB = NewMasterNode(knots[knots.Length-1],nodeScale);
		nodeA.AddNode (nodeB);
		nodeB.AddNode (nodeA);
		CiDyEdge tmpEdge = new CiDyEdge (nodeA, nodeB);
		//Create the GameObject that the CiDyRoadComponent Will be on.
		GameObject newObject = new GameObject (tmpEdge.name);
		newObject.transform.parent = roadHolder.transform;
		//Set to Road Layer and Tag
		newObject.tag = roadTag;
		newObject.layer = LayerMask.NameToLayer(roadTag);
		//Create CiDyRoad and Set its basic information(BSpline Path and RoadWidth.
		CiDyRoad newRoad = newObject.AddComponent<CiDyRoad> ();
		newRoad.InitilizeRoad(knots,roadWidth,roadSegmentLength, flattenAmount,nodeA,nodeB, newObject, this, false);
		//Add Road to Graph.
		AddRoad (newObject);
	}

	//First and Last Knot will be turned into a Node in the MasterGraph
	public bool CreateRoadFromKnots(CiDyNode nodeA, CiDyNode nodeB, Vector3[] knots, float roadWidth, float nodeScale, int roadSegmentLength, int flattenAmount){
		//Debug.Log ("Creating Knots");
		//Create tmp Edge for Intersection Testing.
		CiDyEdge tmpEdge = new CiDyEdge(nodeA, nodeB);
		//Does this Edge Already Exist?
		if(DuplicateEdge(tmpEdge)){
			Debug.Log("Duplicate Edge!");
			return false;
		}
		//We Can Make this Connection :)
		AddEdge(tmpEdge);//Add Edge to Graph.
		//Create the GameObject that the CiDyRoadComponent Will be on.
		GameObject newObject = new GameObject (tmpEdge.name);
		newObject.transform.parent = roadHolder.transform;
		//Set to Road Layer and Tag
		newObject.tag = roadTag;
		newObject.layer = LayerMask.NameToLayer(roadTag);
		//Create CiDyRoad and Set its basic information(BSpline Path and RoadWidth.
		CiDyRoad newRoad = newObject.AddComponent<CiDyRoad> ();
		newRoad.InitilizeRoad(knots,roadWidth,roadSegmentLength, flattenAmount,nodeA,nodeB, newObject, this, false);
		//Add Road to Graph.
		AddRoad (newObject);
		return true;
	}


	//First and Last Knot will be turned into a Node in the MasterGraph
	public void CreateRoadFromKnots(Vector3[] knots, float roadWidth, float nodeScale, int roadSegmentLength, int flattenAmount){
        //Debug.Log ("Creating Knots");
        CiDyNode nodeA = masterGraph.Find(x => x.position == knots[0]);
        if (nodeA == null)
        {
            nodeA = NewMasterNode(knots[0], nodeScale);
        }

        CiDyNode nodeB = masterGraph.Find(x => x.position == knots[knots.Length-1]);
        if (nodeB == null)
        {
            nodeB = NewMasterNode(knots[knots.Length - 1], nodeScale);
        }
        //Add Nodes to eachother
		nodeA.AddNode (nodeB);
		nodeB.AddNode (nodeA);
		CiDyEdge tmpEdge = new CiDyEdge (nodeA, nodeB);
        //Does this Edge Already Exist?
        if (!AddEdge(tmpEdge))
        {
            Debug.Log("Duplicate Edge!");
            return;
        }
        //Create the GameObject that the CiDyRoadComponent Will be on.
        GameObject newObject = new GameObject (tmpEdge.name);
		newObject.transform.parent = roadHolder.transform;
		//Set to Road Layer and Tag
		newObject.tag = roadTag;
		newObject.layer = LayerMask.NameToLayer(roadTag);
		//Create CiDyRoad and Set its basic information(BSpline Path and RoadWidth.
		CiDyRoad newRoad = newObject.AddComponent<CiDyRoad> ();
		newRoad.InitilizeRoad(knots,roadWidth,roadSegmentLength, flattenAmount,nodeA,nodeB, newObject, this, true);
		//Add Road to Graph.
		AddRoad (newObject);
	}

    //Overload that takes Nodes for its Ends.
    //First and Last Knot will be turned into a Node in the MasterGraph
    public void CreateRoadFromKnots(Vector3[] knots, CiDyNode nodeA, CiDyNode nodeB, float roadWidth, float nodeScale, int roadSegmentLength, int flattenAmount)
    {
        //Add Nodes to eachother
        nodeA.AddNode(nodeB);
        nodeB.AddNode(nodeA);
        CiDyEdge tmpEdge = new CiDyEdge(nodeA, nodeB);
        //Does this Edge Already Exist?
        if (DuplicateEdge(tmpEdge))
        {
            Debug.Log("Duplicate Edge!");
            return;
        }
        AddEdge(tmpEdge);
        //Create the GameObject that the CiDyRoadComponent Will be on.
        GameObject newObject = new GameObject(tmpEdge.name);
        newObject.transform.parent = roadHolder.transform;
        //Set to Road Layer and Tag
        newObject.tag = roadTag;
        newObject.layer = LayerMask.NameToLayer(roadTag);
        //Create CiDyRoad and Set its basic information(BSpline Path and RoadWidth.
        CiDyRoad newRoad = newObject.AddComponent<CiDyRoad>();
        newRoad.InitilizeRoad(knots, roadWidth, roadSegmentLength, flattenAmount, nodeA, nodeB, newObject, this, false);
        //Add Road to Graph.
        AddRoad(newObject);
    }

    //This function will drop the Points to the Terrains Heights
    public void ContourPathToTerrain(ref List<Vector3> path){
        //Iterate through Terrain Stored Heights
        if (terrain != null)
        {
            //Debug.Log("Contour");
            //Get Stored Heights
            float[,] terrHeights = ReturnHeights();
            //Grab Area around Point in Terrain for Height Samples
            int mWidth = terrain.terrainData.heightmapWidth;
            int mHeight = terrain.terrainData.heightmapHeight;
            Vector3 tSize = terrain.terrainData.size;
            Vector3 terrPos = terrain.transform.position;
            Vector3 mapScale = terrain.terrainData.heightmapScale;
            // we set an offset so that all the raising terrain is under this game object
            int xDetail = Mathf.RoundToInt((mWidth / tSize.x) * 2);
            int zDetail = Mathf.RoundToInt((mHeight / tSize.z) * 2);
            //Iterate through Path Points and Adjust to its heights.
            for (int i = 0; i < path.Count; i++)
            {
                //Translate World Space to Terraian Space
                Vector3 coord = GetNormalizedPositionRelativeToTerrain(path[i],terrPos,tSize);
                // get the position of the terrain heightmap where this Game Object is
                int posXInTerrain = (int)(coord.x * mWidth);
                int posYInTerrain = (int)(coord.z * mHeight);
                int offsetX = posXInTerrain - (xDetail / 2);
                int offsetZ = posYInTerrain - (zDetail / 2);
                //Make sure Offset is in Bounds
                if (offsetX < 0)
                {
                    offsetX = 0;
                    //Debug.Log("Fixed Offset X");
                }
                else
                {
                    //Handle Positive Fix
                    if ((offsetX + xDetail) > mWidth)
                    {
                        int diff = (mWidth - offsetX);
                        offsetX -= (mWidth - diff);
                        //Debug.Log("Diff: " + diff + " Width: " + mWidth);
                    }
                }
                if (offsetZ < 0)
                {
                    offsetZ = 0;
                    //Debug.Log("Fixed Offset Z");
                }
                else
                {
                    if ((offsetZ + zDetail) > mHeight)
                    {
                        int diff = (mHeight - offsetZ);
                        offsetZ -= (mHeight - diff);
                        //Debug.Log("Diff: " + diff + " Height: " + mHeight);
                    }
                }
                // get the grass map of the terrain under this game object
                float[,] map = GetHeightsFromArray(offsetX,offsetZ, xDetail, zDetail, terrHeights);
                //Iterate through Map Points and Grab Closest Points Terrain Height Sample.
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    for (int y = 0; y < map.GetLength(1); y++)
                    {
                        //Its original Height
                        float oHeight = map[x, y];
                        //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                        float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * ((posXInTerrain - offsetX) + y));
                        float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * ((posYInTerrain - offsetZ) + x));
                        float pY = (oHeight * tSize.y) + (terrPos.y / mapScale.y);//((tSize.y/oHeight) + terrPos.y + mapScale.y);
                        Vector3 p0 = new Vector3(pX, pY, pZ);
                        path[i] = new Vector3(path[i].x, pY + 0.1f, path[i].z);
                        CiDyUtils.MarkPoint(p0, x + y);
                    }
                }

                /*Vector3 rayPos = path[i] + (Vector3.up * 1000);
                if (Physics.Raycast(rayPos, Vector3.down, out hit, Mathf.Infinity, roadMask))
                {
                    path[i] = new Vector3(path[i].x, hit.point.y, path[i].z);
                }*/
            }
        }
        else
        {
            //No Terrain to Match Path To.
            return;
        }
    }

    //This function will drop the Points to the Terrains Heights
    public void ContourPathToTerrain(ref Vector3[] path)
    {
        if (terrain == null) {
            //No Terrain to Match Path To.
            return;
        }
        //Iterate through Terrain Stored Heights
        if (terrain != null)
        {
            //Debug.Log("Contour");
            //Get Stored Heights
            float[,] terrHeights = ReturnHeights();
            //Grab Area around Point in Terrain for Height Samples
            int mWidth = terrain.terrainData.heightmapWidth;
            int mHeight = terrain.terrainData.heightmapHeight;
            // we set an offset so that all the raising terrain is under this game object
            int xDetail = Mathf.RoundToInt((terrain.terrainData.heightmapWidth / terrain.terrainData.size.x) * 2);
            int zDetail = Mathf.RoundToInt((terrain.terrainData.heightmapHeight / terrain.terrainData.size.z) * 2);
            Vector3 tSize = terrain.terrainData.size;
            Vector3 terrPos = terrain.transform.position;
            Vector3 mapScale = terrain.terrainData.heightmapScale;
            //Iterate through Path Points and Adjust to its heights.
            for (int i = 0; i < path.Length; i++)
            {
                //Translate World Space to Terraian Space
                Vector3 coord = GetNormalizedPositionRelativeToTerrain(path[i],terrPos,tSize);
                // get the position of the terrain heightmap where this Game Object is
                int posXInTerrain = (int)(coord.x * mWidth);
                int posYInTerrain = (int)(coord.z * mHeight);
                int offsetX = posXInTerrain - (xDetail / 2);
                int offsetZ = posYInTerrain - (zDetail / 2);

                //Make sure Offset is in Bounds
                if (offsetX < 0)
                {
                    offsetX = 0;
                    //Debug.Log("Fixed Offset X");
                }
                else
                {
                    //Handle Positive Fix
                    if ((offsetX + xDetail) > mWidth)
                    {
                        int diff = (mWidth - offsetX);
                        offsetX -= (mWidth - diff);
                        //Debug.Log("Diff: " + diff + " Width: " + mWidth);
                    }
                }
                if (offsetZ < 0)
                {
                    offsetZ = 0;
                    //Debug.Log("Fixed Offset Z");
                }
                else
                {
                    if ((offsetZ + zDetail) > mHeight)
                    {
                        int diff = (mHeight - offsetZ);
                        offsetZ -= (mHeight - diff);
                        //Debug.Log("Diff: " + diff + " Height: " + mHeight);
                    }
                }
                //Debug.Log("Terrain Width: " + t.terrainData.detailWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
                // get the grass map of the terrain under this game object
                //int[,] map = t.terrainData.GetDetailLayer(offsetX, offsetZ, xDetail, zDetail, 0);

                //Debug.Log("Contour Path");
                // get the grass map of the terrain under this game object
                float[,] map = GetHeightsFromArray(offsetX, offsetZ, xDetail, zDetail, terrHeights);
                //Iterate through Map Points and Grab Closest Points Terrain Height Sample.
                for (int x = 0; x < xDetail; x++)
                {
                    for (int y = 0; y < zDetail; y++)
                    {
                        //Its original Height
                        float oHeight = map[x, y];
                        //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                        //float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * ((posXInTerrain - offsetX) + y));
                        //float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * ((posYInTerrain - offsetZ) + x));
                        float pY = (oHeight * tSize.y) + (terrPos.y / mapScale.y);//((tSize.y/oHeight) + terrPos.y + mapScale.y);
                        //Vector3 p0 = new Vector3(pX, pY, pZ);
                        path[i] = new Vector3(path[i].x, pY+0.1f, path[i].z);
                    }
                }
            }
        }
    }

    //Terrain Functions
    public Terrain terrain;
    [SerializeField]
    private SerializedTreeInstance[] terrainTrees;// = terrain.terrainData.treeInstances;
    [SerializeField]
    private TerrainDetail[] terrainGrass;// = terrain.terrainData.detailPrototypes;
    public int hmWidth;
	public int hmHeight;
	public int radius = 11;
	public float zBuffer = 0.08f;
	
	public float edgeOffset = 4f;
	public float smoothBorder = 6f;

	public float blendProgress = 0f;//Used to Show the User How much time remains for blending process

    //Update City Nodes Height Positions & Roads to new Stored Terrain Heights.
    public IEnumerator UpdateCityGraph() {

        if (masterGraph.Count < 1 || terrain == null) {
            //Stop here as there cannot be any graph to update.
            yield break;
        }

         #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Updating City Network to Terrain:", " Preping: ",0f);
        #endif

        curProblems = (masterGraph.Count + roads.Count);
        totalProblems = curProblems;

        //Get Stored Heights
        float[,] terrHeights = ReturnHeights();
        //Grab Area around Point in Terrain for Height Samples
        int mWidth = terrain.terrainData.heightmapWidth;
        int mHeight = terrain.terrainData.heightmapHeight;
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((terrain.terrainData.heightmapWidth / terrain.terrainData.size.x) * 2);
        int zDetail = Mathf.RoundToInt((terrain.terrainData.heightmapHeight / terrain.terrainData.size.z) * 2);
        Vector3 tSize = terrain.terrainData.size;
        Vector3 terrPos = terrain.transform.position;
        Vector3 mapScale = terrain.terrainData.heightmapScale;
        //Iterate through Node Points
        for (int i = 0; i < masterGraph.Count; i++)
        {
            Vector3 nodePos = masterGraph[i].position;
            //Translate World Space to Terraian Space
            Vector3 coord = GetNormalizedPositionRelativeToTerrain(nodePos,terrPos,tSize);
            // get the position of the terrain heightmap where this Game Object is
            int posXInTerrain = (int)(coord.x * mWidth);
            int posYInTerrain = (int)(coord.z * mHeight);
            int offset = xDetail / 2;
            // get the grass map of the terrain under this game object
            float[,] map = GetHeightsFromArray(posXInTerrain - offset, posYInTerrain - offset, xDetail, zDetail, terrHeights);
            //Iterate through Map Points and Grab Closest Points Terrain Height Sample.
            for (int x = 0; x < xDetail; x++)
            {
                for (int y = 0; y < xDetail; y++)
                {
                    //Its original Height
                    float oHeight = map[x, y];
                    //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                    //float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * ((posXInTerrain - offset) + y));
                    //float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * ((posYInTerrain - offset) + x));
                    float pY = (oHeight * tSize.y) + (terrPos.y / mapScale.y);//((tSize.y/oHeight) + terrPos.y + mapScale.y);
                    //Vector3 p0 = new Vector3(pX, pY, pZ);
                    masterGraph[i].MoveNode(new Vector3(nodePos.x, pY + 0.1f, nodePos.z));
                }
            }
            curProblems--;
            #if UNITY_EDITOR
                float percentage = Mathf.Round(((float)i / (float)masterGraph.Count) * 100);
                UnityEditor.EditorUtility.DisplayProgressBar("Updating City Network to Terrain:", " Shifting Intersections: " + percentage + "% " + "(" + i.ToString() + "/" + masterGraph.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
            #endif
        }
        if (roads.Count > 0)
        {
            for (int i = 0; i < roads.Count; i++) {
                CiDyRoad road = roads[i].GetComponent<CiDyRoad>();
                road.ReplotRoad(road.cpPoints);//Raw Path must be the Control Points
                //Update Graph if needed
                if (cells.Count > 0)
                {
                    UpdateRoadCell(road);
                }
                curProblems--;
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i / (float)roads.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Updating City Network to Terrain:", "Replotting Roads: " + percentage + "% " + "(" + i.ToString() + "/" + roads.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }

        //End Progress Bar
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Updating City Network to Terrain:", "Finalizing: ", (1.0f - ((float)curProblems / (float)totalProblems)));
        #endif
        yield return null;
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
        #endif
        yield break;
    }

    //Generate All Population Logic for All of our Cities Cells.
    public IEnumerator GenerateSideWalkPopulation()
    {
        /*if (masterGraph.Count < 1 || terrain == null)
        {
            //Stop here as there cannot be any graph to update.
            yield break;
        }

        #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Updating City Population:", " Preping: ", 0f);
        #endif

        List<List<Vector3>> pedestrianPaths = new List<List<Vector3>>(0);
        List<float> pathRadius = new List<float>(0);
        curProblems = cells.Count;
        totalProblems = curProblems;//All Cells are Total Problems.
        //Iterate through the Cell and Generate Its Population Loop Spline.
        Debug.Log("Iterated through the Cell and Generated its SideWalk Loops.");
        //Debug.Log("Starting Blend Cells");
        if (cells.Count > 0)
        {
            Debug.Log("Checking Cells");
            for (int i = 0; i < cells.Count; i++)
            {
                //Blend Sidewalks
                GameObject[] sideWalks = cells[i].sideWalkHolders;
                //Iterate through this cells sideWalks
                for (int j = 0; j < sideWalks.Length; j++)
                {
                    if (sideWalks[j] == null)
                    {
                        continue;
                    }

                    //Get Road LayerMask
                    int storedLayer = sideWalks[j].layer;
                    sideWalks[j].layer = LayerMask.NameToLayer("Road");
                    Transform sideWalk = sideWalks[j].transform;
                    //Grab Mesh from Holder and Plot Walking Path in the Center of the Path.
                    if (sideWalks[j].GetComponent<MeshFilter>().sharedMesh != null)
                    {

                        //Send Zero for Layermask as we will be creating a collider for the testing phase. (it will have its layer)
                        //BlendSideWalk(terrain, cells[i].gameObject.transform, cells[i].interiorPoints.ToArray(), cells[i].sideWalkWidth, roadMask2, cells[i].sideWalkHeight);
                        List<Vector3> midPoints = new List<Vector3>(0);
                        GeneratePopulation(ref midPoints, terrain, cells[i].gameObject.transform, cells[i].interiorPoints.ToArray(), cells[i].sideWalkWidth, roadMask2, cells[i].sideWalkHeight);
                        pedestrianPaths.Add(midPoints);
                        pathRadius.Add(cells[i].sideWalkWidth);
                    }
                    //Return SideWalk back now that we have completed its Test
                    //sideWalks[j].layer = storedLayer;
                }
                curProblems--;
                Debug.Log("Updated Problems: "+curProblems);
                #if UNITY_EDITOR
                float percentage = Mathf.Round(((float)i / (float)cells.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Generating Population:", " Cells: " + percentage + "% " + "(" + i.ToString() + "/" + cells.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }
        Debug.Log("End Clear Progress");
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        if (populationInstance == null)
        {
            //Create a Population Instance
            GameObject mngr = new GameObject("Pedestrian Manager");
            populationInstance = mngr.AddComponent<CiDyPopulationManager>();
        }
        if (populationInstance != null)
        {
            Debug.Log("Got Population Instance");
            //Initialize Population
            populationInstance.InitilizePopulation(pedestrianPaths, pathRadius);
        }*/
        yield break;
    }

    //This Function will Update All the Road Materials and Intersection Materials.
    //Update City Nodes Height Positions & Roads to new Stored Terrain Heights.
    public IEnumerator UpdateAllMaterials() {
        //Debug.Log("Update All Materials");
        if (masterGraph.Count < 1) {
            //Stop here as there cannot be any graph to update.
            yield break;
        }

         #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Replacing City Materials:", " Preping: ",0f);
        #endif

        curProblems = (masterGraph.Count + roads.Count);
        totalProblems = curProblems;
        //Iterate through Node Points
        for (int i = 0; i < masterGraph.Count; i++)
        {
            masterGraph[i].ReplaceMaterials();
            
            curProblems--;
            #if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Replacing City Materials:", " Replacing Intersections: ", (1.0f - ((float)curProblems / (float)totalProblems)));
            #endif
        }
        if (roads.Count > 0)
        {
            for (int i = 0; i < roads.Count; i++) {
                CiDyRoad road = roads[i].GetComponent<CiDyRoad>();
                road.ChangeRoadMaterial();
                /*road.ReplotRoad(road.cpPoints);//Raw Path must be the Control Points
                //Update Graph if needed
                if (cells.Count > 0)
                {
                    UpdateRoadCell(road);
                }*/
                curProblems--;
                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("Replacing City Materials:", "Replacing Roads: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }

        //End Progress Bar
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Replacing City Materials:", "Finalizing: ", (1.0f - ((float)curProblems / (float)totalProblems)));
        #endif
        yield return null;
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
        #endif
        yield break;
    }


    public IEnumerator BlendTerrain(){
		Debug.Log ("Called Blend Coroutine");
		if(terrain != null){
			if(originalHeights.Length == 0){
				Debug.Log("Graph doesn't have any stored Heights. Storing Heights Before Blend");
				GrabOriginalHeights();
                /*//Grab Vegitation
                if (terrainTrees.Length == 0 && terrainGrass == null) {
                    GrabTerrainVegitation();
                }*/
			}
			//Debug.Log ("Blend Terrain");
			blendProgress = 0;
			//Restore Original Heights first as we may have changed something already
			RestoreOriginalTerrainHeights();
			int totalProblems = (masterGraph.Count+roads.Count+cells.Count);
			int curProblems = totalProblems;
            //Blend Cell SideWalks
            if(cells.Count > 0){
				for(int i = 0;i<cells.Count;i++){
					//Blend Cells Building Lot Footprints
					for(int j = 0;j<cells[i].cidyLots.Count;j++){
						//List<Vector3> lot = cells[i].lots[j].vectorList;
						if(!cells[i].cidyLots[j].empty){
							List<Vector3> lot = cells[i].cidyLots[j].lotPrint;
							BlendPoly(lot,radius,zBuffer,edgeOffset,smoothBorder);
						}
					}
					//Blend Sidewalks
					GameObject[] sideWalks = cells[i].sideWalkHolders;
					//Iterate through this cells sideWalks
					for(int j = 0;j<sideWalks.Length;j++){
						//Grab Mesh from Holder and Blend to terrain
						Mesh sideWalkMesh = sideWalks[j].GetComponent<MeshFilter>().sharedMesh;
						if(sideWalkMesh != null){
							BlendSideWalk(cells[i].transform.position,sideWalkMesh,cells[i].sideWalkWidth,radius,zBuffer,edgeOffset,smoothBorder);
						}
						//Now grab corner Mesh
						sideWalkMesh = sideWalks[j].transform.Find(("CornerWalk"+j)).GetComponent<MeshFilter>().sharedMesh;
						if(sideWalkMesh!=null){
							BlendMesh(sideWalkMesh,cells[i].transform.position,radius,zBuffer,edgeOffset,smoothBorder,true);
						}
					}
                    curProblems--;
					blendProgress = Mathf.InverseLerp(totalProblems,0,curProblems);
					//designer.blendProgress = Mathf.Round(blendProgress*100);
					yield return new WaitForSeconds(0);
				}
			}
            if (roads.Count > 0)
            {
                //Iterate through Roads and Blend Terrain to Roads
                for (int i = 0; i < roads.Count; i++)
                {
                    //Grab RoadMesh
                    Mesh roadMesh = roads[i].GetComponent<MeshFilter>().sharedMesh;
                    //Call Blend for this RoadMesh
                    BlendRoad(roadMesh, roads[i].GetComponent<CiDyRoad>().width, radius, zBuffer, edgeOffset, smoothBorder);
                    curProblems--;
                    blendProgress = Mathf.InverseLerp(totalProblems, 0, curProblems);
                    //designer.blendProgress = Mathf.Round(blendProgress * 100);
                    yield return new WaitForSeconds(0);
                }
            }
            if(masterGraph.Count>1){
				//Flatten Node Intersections First.
				for(int i = 0;i<masterGraph.Count;i++){
					Mesh nodeMesh = masterGraph[i].mFilter.sharedMesh;
					BlendMesh(nodeMesh, masterGraph[i].position, radius, zBuffer, edgeOffset, smoothBorder,true);
					curProblems--;
					blendProgress = Mathf.InverseLerp(totalProblems,0,curProblems);
					//designer.blendProgress = Mathf.Round(blendProgress*100);
					yield return new WaitForSeconds(0);
				}
			}
            yield return null;
		}
	}

	public IEnumerator BlendCells(){
        //Debug.Log("Starting Blend Cells");
		if(cells.Count > 0){
			for(int i = 0;i<cells.Count;i++){
				//Blend Cells Building Lot Footprints
				for(int j = 0;j<cells[i].cidyLots.Count;j++){
					if(!cells[i].cidyLots[j].empty){
						List<Vector3> lot = cells[i].cidyLots[j].lotPrint;
                        //Send Zero for Layermask as we will be creating a collider for the testing phase. (it will have its layer)
                        BlendPoly(lot.ToArray(), 12, false, 0);
                    }
				}
				//Blend Sidewalks
				GameObject[] sideWalks = cells[i].sideWalkHolders;
				//Iterate through this cells sideWalks
				for(int j = 0;j<sideWalks.Length;j++){
                    if (sideWalks[j] == null) {
                        continue;
                    }
                    
                    //Get Road LayerMask
                    int storedLayer = sideWalks[j].layer;
                    sideWalks[j].layer = LayerMask.NameToLayer("Road");
                    Transform sideWalk = sideWalks[j].transform;
                    //Grab Mesh from Holder and Blend to terrain
                    Mesh sideWalkMesh = sideWalks[j].GetComponent<MeshFilter>().sharedMesh;
					if(sideWalkMesh != null){
                        
                        //Send Zero for Layermask as we will be creating a collider for the testing phase. (it will have its layer)
                        //BlendPoly(verts, (int)cells[i].sideWalkWidth, false, roadMask2);
                        BlendSideWalk(terrain, cells[i].gameObject.transform, cells[i].interiorPoints.ToArray(), cells[i].sideWalkWidth, roadMask2,cells[i].sideWalkHeight);
                    }
                    //Return SideWalk back now that we have completed its Test
                    //sideWalks[j].layer = storedLayer;
                }
                //Turn On Terrain Collider for Height Check
                /*TerrainCollider terrainCollider = terrain.gameObject.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                    terrainCollider.enabled = true;
                //Turn off Collider
                if (terrainCollider != null)
                    terrainCollider.enabled = false;
                    */
                curProblems--;
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i/(float)cells.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Blending Terrain:", " Cells: "+percentage+"% "+"("+i.ToString() + "/" + cells.Count+")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
			}
		}
        yield break;
	}

    public IEnumerator CreateFoundation(){
        //Debug.Log("Starting Blend Cells");
		if(cells.Count > 0){
			for(int i = 0;i<cells.Count;i++){
                //Turn On Terrain Collider for Height Check
                TerrainCollider terrainCollider = terrain.gameObject.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                    terrainCollider.enabled = true;
                //Now that we have Completed Blending Terrain for this Cell. Lets extrude Foundations under Buildings that need it.
                for (int j = 0; j < cells[i].cidyLots.Count; j++)
                {
                    if (!cells[i].cidyLots[j].empty)
                    {
                        //Debug.Log("Check Lot: ");
                        //Grab Building or Buildings of this lot
                        GameObject[] buildings = cells[i].cidyLots[j].buildings;
                        if (buildings.Length > 0)
                        {
                            bool used = cells[i].usePrefabBuildings;
                            //Extrude Foundations for Buildings that have terrain Below there Foundation by threshold 
                            for (int k = 0; k < buildings.Length; k++)
                            {
                                CheckFoundation(buildings[k], cells[i].cidyLots[j].lotPrint, 0.0618f, used);
                            }
                        }
                    }
                }
                //Turn off Collider
                if (terrainCollider != null)
                    terrainCollider.enabled = false;

                curProblems--;
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i/(float)cells.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Blending Terrain:", " Cells: "+percentage+"% "+"("+i.ToString() + "/" + cells.Count+")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
			}
		}
        yield break;
	}

	public IEnumerator BlendRoads(){
        ClearSphereSpots();
		//Iterate through Roads and Blend Terrain to Roads
		for(int i = 0;i<roads.Count;i++){
			//Grab RoadMesh
			//Mesh roadMesh = roads[i].GetComponent<MeshFilter>().sharedMesh;
            //Call Blend for this RoadMesh
            //BlendRoad(roadMesh, roads[i].GetComponent<CiDyRoad>().width,radius,zBuffer,edgeOffset,smoothBorder);
            BlendRoad(terrain, roads[i], Mathf.RoundToInt(smoothBorder));
			curProblems--;
            #if UNITY_EDITOR
                float percentage = Mathf.Round(((float)i/(float)roads.Count) * 100);
                UnityEditor.EditorUtility.DisplayProgressBar("Blending Terrain:", " Roads: "+ percentage + "% " + "(" + i.ToString() + "/" + roads.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
            #endif
		}
        yield break;
	}

	public IEnumerator BlendNodes(){
        Vector3 terrPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        Vector3 mapScale = terrain.terrainData.heightmapScale;
        if (masterGraph.Count>1){
            //Flatten Node Intersections First.
            for (int i = 0;i<masterGraph.Count;i++){
                //Grab Mesh Data
                Mesh nodeMesh = masterGraph[i].mFilter.sharedMesh;
                //Grab Intersection Layer Before Test
                int storedLayer = masterGraph[i].intersection.layer;
                int roadRadius = (int)(masterGraph[i].connectedRoads[0].width);
                //Check if Node is a Connecting Road or a Standard Intersection or Culdesac
                if (masterGraph[i].type == CiDyNode.IntersectionType.continuedSection)
                {
                    //Debug.Log("Node Set to Continued Section: "+masterGraph[i].name);
                    //Remove Trees and Grass(Details that are under the Road Path)
                    //Make sure Road is in Road Layer( Just incase user somehow  changed it)
                    //Change Layer to Road(It should be road already, But just in case)
                    masterGraph[i].intersection.layer = LayerMask.NameToLayer("Road");
                    if (nodeMesh != null)
                    {
                        //Grab Mid Points of Road Mesh in World Space.
                        for (int j = 0; j < nodeMesh.vertices.Length - 2; j += 2)
                        {
                            //Send Point V0, V1, V2, V3
                            BlendTerrainOfRoad(terrPos, tSize, mapScale, nodeMesh.vertices[j]+masterGraph[i].position, nodeMesh.vertices[j + 1] + masterGraph[i].position, nodeMesh.vertices[j + 2] + masterGraph[i].position, nodeMesh.vertices[j + 3] + masterGraph[i].position, roadRadius, roadMask2, hmWidth,hmHeight);
                        }
                    }
                }
                else
                {
                    if (nodeMesh.vertices.Length > 0)
                    {
                        //Update its Layer for Testing
                        masterGraph[i].intersection.layer = LayerMask.NameToLayer("Road");
                        Vector3[] verts = new Vector3[nodeMesh.vertices.Length];
                        for (int j = 0; j < verts.Length; j++)
                        {
                            verts[j] = nodeMesh.vertices[j] + masterGraph[i].position;
                        }
                        //verts.Reverse();
                        //BlendMesh(nodeMesh, masterGraph[i].position, radius, zBuffer, edgeOffset, smoothBorder,true);
                        BlendPoly(verts, roadRadius, true, roadMask2);
                    }
                    //Return Layer to normal now that testing is completed
                    masterGraph[i].intersection.layer = storedLayer;
                }
                curProblems--;
				#if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i/(float)masterGraph.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Blending Terrain:", " Intersections: "+ percentage + "% " + "(" + i.ToString() + "/" + masterGraph.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
                /*float newTime = Time.realtimeSinceStartup;
                float stepTime = newTime - currentTime;
                currentTime = newTime;
                accumulatedTime += stepTime;
                if (accumulatedTime > (1f/2f))
                {
                    accumulatedTime = 0f;
                    yield return null;
                }*/
			}
		}
        yield break;
	}


    public void StartBlending() {
        StartCoroutine(UpdateTerrainDetails());
    }

    //float currentTime = 0;
    //float accumulatedTime = 0;

    //Thread Pool
    //List<Thread> allThreads = new List<Thread>(0);
    public GameObject prefab;

    public IEnumerator UpdateTerrainDetails() {
        //Update Terrain Details as the Graph Has Changed a Road or Added a new Road.
        if (terrain == null) {
            //End Coroutine
            yield break;
        }
        //Reset All Terrain Data before making Changes.
        RestoreOriginalTerrainHeights();
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Blending City to Terrain:", " Preping: ",0f);
        #endif
        //Get Current Time
        //currentTime = Time.realtimeSinceStartup;
        //accumulatedTime = 0;
        //Calculate Starting Blend Time
        CalculateBlendTime();
        //Grab Terrain
        //Before Performing TerrainHeight Test. Turn OFF Its Collider as we will use Raycasting on the Road.
        TerrainCollider terrainCollider = terrain.gameObject.GetComponent<TerrainCollider>();
        bool terrainColliderWasEnabled = terrainCollider.enabled;
        if (terrainCollider != null)
            terrainCollider.enabled = false;


        //Blend Cells
        //Thread blendCellThread = new Thread(() => { BlendCells(); });
        //blendCellThread.Start();//Start the Thread.
        yield return StartCoroutine(BlendCells());
        //Blend Nodes
        //Thread blendNodeThread = new Thread(() => { BlendNodes(); });
        //blendNodeThread.Start();//Start the Thread.
        yield return StartCoroutine(BlendNodes());
        //Blend Roads
        //Thread blendRoadThread = new Thread(() => { BlendRoads(); });
        //blendRoadThread.Start();//Start the Thread.
        yield return StartCoroutine(BlendRoads());
        //Create Foundation 
        //Thread createFoundationThread = new Thread(() => { CreateFoundation(); });
        //createFoundationThread.Start();//Start the Thread.
        yield return StartCoroutine(CreateFoundation());
        //Clear Grass and Trees from Under the Cells
        if (cells.Count > 0)
        {
            //Debug.Log("Cells");
            for (int i = 0; i < cells.Count; i++)
            {
                //Clear Cells under Lot Footprints
                for (int j = 0; j < cells[i].cidyLots.Count; j++)
                {
                    //Debug.Log("Cell");
                    //List<Vector3> lot = cells[i].lots[j].vectorList;
                    if (!cells[i].cidyLots[j].empty)
                    {
                        //Debug.Log("Cell Detail");
                        List<Vector3> lot = cells[i].cidyLots[j].lotPrint;
                        RemoveDetailUnderPoly(terrain, lot, false);
                    }
                }
                //Clear Under SideWalks
                GameObject[] sideWalks = cells[i].sideWalkHolders;
                //Iterate through this cells sideWalks
                for (int j = 0; j < sideWalks.Length; j++)
                {
                    if(sideWalks[j] == null)
                    {
                        continue;
                    }
                    //Grab Mesh from Holder and Blend to terrain
                    //Mesh sideWalkMesh = sideWalks[j].GetComponent<MeshFilter>().sharedMesh;
                    //if (sideWalkMesh != null)
                    //{
                        RemoveDetailUnderSideWalk(terrain, sideWalks[j], cells[i].sideWalkWidth);
                    //}
                    //Now grab corner Mesh
                    //sideWalkMesh = sideWalks[j].transform.Find(("CornerWalk" + j)).GetComponent<MeshFilter>().sharedMesh;
                    //if (sideWalkMesh != null)
                    //{
                     //   Vector3[] meshPoly = new Vector3[sideWalkMesh.vertices.Length];
                      //  for (int v = 0; v < meshPoly.Length; v++)
                       // {
                        //    meshPoly[v] = sideWalkMesh.vertices[v] + sideWalks[j].transform.position;
                        //}
                        //Grab its Mesh and Send its Poly Data
                        //RemoveDetailUnderPoly(terrain, meshPoly.ToList(), true);
                    //}
                }
                curProblems--;
                //Debug.Log("CurPoblems: "+curProblems+" Final Percent: "+ (1.0f - ((float)curProblems / (float)totalProblems)));
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i / (float)cells.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Clearing Vegitation:", " Cells: "+ percentage + "% " + "(" + i.ToString() + "/" + cells.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }
        //Update Intersection Areas
        if (masterGraph.Count > 1)
        {
            //Flatten Node Intersections First.
            for (int i = 0; i < masterGraph.Count; i++)
            {
                if (masterGraph[i].type != CiDyNode.IntersectionType.continuedSection)
                {
                    RemoveDetailUnderNode(terrain, masterGraph[i]);
                }
                else
                {
                    RemoveDetailUnderRoadNode(terrain, masterGraph[i]);
                }
                curProblems--;
                //Debug.Log("CurPoblems: "+curProblems);
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i / (float)masterGraph.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Clearing Vegitation:", " Intersections: "+ percentage + "% " + "(" + i.ToString() + "/" + masterGraph.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }
        //Update Roads
        if (roads.Count > 0)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                RemoveDetailUnderRoad(terrain, roads[i]);
                curProblems--;
                //Debug.Log("CurPoblems: "+(curProblems/totalProblems));
                #if UNITY_EDITOR
                    float percentage = Mathf.Round(((float)i / (float)roads.Count) * 100);
                    UnityEditor.EditorUtility.DisplayProgressBar("Clearing Vegitation:", " Roads: "+ percentage + "% " + "(" + i.ToString() + "/" + roads.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
            }
        }
        
        /*//Clone All Terrain Height Map Data before we begin blending.(This must be done in sequence as previous data is used in the next iterations)
        float[,] storedHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
        Debug.Log("StoredHeights: "+storedHeights.LongLength);
        //Get All Road Data (CiDyRoad and Meshes)
        ThreadMesh[] storedMeshes = new ThreadMesh[roads.Count];
        CiDyRoad[] storedRoads = new CiDyRoad[roads.Count];
        for (int i = 0; i < storedRoads.Length; i++) {
            storedRoads[i] = roads[i].GetComponent<CiDyRoad>();
            storedMeshes[i] = new ThreadMesh(roads[i].GetComponent<MeshFilter>().sharedMesh);
            //Change layer as well
            //Change Layer to Road(It should be road already, But just in case)
            roads[i].layer = LayerMask.NameToLayer("Road");
        }*/
        Vector3 terrPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        Vector3 mapScale = terrain.terrainData.heightmapScale;
        int hmRes = terrain.terrainData.heightmapResolution;
        //allThreads.Add(newThread);
        //.Start();
        //Start Terrain Blending Thread A Single Thread will work through All Blending
        //Thread blendingThread = new Thread(() => { ThreadedBlending(terrPos,tSize, mapScale, storedHeights, storedRoads,storedMeshes);});
        //blendingThread.Start();//Start the Thread.
        //allThreads.Add(blendingThread);
        //Blend Terrain Heights to Cells, Roads,Nodes Etc
        //Start Thread.

        /*//Blend Cells
        Thread blendCellThread = new Thread(() => { BlendCells();});
        blendCellThread.Start();//Start the Thread.
        //yield return StartCoroutine(BlendCells());
        //Blend Nodes
        Thread blendNodeThread = new Thread(() => { BlendNodes(); });
        blendNodeThread.Start();//Start the Thread.
        //yield return StartCoroutine(BlendNodes());
        //Blend Roads
        Thread blendRoadThread = new Thread(() => { BlendRoads(); });
        blendRoadThread.Start();//Start the Thread.
        //yield return StartCoroutine(BlendRoads());
        //Create Foundation 
        Thread createFoundationThread = new Thread(() => { CreateFoundation(); });
        createFoundationThread.Start();//Start the Thread.*/
        //yield return StartCoroutine(CreateFoundation());
        //Finalize Terrain
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayProgressBar("Blend Terrain:", "Finalizing Terrain", (1.0f - ((float)curProblems / (float)totalProblems)));
        #endif
        //Turn On Terrain Collider
        if (terrainCollider != null)
            terrainCollider.enabled = true;
        //Flush Terrain
        terrain.Flush();
        //Clear Progress Bar
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
        #endif
        yield break;
    }

    void TestThread(float[,] storedHeights, Vector3 terrPos, Vector3 tSize, Vector3 mapScale) {
        //Show me the Terrain Heights 
        int skip = 0;//Used to skip every other.
        for (int x = 0; x < storedHeights.GetLength(0); x++)
        {
            for (int z = 0; z < storedHeights.GetLength(1); z++)
            {
               // if (x > (storedHeights.GetLength(0) / 2) && z > (storedHeights.GetLength(1) / 2))
                //{
                    float pX = (hmWidth / tSize.x / hmWidth) + terrPos.x + (mapScale.x * x);
                    float pY = storedHeights[x,z];//(storedHeights[z, x] * tSize.y) + terrPos.y;
                    float pZ = (hmHeight / tSize.z / hmHeight) + terrPos.z + (mapScale.z * z);
                    //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                    //float pX = (hmWidth / tSize.x / hmWidth) + terrPos.x + (mapScale.x * z);
                    //float pZ = (hmHeight / tSize.z / hmHeight) + terrPos.z + (mapScale.z * x);
                    Vector3 p0 = new Vector3(pX, pY, pZ);
                    switch (skip)
                    {
                        case 0:
                            p0.y = 0;
                            //GameObject tmpSphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            //tmpSphere.transform.position = p0;
                            //GameObject.Instantiate(prefab, p0, Quaternion.identity);
                            skip++;
                            break;
                        case 1:
                            //GameObject.Instantiate(prefab, p0, Quaternion.identity);
                            skip++;
                            break;
                        case 2:
                            p0 = Vector3.Lerp(p0, new Vector3(p0.x, 0, p0.z), 0.9f);
                            //GameObject.Instantiate(prefab, p0, Quaternion.identity);
                            skip = 0;
                            break;
                    }
                    storedHeights[x, z] = p0.y;
                //}
            }
        }
        Action ModfiyHeights = () =>
        {
            //Integrate the Function on Main Thread
            /*for (int x = 0; x < fullHeights.GetLength(0); x++)
            {
                for (int z = 0; z < fullHeights.GetLength(1); z++)
                {
                    if (x > (fullHeights.GetLength(0) / 2) && z > (fullHeights.GetLength(1) / 2))
                    {
                        float pX = (hmWidth / tSize.x / hmWidth) + terrPos.x + (mapScale.x * x);
                        float pY = (fullHeights[z, x] * tSize.y) + terrPos.y;
                        float pZ = (hmHeight / tSize.z / hmHeight) + terrPos.z + (mapScale.z * z);
                        //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                        //float pX = (hmWidth / tSize.x / hmWidth) + terrPos.x + (mapScale.x * z);
                        //float pZ = (hmHeight / tSize.z / hmHeight) + terrPos.z + (mapScale.z * x);
                        Vector3 p0 = new Vector3(pX, pY, pZ);
                        GameObject.Instantiate(prefab, p0, Quaternion.identity);
                    }
                }
            }*/
            terrain.terrainData.SetHeights(0, 0, storedHeights);

        };
        //Queue Action to Integrate Data
        QueueMainThreadFunction(ModfiyHeights);
        #if UNITY_EDITOR
            //If in editor Update
            EditorApplication.update += Update;
        #endif
    }

    void ThreadedBlending(Vector3 terrPos,Vector3 tSize, Vector3 mapScale, float[,] storedHeights, CiDyRoad[] roads, ThreadMesh[] roadMeshes) {
        Debug.Log("Starting ThreadedBlending");
        //Run Blending Logic on Stored Heights Data.
        //Blend Roads
        //Iterate through Roads and Blend Terrain to Roads
       /* for (int j= 0; j < roads.Length; j++)
        {
            CiDyRoad rd = roads[j];
            //Remove Trees and Grass(Details that are under the Road Path)
            int roadRadius = (int)(rd.width);
            //Lets translate Road Mesh Points to TerrainDetail Points.
            ThreadMesh roadMesh = roadMeshes[j];
            //Make sure Road is in Road Layer( Just incase user somehow  changed it)
            if (roadMesh != null)
            {
                //Grab Mid Points of Road Mesh in World Space.
                for (int i = 0; i < roadMesh.verts.Length - 2; i += 2)
                {
                    //Send Point V0, V1, V2, V3
                    BlendTerrainOfRoad(storedHeights,terrPos, tSize, mapScale, roadMesh.verts[i], roadMesh.verts[i + 1], roadMesh.verts[i + 2], roadMesh.verts[i + 3], roadRadius, roadMask2,hmWidth,hmHeight);
                }
            }

            curProblems--;
            /*#if UNITY_EDITOR
                float percentage = Mathf.Round(((float)i / (float)roads.Count) * 100);
                UnityEditor.EditorUtility.DisplayProgressBar("Blending Terrain:", " Roads: " + percentage + "% " + "(" + i.ToString() + "/" + roads.Count + ")", (1.0f - ((float)curProblems / (float)totalProblems)));
            #endif*//*
        }
        Debug.Log("Completed ThreadedBlending");
        //Update Actual Terrain Data on Main Thread

        // Now we need to modify a Unity gameobject
        Action UpdateTerrain = () => {
            //terrain.terrainData.SetHeights(0, 0, storedHeights);
            //Iterate through and Create Nodes to show the Terrain Heights.
            for (int x = 0; x < storedHeights.GetLength(0); x++) {
                for (int y = 0; y < storedHeights.GetLength(1); y++) {
                    CiDyUtils.MarkPoint(new Vector3(x, storedHeights[x, y], y), x + y);
                }
            }
        };

        // NOTE: We still aren't allowed to call this from a child thread
        //aFunction();

        QueueMainThreadFunction(UpdateTerrain);
        #if UNITY_EDITOR
                EditorApplication.update += Update;
            #endif*/
    }

    void ReAddTrees(Terrain t)
    {
        if (terrainTrees == null) {
            Debug.Log("Error Trees are Gone");
            return;
        }
        //Convert Saved Tree Array to TreeInstance[]
        TreeInstance[] trees = new TreeInstance[terrainTrees.Length];
        for (int i = 0; i < trees.Length; i++) {
            //Convert Serialized to TreeInstance
            trees[i] = terrainTrees[i].Instance();
        }
        //Set Data to Terrain
        t.terrainData.treeInstances = trees;
    }

    // Set all pixels in a detail map to zero.
    void ReAddGrass(Terrain t)
    {
        //Debug.Log("ReAddGrass");
        if (terrainGrass == null) {
            Debug.Log("No Grass Stored!");
            return;
        }

        int[] layers = terrain.terrainData.GetSupportedLayers(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
        //Debug.Log("Updated Map: "+updatedMap.Length+" Total Layers: "+ layers.Length);
        for (int i = 0; i < layers.Length; i++)
        {
            int[,] updatedMap = terrainGrass[i].ReturnMap();
            // Assign the modified map back.
            t.terrainData.SetDetailLayer(0, 0, layers[i], updatedMap);
        }
    }

    // Set all pixels in a detail map to zero.
    void RemoveGrass(Terrain t, Vector3 position, float radius)
    {
        Debug.Log("Removing Grass");
        // Get all Grass in Layer 0
        var map = t.terrainData.GetDetailLayer(0, 0, t.terrainData.detailWidth, t.terrainData.detailHeight, 0);

        int TerrainDetailMapSize = t.terrainData.detailResolution;
        if (t.terrainData.size.x != t.terrainData.size.z)
        {
            Debug.Log("X and Y Size of terrain have to be the same (RemoveGrass.CS Line 43)");
            return;
        }

        float PrPxSize = TerrainDetailMapSize / t.terrainData.size.x;

        Vector3 TexturePoint3D = position - t.transform.position;
        TexturePoint3D = TexturePoint3D * PrPxSize;

        float[] xymaxmin = new float[4];
        xymaxmin[0] = TexturePoint3D.z + radius;
        xymaxmin[1] = TexturePoint3D.z - radius;
        xymaxmin[2] = TexturePoint3D.x + radius;
        xymaxmin[3] = TexturePoint3D.x - radius;

        for (int y = 0; y < t.terrainData.detailHeight; y++)
        {
            for (int x = 0; x < t.terrainData.detailWidth; x++)
            {

                if (xymaxmin[0] > x && xymaxmin[1] < x && xymaxmin[2] > y && xymaxmin[3] < y)
                    map[x, y] = 0;
            }
        }
        t.terrainData.SetDetailLayer(0, 0, 0, map);
    }

    // Set all pixels in a detail map that are under the Center Road points within Radius to zero.
    void RemoveGrassAtPoint(Terrain t, Vector3 center, int radius)
    {
        //Debug.Log("Removing Grass Under Object");
        int mWidth = t.terrainData.detailWidth;
        int mHeight = t.terrainData.detailHeight;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center,t.transform.position,t.terrainData.size);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((t.terrainData.detailWidth / t.terrainData.size.x) * radius);
        int zDetail = Mathf.RoundToInt((t.terrainData.detailHeight / t.terrainData.size.z) * radius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posYInTerrain - (zDetail / 2);
        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        //Debug.Log("Terrain Width: " + t.terrainData.detailWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
        // get the grass map of the terrain under this game object
        int[,] map = t.terrainData.GetDetailLayer(offsetX, offsetZ, xDetail, zDetail, 0);
        //Debug.Log("Map Length: " + map.LongLength);

        float[] xymaxmin = new float[4];
        xymaxmin[0] = coord.z + zDetail;
        xymaxmin[1] = coord.z - zDetail;
        xymaxmin[2] = coord.x + xDetail;
        xymaxmin[3] = coord.x - xDetail;

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                //map[x, y] = 0;
                if (xymaxmin[0] > x && xymaxmin[1] < x && xymaxmin[2] > y && xymaxmin[3] < y)
                {
                    //Debug.Log("Cleared: " + x+" Y: "+y);
                    map[x, y] = 0;
                }
            }
        }

        //How Many Layers are there in this area?
        int[] layers = t.terrainData.GetSupportedLayers(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
        for (int i = 0; i < layers.Length; i++)
        {
            //Set Map back to Detail
            t.terrainData.SetDetailLayer(offsetX, offsetZ, layers[i], map);
        }
    }

    void RemoveDetailUnderSideWalk(Terrain t, GameObject sideWalk, float sideWalkwidth)
    {
        Mesh sideWalkMesh = sideWalk.GetComponent<MeshFilter>().sharedMesh;
        List<Vector3> midPoints = new List<Vector3>(0);
        //Find Useable Points along the Path of the Mesh for Grass/Tree Area Removal. (Close to SideWalkWidth)
        for (int i = 0; i < sideWalkMesh.vertices.Length-3; i++) {
            //Grab Three Points starting with this one as the first.
            Vector3[] tmpArray = new Vector3[3];
            tmpArray[0] = sideWalkMesh.vertices[i];
            tmpArray[1] = sideWalkMesh.vertices[i + 1];
            tmpArray[2] = sideWalkMesh.vertices[i + 2];
            //Plane quadPlane = new Plane(tmpArray[0], tmpArray[1], tmpArray[2]);
            //Vector3 quadNormal = quadPlane.normal;
            //Plane upPlane = new Plane(Vector3.zero, new Vector3(1, 0, 0),new Vector3(1,0,1));
            //Vector3 upNormal = upPlane.normal;
            //float angle = Vector3.Angle(upNormal, quadNormal);
            //if (angle <= 45) {
                //We are in an up angle.
                //Find Centroid
                Vector3 center = Vector3.zero;
                for (int j = 0; j < tmpArray.Length; j++) {
                    center += tmpArray[j];
                }
                center = center / 3;
                midPoints.Add(center);
                RemoveGrassAtPoint(t, center, Mathf.RoundToInt(sideWalkwidth*1.9f));
                //CiDyUtils.MarkPoint(center, i);
            //}
        }
        //Vector3 centerOfSideWalk = (sideWalkMesh.vertices[0] + sideWalkMesh.vertices[sideWalkMesh.vertices.Length - 1]) / 2;
        //Remove Trees and Grass(Details that are under the Road Path)
        int roadRadius = Mathf.RoundToInt(sideWalkwidth);
        //Lets translate Road Mesh Points to TerrainDetail Points.
        //Vector3[] midPoints = new Vector3[(sideWalkMesh.vertices.Length/2)-1];//Determine Mid Points
        /*List<Vector3> midPoints = new List<Vector3>(0);
        //Grab Inner Line of Side Walk Mesh
        for (int j = 0; j < sideWalkMesh.vertices.Length; j+=2) {
            midPoints.Add(sideWalkMesh.vertices[j] + sideWalk.transform.position);
        }
        //Take the Verts Line and Split it based on Desired Width.
        midPoints = CiDyUtils.SplitLine(midPoints.ToArray(), sideWalkwidth);
        midPoints.Add(sideWalkMesh.vertices[sideWalkMesh.vertices.Length-1] + sideWalk.transform.position);
        Vector3 dir = Vector3.zero;
        //Move Mid Points To the Half Distance of there SideWalk Area.
        for (int i = 0; i < midPoints.Count; i++) {
            //At Beggining or Middle
            if (i != midPoints.Count - 1)
            {
                dir = midPoints[i + 1] - midPoints[i];
            }
            else {
                //Last Point
                dir = midPoints[i] - midPoints[i - 1];
            }
            Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;
            //Project Point Right
            Vector3 p = midPoints[i] + (-left * (sideWalkwidth / 2));
            //Remove Grass at Mid Point
            RemoveGrassAtPoint(t, p, Mathf.RoundToInt(sideWalkwidth + 3.2f));
        }
        */
        //Remove Trees
        //Now Remove Trees.
        //Grab Current Trees of the Terrain
        List<TreeInstance> treeInstances = t.terrainData.treeInstances.ToList();
        //Iterate through Road Points and Determine Distance
        for (int i = 0; i < midPoints.Count; i++)
        {
            Vector3 TexturePoint3D = midPoints[i];
            //Flatten Y
            TexturePoint3D.y = 0;
            for (int j = 0; j < treeInstances.Count; j++)
            {
                // The the actual world position of the current tree we are checking
                Vector3 currentTreeWorldPosition = Vector3.Scale(treeInstances[j].position, t.terrainData.size) + t.transform.position;
                //Flatten Y
                currentTreeWorldPosition.y = 0;
                //If distance to this tree and Point are within Radius. Remove It 
                float dist = Vector3.Distance(currentTreeWorldPosition, TexturePoint3D);
                if (dist <= (roadRadius / 2) + 1.6f)
                {
                    // Remove the tree from the terrain tree list
                    treeInstances.RemoveAt(j);
                }
            }
        }
        //Set Tree Instances of Terrain to Match Updated tree List
        t.terrainData.treeInstances = treeInstances.ToArray();
    }

    void BlendRoad(Terrain t, GameObject road, int smoothBorder)
    {
        CiDyRoad rd = road.GetComponent<CiDyRoad>();
        //Remove Trees and Grass(Details that are under the Road Path)
        int roadRadius = (int)(rd.width);
        //Lets translate Road Mesh Points to TerrainDetail Points.
        Mesh roadMesh = road.GetComponent<MeshFilter>().sharedMesh;//Grab Vertices
        //Make sure Road is in Road Layer( Just incase user somehow  changed it)
        //Change Layer to Road(It should be road already, But just in case)
        road.layer = LayerMask.NameToLayer("Road");
        Vector3 terrPos = t.transform.position;
        Vector3 tSize = t.terrainData.size;
        Vector3 mapScale = t.terrainData.heightmapScale;
        if (roadMesh != null)
        {
            //Grab Mid Points of Road Mesh in World Space.
            for (int i = 0; i < roadMesh.vertices.Length - 2; i += 2)
            {
                //Send Point V0, V1, V2, V3
                BlendTerrainOfRoad(terrPos,tSize, mapScale, roadMesh.vertices[i], roadMesh.vertices[i + 1], roadMesh.vertices[i + 2], roadMesh.vertices[i + 3], roadRadius, roadMask2,hmWidth,hmHeight);
            }
        }
    }

    // Set all pixels in a detail map that are under the Center Road points within Radius to zero.
    void RemoveDetailUnderRoad(Terrain t, GameObject road)
    {
        CiDyRoad rd = road.GetComponent<CiDyRoad>();
        //Vector3 centerOfRoad = (rd.nodeA.position + rd.nodeB.position)/ 2;
        //Remove Trees and Grass(Details that are under the Road Path)
        int roadRadius = (int)rd.width;
        //int roadLength = (int)Vector3.Distance(rd.nodeA.position,rd.nodeB.position);
        //Lets translate Road Mesh Points to TerrainDetail Points.
        Mesh roadMesh = road.GetComponent<MeshFilter>().sharedMesh;//Grab Vertices
        if (roadMesh == null || roadMesh.vertices.Length == 0) {
            return;
        }
        Vector3[] midPoints = new Vector3[roadMesh.vertices.Length];//Determine Mid Points
        //Grab Mid Points of Road Mesh in World Space.
        for (int i = 0; i < midPoints.Length-1; i++) {
            midPoints[i] = (roadMesh.vertices[i] + roadMesh.vertices[i + 1]) / 2;
            //Remove Grass
            RemoveGrassAtPoint(t, midPoints[i], roadRadius);
        }

        //Remove Trees
        //Now Remove Trees.
        //Grab Current Trees of the Terrain
        List<TreeInstance> treeInstances = t.terrainData.treeInstances.ToList();
        //Iterate through Road Points and Determine Distance
        for (int i = 0; i < midPoints.Length; i++)
        {
            Vector3 TexturePoint3D = midPoints[i] - road.transform.position;
            //Flatten Y
            TexturePoint3D.y = 0;
            for (int j = 0; j < treeInstances.Count; j++)
            {
                //Get Current Tree we are testing
                TreeInstance currentTree = treeInstances[j];
                // The the actual world position of the current tree we are checking
                Vector3 currentTreeWorldPosition = Vector3.Scale(treeInstances[j].position, t.terrainData.size) + t.transform.position;
                currentTreeWorldPosition.y = 0;
                //If distance to this tree and Point are within Radius. Remove It 
                float dist = Vector3.Distance(currentTreeWorldPosition, TexturePoint3D);
                if (dist <= ((roadRadius/2)+1.6f))
                {
                    // Remove the tree from the terrain tree list
                    treeInstances.RemoveAt(j);
                }
            }
        }
        //Set Tree Instances of Terrain to Match Updated tree List
        t.terrainData.treeInstances = treeInstances.ToArray();
    }

    // Set all pixels in a detail map that are under the Center Road points within Radius to zero.
    void RemoveDetailUnderRoadNode(Terrain t, CiDyNode node)
    {
        //Debug.Log("Remove Detail Under Road Node: "+node.nodeNumber);
        //Vector3 centerOfRoad = node.position;
        //Remove Trees and Grass(Details that are under the Road Path)
        int roadRadius = (int)node.maxRadius;
        //Lets translate Road Mesh Points to TerrainDetail Points.
        Mesh roadMesh = node.intersection.GetComponent<MeshFilter>().sharedMesh;//Grab Vertices
        Vector3[] midPoints = new Vector3[roadMesh.vertices.Length];//Determine Mid Points
        //Grab Mid Points of Road Mesh in World Space.
        for (int i = 0; i < midPoints.Length - 1; i++)
        {
            midPoints[i] = ((roadMesh.vertices[i]+node.position) + (roadMesh.vertices[i + 1]+node.position)) / 2;
            //Remove Grass
            RemoveGrassAtPoint(t, midPoints[i], roadRadius);
        }

        //Remove Trees
        //Now Remove Trees.
        //Grab Current Trees of the Terrain
        List<TreeInstance> treeInstances = t.terrainData.treeInstances.ToList();
        //Iterate through Road Points and Determine Distance
        for (int i = 0; i < midPoints.Length; i++)
        {
            Vector3 TexturePoint3D = midPoints[i] - node.position;
            //Flatten Y
            TexturePoint3D.y = 0;
            for (int j = 0; j < treeInstances.Count; j++)
            {
                //Get Current Tree we are testing
                TreeInstance currentTree = treeInstances[j];
                // The the actual world position of the current tree we are checking
                Vector3 currentTreeWorldPosition = Vector3.Scale(currentTree.position, t.terrainData.size) + t.transform.position;
                currentTreeWorldPosition.y = 0;
                //If distance to this tree and Point are within Radius. Remove It 
                float dist = Vector3.Distance(currentTreeWorldPosition, TexturePoint3D);
                if (dist <= (roadRadius / 2) + 1.6f)
                {
                    // Remove the tree from the terrain tree list
                    treeInstances.RemoveAt(j);
                }
            }
        }
        //Set Tree Instances of Terrain to Match Updated tree List
        t.terrainData.treeInstances = treeInstances.ToArray();
    }

    // Set all pixels in a detail map that are under the Center Road points within Radius to zero.
    void RemoveDetailUnderNode(Terrain t, CiDyNode node)
    {
        //Determine Radius of Node. Based on its farthest stretched point.
        //Check Radius from Center of Node. Then Check Map Point if its inside the Poly, We will Clear it from the Map.
        Mesh nodeMesh = node.mFilter.sharedMesh;
        float farthest = 0f;

        //int indexNode = 0;
        int radius = node.maxRadius;
        Vector3[] meshPoly = new Vector3[nodeMesh.vertices.Length];
        for (int i = 0; i < nodeMesh.vertices.Length; i++)
        {
            float dist = Vector3.Distance(nodeMesh.vertices[i], node.position);
            if (dist > farthest) {
                farthest = dist;
                //indexNode = i;
            }
            meshPoly[i] = (nodeMesh.vertices[i] + node.position);
        }

        //Is farthest greater than node.maxRadius * 2?
        if (farthest > radius) {
            radius = (int)farthest;
        }

        meshPoly.Reverse();
        //Debug.Log("Node Radius: "+radius);
        //Grab Map Area Around Node by Max Radius of Node
        //Debug.Log("Removing Grass Under Object");
        int mWidth = t.terrainData.detailWidth;
        int mHeight = t.terrainData.detailHeight;
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(node.position,terrPos,tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((t.terrainData.detailWidth / t.terrainData.size.x) * radius);
        int zDetail = Mathf.RoundToInt((t.terrainData.detailHeight / t.terrainData.size.z) * radius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posYInTerrain - (zDetail / 2);

        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        int[] layers = t.terrainData.GetSupportedLayers(0, 0, t.terrainData.detailWidth, t.terrainData.detailHeight);
        //Debug.Log("Layers: " + layers.Length);
        for (int n = 0; n < layers.Length; n++)
        {
            //Debug.Log("Terrain Width: " + t.terrainData.detailWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
            // get the grass map of the terrain under this game object
            int[,] map = t.terrainData.GetDetailLayer(offsetX, offsetZ, xDetail, zDetail, layers[n]);
            float mapScale = t.terrainData.detailResolution;
            //Debug.Log("X: "+t.terrainData.heightmapScale.x+" Y: "+t.terrainData.heightmapScale.z+" ResolutionMap: "+tSize.x/mapScale);
            //Iterate through Grabbed Map Area and Clear Points that are within the Mesh Poly Points.
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    float pX = (mWidth / tSize.x / mWidth) + terrPos.x + ((tSize.x / mapScale) * ((offsetX + y)));
                    float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + ((tSize.z / mapScale) * ((offsetZ + x)));
                    Vector3 p0 = new Vector3(pX, 0, pZ);
                    if (CiDyUtils.PointInsidePolygon(meshPoly.ToList(), p0))
                    {
                        //Debug.Log("True: "+x+" "+y);
                        map[x, y] = 0;
                    }
                }
            }

            //Set Information back to Map
            t.terrainData.SetDetailLayer(offsetX, offsetZ, layers[n], map);
        }
        //Remove Trees
        //Grab Current Trees of the Terrain
        TreeInstance[] trees = t.terrainData.treeInstances;
        List<TreeInstance> treeInstances = new List<TreeInstance>(0);

        for (int j = 0; j < trees.Length; j++)
        {
            // The the actual world position of the current tree we are checking
            Vector3 currentTreeWorldPosition = Vector3.Scale(trees[j].position, tSize) + terrPos;
            currentTreeWorldPosition.y = 0;
            //Is this Tree within MeshPoly?
            if (!CiDyUtils.PointInsidePolygon(meshPoly.ToList(), currentTreeWorldPosition))
            {
                //Debug.Log("True: "+x+" "+y);
                // Remove the tree from the terrain tree list
                treeInstances.Add(trees[j]);
            }
        }

        //Set Tree Instances of Terrain to Match Updated tree List
        t.terrainData.treeInstances = treeInstances.ToArray();
    }

    // Set all pixels in a detail map that are under the Poly Points(Assumes CounterClockwise Order) within Radius to zero.
    void RemoveDetailUnderPoly(Terrain t, List<Vector3> poly, bool removeBoth)
    {
        //Debug.Log("Removed");
        //For us to Grab a Large Enought Area of the Map in Question. We will have to determine the Radius of the Poly Shape.
        Vector3 Centroid = CiDyUtils.FindCentroid(poly);
    
        int farthest = 0;
        for (int i = 0; i < poly.Count; i++) {
            float dist = Vector3.Distance(new Vector3(poly[i].x,0,poly[i].z), new Vector3(Centroid.x,0,Centroid.z));
            if (dist > farthest) {
                farthest = Mathf.RoundToInt(dist);
            }
        }
        //Calculate Radius
        radius = farthest * 2;
        //Debug.Log("Node Radius: "+radius);
        //Grab Map Area Around Node by Max Radius of Node
        //Debug.Log("Removing Grass Under Object");
        int mWidth = t.terrainData.detailWidth;
        int mHeight = t.terrainData.detailHeight;
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(Centroid,terrPos,tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((t.terrainData.detailWidth / t.terrainData.size.x) * radius);
        int zDetail = Mathf.RoundToInt((t.terrainData.detailHeight / t.terrainData.size.z) * radius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posYInTerrain - (zDetail / 2);

        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        //Debug.Log("Terrain Width: " + t.terrainData.detailWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
        //Get Layers
        int[] layers = t.terrainData.GetSupportedLayers(0, 0, t.terrainData.detailWidth,t.terrainData.detailHeight);
        for (int i = 0; i < layers.Length; i++)
        {
            // get the grass map of the terrain under this game object
            int[,] map = t.terrainData.GetDetailLayer(offsetX, offsetZ, xDetail, zDetail, layers[i]);
            //Check Radius from Center of Node. Then Check Map Point if its inside the Poly, We will Clear it from the Map.
            float mapScale = t.terrainData.detailResolution;

            if (removeBoth)
            {
                //Iterate through Grabbed Map Area and Clear Points that are within the Mesh Poly Points.
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    for (int y = 0; y < map.GetLength(1); y++)
                    {
                        float pX = (mWidth / tSize.x / mWidth) + terrPos.x + ((tSize.x / mapScale) * ((offsetX + y)));
                        float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + ((tSize.z / mapScale) * ((offsetZ + x)));
                        Vector3 p0 = new Vector3(pX, 0, pZ);
                        float curDist = Vector3.Distance(p0, Centroid);
                        if (curDist <= (radius / 2))
                        {
                            map[x, y] = 0;
                        }
                        else if (CiDyUtils.PointInsideOrOnLinePolygon(poly, p0))
                        {
                            //Debug.Log("True: "+x+" "+y);
                            map[x, y] = 0;
                        }
                    }
                }
                //Set Information back to Map
                t.terrainData.SetDetailLayer(offsetX, offsetZ, layers[i], map);
            }
        }
        //Remove Trees
        //Grab Current Trees of the Terrain
        TreeInstance[] trees = t.terrainData.treeInstances;
        List<TreeInstance> treeInstances = new List<TreeInstance>(0);

        for (int j = 0; j < trees.Length; j++)
        {
            // The the actual world position of the current tree we are checking
            Vector3 currentTreeWorldPosition = Vector3.Scale(trees[j].position, tSize) + terrPos;
            currentTreeWorldPosition.y = 0;
            //Is this Tree within MeshPoly?
            if (!CiDyUtils.PointInsidePolygon(poly, currentTreeWorldPosition))
            {
                //Debug.Log("True: "+x+" "+y);
                // Remove the tree from the terrain tree list
                treeInstances.Add(trees[j]);
            }
        }

        //Set Tree Instances of Terrain to Match Updated tree List
        t.terrainData.treeInstances = treeInstances.ToArray();
    }

    public void FlushTerrain(){
		//Flush Terrain so all changes will take full effect.
		terrain.Flush();
	}

	public int totalProblems = 0;
    public int curProblems = 0;

	public int CalculateBlendTime (){
		//Debug.Log ("CalculateBlendTime");
		totalProblems = 0;
		if(terrain != null){
			totalProblems = (masterGraph.Count+roads.Count+cells.Count) * 2;
			curProblems = totalProblems;
		}
        //Debug.Log ("CalculateBlendTime: "+totalProblems+" CurProblems: "+curProblems);
        return totalProblems;
	}

	public void GrabOriginalHeights(){
		if(terrain==null){
			terrain = Terrain.activeTerrain;
            if (terrain == null) {
                //There is No Active Terrain in the Scene.
                return;
            }
		}
		hmWidth = terrain.terrainData.heightmapWidth;
		hmHeight = terrain.terrainData.heightmapHeight;
		//Debug.Log ("HeightMapWidth " + hmWidth+" HeightMapHeight "+hmHeight+" TerrainDataSize XYZ: "+terrain.terrainData.size+" TerrainWorldPos "+terrain.transform.position);
		float[,] tmpHeights = terrain.terrainData.GetHeights(0,0,hmWidth,hmHeight);
		originalHeights = new float[hmWidth*hmHeight];
		for(int i = 0;i<hmWidth;i++){
			for(int j = 0;j<hmHeight;j++){
				originalHeights[j*hmWidth+i] = tmpHeights[i,j];
			}
		}

        /*//Grab Details and Trees Data.
        TreeInstance[] trees = terrain.terrainData.treeInstances;
        terrainTrees = new SerializedTreeInstance[trees.Length];
        for (int i = 0; i < terrainTrees.Length; i++) {
            terrainTrees[i] = new SerializedTreeInstance(trees[i].color, trees[i].heightScale, trees[i].lightmapColor, trees[i].position, trees[i].prototypeIndex, trees[i].rotation, trees[i].widthScale);
        }
        //Debug.Log("Trees: " + terrainTrees.Length);
        var tmpGrass = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, 0);
        terrainGrass = new TerrainDetail(tmpGrass, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);*/
        //Debug.Log("Grabbed Grass: " + terrainGrass.Length());   
	}

    public void GrabTerrainVegitation()
    {
        //Debug.Log("Grab Vegitation");
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogError("No Terrain: Cannot Grab Vegiation Data");
                //There is No Active Terrain in the Scene.
                return;
            }
        }

            TreeInstance[] trees = terrain.terrainData.treeInstances;
            terrainTrees = new SerializedTreeInstance[trees.Length];
            for (int i = 0; i < terrainTrees.Length; i++)
            {
                terrainTrees[i] = new SerializedTreeInstance(trees[i].color, trees[i].heightScale, trees[i].lightmapColor, trees[i].position, trees[i].prototypeIndex, trees[i].rotation, trees[i].widthScale);
            }
            //Debug.Log("Trees: " + terrainTrees.Length);

            int[] layers = terrain.terrainData.GetSupportedLayers(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
            for (int i = 0; i < layers.Length; i++)
            {
                var tmpGrass = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, layers[i]);
                terrainGrass[i] = new TerrainDetail(tmpGrass, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight);
                //Debug.Log("Grabbed Grass: " + terrainGrass.Length);
            }
    }

    //Function that returns Stored Heights into a Float[,] for the Terrain System
    public float[,] ReturnHeights() {
        float[,] tmpHeights = new float[hmWidth, hmHeight];
        for (int i = 0; i < hmWidth; i++)
        {
            for (int j = 0; j < hmHeight; j++)
            {
                tmpHeights[i, j] = originalHeights[j * hmWidth + i];
            }
        }
        //Return Heights Float[,]
        return tmpHeights;
    }

    //Get an array of heightmap samples(xBase:First x index of heightmap samples to retrieve, yBase:First y index of heightmap samples to retrieve, 
    //width:Number of samples to retrieve along the heightmap's x axis, height:Number of samples to retrieve along the heightmap's y axis, Source Array float[,])
    float[,] GetHeightsFromArray(int baseX, int baseY, int width, int height, float[,] sourceMap)
    {
        //Debug.Log("X Index: " + baseX + " Y Index: " + baseY + " Width: " + width + " Height: " + height);

        float[,] map = new float[width, height];//Initialize Returned Array

        //Iterate through Array starting at
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map[y, x] = sourceMap[baseY + y, baseX + x];
            }
        }

        return map;
    }

    float[,] SetHeightsToArray(int baseX, int baseY, float[,] updatedMap, float[,] sourceMap)
    {
        //Debug.Log("X Index: " + baseX + " Y Index: " + baseY + " Width: " + width + " Height: " + height);

        //Iterate through Array starting at
        for (int y = 0; y < sourceMap.GetLength(1); y++)
        {
            for (int x = 0; x < sourceMap.GetLength(0); x++)
            {
                sourceMap[y, x] = updatedMap[baseY + y, baseX + x];
            }
        }

        return sourceMap;
    }

    public void RestoreOriginalTerrainHeights(){
        //Debug.Log ("Restore Terrain Heights");
        //Return Terrain Details to Original State
        ReAddGrass(terrain);//Reset Grass Map
        ReAddTrees(terrain);//Reset Tree Map
        //Extrapolate Flattend Array to multidimensional Array
        float[,] tmpHeights = new float[hmWidth,hmHeight];
		for(int i = 0;i<hmWidth;i++){
			for(int j = 0;j<hmHeight;j++){
				tmpHeights[i,j] = originalHeights[j*hmWidth+i];
			}
		}
        /*Action aFunction = () => {
            terrain.terrainData.SetHeights(0, 0, tmpHeights);
        };*/
        terrain.terrainData.SetHeights (0, 0, tmpHeights);
	}
    //Fires when Play is Stopped or Graph is Destroyed
    /*void OnDestroy(){
		//Debug.Log ("Reset Terrain Heights to Original");
		//RestoreOriginalTerrainHeights ();
	}*/

    //Assumed Fed Counter Clockwise and Is a Cyclable List
    public void BlendPoly(Vector3[] poly, int radius, bool hasCollider, int layerMask) {
        GameObject tmpCollider = null;
        //If No Collider is Present. Make a Temporary Collider for the Polygon outline Shape.
        if (!hasCollider)
        {
            //Debug.Log("Make Collider Mesh using Polygon Outline");
            //////////////////Test Triangles and UVS for Mesh
            Vector2z[] vertices2D = new Vector2z[poly.Length];
            for (int i = 0; i < vertices2D.Length; i++)
            {
                vertices2D[i] = new Vector2z(poly[i].x, poly[i].z);
            }
            int[] indices = EarClipper.Triangulate(vertices2D);

            Vector2[] newUVs = new Vector2[poly.Length];
            for (int i = 0; i < newUVs.Length; i++)
            {
                newUVs[i] = new Vector2(poly[i].x, poly[i].z);
            }
            tmpCollider = new GameObject("TmpCollider");
            MeshFilter mFilter = tmpCollider.AddComponent<MeshFilter>();
            Mesh colliderMesh = new Mesh();
            MeshCollider mCollider = tmpCollider.AddComponent<MeshCollider>();
            //Make Mesh.
            colliderMesh.vertices = poly;
            colliderMesh.triangles = indices.ToArray();//Flip Indices so the Mesh Collider can be raycast hit from below
            colliderMesh.uv = newUVs;
            //colliderMesh.RecalculateBounds();
            mFilter.mesh = colliderMesh;
            mCollider.sharedMesh = colliderMesh;
            tmpCollider.transform.localScale = tmpCollider.transform.localScale;
            // Bit shift the index of the layer ("Road") to get a bit mask
            int mask = 1 << LayerMask.NameToLayer("Road");
            tmpCollider.layer = LayerMask.NameToLayer("Road");
            //Update LayerMask for Blend Poly Function
            layerMask = mask;
        }
        for (int i = 0; i < poly.Length; i++)
        {
            Vector3 pA;
            Vector3 pB;
            if (i == poly.Length - 1)
            {
                //Cycle
                //Project the Two Lines by radius * 2
                pA = poly[i];
                pB = poly[0];
            }
            else
            {
                //Begining or Middle take next Point
                //Project the Two Lines by radius * 2
                pA = poly[i];
                pB = poly[i + 1];
            }
            //Blend Terrain Under Poly Shape
            BlendTerrainAtPolygon(terrain, pA, pB, radius, layerMask);
        }
        //Now Destroy tmp Collider if Applicalble
        if (!hasCollider && tmpCollider) {
            DestroyImmediate(tmpCollider);
        }
    }

    //This will flatten Terrain Points under Intersection Mesh.
    public void BlendMesh(Mesh blendMesh,Vector3 center, int radius, float zBuffer, float edgeOffset, float smoothBorder, bool useCentroid){
		//Grab Mesh Verts and translate to WorldPos.
		List<Vector3> meshPoly = blendMesh.vertices.ToList();
		for(int i = 0;i<meshPoly.Count;i++){
			meshPoly[i]+=center;
		}
		//Create Centroid for Proper Radius Calculations
		if(useCentroid){
			center = CiDyUtils.FindCentroid (meshPoly);
			center.y = meshPoly[0].y;
		}
		radius = radius*2;
		int hmWidth = terrain.terrainData.heightmapWidth;
		int hmHeight = terrain.terrainData.heightmapHeight;
		Vector3 terrPos = terrain.GetPosition ();
		Vector3 tSize = terrain.terrainData.size;
		int heightRes = terrain.terrainData.heightmapResolution;
		//Convert From Terrain Space to WorldSpace
		Vector3 mapScale = terrain.terrainData.heightmapScale;
		Vector3 coord = GetNormalizedPositionRelativeToTerrain (center,terrPos,tSize);
		// get the position of the terrain heightmap where this game object is
		int posXInTerrain = (int)(coord.x*hmWidth); 
		int posYInTerrain = (int)(coord.z*hmHeight);
		// we set an offset so that all the raising terrain is under this game object
		int offset = radius/2;
		// get the heights of the terrain under this game object
		float[,] heights = terrain.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset,radius,radius);
		CiDyVector3[,] samples = new CiDyVector3[radius,radius];
		//Debug.Log ("MapScale: " + mapScale);
		//int count = 0;
		//Iterate through Heights
		for(int z = 0;z<radius;z++){
			for(int x = 0;x<radius;x++){
				float pX = (hmWidth/tSize.x/heightRes)+terrPos.x+(mapScale.x*((posXInTerrain-offset)+x));
				//float pY = (heights[z,x]*tSize.y)+terrPos.y;
				float pZ = (hmHeight/tSize.z/heightRes)+terrPos.z+(mapScale.z*((posYInTerrain-offset)+z));
				Vector3 p0 = new Vector3 (pX,0,pZ);
				//CiDyUtils.MarkPoint(p0,z);
				if(CiDyUtils.PointInsidePolygon(meshPoly,p0)){
					//This point needs to be flattend
					float newHeight = center.y-zBuffer;
					heights[z,x] = Mathf.InverseLerp(0,tSize.y,newHeight);
                    CiDyVector3 p1 = new CiDyVector3(p0)
                    {
                        insidePoly = true
                    };
                    samples[z,x] = p1;
				} else {
					CiDyVector3 p1 = new CiDyVector3(p0);
					samples[z,x] = p1;
				}
			}
		}
		//Iterate through Heights
		for(int z = 0;z<radius;z++){
			for(int x = 0;x<radius;x++){
				CiDyVector3 p0 = samples[z,x];
				if(!p0.insidePoly){
					float closestPoint = Mathf.Infinity;
					//Vector3 closesetIntersection = Vector3.zero;
					Vector3 intersection = Vector3.zero;
					//We want to test our Surrounding Points. If any are inside then find the intersection with the Polygon Edge that is Closest to p0
					//Can we test above?
					if(z<radius-1){
						//We have another row above us.
						//Test Up.
						if(samples[z+1,x].insidePoly){
							//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
							Vector3 n0 = p0.pos;
							Vector3 n1 = samples[z+1,x].pos;
							if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection,true)){
								//Calculate Dist and Determine if its the Best Found Match.
								float dist = Vector3.Distance(intersection,n0);
								if(dist<closestPoint){
									//closesetIntersection = intersection;
									closestPoint = dist;
								}
							}
						}
						//Can we Test to the Left?
						if(x>0){
							//Test Upper Left
							if(samples[z+1,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z+1,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection,true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
							//Test Left
							if(samples[z,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection,true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
						//Can we Test Right?
						if(x<radius-1){
							//Test Upper Right
							if(samples[z+1,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z+1,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
							//Test Right
							if(samples[z,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection,true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
					}
					//Can we Test Below?
					if(z>0){
						//Test Below
						if(samples[z-1,x].insidePoly){
							//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
							Vector3 n0 = p0.pos;
							Vector3 n1 = samples[z-1,x].pos;
							if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection, true)){
								//Calculate Dist and Determine if its the Best Found Match.
								float dist = Vector3.Distance(intersection,n0);
								if(dist<closestPoint){
									//closesetIntersection = intersection;
									closestPoint = dist;
								}
							}
						}
						//Can we Test Left?
						if(x>0){
							//Test Left
							if(samples[z-1,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z-1,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
						//Can we Test Right?
						if(x<radius-1){
							//Test Right
							if(samples[z-1,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z-1,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,meshPoly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
					}
					//Now that we have our Closest Intersection Point and Distance.
					closestPoint = closestPoint-edgeOffset;
					float b = Mathf.InverseLerp(smoothBorder,0,closestPoint);
					float oHeight = heights[z,x];
					float newHeight = center.y-zBuffer;
					float eHeight = Mathf.InverseLerp(0,tSize.y,newHeight);
					heights[z,x] = b*eHeight+(1-b)*oHeight;
				}
			}
		}
		//Set the new height
		terrain.terrainData.SetHeights(posXInTerrain-offset,posYInTerrain-offset,heights);
	}

	//This will flatten Terrain Points under Intersection Mesh.
	public void BlendPoly(List<Vector3> poly, int radius, float zBuffer, float edgeOffset, float smoothBorder){
		//Create Centroid for Proper Radius Calculations
		Vector3 center = CiDyUtils.FindCentroid (poly,1);
		radius = radius*2;
		int hmWidth = terrain.terrainData.heightmapWidth;
		int hmHeight = terrain.terrainData.heightmapHeight;
		Vector3 terrPos = terrain.GetPosition ();
		Vector3 tSize = terrain.terrainData.size;
		int heightRes = terrain.terrainData.heightmapResolution;
		//Convert From Terrain Space to WorldSpace
		Vector3 mapScale = terrain.terrainData.heightmapScale;
		Vector3 coord = GetNormalizedPositionRelativeToTerrain (center, terrPos, tSize);
		// get the position of the terrain heightmap where this game object is
		int posXInTerrain = (int)(coord.x*hmWidth); 
		int posYInTerrain = (int)(coord.z*hmHeight);
		// we set an offset so that all the raising terrain is under this game object
		int offset = radius/2;
		// get the heights of the terrain under this game object
		float[,] heights = terrain.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset,radius,radius);
		CiDyVector3[,] samples = new CiDyVector3[radius,radius];
		//Debug.Log ("MapScale: " + mapScale);
		//int count = 0;
		//Iterate through Heights
		for(int z = 0;z<radius;z++){
			for(int x = 0;x<radius;x++){
				float pX = (hmWidth/tSize.x/heightRes)+terrPos.x+(mapScale.x*((posXInTerrain-offset)+x));
				//float pY = (heights[z,x]*tSize.y)+terrPos.y;
				float pZ = (hmHeight/tSize.z/heightRes)+terrPos.z+(mapScale.z*((posYInTerrain-offset)+z));
				Vector3 p0 = new Vector3 (pX,0,pZ);
				//CiDyUtils.MarkPoint(p0,z);
				if(CiDyUtils.PointInsidePolygon(poly,p0)){
					//This point needs to be flattend
					float newHeight = center.y-zBuffer;
					heights[z,x] = Mathf.InverseLerp(0,tSize.y,newHeight);
                    CiDyVector3 p1 = new CiDyVector3(p0)
                    {
                        insidePoly = true
                    };
                    samples[z,x] = p1;
				} else {
					CiDyVector3 p1 = new CiDyVector3(p0);
					samples[z,x] = p1;
				}
			}
		}
		//Iterate through Heights
		for(int z = 0;z<radius;z++){
			for(int x = 0;x<radius;x++){
				CiDyVector3 p0 = samples[z,x];
				if(!p0.insidePoly){
					float closestPoint = Mathf.Infinity;
					//Vector3 closesetIntersection = Vector3.zero;
					Vector3 intersection = Vector3.zero;
					//We want to test our Surrounding Points. If any are inside then find the intersection with the Polygon Edge that is Closest to p0
					//Can we test above?
					if(z<radius-1){
						//We have another row above us.
						//Test Up.
						if(samples[z+1,x].insidePoly){
							//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
							Vector3 n0 = p0.pos;
							Vector3 n1 = samples[z+1,x].pos;
							if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
								//Calculate Dist and Determine if its the Best Found Match.
								float dist = Vector3.Distance(intersection,n0);
								if(dist<closestPoint){
									//closesetIntersection = intersection;
									closestPoint = dist;
								}
							}
						}
						//Can we Test to the Left?
						if(x>0){
							//Test Upper Left
							if(samples[z+1,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z+1,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
							//Test Left
							if(samples[z,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
						//Can we Test Right?
						if(x<radius-1){
							//Test Upper Right
							if(samples[z+1,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z+1,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
							//Test Right
							if(samples[z,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
					}
					//Can we Test Below?
					if(z>0){
						//Test Below
						if(samples[z-1,x].insidePoly){
							//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
							Vector3 n0 = p0.pos;
							Vector3 n1 = samples[z-1,x].pos;
							if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
								//Calculate Dist and Determine if its the Best Found Match.
								float dist = Vector3.Distance(intersection,n0);
								if(dist<closestPoint){
									//closesetIntersection = intersection;
									closestPoint = dist;
								}
							}
						}
						//Can we Test Left?
						if(x>0){
							//Test Left
							if(samples[z-1,x-1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z-1,x-1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
						//Can we Test Right?
						if(x<radius-1){
							//Test Right
							if(samples[z-1,x+1].insidePoly){
								//This Poly is inisde Polygon. Perform Line Intersection Test and Store result.
								Vector3 n0 = p0.pos;
								Vector3 n1 = samples[z-1,x+1].pos;
								if(CiDyUtils.IntersectsList(n0,n1,poly, ref intersection, true)){
									//Calculate Dist and Determine if its the Best Found Match.
									float dist = Vector3.Distance(intersection,n0);
									if(dist<closestPoint){
										//closesetIntersection = intersection;
										closestPoint = dist;
									}
								}
							}
						}
					}
					//Now that we have our Closest Intersection Point and Distance.
					closestPoint = closestPoint-edgeOffset;
					float b = Mathf.InverseLerp(smoothBorder,0,closestPoint);
					float oHeight = heights[z,x];
					float newHeight = center.y-zBuffer;
					float eHeight = Mathf.InverseLerp(0,tSize.y,newHeight);
					heights[z,x] = b*eHeight+(1-b)*oHeight;
				}
			}
		}
		//Set the new height
		terrain.terrainData.SetHeights(posXInTerrain-offset,posYInTerrain-offset,heights);
	}

    void BlendSideWalk(Terrain t, Transform cell, Vector3[] sideWalkEdge, float sideWalkwidth, int layerMask, float sideWalkHeight)
    {
        List<Vector3> midPoints = new List<Vector3>(0);
        //Calculate Points to Blend that are in the middle of the sidewalk.
        for (int i = 0; i < sideWalkEdge.Length; i++) {
            Vector3 p0 = sideWalkEdge[i];
            Vector3 p1;
            if (i == sideWalkEdge.Length - 1)
            {
                p1 = sideWalkEdge[0];
            }
            else
            {
                p1 = sideWalkEdge[i + 1];
            }
            //Offset to proper world space
            p0 = p0 + cell.position;
            p1 = p1 + cell.position;
            if (UnityEngine.Vector3.Distance(p0, p1) <= 0.1618f)
            {
                continue;
            }
            //Calculate our current direction
            Vector3 fwd;
            fwd = (p1 - p0).normalized;
            if (i == sideWalkEdge.Length - 1)
            {
                fwd = -fwd;
            }
            //Now that we know our forward direction and World Up. Lets get cross for Left Direction.
            Vector3 left = -Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 finalPoint = p0 + (left * sideWalkwidth / 2);
            //Now that we have the Left Direction. Move the Points over to the Middle.
            midPoints.Add(finalPoint);
        }
        //Mesh sideWalkMesh = sideWalk.GetComponent<MeshFilter>().sharedMesh;
        //Remove Trees and Grass(Details that are under the Road Path)
        int sideWalkRadius = Mathf.RoundToInt(sideWalkwidth*2);
        Vector3 dir = Vector3.zero;
        /*//Move Mid Points To the Half Distance of there SideWalk Area.
        for (int i = 0; i < midPoints.Count; i++)
        { 
            //At Beggining or Middle
            if (i != midPoints.Count - 1)
            {
                dir = midPoints[i + 1] - midPoints[i];
            }
            else
            {
                //Last Point
                dir = midPoints[i] - midPoints[i - 1];
            }
            Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;
            //Project Point Right
            Vector3 p = midPoints[i] + (-left * (sideWalkRadius/2));
            midPoints[i] = p;
        }*/
        //Grab Mid Points of Road Mesh in World Space.
        for (int i = 0; i < midPoints.Count; i ++)
        {
            int p1 = i + 1;
            if (i == midPoints.Count - 1) {
                p1 = 0;
            }
            //Grab Mid Point of Left and Right Points of Road Mesh
            Vector3 midPoint = midPoints[i];
            //Grab Next Mid Point
            Vector3 midPoint2 = midPoints[p1];
            //Remove Grass
            BlendTerrainAtLine(t, midPoint, midPoint2, sideWalkRadius, layerMask, sideWalkHeight);
        }
    }

    //Terrain Blending Function Requires SideWalkMesh
    void BlendSideWalk(Vector3 transformPos, Mesh blendMesh, float roadWidth, int radius, float zBuffer, float edgeOffset, float smoothBorder){
		int hmWidth = terrain.terrainData.heightmapWidth;
		int hmHeight = terrain.terrainData.heightmapHeight;
		Vector3 terrPos = terrain.GetPosition ();
		Vector3 tSize = terrain.terrainData.size;
		int heightRes = terrain.terrainData.heightmapResolution;
		//Convert From Terrain Space to WorldSpace
		Vector3 mapScale = terrain.terrainData.heightmapScale;
		//Grab local Heights to the Mesh Vertices
		Vector3[] meshVerts = blendMesh.vertices;
		for(int i = 0;i<meshVerts.Length;i++){
			meshVerts[i] = meshVerts[i]+transformPos;
		}

		//Grab Four at a Time
		for(int j = 0;j<meshVerts.Length-2;j+=2){
			Vector3 center = meshVerts[j];
			center+=(meshVerts[j+1]+meshVerts[j+2]+meshVerts[j+3]);
			center = center/4;
			Plane quadPlane = new Plane (meshVerts [j+1], meshVerts [j+2], meshVerts [j+3]);
			//Test CenterLine
			Vector3 p0 = (meshVerts[j]+meshVerts[j+1])/2;
			Vector3 p1 = (meshVerts[j+2]+meshVerts[j+3])/2;
			//CiDyUtils.MarkPoint(p0,j);
			//CiDyUtils.MarkPoint(p1,j);
			Vector3 fwd = (p1-p0).normalized;
			Vector3 left = Vector3.Cross(fwd,Vector3.up).normalized;
			Vector3 segPoint = Vector3.zero;
			float r = 0;
			float s = 0;
			//Values used for Height Smoothing
			float b = 1;//Coefficient
			float oHeight = 0;//Orig Height
			float eHeight = 0;//Enforced Height
			Vector3 coord = GetNormalizedPositionRelativeToTerrain (center, terrPos, tSize);
			// get the position of the terrain heightmap where this game object is
			int posXInTerrain = (int)(coord.x*hmWidth); 
			int posYInTerrain = (int)(coord.z*hmHeight);
			// we set an offset so that all the raising terrain is under this game object
			int offset = radius/2;
			// get the heights of the terrain under this game object
			float[,] heights = terrain.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset,radius,radius);
			//Debug.Log ("MapScale: " + mapScale);
			int count = 0;
			Vector3[] terrainVectors = new Vector3[heights.Length];
			/*Vector3 rayPos = segPoint+(Vector3.left*smoothBorder);
			float leftOHeight = terrain.SampleHeight(rayPos);
			Vector3 rayPos = segPoint+(Vector3.right*(roadWidth+smoothBorder));
			float rightOHeight = terrain.SampleHeight(rayPos);*/
			//Iterate through Heights
			for(int z = 0;z<radius;z++){
				for(int x = 0;x<radius;x++){
					float pX = (hmWidth/tSize.x/heightRes)+terrPos.x+(mapScale.x*((posXInTerrain-offset)+x));
					float pY = (heights[z,x]*tSize.y)+terrPos.y;
					float pZ = (hmHeight/tSize.z/heightRes)+terrPos.z+(mapScale.z*((posYInTerrain-offset)+z));
					terrainVectors[count] = new Vector3 (pX,pY,pZ);
					Vector3 p = new Vector3(pX,0,pZ);
					Vector3 p2 = new Vector3(p0.x,0,p0.z);
					Vector3 p3 = new Vector3(p1.x,0,p1.z);
					//Is the Position inside Quad or Outside?
					float distFromLine = CiDyUtils.DistanceToLine(p,p2,p3,ref r,ref s);
					if(r<-0.2||r>1.2 || distFromLine > (roadWidth/2+edgeOffset+smoothBorder)){
						//Debug.Log("Out of Range");	
						//Update Count
						count++;
						continue;
					}
					//We need to Determine the B Value based on Interpolation from 0-SmoothBorder
					Vector3 origP = terrainVectors[count]+(Vector3.up*1000);
					Ray ray = new Ray(origP,Vector3.down);
					float rayDistance;
					if (quadPlane.Raycast(ray, out rayDistance)){
						Vector3 planeHit = ray.GetPoint(rayDistance);
						eHeight = planeHit.y-zBuffer;
						//CiDyUtils.MarkPoint(planeHit,count);
					}
					if(distFromLine <= roadWidth/2+edgeOffset){
						//Inside Quad coefficient = 1;
						b=1;
						oHeight = 0;
					} else {
						//Find Line of Intersection
						Vector3 leftSegP = terrainVectors[count]+(left*1000);
						Vector3 rightSegP = terrainVectors[count]+(-left*1000);
						Vector3 centFwd = p0+(fwd*1000);
						Vector3 centBck = p0+(-fwd*1000);
						CiDyUtils.LineIntersection(centFwd,centBck,leftSegP,rightSegP, ref segPoint);
						segPoint.y = center.y;
						//CiDyUtils.MarkPoint(segPoint,99);
						if(s>0){
							Vector3 rayPos = segPoint+(-left*(roadWidth/2+edgeOffset+smoothBorder))+Vector3.up*1000;
							if(Physics.Raycast(rayPos,Vector3.down, out hit, Mathf.Infinity, roadMask)){
								oHeight = hit.point.y;
								//CiDyUtils.MarkPoint(hit.point,j);
							}
							/*Vector3 smpleHeight = GetNormalizedPositionRelativeToTerrain (rayPos, terrain);
							// get the position of the terrain heightmap where this game object is
							int posX = (int)(smpleHeight.x*hmWidth); 
							int posY = (int)(smpleHeight.z*hmHeight);
							oHeight = terrain.terrainData.GetHeight(posX-offset,posY-offset);*/
						} else if(s<0){
							Vector3 rayPos = segPoint+(left*(roadWidth/2+edgeOffset+smoothBorder))+Vector3.up*1000;
							if(Physics.Raycast(rayPos,Vector3.down,out hit, Mathf.Infinity, roadMask)){
								oHeight = hit.point.y;
								//CiDyUtils.MarkPoint(hit.point,j);
							}
							/*Vector3 smpleHeight = GetNormalizedPositionRelativeToTerrain (rayPos, terrain);
							// get the position of the terrain heightmap where this game object is
							int posX = (int)(smpleHeight.x*hmWidth); 
							int posY = (int)(smpleHeight.z*hmHeight);
							oHeight = terrain.terrainData.GetHeight(posX-offset,posY-offset);*/
						}
						//Outside Quad but within Smoothing.
						float dist = distFromLine-(edgeOffset+roadWidth/2);
						b = Mathf.InverseLerp((roadWidth/2)+smoothBorder,0,dist);
					}
					//Debug.Log("DistFromLine: "+distFromLine+" /R: "+r+" /S: "+s+" /for sample: "+count);
					//oHeight = Mathf.InverseLerp(0,tSize.y,oHeight);
					oHeight = oHeight/tSize.y;
					eHeight = eHeight/tSize.y;
					//if(((b*eHeight)+(1-b)*oHeight) == 0){
					//	Debug.Log("Within Range B: "+b+" Enforced Height: "+eHeight+" OrigHeight: "+oHeight);
					//}
					//eHeight = Mathf.InverseLerp(0,tSize.y,eHeight);
					//Debug.Log("Within Range B: "+b+" Enforced Height: "+eHeight+" OrigHeight: "+oHeight);
					//Update Count
					count++;
					heights[z,x] = ((b*eHeight)+(1-b)*oHeight);
				}
			}
			//Set the new height
			terrain.terrainData.SetHeights(posXInTerrain-offset,posYInTerrain-offset,heights);
		}
	}

    //Updated Blend Terrain At Point
    // Set all Terrain Heights to just under our Position in a Heights map that are under the Position in Space and Radius
    void BlendTerrainAtLine(Terrain t, Vector3 pA, Vector3 pB, int radius, int mask, float offsetHeight)
    {
        Vector3 center = (pA + pB) / 2;
        int smoothBorder = radius * 2;
        int scaledRadius = (radius * 4) + smoothBorder;
        //Debug.Log("Blending Terrain Under Object, Border: "+smoothBorder+" Radius: "+radius);
        int mWidth = t.terrainData.heightmapWidth;
        int mHeight = t.terrainData.heightmapHeight;
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center, terrPos, tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posZInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((mWidth / tSize.x) * scaledRadius);
        int zDetail = Mathf.RoundToInt((mHeight / tSize.z) * scaledRadius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posZInTerrain - (zDetail / 2);
        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        // get the Height map of the terrain under this game object
        float[,] map = terrain.terrainData.GetHeights(offsetX, offsetZ, xDetail, zDetail);//GetHeightsFromArray(offsetX, offsetZ, xDetail, zDetail, storedHeights);
        Vector3 mapScale = t.terrainData.heightmapScale;
        float totalSmoothDist = smoothBorder;  

        for (int x = 0; x < xDetail; x++)
        {
            for (int y = 0; y < zDetail; y++)
            {
                //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                /*float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * ((posXInTerrain - offsetZ) + y));
                float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * ((offsetX) + x));*/
                float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * (offsetX + y));
                float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * (offsetZ + x));
                Vector3 p0 = new Vector3(pX, 0, pZ);
                Vector3 worldPos = new Vector3(pX, center.y+1000, pZ);
                //worldPos += terrPos;
                Ray ray = new Ray(worldPos, Vector3.down);
                RaycastHit hit;
                //CiDyUtils.MarkPoint(new Vector3(pX, center.y-9f, pZ),x+y);
                float centerDist = Vector3.Distance(p0, new Vector3(center.x, 0, center.z));
                float r = 0;
                float s = 0;
                CiDyUtils.DistanceToLineR(p0, pA, pB, ref r, ref s);
                
                if (r < 1f && r > 0f)
                {
                    //Now Check Distance
                    if (centerDist <= radius)
                    {
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                        {
                            //Flatten This Point
                            map[x, y] = ((hit.point.y - offsetHeight) / tSize.y);//flattenPos.y;
                        }
                        else
                        {
                            //Run Special Line Test to determine what Enforced Height & Original Height the Lerp function should be based on.
                            //float oHeight = map[x, y];//Original Height in Map Before
                            //What is the R Value(Lerp Value)
                            //Lerp Height Value
                            map[x, y] = (Vector3.Lerp(pA, pB, r).y - 0.1f) / tSize.y;
                        }
                    }
                    else if (centerDist <= (radius + smoothBorder))
                    {
                        //CiDyUtils.MarkPoint(new Vector3(pX, center.y-5f, pZ), (x + y) + 77777);
                        Vector3 dir = p0 - new Vector3(center.x, 0, center.z);
                        var distance = dir.magnitude;
                        var direction = dir / distance; // This is now the normalized direction.
                        Vector3 newPoint = new Vector3(center.x, 0, center.z);
                        newPoint = newPoint + (direction * radius);

                        float curDist = Vector3.Distance(newPoint, p0);
                        float coefficient = (curDist / totalSmoothDist);
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                        {
                            //Flatten This Point
                            map[x, y] = ((hit.point.y - offsetHeight) / tSize.y);
                        }
                        else
                        {
                            //Run Special Line Test to determine what Enforced Height & Original Height the Lerp function should be based on.
                            float oHeight = map[x, y];//Original Height in Map Before
                            //What is the R Value(Lerp Value)
                            ///map[x, y] = (Vector3.Lerp(pA, pB, r).y - 0.1f) / tSize.y;
                            //Lerp Height Value
                            map[x, y] = Mathf.Lerp(((Vector3.Lerp(pA, pB, r).y - 0.1f) / tSize.y), oHeight, coefficient);
                        }
                    }
                }
                else {
                    //Only Blend Areas not under a Road Piece
                    if (!Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                    {
                        if (s < 0f)
                        {
                            //We are Outside the Line Range and Outside the Interior of the Polygon
                            //Special Case We want to lerp the Smoothing based on nearest Point position.
                            if (r > 1f)
                            {
                                //Project PB left by Direction
                                Vector3 dir = pB - pA;
                                var distance = dir.magnitude;
                                //var direction = dir / distance; // This is now the normalized direction.
                                Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;
                                Vector3 newPoint = (new Vector3(pB.x, 0, pB.z) + (left * radius));
                                float pointDist = Vector3.Distance(p0, newPoint);
                                float coefficient = (pointDist / totalSmoothDist);
                                //Run Special Line Test to determine what Enforced Height & Original Height the Lerp function should be based on.
                                float oHeight = map[x, y];//Original Height in Map Before
                                //What is the R Value(Lerp Value)
                                //Lerp Height Value
                                map[x, y] = Mathf.Lerp(((pB.y - 0.1f) / tSize.y), oHeight, coefficient);
                            }
                        }
                    }
                }
            }
        }

        //Set Map back to Detail
        t.terrainData.SetHeights(offsetX, offsetZ, map);
    }

    //Updated Blend Terrain At Point
    // Set all Terrain Heights to just under our Position in a Heights map that are under the Position in Space and Radius
    void BlendSideWalkAtLine(Terrain t, Vector3 pA, Vector3 pB, int radius, int roadMask)
    {
        Vector3 center = (pA + pB) / 2;
        int smoothBorder = radius * 2;
        int scaledRadius = (radius * 4) + smoothBorder;
        Debug.Log("Blending Terrain Under Object, Border: "+smoothBorder+" Radius: "+radius);
        int mWidth = t.terrainData.heightmapWidth;
        int mHeight = t.terrainData.heightmapHeight;
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center, terrPos, tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((mWidth / t.terrainData.size.x) * scaledRadius);
        int zDetail = Mathf.RoundToInt((mHeight / t.terrainData.size.z) * scaledRadius);
        int offsetX = posXInTerrain - (xDetail/2);
        int offsetZ = posYInTerrain - (zDetail/2);
        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        //Debug.Log("Terrain Width: " + t.terrainData.heightmapWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
        // get the grass map of the terrain under this game object
        float[,] map = t.terrainData.GetHeights(offsetX, offsetZ, xDetail, zDetail);
        //Debug.Log("Height Map Length: " + map.Length);
        //Debug.Log(coord.x + ", " + coord.z);
        Vector3 mapScale = t.terrainData.heightmapScale;
        //float totalSmoothDist = smoothBorder;

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                float pX = (mWidth / tSize.x / mWidth) + (terrPos.x * tSize.x) + (mapScale.x * ((offsetX) + y));
                float pZ = (mHeight / tSize.z / mHeight) + (terrPos.z * tSize.z) + (mapScale.z * ((offsetZ) + x));
               Vector3 p0 = new Vector3(pX, 0, pZ);
                Vector3 worldPos = new Vector3(pX, center.y + 1000, pZ);
                //worldPos += terrPos;
                Ray ray = new Ray(worldPos, Vector3.down);
                RaycastHit hit;
                //CiDyUtils.MarkPoint(new Vector3(pX, center.y-9f, pZ),x+y);
                //float centerDist = Vector3.Distance(p0, new Vector3(center.x, 0, center.z));
                float r = 0;
                float s = 0;
                CiDyUtils.DistanceToLineR(p0, pA, pB, ref r, ref s);


                if (Physics.Raycast(ray, out hit, Mathf.Infinity, roadMask))
                {
                    //Flatten This Point
                    map[x, y] = ((hit.point.y - 0.1f) / tSize.y);//flattenPos.y;
                }
            }
        }

        //Set Map back to Detail
        t.terrainData.SetHeights(offsetX, offsetZ, map);
    }

    //Updated Blend Terrain At Point
    // Set all Terrain Heights to just under our Position in a Heights map that are under the Position in Space and Radius
    void BlendTerrainOfRoad(Vector3 terrPos, Vector3 tSize, Vector3 mapScale, Vector3 pA, Vector3 pB, Vector3 pC, Vector3 pD, int radius, int mask, int mWidth, int mHeight)
    {
        //Calculate Center of Four Points
        Vector3 midA = (pA + pB) / 2;
        Vector3 midB = (pC + pD) / 2;
        Vector3 center = (midA + midB) / 2;
        //float smoothOffset = radius * 0.1618f;
        int smoothBorder = radius * 3;
        int scaledRadius = (radius * 4) + smoothBorder;
        //Debug.Log("Blending Terrain Under Object, Border: "+smoothBorder+" Radius: "+radius);
        //Get Normalized Point of Center.
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center, terrPos, tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posZInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((mWidth / tSize.x) * scaledRadius);
        int zDetail = Mathf.RoundToInt((mHeight / tSize.z) * scaledRadius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posZInTerrain - (zDetail / 2);
        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        // get the Height map of the terrain under this game object
        float[,] map = terrain.terrainData.GetHeights(offsetX, offsetZ, xDetail, zDetail);//GetHeightsFromArray(offsetX, offsetZ, xDetail, zDetail, storedHeights);
        float totalSmoothDist = smoothBorder;
        //Queue This Function for Main Thread execution.
        //Action aFunction = () => {
        
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(1); z++)
            {
                //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * (offsetX + z));
                float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * (offsetZ + x));
                Vector3 p0 = new Vector3(pX, 0, pZ);
                Vector3 worldPos = new Vector3(pX, center.y+1000, pZ);
                //Create Ray
                Ray ray = new Ray(worldPos, Vector3.down);
                RaycastHit hit;
                //CiDyUtils.MarkPoint(new Vector3(pX, center.y-9f, pZ),x+y);
                //float centerDist = Vector3.Distance(p0, new Vector3(center.x, 0, center.z));
                //Get Point Data of Top Line and Bottom Line to determine what Points are inside the Desired blending Area.
                float Tr = 0;//Top Lines R Value
                float Ts = 0;//Top Lines S Value
                float Br = 0;//Bottom Lines R Value
                float Bs = 0;//Bottom Lines S Value
                CiDyUtils.DistanceToLineR(p0, pC, pD, ref Tr, ref Ts);//Top Line Dist
                CiDyUtils.DistanceToLineR(p0, pA, pB, ref Br, ref Bs);
                //Only Look at Points that are within Ts>=0 && bS <= 0
                if (Ts >= 0.0f && Bs <= 0.0f)
                {
                    //Check for hits against the Road
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                    {
                        map[x, z] = ((hit.point.y - 0.1f) / tSize.y);//flattenPos.y;
                        //CiDyUtils.MarkPoint(hit.point, 999);
                    }
                    else
                    {
                        //Get Height at Terrain Position currently (Original Height)
                        float oHeight = map[x, z];
                        //Get Center Line R/S Data
                        float Cr = 0;//Center Line R Value
                        float Cs = 0;//Center Line S Value
                        CiDyUtils.DistanceToLineR(p0, midA, midB, ref Cr, ref Cs);
                        //Determine which side of road this point is on. Left of center or right?
                        //Is it left or Right of center Line?
                        if (Cs < 0.0f)
                        {
                            //Left of line.
                            //Grab left Edge of Mesh Segment we are testing.
                            float Lr = 0;//Center Line R Value
                            float Ls = 0;//Center Line S Value
                            float LDist = CiDyUtils.DistanceToLineR(p0, pA, pC, ref Lr, ref Ls);
                            float coefficient = (LDist / totalSmoothDist);
                            if (LDist <= (smoothBorder))
                            {
                                //Lerp the height of this point
                                map[x, z] = Mathf.Lerp(((Vector3.Lerp(pA, pC, Lr).y - 0.1f) / tSize.y), oHeight, coefficient);
                            }
                        }
                        else if (Cs > 0.0f)
                        {
                            //Right of Line
                            //Grab left Edge of Mesh Segment we are testing.
                            float Rr = 0;//Center Line R Value
                            float Rs = 0;//Center Line S Value
                            float RDist = CiDyUtils.DistanceToLineR(p0, pB, pD, ref Rr, ref Rs);
                            float coefficient = (RDist / totalSmoothDist);
                            if (RDist <= (smoothBorder))
                            {
                                //Lerp the height of this point
                                map[x, z] = Mathf.Lerp(((Vector3.Lerp(pA, pC, Rr).y - 0.1f) / tSize.y), oHeight, coefficient);
                            }
                        }
                    }
                }
            }
        }

        terrain.terrainData.SetHeights(offsetX, offsetZ, map);
        //Set Map back to Detail
        //SetHeightsToArray(offsetX, offsetZ, map, storedHeights);
        //};
        //Queue this Up.
        /*QueueMainThreadFunction(aFunction);
        #if UNITY_EDITOR
            EditorApplication.update += Update;
        #endif*/
    }

    //Updated Blend Terrain to Polygon (Assumed Fead polygon outline in counter clockwise and that the Shape has a Collider)
    // Set all Terrain Heights to just under our Position in a Heights map that are under the Position in Space and Radius
    void BlendTerrainAtPolygon(Terrain t, Vector3 pA, Vector3 pB, int radius, int mask)
    {
        //radius = smoothBorder;
        //Debug.Log("Radius: " + radius);
        Vector3 center = (pA + pB) / 2;
        //float smoothOffset = radius * 0.1618f;
        int smoothBorder = radius * 3;
        int scaledRadius = (radius * 4) + smoothBorder;
        //Debug.Log("Blending Terrain Under Object, Border: "+smoothBorder+" Radius: "+radius);
        int mWidth = t.terrainData.heightmapWidth;
        int mHeight = t.terrainData.heightmapHeight;
        //Debug.Log(coord.x + ", " + coord.z);
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center, terrPos, tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((t.terrainData.heightmapWidth / t.terrainData.size.x) * scaledRadius);
        int zDetail = Mathf.RoundToInt((t.terrainData.heightmapHeight / t.terrainData.size.z) * scaledRadius);
        int offsetX = posXInTerrain - (xDetail / 2);
        int offsetZ = posYInTerrain - (zDetail / 2);
        //Make sure Offset is in Bounds
        if (offsetX < 0)
        {
            offsetX = 0;
            //Debug.Log("Fixed Offset X");
        }
        else
        {
            //Handle Positive Fix
            if ((offsetX + xDetail) > mWidth)
            {
                int diff = (mWidth - offsetX);
                offsetX -= (mWidth - diff);
                //Debug.Log("Diff: " + diff + " Width: " + mWidth);
            }
        }
        if (offsetZ < 0)
        {
            offsetZ = 0;
            //Debug.Log("Fixed Offset Z");
        }
        else
        {
            if ((offsetZ + zDetail) > mHeight)
            {
                int diff = (mHeight - offsetZ);
                offsetZ -= (mHeight - diff);
                //Debug.Log("Diff: " + diff + " Height: " + mHeight);
            }
        }
        //Debug.Log("Terrain Width: " + t.terrainData.heightmapWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
        // get the grass map of the terrain under this game object
        float[,] map = t.terrainData.GetHeights(offsetX, offsetZ, xDetail, zDetail);
        //Debug.Log("Height Map Length: " + map.Length);
        Vector3 mapScale = t.terrainData.heightmapScale;
        float totalSmoothDist = smoothBorder;

        // Setup the command and result buffers
        /*var results = new NativeArray<RaycastHit>(map.GetLength(0), Allocator.Temp);
        var commands = new NativeArray<RaycastCommand>((int)map.LongLength, Allocator.Temp);*/

        int count = 0;
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                count++;
                //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * (offsetX + y));
                float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * (offsetZ + x));
                Vector3 p0 = new Vector3(pX, 0, pZ);
                Vector3 worldPos = new Vector3(pX, center.y + 1000, pZ);
                //Create Raycast Array
                Vector3 direction = Vector3.down;
                //commands[count] = new RaycastCommand(worldPos, direction);
                //Debug.LogError("Finsihing putting in Batch Raycast");
                //worldPos += terrPos;
                Ray ray = new Ray(worldPos, Vector3.down);
                RaycastHit hit;
                //CiDyUtils.MarkPoint(new Vector3(pX, center.y-9f, pZ),x+y);
                float centerDist = Vector3.Distance(p0, new Vector3(center.x, 0, center.z));
                float r = 0;//Behind or In Front of Line(0-1) anythihng below or above is outside of line.
                float s = 0;//Left and Right of Line
                float dist = CiDyUtils.DistanceToLineR(p0, new Vector3(pA.x, 0, pA.z), new Vector3(pB.x, 0, pB.z), ref r, ref s);
                //If Inside Polygon Area and Line Are. We Want to only worry about Hitting a Collider point.
                if (s < 0f)
                {
                    if (r > 1f || r < 0f)
                    {
                        //Only look at points within the Line Segment
                        continue;
                    }
                }
                //If its right of Line and within Spacing. Dist from line is used to determine coeeficient
                if (s > 0f)
                {
                    //Are we in the Line Range? or Out of it?
                    if (r > 0f && r < 1f)
                    {
                        //Outside Polygon but inside Line Space
                        float coefficient = (dist / totalSmoothDist);
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                        {
                            //Flatten This Point
                            map[x, y] = ((hit.point.y-0.1f) / tSize.y);
                        }
                        else
                        {
                            //Run Special Line Test to determine what Enforced Height & Original Height the Lerp function should be based on.
                            float oHeight = map[x, y];//Original Height in Map Before
                            //What is the R Value(Lerp Value)
                            ///map[x, y] = (Vector3.Lerp(pA, pB, r).y - 0.1f) / tSize.y;
                            //Lerp Height Value
                            map[x, y] = Mathf.Lerp(((Vector3.Lerp(pA, pB, r).y-0.1f) / tSize.y), oHeight, coefficient);
                        }
                    }
                    else
                    {
                        //We are Outside the Line Range and Outside the Interior of the Polygon
                        //Special Case We want to lerp the Smoothing based on nearest Point position.
                        if (r > 1f)
                        {
                            float pointDist = Vector3.Distance(p0, new Vector3(pB.x, 0, pB.z));
                            float coefficient = (pointDist / totalSmoothDist);
                            //Run Special Line Test to determine what Enforced Height & Original Height the Lerp function should be based on.
                            float oHeight = map[x, y];//Original Height in Map Before
                            //What is the R Value(Lerp Value)
                            //Lerp Height Value
                            map[x, y] = Mathf.Lerp(((pB.y-0.1f) / tSize.y), oHeight, coefficient);
                        }
                    }
                }
                else
                {
                    //Points are within Polygon Interior
                    //Now Check Distance
                    if (centerDist <= (radius + smoothBorder))
                    {
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
                        {
                            //Flatten This Point
                            map[x, y] = ((hit.point.y-0.1f)/tSize.y);
                        }
                    }
                }
            }
        }

        //Set Map back to Detail
        t.terrainData.SetHeights(offsetX, offsetZ, map);
    }

    //Updated Blend Terrain At Point
    // Set all Terrain Heights to just under our Position in a Heights map that are under the Position in Space and Radius
    void BlendTerrainAtPoint(Terrain t, Vector3 center, int radius)
    {
        int smoothBorder = radius;
        int scaledRadius = (radius * 4) + smoothBorder;
        //Debug.Log("Blending Terrain Under Object, Border: "+smoothBorder+" Radius: "+radius);
        int mWidth = t.terrainData.heightmapWidth;
        int mHeight = t.terrainData.heightmapHeight;
        Vector3 tSize = t.terrainData.size;
        Vector3 terrPos = t.transform.position;
        Vector3 mapScale = t.terrainData.heightmapScale;
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(center, terrPos, tSize);
        // get the position of the terrain heightmap where this Game Object is
        int posXInTerrain = (int)(coord.x * mWidth);
        int posYInTerrain = (int)(coord.z * mHeight);
        // we set an offset so that all the raising terrain is under this game object
        int xDetail = Mathf.RoundToInt((t.terrainData.heightmapWidth / t.terrainData.size.x) * scaledRadius);
        int zDetail = Mathf.RoundToInt((t.terrainData.heightmapHeight / t.terrainData.size.z) * scaledRadius);
        int offset = xDetail / 2;
        //Debug.Log("Terrain Width: " + t.terrainData.heightmapWidth + " TerrainSize.x: " + t.terrainData.size.x + " XDetail: " + xDetail + " ZDetail: " + zDetail);
        // get the grass map of the terrain under this game object
        float[,] map = t.terrainData.GetHeights(posXInTerrain - offset, posYInTerrain - offset, xDetail, zDetail);
        //Debug.Log("Height Map Length: " + map.Length);
        //Debug.Log(coord.x + ", " + coord.z);
        float oHeight = 0;//Original Height
        float eHeight = coord.y;//The Enforced Height
        float totalSmoothDist = smoothBorder;

        for (int x = 0; x < xDetail; x++)
        {
            for (int y = 0; y < zDetail; y++)
            {
                //Grab Original Height
                oHeight = map[x, y];//Original Height in Map Before
                //Convert Graph Point to World Coords and Compare to World Space 2D and determine if its within Radius+SmoothBorder
                float pX = (mWidth / tSize.x / mWidth) + terrPos.x + (mapScale.x * ((posXInTerrain - offset) + y));
                float pZ = (mHeight / tSize.z / mHeight) + terrPos.z + (mapScale.z * ((posYInTerrain - offset) + x));
                Vector3 p0 = new Vector3(pX, 0, pZ);
                Vector3 worldPos = new Vector3(pX, center.y - 10000, pZ);
                //worldPos += terrPos;
                Ray ray = new Ray(worldPos, Vector3.up);
                RaycastHit hit;
                //CiDyUtils.MarkPoint(new Vector3(pX, center.y-9f, pZ),x+y);
                float centerDist = Vector3.Distance(p0, new Vector3(center.x, 0, center.z));
                //Now Check Distance
                if (centerDist <= radius)
                {
                    if (Physics.Raycast(ray, out hit))
                    {
                        //CiDyUtils.MarkPoint(hit.point, x + y);
                        //CiDyUtils.MarkPoint(new Vector3(pX, center.y, pZ), (x + y)+9999);
                        //Flatten No Smoothing
                        //Flatten This Point
                        map[x, y] = ((hit.point.y - 0.1f) / tSize.y);//flattenPos.y;
                    }
                }
                else if (centerDist <= (radius + smoothBorder))
                {
                    //CiDyUtils.MarkPoint(new Vector3(pX, center.y-5f, pZ), (x + y) + 77777);
                    Vector3 dir = p0 - new Vector3(center.x, 0, center.z);
                    var distance = dir.magnitude;
                    var direction = dir / distance; // This is now the normalized direction.
                    Vector3 newPoint = new Vector3(center.x, 0, center.z);
                    newPoint = newPoint + (direction * radius);

                    float curDist = Vector3.Distance(newPoint, p0);
                    float coefficient = (curDist / totalSmoothDist);
                    if (Physics.Raycast(ray, out hit))
                    {
                        //Flatten This Point
                        map[x, y] = ((hit.point.y-0.1f) / tSize.y);//flattenPos.y;
                    }
                    else
                    {
                        map[x, y] = Mathf.Lerp(eHeight, oHeight, coefficient);
                    }
                }
            }
        }

        //Set Map back to Detail
        t.terrainData.SetHeights(posXInTerrain - offset, posYInTerrain - offset, map);
    }
    
    //Terrain Blending Function Requires Road Mesh
    public void BlendRoad(Mesh blendMesh, float roadWidth, int radius, float zBuffer, float edgeOffset, float smoothBorder){
		int hmWidth = terrain.terrainData.heightmapWidth;
		int hmHeight = terrain.terrainData.heightmapHeight;
		Vector3 terrPos = terrain.GetPosition ();
		Vector3 tSize = terrain.terrainData.size;
		int heightRes = terrain.terrainData.heightmapResolution;
		//Convert From Terrain Space to WorldSpace
		Vector3 mapScale = terrain.terrainData.heightmapScale;
		//Grab local Heights to the Mesh Vertices
		Vector3[] meshVerts = blendMesh.vertices;
		/*List<Vector3> reverseVerts = new List<Vector3> (meshVerts);
		reverseVerts = reverseVerts.OrderBy(x => x.y).ToList();
		meshVerts = reverseVerts.ToArray ();*/
		//Grab Four at a Time
		for(int j = 0;j<meshVerts.Length-2;j+=2){
			Vector3 center = meshVerts[j];
			center+=(meshVerts[j+1]+meshVerts[j+2]+meshVerts[j+3]);
			center = center/4;
			Plane quadPlane = new Plane (meshVerts [j+1], meshVerts [j+2], meshVerts [j+3]);
			//Test CenterLine
			Vector3 p0 = (meshVerts[j]+meshVerts[j+1])/2;
			Vector3 p1 = (meshVerts[j+2]+meshVerts[j+3])/2;
			Vector3 fwd = (p1-p0).normalized;
			Vector3 left = Vector3.Cross(fwd,Vector3.up).normalized;
			Vector3 segPoint = Vector3.zero;
			float r = 0;
			float s = 0;
			//Values used for Height Smoothing
			float b = 1;//Coefficient
			float oHeight = 0;//Orig Height
			float eHeight = 0;//Enforced Height
			Vector3 coord = GetNormalizedPositionRelativeToTerrain (center, terrPos, tSize);
			// get the position of the terrain heightmap where this game object is
			int posXInTerrain = (int)(coord.x*hmWidth); 
			int posYInTerrain = (int)(coord.z*hmHeight);
			// we set an offset so that all the raising terrain is under this game object
			int offset = radius/2;
			// get the heights of the terrain under this game object
			float[,] heights = terrain.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset,radius,radius);
			//Debug.Log ("MapScale: " + mapScale);
			int count = 0;
			Vector3[] terrainVectors = new Vector3[heights.Length];
			/*Vector3 rayPos = segPoint+(Vector3.left*smoothBorder);
			float leftOHeight = terrain.SampleHeight(rayPos);
			Vector3 rayPos = segPoint+(Vector3.right*(roadWidth+smoothBorder));
			float rightOHeight = terrain.SampleHeight(rayPos);*/
			//Iterate through Heights
			for(int z = 0;z<radius;z++){
				for(int x = 0;x<radius;x++){
					float pX = (hmWidth/tSize.x/heightRes)+terrPos.x+(mapScale.x*((posXInTerrain-offset)+x));
					float pY = (heights[z,x]*tSize.y)+terrPos.y;
					float pZ = (hmHeight/tSize.z/heightRes)+terrPos.z+(mapScale.z*((posYInTerrain-offset)+z));
					terrainVectors[count] = new Vector3 (pX,pY,pZ);
					Vector3 p = new Vector3(pX,0,pZ);
					//Is the Position inside Quad or Outside?
					float distFromLine = CiDyUtils.DistanceToLineR(p,p0,p1,ref r,ref s);
					if(r<-0.2||r>1.2 || distFromLine > (roadWidth/2+edgeOffset+smoothBorder)){
						//Debug.Log("Out of Range");	
						//Update Count
						count++;
						continue;
					}
					//We need to Determine the B Value based on Interpolation from 0-SmoothBorder
					Vector3 origP = terrainVectors[count]+(Vector3.up*1000);
					Ray ray = new Ray(origP,Vector3.down);
					float rayDistance;
					if (quadPlane.Raycast(ray, out rayDistance)){
						Vector3 planeHit = ray.GetPoint(rayDistance);
						eHeight = planeHit.y-zBuffer;
						//CiDyUtils.MarkPoint(planeHit,count);
					}
					if(distFromLine <= roadWidth/2+edgeOffset){
						//Inside Quad coefficient = 1;
						b=1;
						oHeight = 0;
					} else {
						//Find Line of Intersection
						Vector3 leftSegP = terrainVectors[count]+(left*1000);
						Vector3 rightSegP = terrainVectors[count]+(-left*1000);
						Vector3 centFwd = p0+(fwd*1000);
						Vector3 centBck = p0+(-fwd*1000);
						CiDyUtils.LineIntersection(centFwd,centBck,leftSegP,rightSegP, ref segPoint);
						segPoint.y = center.y;
						//CiDyUtils.MarkPoint(segPoint,99);
						if(s>0){
							Vector3 rayPos = segPoint+(-left*(roadWidth/2+edgeOffset+smoothBorder))+Vector3.up*1000;
							if(Physics.Raycast(rayPos,Vector3.down, out hit, Mathf.Infinity, roadMask)){
								oHeight = hit.point.y;
							}
							/*Vector3 smpleHeight = GetNormalizedPositionRelativeToTerrain (rayPos, terrain);
							// get the position of the terrain heightmap where this game object is
							int posX = (int)(smpleHeight.x*hmWidth); 
							int posY = (int)(smpleHeight.z*hmHeight);
							oHeight = terrain.terrainData.GetHeight(posX-offset,posY-offset);*/
						} else if(s<0){
							Vector3 rayPos = segPoint+(left*(roadWidth/2+edgeOffset+smoothBorder))+Vector3.up*1000;
							if(Physics.Raycast(rayPos,Vector3.down,out hit, Mathf.Infinity, roadMask)){
								oHeight = hit.point.y;
							}
							/*Vector3 smpleHeight = GetNormalizedPositionRelativeToTerrain (rayPos, terrain);
							// get the position of the terrain heightmap where this game object is
							int posX = (int)(smpleHeight.x*hmWidth); 
							int posY = (int)(smpleHeight.z*hmHeight);
							oHeight = terrain.terrainData.GetHeight(posX-offset,posY-offset);*/
						}
						//Outside Quad but within Smoothing.
						float dist = distFromLine-(edgeOffset+roadWidth/2);
						b = Mathf.InverseLerp((roadWidth/2)+smoothBorder,0,dist);
					}
					//Debug.Log("DistFromLine: "+distFromLine+" /R: "+r+" /S: "+s+" /for sample: "+count);
					//oHeight = Mathf.InverseLerp(0,terrain.terrainData.size.y,oHeight);
					oHeight = oHeight/tSize.y;
					eHeight = eHeight/tSize.y;
					//eHeight = Mathf.InverseLerp(0,tSize.y,eHeight);
					//Debug.Log("Within Range B: "+b+" Enforced Height: "+eHeight+" OrigHeight: "+oHeight);
					//Update Count
					count++;
					heights[z,x] = ((b*eHeight)+(1-b)*oHeight);
				}
			}
			//Set the new height
			terrain.terrainData.SetHeights(posXInTerrain-offset,posYInTerrain-offset,heights);
		}
	}

    //This Function will take a building GameObject and Extrude its Box Collider if the Terrain Below it is farther than the Threshold.
    public void CheckFoundation(GameObject building, List<Vector3> buildingLot, float threshold, bool prefabBuilding) {
        //Check for Nulls
        if (building == null || building.GetComponentInChildren<Renderer>() == null)
        {
            Debug.Log("No Building or No Renderer attached to Building");
            return;
        }
        if (!prefabBuilding && buildingLot.Count == 0) {
            //Check for Nulls
            Debug.Log("Building Lot Empty");
            return;
        }
        //First we want to Store the Buildings placement
        Vector3 storedPos = building.transform.position;
        Quaternion rot = building.transform.rotation;

        //Reset Pos/Rot
        building.transform.position = Vector3.zero;
        building.transform.rotation = Quaternion.identity;

        Bounds prefabBounds = building.GetComponentInChildren<Renderer>().bounds;
        Vector3 extents = prefabBounds.extents;
        Vector3 boundsCntr = prefabBounds.center;
        //Restore Position/rotation to Determine If Building is Above Terrain Height by Threshold
        building.transform.position = storedPos;
        building.transform.rotation = rot;
        //Extract bounds for Current Rotation/Position
        Vector3[] boundFootPrint = new Vector3[4];
        boundFootPrint[0] = (boundsCntr) + new Vector3(extents.x, -extents.y, extents.z);//Bottom Front Right
        boundFootPrint[1] = (boundsCntr) + new Vector3(-extents.x, -extents.y, extents.z);//Bottom Front Left
        boundFootPrint[2] = (boundsCntr) + new Vector3(-extents.x, -extents.y, -extents.z);//Bottom Back Left
        boundFootPrint[3] = (boundsCntr) + new Vector3(extents.x, -extents.y, -extents.z);//Bottom Back Right
        //Translate based on Transform Directions
        for (int j = 0; j < boundFootPrint.Length; j++)
        {
            boundFootPrint[j] = building.transform.TransformPoint(boundFootPrint[j]);
        }

        if (prefabBuilding)
        {
            //Test Bounds Against Terrain.
            float depth = 0;
            //Feed Bounds Cycle into HeightCheckFunction
            if (BuildingIsFloating(boundFootPrint, threshold, ref depth, roadMask))
            {
                //Debug.Log("Build Foundation: "+(depth+0.1f));
                //Return to Zero to Create Extrude Mesh
                building.transform.position = Vector3.zero;
                building.transform.rotation = Quaternion.identity;
                //Grab Current Bounds for Extrusion Generation
                boundFootPrint[0] = (boundsCntr) + new Vector3(extents.x, -extents.y, extents.z);//Bottom Front Right
                boundFootPrint[1] = (boundsCntr) + new Vector3(-extents.x, -extents.y, extents.z);//Bottom Front Left
                boundFootPrint[2] = (boundsCntr) + new Vector3(-extents.x, -extents.y, -extents.z);//Bottom Back Left
                boundFootPrint[3] = (boundsCntr) + new Vector3(extents.x, -extents.y, -extents.z);//Bottom Back Right
                                                                                                  //Translate based on Transform Directions
                for (int j = 0; j < boundFootPrint.Length; j++)
                {
                    boundFootPrint[j] = building.transform.TransformPoint(boundFootPrint[j]);
                }
                //Create Foundation as this building is Above Threshold
                Vector3[] polygon;
                //Create Foundation as this building is Above Threshold
                polygon = new Vector3[boundFootPrint.Length];

                for (int i = 0; i < boundFootPrint.Length; i++)
                {
                    Vector3 pos = boundFootPrint[i];
                    //Move Points Y Value down by depth
                    polygon[i] = new Vector3(pos.x, pos.y - (depth + 0.1f), pos.z) - building.transform.position;
                }
                //Call Extrude
                Mesh extrudedMesh = CiDyUtils.ExtrudePrint(polygon, (depth + 0.1f), building.transform, false);
                GameObject foundation = null;
                if (building.transform.Find("FoundationMesh") != null)
                {
                    foundation = building.transform.Find("FoundationMesh").gameObject;
                }
                //Set Created Mesh to Building Object.
                if (foundation == null)
                {
                    //Create Foundation Mesh
                    foundation = new GameObject("FoundationMesh");
                    //Add Renderer and Filter
                    foundation.AddComponent<MeshRenderer>();
                    foundation.AddComponent<MeshFilter>().sharedMesh = extrudedMesh;
                    //Nest Into Building
                    foundation.transform.parent = building.transform;
                    //Set Material
                    if (foundationMaterial != null)
                    {
                        foundation.GetComponent<MeshRenderer>().sharedMaterial = foundationMaterial;
                    }
                    else
                    {
                        foundation.GetComponent<MeshRenderer>().sharedMaterial = (Material)Resources.Load("CiDyResources/FoundationMaterial");
                    }
                    //Add Foundation to Buildings LODGroup (If applicable)
                    LODGroup lodGroup = building.GetComponent<LODGroup>();
                    if (lodGroup != null)
                    {

                        //Grab Current Array
                        LOD[] lods = lodGroup.GetLODs();
                        for (int i = 0; i < lods.Length; i++)
                        {
                            //Add this foundation Mesh to the Highest Quality LOD Group
                            List<Renderer> lodRenderers = lods[i].renderers.ToList();
                            if (i == 0)
                            {
                                //Add this foundation Mesh to the Highest Quality LOD Group
                                lodRenderers.Add(foundation.GetComponent<MeshRenderer>());

                            }
                            //Set LOD
                            LOD currentLOD = lods[i];
                            lods[i] = new LOD(currentLOD.screenRelativeTransitionHeight / (i + 1), lodRenderers.ToArray());
                        }
                        //Copy Total Array over the LODGroup LODS
                        lodGroup.SetLODs(lods);
                        lodGroup.RecalculateBounds();
                    }
                }
                else
                {
                    //Update foundation
                    foundation.GetComponent<MeshFilter>().sharedMesh = extrudedMesh;
                }
                //Restore Position/rotation
                building.transform.position = storedPos;
                building.transform.rotation = rot;
            }
        }
        else {
            //Test Bounds Against Terrain.
            float depth = 0;
            //Feed Bounds Cycle into HeightCheckFunction
            if (BuildingIsFloating(buildingLot.ToArray(), threshold, ref depth, roadMask))
            {
                //Create Foundation as this building is Above Threshold
                Vector3[] polygon;
                //Create Foundation as this building is Above Threshold
                polygon = new Vector3[buildingLot.Count];
                for (int i = 0; i < buildingLot.Count; i++)
                {
                    Vector3 pos = buildingLot[i];
                    //Move Points Y Value down by depth
                    polygon[i] = new Vector3(pos.x, pos.y - (depth + 0.1f), pos.z);
                }
                //Call Extrude
                Mesh extrudedMesh = CiDyUtils.ExtrudePrint(polygon, (depth + 0.1f), building.transform, false);
                //building.transform.position = storedPos;
                GameObject foundation = null;
                if (building.transform.Find("FoundationMesh") != null)
                {
                    foundation = building.transform.Find("FoundationMesh").gameObject;
                }
                //Set Created Mesh to Building Object.
                if (foundation == null)
                {
                    //Create Foundation Mesh
                    foundation = new GameObject("FoundationMesh");
                    //Add Renderer and Filter
                    foundation.AddComponent<MeshRenderer>();
                    foundation.AddComponent<MeshFilter>().sharedMesh = extrudedMesh;
                    //Nest Into Building
                    foundation.transform.parent = building.transform;
                    //Set Material
                    if (foundationMaterial != null)
                    {
                        foundation.GetComponent<MeshRenderer>().sharedMaterial = foundationMaterial;
                    }
                    else
                    {
                        foundation.GetComponent<MeshRenderer>().sharedMaterial = (Material)Resources.Load("CiDyResources/FoundationMaterial");
                    }
                    //Add Foundation to Buildings LODGroup (If applicable)
                    LODGroup lodGroup = building.GetComponent<LODGroup>();
                    if (lodGroup != null)
                    {

                        //Grab Current Array
                        LOD[] lods = lodGroup.GetLODs();
                        for (int i = 0; i < lods.Length; i++)
                        {
                            //Add this foundation Mesh to the Highest Quality LOD Group
                            List<Renderer> lodRenderers = lods[i].renderers.ToList();
                            if (i == 0)
                            {
                                //Add this foundation Mesh to the Highest Quality LOD Group
                                lodRenderers.Add(foundation.GetComponent<MeshRenderer>());

                            }
                            //Set LOD
                            LOD currentLOD = lods[i];
                            lods[i] = new LOD(currentLOD.screenRelativeTransitionHeight / (i + 1), lodRenderers.ToArray());
                        }
                        //Copy Total Array over the LODGroup LODS
                        lodGroup.SetLODs(lods);
                        lodGroup.RecalculateBounds();
                    }
                }
                else
                {
                    //Update foundation
                    foundation.GetComponent<MeshFilter>().sharedMesh = extrudedMesh;
                }
                //Restore Position/rotation
                building.transform.position = storedPos;
                building.transform.rotation = rot;
            }
        }
    }
    
    //Assumed Fed Counter Clockwise and Is a Cyclable List. Returns True or False based on if the Buliding bounds are too high above terrain.(Terrain Mask)
    bool BuildingIsFloating(Vector3[] poly, float threshold, ref float farthestPoint, LayerMask mask)
    {
        RaycastHit hit;
        //Iterate through points and Shoot Raycast Downward and See how far it goes before hitting something other then ourself.
        for (int i = 0; i < poly.Length; i++)
        {
            Vector3 point = poly[i];
            //Shoot a Ray from this point down.
            Ray ray = new Ray(point, Vector3.down);
            if(Physics.Raycast(ray, out hit, 100, mask))
            {
                //CiDyUtils.MarkPoint(hit.point, i);
                float curDist = Mathf.Abs(hit.point.y - poly[i].y);
                //What did we hit and was the Distance greater than threshold?
                if (curDist > threshold && curDist > farthestPoint) {
                    //Update Farthest Tracking
                    farthestPoint = curDist;
                }
            }
        }
        if (farthestPoint != 0) {
            farthestPoint *= 2;
            return true;
        }

        //return false if here
        return false;
    }
    //Translate WorldPos to Terrain Position
    protected Vector3 GetNormalizedPositionRelativeToTerrain(Vector3 pos, Vector3 terrPos, Vector3 tSize)
	{
		Vector3 tempCoord = (pos - terrPos);
		Vector3 coord;
		coord.x = tempCoord.x / tSize.x;
		coord.y = tempCoord.y / tSize.y;
		coord.z = tempCoord.z / tSize.z;
		return coord;
	}

    protected Vector3 GetNormalizedTerrainPositionRelativeToWorld(Vector3 pos, Terrain terrain)
    {
        Vector3 tempCoord = (pos + terrain.transform.position);
        Vector3 coord;
        coord.x = tempCoord.x * terrain.terrainData.size.x;
        coord.y = tempCoord.y * terrain.terrainData.size.y;
        coord.z = tempCoord.z * terrain.terrainData.size.z;
        return coord;
    }

    //ALL FUNCTIONS NEEDED FOR ROAD Sampling and Terrain editing
    //What type of Samplining is the user currenlty wanting
    public enum HeightSample {
		EvenElevation,
		MinElevationDiff,
		MinElevation
	}
	
	public HeightSample sampleType = HeightSample.EvenElevation;
	
	//Path Plotting Variables
	int dSample = 20;//How many path Plot points from start to dst.
	public float fovRange = 2.5f;//FOV Range for Samples
	public int nSample = 20;//Amount of Samples Taken in FOV Range.
	public float devAngle = 45.0f;//Maximum Deviation Angle from Destination
	float dSnap = 5f;
	public float slopeHeight = 1000;
	RaycastHit sampleHit;

	//Even Elevation Diff
	Vector3 SampleEvenDiffHeight(Vector3 srt, Vector3 dst){
		float curTotalDist = Vector3.Distance (srt,dst);
		//Debug.Log ("TD"+curTotalDist);
		float curSamples = (curTotalDist/fovRange);
		float curTotalElv = Mathf.Abs(srt.y-dst.y);
		float desiredDiff = (curTotalElv/curSamples);
		
		//Calculate Sampling Positions
		//Define Direction
		Vector3 dir = (new Vector3 (dst.x, srt.y, dst.z) -srt).normalized;
		Vector3 leftDir = Vector3.Cross (dir, Vector3.up);
		Vector3 v3End1 = (srt + (dir*fovRange)+ leftDir*fovRange);
		Vector3 v3End2 = (srt+(dir*fovRange)+(-leftDir)*fovRange);
		Vector3 v3A = v3End1 - srt;  // Step 1
		Vector3 v3B = v3End2 - srt;
		float total_angle = Vector3.Angle (v3A, v3B); // Step 2 
		float delta_angle = total_angle / (float)(nSample - 1); // Step 3
		Vector3 axis = Vector3.Cross (v3A, v3B);  // Step 4
		Quaternion q = Quaternion.AngleAxis (delta_angle, axis);  // Step 5
		
		float bestDiff = Mathf.Infinity;//Start at infinity so any number first found will be accepted.
		float bestAngle = 360;
		//float bestDist = Mathf.Infinity;
		Vector3 bestPos = new Vector3(-999,999,-999);
		//Debug.Log ("Desired Elevation "+desiredDiff);
		//Test nSample positions
		for (int i = 0; i < nSample; i++){
			//Fire a Raycast downward from World Pos and test Ground Height.
			//Vector3 dwn = new Vector3(turtle.transform.position.x,turtle.transform.position.y-200,turtle.transform.position.z);
			//Move the Start of the Test High above the possible ground height.
			Vector3 rayPos = srt + v3A;
			//GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			rayPos.y+=slopeHeight;
			//sphere.transform.position = rayPos;
			//Fire raycast downward to test ground Height
			if(Physics.Raycast(rayPos, Vector3.down, out sampleHit, Mathf.Infinity, roadMask)){
				//CiDyUtils.MarkPoint(sampleHit.point,i);
				//sampleHit.point = new Vector3(sampleHit.point.x,sampleHit.point.y+heightSpacing,sampleHit.point.z);
				Vector3 targetDir = new Vector3(sampleHit.point.x,0,sampleHit.point.z) - new Vector3(srt.x,0,srt.z);
				Vector3 forward = (new Vector3 (dst.x, srt.y, dst.z)-srt);
				float angle = Vector3.Angle(targetDir, forward);
				float curDiff = Mathf.Abs(Mathf.Floor(sampleHit.point.y-srt.y)-desiredDiff);
				//We need to determine the Smallest acesion/Desecion step to progress to the curDst
				//Debug.Log("CurDiff "+curDiff);
				if(curDiff<bestDiff){
					bestDiff = curDiff;
					bestAngle = angle;
					bestPos = sampleHit.point;
					//Debug.Log("Chose Diff = "+bestDiff+" angle "+bestAngle);
				} else if(curDiff==bestDiff){
					if(angle<bestAngle){
						bestDiff = curDiff;
						bestAngle = angle;
						bestPos = sampleHit.point;
					}
				}
			}
			// Step 6
			v3A = q * v3A;
		}
		if(bestPos != new Vector3(-999,999,-999)){
			//GameObject selectedPlace = CiDyUtils.MarkPoint(bestPos,999);
			//selectedPlace.GetComponent<MeshRenderer>().material = (Material)Resources.Load("ActiveMaterial");
			//We have a position. :)
			//This is our new Start Point for the Next Plot
			return bestPos;
		}
		Debug.LogWarning("No position found in sampling");
		return bestPos;
	}
	
	//Min Elevation Diff
	Vector3 SampleMinDiffHeight(Vector3 srt, Vector3 dst){
		//Calculate Sampling Positions
		//Define Direction
		Vector3 dir = (new Vector3 (dst.x, srt.y, dst.z) -srt).normalized;
		Vector3 leftDir = Vector3.Cross (dir, Vector3.up);
		Vector3 v3End1 = (srt + (dir*fovRange)+ leftDir*fovRange);
		Vector3 v3End2 = (srt+(dir*fovRange)+(-leftDir)*fovRange);
		Vector3 v3A = v3End1 - srt;  // Step 1
		Vector3 v3B = v3End2 - srt;
		float total_angle = Vector3.Angle (v3A, v3B); // Step 2 
		float delta_angle = total_angle/(float)(nSample - 1); // Step 3
		Vector3 axis = Vector3.Cross(v3A, v3B);  // Step 4
		Quaternion q = Quaternion.AngleAxis(delta_angle, axis);  // Step 5
		
		float bestDiff = Mathf.Infinity;//Start at infinity so any number first found will be accepted.
		float bestAngle = 360f;
		Vector3 bestPos = new Vector3(-999,999,-999);
		//Debug.Log ("Desired Elevation "+desiredDiff);
		//Test nSample positions
		for (int i = 0; i < nSample; i++){
			//Fire a Raycast downward from World Pos and test Ground Height.
			//Move the Start of the Test High above the possible ground height.
			Vector3 rayPos = srt + v3A;
			//GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			rayPos.y+=slopeHeight;
			//sphere.transform.position = rayPos;
			//Fire raycast downward to test ground Height
			if(Physics.Raycast(rayPos, Vector3.down, out sampleHit, Mathf.Infinity, roadMask)){
				//if(sampleHit.collider.tag != "Road"){
				Vector3 targetDir = new Vector3(sampleHit.point.x,0,sampleHit.point.z) - new Vector3(srt.x,0,srt.z);
				Vector3 forward = (new Vector3 (dst.x, srt.y, dst.z)-srt);
				float angle = Vector3.Angle(targetDir, forward);
				//We need to determine the Smallest acesion/Desecion step to progress to the curDst
				float curDiff = Mathf.Abs(Mathf.Floor(sampleHit.point.y-srt.y));
				if(curDiff<=bestDiff){
					if(angle<=bestAngle){
						bestDiff = curDiff;
						bestAngle = angle;
						bestPos = sampleHit.point;
					}
				}
				//}
			}
			// Step 6
			v3A = q * v3A;
		}
		if(bestPos != new Vector3(-999,999,-999)){
			//We have a position. :)
			//This is our new Start Point for the Next Plot
			return bestPos;
		}
		Debug.LogWarning("No position found in sampling");
		return bestPos;
	}
	
	//Minimum Elevation
	Vector3 SampleMinHeight(Vector3 srt, Vector3 dst){
		//Calculate Sampling Positions
		Vector3 dir = (new Vector3 (dst.x, srt.y, dst.z) -srt).normalized;
		Vector3 leftDir = Vector3.Cross (dir, Vector3.up);
		Vector3 v3End1 = (srt + (dir*fovRange)+ leftDir*fovRange);
		Vector3 v3End2 = (srt+(dir*fovRange)+(-leftDir)*fovRange);
		Vector3 v3A = v3End1 - srt;  // Step 1
		Vector3 v3B = v3End2 - srt;
		float total_angle = Vector3.Angle (v3A, v3B); // Step 2 
		float delta_angle = total_angle / (float)(nSample - 1); // Step 3
		Vector3 axis = Vector3.Cross (v3A, v3B);  // Step 4
		Quaternion q = Quaternion.AngleAxis (delta_angle, axis);  // Step 5
		
		float bestHeight = Mathf.Infinity;
		float bestAngle = 360f;
		Vector3 bestPos = new Vector3(-999,999,-999);
		//float elevationDiff = (srt.y-dst.y);
		//Test nSample positions
		for (int i = 0; i < nSample; i++){
			//Fire a Raycast downward from World Pos and test Ground Height.
			//Move the Start of the Test High above the possible ground height.
			Vector3 rayPos = srt + v3A;
			//GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			rayPos.y+=slopeHeight;
			//sphere.transform.position = rayPos;
			//Fire raycast downward to test ground Height
			if(Physics.Raycast(rayPos, Vector3.down, out sampleHit, Mathf.Infinity, roadMask)){
				//if(sampleHit.collider.tag != "Road"){
				Vector3 targetDir = new Vector3(sampleHit.point.x,0,sampleHit.point.z) - new Vector3(srt.x,0,srt.z);
				Vector3 forward = (new Vector3 (dst.x, srt.y, dst.z)-srt);
				float angle = Vector3.Angle(targetDir, forward);
				float curHeight = Mathf.Abs(Mathf.Floor(sampleHit.point.y));
				if(curHeight<=bestHeight){
					if(angle <= bestAngle){
						//Perform angle Test
						bestHeight = curHeight;
						bestAngle = angle;
						bestPos = sampleHit.point;
					}
				}
				//}
			}
			// Step 6
			v3A = q * v3A;
		}
		if(bestPos != new Vector3(-999,999,-999)){
			//We have a position. :)
			//This is our new Start Point for the Next Plot
			return bestPos;
		}
		Debug.LogWarning("No position found in sampling");
		return bestPos;
	}
	//Plot Path
	public List<Vector3> PlotPath(CiDyNode srtNode, CiDyNode dstNode){
		//Debug.Log ("Plotting "+srtNode.name+" "+srtNode.position+" "+dstNode.name+" "+dstNode.position);
		//Call intersections to update there Meshes so we Know where our road will merge(start and End).
		//Update End Points with proper intersection Pieces.
		//CiDyNode nd1 = srtNode;
		//nd1.UpdateRoad(dstNode.transform.position,roadWidth);
		//CiDyNode nd2 = dstNode;
		//nd2.UpdateRoad (srtNode.transform.position,roadWidth);
		Vector3 srt = srtNode.position;
		Vector3 dst = dstNode.position;
		//The start points need to be moved in the Proper Direction by RoadWidth/2
		//Vector3 dir = (new Vector3(dst.x,0,dst.z)- new Vector3(srt.x,0,srt.z)).normalized;
		//Vector3 dir = (dst-srt).normalized;
		/*srt = (srt+(dir*roadWidth/2f));
		srt.y = srtNode.position.y;
		dst = (dst+(-dir*roadWidth/2f));
		dst.y = dstNode.position.y;*/
		
		List<Vector3> path = new List<Vector3>();//The Plotted Points
		List<Vector3> path2 = new List<Vector3>();//
		//Debug.Log ("Called Plot Path srt = " + srt + " dst = " + dst + " Road Name = " + roadName);
		//Dynamcially Set DSmaple based on Dist between the two points.
		float dSampleDist = Vector3.Distance (srt, dst);
		dSample = (Mathf.FloorToInt(dSampleDist/fovRange));
		//Set Dsnap
		dSnap = (fovRange * 2);
		bool isOn = false;
		//Save Start Point
		///Vector3 startPoint = srt;
		//Store last as it will change
		//Vector3 lastPoint = dst;
		/*GameObject go = GameObject.CreatePrimitive (PrimitiveType.Cube);
		go.name = "lastPoint";
		go.transform.position = lastPoint;*/
		//srt += (dir);
		//dst += (-dir);
		/*go = GameObject.CreatePrimitive (PrimitiveType.Cube);
		go.name = "DirPoint";
		go.transform.position = dst;*/
		//Add Start Point
		path.Add(srt);
		path2.Add (dst);
		//Plot dSample amount of points from start to dst.
		for (int j=0;j<dSample;j++){
			//Check distance against DSnap to determine if we have reached our goal
			float dist = Vector3.Distance(dst, srt);
			if(dist <= dSnap){
				//We have reached our goal end Plotting
				break;
			}
			//what type of Sampling to we want?
			if(sampleType == HeightSample.MinElevation){
				//Visual Feedback
				//GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				if(!isOn){
					srt = SampleMinHeight(srt,dst);
					path.Add(srt);
					isOn = true;
					//Visual Feedback
					//go.transform.position = srt;
				} else {
					dst = SampleMinHeight(dst,srt);
					path2.Add(dst);
					isOn = false;
					//Visual Feedback
					//go.transform.position = dst;
				}
			} else if(sampleType == HeightSample.MinElevationDiff){
				//Visual Feedback
				//GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				if(!isOn){
					srt = SampleMinDiffHeight(srt,dst);
					path.Add(srt);
					isOn = true;
					//Visual Feedback
					//go.transform.position = srt;
				} else {
					dst = SampleMinDiffHeight(dst,srt);
					path2.Add(dst);
					isOn = false;
					//Visual Feedback
					//go.transform.position = dst;
				}
			} else if(sampleType == HeightSample.EvenElevation){
				//Visual Feedback
				//GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				if(!isOn){
					srt = SampleEvenDiffHeight(srt,dst);
					path.Add(srt);
					isOn = true;
					//Visual Feedback
					//go.transform.position = srt;
				} else {
					dst = SampleEvenDiffHeight(dst,srt);
					path2.Add(dst);
					isOn = false;
					//Visual Feedback
					//go.transform.position = dst;
				}
			}
			//yield return new WaitForSeconds (0f);
		}
		//Flip path2 list.
		for(int i = path2.Count-1;i>=0;i--){
			path.Add(path2[i]);
		}
		//Return path
		return path;
	}



	//This function will test if the node can be moved to the new Position without causing errors in the graph.(Edge Intersections) 
	public bool CanMove(ref CiDyNode moveData, Vector3 newPos){
		//Debug.Log ("Moving Node " + moveData.name+" Pos: "+moveData.position+" new Pos"+ newPos);
		//This node has edges that are attached. To Easily Determine if its new pos will cause intersection we create a fake edge from its current
		//pos to the new pos and test that edge for intersection.(This simple test will rule out alot of scenarios without testing the entire graph)
		//Create tmpGameObject
		//GameObject tmpNode = Instantiate (nodePrefab, newPos, Quaternion.identity) as GameObject;
		//CiDyNode tmpData = (CiDyNode)tmpNode.GetComponent(typeof(CiDyNode));
		//Create Node for this point.
		CiDyNode tmpData = NewNode("V"+nodeCount2,newPos, nodeCount2);
		//Debug.Log ("TmpData: " + tmpData.position);
		//Debug.Log ("MoveData: " + moveData.position);
		CiDyEdge testEdge = new CiDyEdge(moveData,tmpData);
		//Simple Test first
		if(EdgeIntersection(testEdge)){
			//Failed Simple Intersections Test Do Not continue further.
			//Destroy GameObject
			//RemoveSubNode(tmpData);
			Debug.LogWarning("Cannot Move Node to new position as this will cause road intersections");
			tmpData.DestroyNode();
			//This move will cause an intersection error.
			return false;
		} else {
			//Passed simple Test. We need to perform the final Test on all edges of this node against the edges of the Graph.
			//In order the Nodes current Edges to not effect the  Test we need to clone the Edges List and Remove them from the cloned list
			//for Testing.
			List<CiDyEdge> testEdges = new List<CiDyEdge>(graphEdges);
			//Remove all Edges of the Moved Node from the cloned Edges List.
			for(int i = 0;i<moveData.adjacentNodes.Count;i++){
				//Reference Adj Node
				CiDyNode adjNode = moveData.adjacentNodes[i];
				//Create Test Edge for removal
				testEdge = new CiDyEdge(moveData, adjNode);
				//Remove Edge from Cloned Graph.
				for(int j = 0;j<testEdges.Count;j++){
					if(testEdge.name == testEdges[j].name){
						//Remove this edge from cloned graph.
						testEdges.RemoveAt(j);
						break;
					}
				}
			}
			//Now iterate through the adjacency List of Nodes again.
			for(int i = 0;i<moveData.adjacentNodes.Count;i++){
				//Reference Adj Node
				CiDyNode adjNode = moveData.adjacentNodes[i];
				//Create tmp Edge between tmpNode and AdjNode.
				//testEdge = new CiDyEdge(tmpData, adjNode);
				testEdge = new CiDyEdge(tmpData,adjNode);
				//Test this edge for Intersection against the Tmp Edge Graph
				if(EdgeIntersection(testEdge,testEdges)){
					//This Edge will cause errors. Return Failed
					//Destroy GameObject
					//DestroyImmediate(tmpNode);
					Debug.LogWarning("Cannot Move Node to new position as this will cause road intersections 2 ="+testEdge.name);
					tmpData.DestroyNode();
					//This move will cause an intersection error.
					return false;
				}
			}
		}
		//Destroy GameObject
		//DestroyImmediate(tmpNode);
		//No intersection detected
		tmpData.DestroyNode ();
		return true;
	}

	//This function will move the node to its new position and update any mesh roads that are tied to it. if any.
	public bool MovedNode(ref CiDyNode nodeData, Vector3 newPos){
		//Debug.Log("MovedNode: "+nodeData.name+" NewPos: "+newPos);
		//Test if We Can Move Node to newPos
		//Are there Roads Connected?
		if(nodeData.adjacentNodes.Count > 0){
			if(NodeAngleTest(ref nodeData, newPos)){
				Debug.LogWarning("Cannot Move Node as this exceeds Angle Limits");
				return false;
			}
			if(!NodePlacement(ref nodeData,newPos)){
				Debug.LogWarning("Cannot Move Node New Position Too Close with other Graph Nodes");
				return false;
			}
			if(!CanMove(ref nodeData,newPos)){
				Debug.LogWarning("Cannot Move Node New Roads Will Cause Intersection");
				return false;
			}
		}

		//If we have Made it To there Then we can Move this Node and Update its Connected Roads as Needed.
		nodeData.MoveNode (newPos);
		//print("Moved Node "+nodeData.name);
		//Now lets update its connected roads
		if(nodeData.adjacentNodes.Count > 0){
			CiDyRoad[] updatedRoads = new CiDyRoad[nodeData.adjacentNodes.Count];
			//This node has roads that need to be updated.
			for(int i = 0;i<nodeData.adjacentNodes.Count;i++){
				CiDyNode adjNode = nodeData.adjacentNodes[i];
				//Debug.Log("Update AdjNode: "+adjNode.name);
				//Determine Road Name.
				CiDyEdge tmpEdge = new CiDyEdge(nodeData,adjNode);
				GameObject tmpRoad = roads.Find(x=> x.name == tmpEdge.name);
				//Plot Path from A-B.
				//List<Vector3> newPath = new List<Vector3>();
                Vector3[] newPath = new Vector3[4];
				Vector3 cent = (nodeData.position+adjNode.position)/2;
                newPath[0] = nodeData.position;//newPath.Add(nodeData.position);
                newPath[1] = (nodeData.position + cent) / 2;//newPath.Add((nodeData.position+cent)/2);
				newPath[2] = (cent+adjNode.position)/2;//newPath.Add((cent+adjNode.position)/2);
                newPath[3] = adjNode.position;//newPath.Add(adjNode.position);
				//Call Update on Road with its new Path.
				CiDyRoad curRoad = tmpRoad.GetComponent<CiDyRoad>();
				curRoad.ReplotRoad(newPath);
				updatedRoads[i] = curRoad;
			}
			for(int i = 0;i<updatedRoads.Length;i++){
				updatedRoads[i].UpdateRoadNodes();
			}
			UpdateNodesCell(nodeData);
		}
		//if here we are done.
		return true;
	}

	//this function will remove the Node and Destroy all of its current Edges from the Graph.
	public void DestroyMasterNode(CiDyNode oldNode){
		//Debug.Log ("Graph.RemoveMasterNode " + oldNode.name);
		//Grab the Node data
		//CiDyNode oldNode = masterGraph.Find(x=> x.name == oldGameObject.name);
		//Destroy the Edges/Adjacency Connections of the OldNode from the Graph.
		if(oldNode.adjacentNodes.Count > 0){
			for(int i = 0;i<oldNode.adjacentNodes.Count;i++){
				//Reference the AdjNode
				CiDyNode adjNode = oldNode.adjacentNodes[i];
				if(adjNode != null){
					adjNode.RemoveNode(oldNode);
					//Create a Edge for Graph Search.
					CiDyEdge oldEdge = new CiDyEdge(oldNode,adjNode);
					RemoveRoad(oldEdge.name);//Only need the Name This MUST BE CALLED before edge removal
					//Remove this edge from the Graph
					RemoveEdge(oldEdge);
				}
			}
		}
		oldNode.DestroyNode ();
		//Remove oldNode from MasterGraph
		masterGraph.Remove(oldNode);
		//oldNode = null;
		//Now that the Node has Been Disconnected from all other Nodes in the Graph We Can Destroy its GameObject
		//DestroyImmediate();
	}

    public bool TooCloseToNodes(Vector3 pos, float minLength) {
        for (int i = 0; i < masterGraph.Count; i++) {
            if (Vector3.Distance(masterGraph[i].position, pos) <= minLength){//(masterGraph[i].maxRadius*2) {
                return true;
            }
        }

        return false;
    }

	/*public bool LineIntersection(Vector3 p1,Vector3 p2, Vector3 p3, Vector3 p4, ref Vector3 intersection)
	{
		//Debug.Log ("Testing Intesrections");
		float Ax,Bx,Cx,Ay,By,Cy,d,e,f,num//;
		float x1lo,x1hi,y1lo,y1hi;

		Ax = p2.x-p1.x;
		Bx = p3.x-p4.x;
		
		
		
		// X bound box test/
		if(Ax<0) {
			x1lo=p2.x; x1hi=p1.x;
		} else {
			x1hi=p2.x; x1lo=p1.x;
		}

		if(Bx>0) {
			if(x1hi < p4.x || p3.x < x1lo) return false;
		} else {
			if(x1hi < p3.x || p4.x < x1lo) return false;
		}

		Ay = p2.z-p1.z;
		By = p3.z-p4.z;

		// Y bound box test//
		if(Ay<0) {                  
			y1lo=p2.z; y1hi=p1.z;
		} else {
			y1hi=p2.z; y1lo=p1.z;
		}

		if(By>0) {
			if(y1hi < p4.z || p3.z < y1lo) return false;
		} else {
			if(y1hi < p3.z || p4.z < y1lo) return false;
		}

		Cx = p1.x-p3.x;
		Cy = p1.z-p3.z;
		d = By*Cx - Bx*Cy;  // alpha numerator//
		f = Ay*Bx - Ax*By;  // both denominator//

		// alpha tests//
		if(f>0) {
			if(d<0 || d>f) return false;
		} else {
			if(d>0 || d<f) return false;
		}

		e = Ax*Cy - Ay*Cx;  // beta numerator//

		// beta tests //
		if(f>0) {                          
			if(e<0 || e>f) return false;
		} else {
			if(e>0 || e<f) return false;
		}

		// check if they are parallel
		if(f==0) return false;
		// compute intersection coordinates //
		num = d*Ax; // numerator //
		//    offset = same_sign(num,f) ? f*0.5f : -f*0.5f;   // round direction //
		//    intersection.x = p1.x + (num+offset) / f;
		intersection.x = p1.x + num / f;

		num = d*Ay;
		//    offset = same_sign(num,f) ? f*0.5f : -f*0.5f;
		//    intersection.z = p1.z + (num+offset) / f;
		intersection.z = p1.z + num / f;

		return true;
	}*/

	//To Compute the AngleBisector Direction we need the predecessor pos, our pos and the successor pos.
	Vector3 AngleBisector(Vector3 predPos, Vector3 curPos, Vector3 succPos){
		//Now that we have our pred. and succes. Lets calculate the angle using there position from ours normalized to 1 unit vector.
		//to find the bisector we need to  find the angle of bisectors's adjacents(b-1,b+1)
		Vector3 v1 = (predPos - curPos).normalized;
		Vector3 v2 = (succPos - curPos).normalized;
		Vector3 bisector = (v1 + v2);
		return bisector;
	}

	//Left/Right Test
	float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up) {
		Vector3 perp = Vector3.Cross(fwd, targetDir);
		float dir = Vector3.Dot(perp, up);
		
		//Right Side
		if (dir > 0f) {
			return 1f;
		} else if (dir < 0f) {
			//Left Side
			return -1f;
		} else {
			//Conicident
			return 0f;
		}
	}

	//New-old, old-new. curPos
	Vector3 AngleBisector2(Vector3 a, Vector3 b, Vector3 c, Vector3 d){
		Vector3 v1 = (a - b).normalized;
		Vector3 v2 = (d - c).normalized;
		Vector3 bisector = (v1+v2);
		return bisector;
	}

	//Determine distance to Line using a Point and LineA-LineB Points.
	public float DistanceToLine(Vector3 p, Vector3 endA, Vector3 endB){
		float a = p.x - endA.x;
		float b = p.z - endA.z;
		float c = endB.x - endA.x;
		float d = endB.z - endA.z;
		
		float dot = a * c + b * d;
		float len_sq = c * c + d * d;
		//Set R which represents between 0-1 float value (0 = at EndA),(1 = at EndB);
		float r = dot/len_sq;
		r = Mathf.Round(r * 100f) / 100f;
		
		
		/*float xx;
		float zz;
		
		//try without Mathf.Approximatly as this is a more intense math calculation. the R value will be aprox as accurate and is already calculated
		//if(r < 0 || (Mathf.Approximately(endA.x,endB.x) && Mathf.Approximately(endA.z,endB.z))){
		if(r < 0 || (endA.x == endB.x && endA.z == endB.z)){
			//Debug.Log("r<0");
			xx = endA.x;
			zz = endA.z;
		} else if(r > 1){
			//Debug.Log("r>1");
			xx = endB.x;
			zz = endB.z;
		} else {
			//Debug.Log("else");
			xx = endA.x+r*c;
			zz = endA.z+r*d;
		}*/
		
		//float dx = p.x - xx;
		//float dz = p.y - zz;
		
		//Update Dist from point to Line Segment
		//dist = Mathf.Sqrt (dx * dx + dz * dz);
		//float dist = Vector2.Distance(new Vector2(p.x,p.z), new Vector2(xx,zz));
		float dist = Mathf.Abs((endB.x-endA.x)*(endA.z-p.z)-(endA.x-p.x)*(endB.z-endA.z))/Mathf.Sqrt(Mathf.Pow(endB.x-endA.x,2)+Mathf.Pow(endB.z-endA.z,2));
		return (Mathf.Round (dist * 100f) / 100f);
	}

	//Lot SubDivision
	float lotDeviance = 0.5f;
	//Regions that will be used for green space.
	//List<List<CiDyNode>> greenSpace = new List<List<CiDyNode>>();
	//Debug Visuals
	List<List<List<CiDyNode>>> regions = new List<List<List<CiDyNode>>>(0);
	//This function will divide a region into specified lotWidth/lotDepth regions.
	public List<List<CiDyNode>> SubdivideLots(List<CiDyNode> blockRegion, float lotWidth, float lotDepth, ref List<List<CiDyNode>> greenSpace){
		//Debug.Log ("Lot Width: " + lotWidth + " LotDepth: " + lotDepth);
		//Pre-Set Nodes cyclic List
		for(int i = 0;i<blockRegion.Count;i++){
			CiDyNode curNode = blockRegion[i];
			if(i==blockRegion.Count-1){
				curNode.succNode = blockRegion[0];
			} else {
				curNode.succNode = blockRegion[i+1];
			}
			//Set Road Access
			curNode.roadAccess = true;
		}
		//bool firstRegion = true;
		//Debug.Log ("SubdivideLots");
		//Make sure lotDeviance is not equal to or higher than lotWidth || lotDepth
		lotDeviance = Mathf.Clamp (lotDeviance,0.1f, 0.9f);
        //Initilize Queue
        List<List<CiDyNode>> regionQueue = new List<List<CiDyNode>>
        {
            blockRegion
        };
        //Initilize testing region
        List<CiDyNode> region = new List<CiDyNode> ();
		//Initilize testing variables
		float splitSize;
		//Initilize Output List
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		greenSpace = new List<List<CiDyNode>>(0);
		//Use while loop to iterate through queue until at desired lot sizes.
		while(regionQueue.Count>0){
			//Grab next region for division
			region = regionQueue[0];
			//Debug.Log("Cutting Region, Count = "+region.Count);
			//Calc the longest road edge and split size
			List<CiDyNode> longestEdge = GetLongestRoad(region);
			/*if(longestEdge.edgeLength <= lotWidth && firstRegion){
				lotWidth=longestEdge.edgeLength-0.1f;
				//Debug.LogWarning("Lot Width Changed "+lotWidth);
			}
			if(firstRegion){
				firstRegion = false;
			}*/
			/*for(int i = 0;i<longestEdge.Count;i++){
				CiDyUtils.MarkPoint(longestEdge[i].position,i);
			}*/
			//Debug.Log("LongestEdge with Access ="+longestEdge[0].edgeLength);
			if(longestEdge.Count > 0){
				//Debug.Log("LongestEdge with Access ="+longestEdge[0].edgeLength);
				if(longestEdge[0].edgeLength <= lotWidth){
					// calc the longest non-road edge and split size
					longestEdge = GetLongestNonRoad(region);
					//Debug.Log("LongestEdge Without Access "+longestEdge[0].edgeLength);
					if(longestEdge[0].edgeLength <= lotDepth){
						// if lot is small enough, add completed region
						outputRegions.Add(region);
						regionQueue.RemoveAt(0);
						//Debug.Log("Completed Region");
						continue;
					} else {
						//Debug.Log("Non-Road Split");
						splitSize = lotDepth;
					} 
				} else {
					//Debug.Log("Road Split");
					splitSize = lotWidth;
				}
			} else {
				// calc the longest non-road edge and split size
				longestEdge = GetLongestNonRoad(region);
				//Debug.Log("LongestEdge Without Access "+longestEdge[0].edgeLength);
				if(longestEdge[0].edgeLength <= lotDepth){
					// if lot is small enough, add completed region
					outputRegions.Add(region);
					regionQueue.RemoveAt(0);
					//Debug.Log("Completed Region");
					continue;
				} else {
					//Debug.Log("Non-Road Split");
					splitSize = lotDepth;
				} 
			}
			
			// calculate the split line points.
			Vector3 sp1 = CalcSplitPoint(longestEdge, splitSize, lotDeviance);
			Vector3 sp2 = sp1+longestEdge[0].perpendicular;
			
			// split and process the new regions and add ones with road access to queue list.
			List<List<CiDyNode>> newRegions = SplitRegion(region, sp1, sp2);
			regionQueue.RemoveAt(0);
			//Debug.Log("Split Region into "+newRegions.Count+" sub-regions");
			//Iterate through
			for(int i = 0;i<newRegions.Count;i++){
				List<CiDyNode> curRegion = newRegions[i];
				if(HasRoadAccess(curRegion)){
					//Debug.Log("RegionQueued");
					regionQueue.Add(curRegion);//add to processing queue
				} else {
					//Debug.Log("Region Added to GreenSpace");
					greenSpace.Add(curRegion);//Store for greenSpace use
				}
			}
		}
		//Debug.Log (outputRegions.Count+" Valid Regions, "+greenSpace.Count+" Green Regions");
		//Return Lists of Sub-Regions
		return outputRegions;
	}

	//This function will divide a region into specified lotWidth/lotDepth regions.
	public List<List<CiDyNode>> SubdivideLots(List<CiDyNode> blockRegion, float lotWidth, float lotDepth){
		//Debug.Log ("Lot Width: " + lotWidth + " LotDepth: " + lotDepth+"Block Region Cnt: "+blockRegion.Count);
		//Pre-Set Nodes cyclic List
		for(int i = 0;i<blockRegion.Count;i++){
			CiDyNode curNode = blockRegion[i];
			if(i==blockRegion.Count-1){
				curNode.succNode = blockRegion[0];
			} else {
				curNode.succNode = blockRegion[i+1];
			}
			//Set Road Access
			curNode.roadAccess = true;
			//Visualize
			/*Debug.Log("i: "+i+" Pos: "+curNode.position);
			GameObject tNode = CiDyUtils.MarkPoint(curNode.position,i);
			cells[0].visualNodes.Add(tNode);*/
		}
		//bool firstRegion = true;
		//Debug.Log ("SubdivideLots");
		//Make sure lotDeviance is not equal to or higher than lotWidth || lotDepth
		lotDeviance = Mathf.Clamp (lotDeviance,0.1f, 0.9f);
        //Initilize Queue
        List<List<CiDyNode>> regionQueue = new List<List<CiDyNode>>
        {
            blockRegion
        };
        //Initilize testing region
        List<CiDyNode> region = new List<CiDyNode> ();
		//Initilize testing variables
		float splitSize;
		//Initilize Output List
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		//Use while loop to iterate through queue until at desired lot sizes.
		while(regionQueue.Count>0){
			//Grab next region for division
			//Debug.Log("Grabbing Region "+regionQueue.Count);
			region = regionQueue[0];
			//Debug.Log("Cutting Region, Count = "+region.Count);
			//Calc the longest road edge and split size
			List<CiDyNode> longestEdge = GetLongestRoad(region);
			/*if(longestEdge.edgeLength <= lotWidth && firstRegion){
				lotWidth=longestEdge.edgeLength-0.1f;
				//Debug.LogWarning("Lot Width Changed "+lotWidth);
			}
			if(firstRegion){
				firstRegion = false;
			}*/
			/*for(int i = 0;i<longestEdge.Count;i++){
				CiDyUtils.MarkPoint(longestEdge[i].position,i);
			}*/
			if(longestEdge.Count > 0){
				//Debug.Log("LongestEdge with Access ="+longestEdge[0].edgeLength);
				if(longestEdge[0].edgeLength <= lotWidth){
					// calc the longest non-road edge and split size
					longestEdge = GetLongestNonRoad(region);
					//Debug.Log("LongestEdge Without Access "+longestEdge[0].edgeLength);
					if(longestEdge[0].edgeLength <= lotDepth){
						// if lot is small enough, add completed region
						outputRegions.Add(region);
						regionQueue.RemoveAt(0);
						//Debug.Log("Completed Region");
						continue;
					} else {
						//Debug.Log("Non-Road Split");
						splitSize = lotDepth;
					} 
				} else {
					//Debug.Log("Road Split");
					splitSize = lotWidth;
				}
			} else {
				// calc the longest non-road edge and split size
				longestEdge = GetLongestNonRoad(region);
				//Debug.Log("LongestEdge Without Access "+longestEdge[0].edgeLength);
				if(longestEdge[0].edgeLength <= lotDepth){
					// if lot is small enough, add completed region
					outputRegions.Add(region);
					regionQueue.RemoveAt(0);
					//Debug.Log("Completed Region");
					continue;
				} else {
					//Debug.Log("Non-Road Split");
					splitSize = lotDepth;
				}
			}
			
			// calculate the split line points.
			Vector3 sp1 = CalcSplitPoint(longestEdge, splitSize, lotDeviance);
			Vector3 sp2 = sp1+longestEdge[0].perpendicular;
			
			// split and process the new regions and add ones with road access to queue list.
			List<List<CiDyNode>> newRegions = SplitRegion(region, sp1, sp2);
			regionQueue.RemoveAt(0);
			//Debug.Log("Split Region into "+newRegions.Count+" sub-regions Region1 "+newRegions[0].Count+" Region2: "+newRegions[1].Count);
			//Iterate through
			for(int i = 0;i<newRegions.Count;i++){
				List<CiDyNode> curRegion = newRegions[i];
				if(HasRoadAccess(curRegion)){
					//Debug.Log("RegionQueued");
					regionQueue.Add(curRegion);//add to processing queue
				} else {
					//Debug.Log("Region Deleted");
					DeleteRegion(curRegion);
				}
			}
		}
		//Debug.Log (outputRegions.Count+" Valid Regions");
		//Return Lists of Sub-Regions
		return outputRegions;
	}

	//This function will find the longest Side with Road Access
	List<CiDyNode> GetLongestRoad(List<CiDyNode> region){
		//Initilize Variables
		List<CiDyNode> longestEdge = new List<CiDyNode>(0);//Longest Edge with Road Access
		float bestDist = 0;//Longest Found
		//Iterate through region. Find corner node and count length until next corner node.
		float curDist = 0;
		List<CiDyNode> storedEdge = new List<CiDyNode> (0);
		//Debug.Log ("Finding Longest Road " + region.Count);
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			if(curNode.isCorner){
				//Debug.Log("Corner");
				//At the Begininng/End of an Edge.
				if(curNode.succNode.roadAccess){
					if(storedEdge.Count == 0){
						//Beginning of List
						storedEdge.Add(curNode);
						curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
					} else {
						//At End of List
						//We have completed an Edge List
						storedEdge.Add(curNode);
						if(curDist>bestDist){
							bestDist = curDist;
							//Store List
							longestEdge = new List<CiDyNode>(storedEdge);
							longestEdge[0].edgeLength = Mathf.Round(curDist*100)/100;
							curDist = 0;
						}
						//Clear tmpList for next viable edge
						storedEdge.Clear();
						if(i!=region.Count-1){
							//This is a viable edge.
							storedEdge.Add(curNode);
							curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
						}
					}
				} else {
					//This edge doesn't have Road Access.
					if(storedEdge.Count > 0){
						//We have completed an Edge List
						storedEdge.Add(curNode);
						if(curDist>bestDist){
							bestDist = curDist;
							//Store List
							longestEdge = new List<CiDyNode>(storedEdge);
							longestEdge[0].edgeLength = Mathf.Round(curDist*100)/100;
							curDist = 0;
						}
						//Clear tmpList for next viable edge
						storedEdge.Clear();
					}
				}
			} else {
				//In the Middle of an Edge
				if(storedEdge.Count > 0){
					//We are building a list.
					storedEdge.Add(curNode);
					curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
				}
			}
		}
		/*if(longestEdge.Count > 0){
			Debug.Log("Set LongestEdge List: "+longestEdge.Count+" Total Edge Length: "+longestEdge[0].edgeLength);
		}*/
		//Return the Longest Edge with Road Access.
		return longestEdge;
	}

	//This function will find the longest Side without Road Access
	List<CiDyNode> GetLongestNonRoad(List<CiDyNode> region){
		//Initilize Variables
		List<CiDyNode> longestEdge = new List<CiDyNode>(0);//Longest Edge with Road Access
		float bestDist = 0;//Longest Found
		//Iterate through region. Find corner node and count length until next corner node.
		float curDist = 0;
		List<CiDyNode> storedEdge = new List<CiDyNode> (0);
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			if(curNode.isCorner){
				//At the Begininng/End of an Edge.
				if(!curNode.succNode.roadAccess){
					if(storedEdge.Count == 0){
						//Beginning of List
						storedEdge.Add(curNode);
						curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
					} else {
						//At End of List
						//We have completed an Edge List
						storedEdge.Add(curNode);
						if(curDist>bestDist){
							bestDist = curDist;
							//Store List
							longestEdge = new List<CiDyNode>(storedEdge);
							longestEdge[0].edgeLength = Mathf.Round(curDist*100)/100;
							curDist = 0;
						}
						//Clear tmpList for next viable edge
						storedEdge.Clear();
						if(i!=region.Count-1){
							//This is a viable edge.
							storedEdge.Add(curNode);
							curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
						}
					}
				} else {
					//This edge doesn't have Road Access.
					if(storedEdge.Count > 0){
						//We have completed an Edge List
						storedEdge.Add(curNode);
						if(curDist>bestDist){
							bestDist = curDist;
							//Store List
							longestEdge = new List<CiDyNode>(storedEdge);
							longestEdge[0].edgeLength = Mathf.Round(curDist*100)/100;
							curDist = 0;
						}
						//Clear tmpList for next viable edge
						storedEdge.Clear();
					}
				}
			} else {
				//In the Middle of an Edge
				if(storedEdge.Count > 0){
					//We are building a list.
					storedEdge.Add(curNode);
					curDist+=Vector3.Distance(curNode.position,curNode.succNode.position);
				}
			}
		}
		//Debug.Log("Set LongestEdge List: "+longestEdge.Count+" Total Edge Length: "+longestEdge[0].edgeLength);
		//Check for Degeneracy event where there are ONLY roads with access
		if(longestEdge.Count == 0){
			longestEdge = GetLongestRoad(region);
		}
		//Return the Longest Edge with Road Access.
		return longestEdge;
	}
	
	//Modified to Handle Chain Edges
	Vector3 CalcSplitPoint(List<CiDyNode> longestEdge, float splitSize, float lotDeviance){
		float factor = Mathf.Round(longestEdge[0].edgeLength/splitSize);
		float fraction = 1/factor;
		float midPosition = Mathf.Round(factor/2)*fraction;
		// calculate longest edge vector src → dst
		Vector3 longestEdgeVec = longestEdge[longestEdge.Count-1].position - longestEdge[0].position;
		//Calculate Perpendicular and store into edge for later.
		longestEdge[0].perpendicular = Vector3.Cross (Vector3.up, longestEdgeVec);
		//Return calculated Split point.
		return longestEdge[0].position+longestEdgeVec*(midPosition+(lotDeviance*fraction));
		//return longestEdge.transform.position+longestEdgeVec*(midPosition+(lotDeviance*(Random.value - 0.5f)*fraction));
	}

	//This algorithm will be used to split the region input by the line A-B.
	List<List<CiDyNode>> SplitRegion(List<CiDyNode> region, Vector3 a, Vector3 b){
		//List of intersection edges during split region process.
		List<CiDyNode> intersectionNodes = new List<CiDyNode>();
		//Variables used for split line perpedicular determination
		Vector3 ab = b - a;
		float lsq = ab.sqrMagnitude;
		//Set S Values of Edge Nodes from split line.
		for(int i = 0;i<region.Count;i+=2){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			//Set CurNode(SrcNode)
			float s = ((a.x-b.x) *(curNode.position.z-a.z)-(a.z-b.z)*(curNode.position.x-a.x))/lsq;
			s = Mathf.Round (s * 100f) / 100f;
			curNode.s = s;
			//set nxtNode(DstNode)
			s = ((a.x-b.x) *(nxtNode.position.z-a.z)-(a.z-b.z)*(nxtNode.position.x-a.x))/lsq;
			s = Mathf.Round (s * 100f) / 100f;
			nxtNode.s = s;
		}
		//Look at all edges that cross split line.
		for(int i=0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			//Make sure this edge intersects the split line.
			if(curNode.s > 0 && nxtNode.s <= 0 || curNode.s <= 0 && nxtNode.s > 0){
				//Debug.Log(curNode.gameObject.name+" s= "+curNode.s);
				//This edge intersects the split line.
				Vector3 cd = nxtNode.position - curNode.position;
				float denom = (ab.x * cd.z) - (ab.z * cd.x);
				Vector3 ca = a - curNode.position;
				float r = ((ca.z * cd.x) - (ca.x * cd.z))/denom; // loc on ab
				float s = ((ca.z * ab.x) - (ca.x * ab.z))/denom; // loc on cd
				CiDyNode intersectionNode;
				if(curNode.s == 0){
					//if split on src then the intersection node is already apart of the graph..
					intersectionNode = curNode;
					//Debug.Log("First Node "+curNode.gameObject.name+" s= "+curNode.s);
				} else if(nxtNode.s == 0){ 
					//if split on dst then the intersection node is already apart of the graph.
					intersectionNode = nxtNode;
					//Debug.Log("Second Node "+nxtNode.gameObject.name+" s= "+nxtNode.s);
				} else {
					//We need to make a new intersection node and insert it into the graph.
					// intersection point calc using cd, splitline ab is flat
					//first Node for the left of Line edge.
					//GameObject newNode = (GameObject)Instantiate(nodePrefab, curNode.transform.position+(s*cd), Quaternion.identity);
					//newNode.transform.parent = nodeHolder.transform;
					//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
					//newNode.name = "V"+nodeCount;
					//newNode.renderer.enabled = false;
					intersectionNode = NewNode("V"+nodeCount2,curNode.position+(s*cd), nodeCount2);
					intersectionNode.name = "V"+nodeCount2;
					//intersectionNode = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
					//intersectionNode.name = newNode.name;
					//Now we want to insert this edge into the list/
					//Set nodes connections
					curNode.succNode = intersectionNode;
					intersectionNode.succNode = nxtNode;
					//Now insert this Node into list at cur location.
					int insertPoint = i;
					region.Insert(insertPoint,intersectionNode);
					i++;
					//Debug.Log("New Node "+intersectionNode.gameObject.name+" s= "+intersectionNode.s);
				}
				//Debug.Log("R = "+r);
				//Update intersection Edges s value for sorting of its location along the split line from source split point.
				intersectionNode.r = r;
				//Update road access
				intersectionNode.roadAccess = curNode.roadAccess;
				intersectionNode.isCorner = true;
				//Add intersection Edge to intersectionEdge List.
				intersectionNodes.Add(intersectionNode);
			}
		}
		//Now sort intersection Nodes by S Value
		intersectionNodes = intersectionNodes.OrderBy(x => x.r).ToList();
		//Clear edges s values.
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			curNode.s = 0;
			nxtNode.s = 0;
		}
		//Create Bridges for Intersection Pairs
		for(int i=0;i<intersectionNodes.Count;i+=2){
			CiDyNode origNodeA = intersectionNodes[i];
			CiDyNode origNodeB = intersectionNodes[i+1];
			//Create there clone nodes
			//Clone A
			//GameObject newNode = (GameObject)Instantiate(nodePrefab, origNodeA.position, Quaternion.identity);
			//newNode.transform.parent = nodeHolder.transform;
			//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
			//newNode.name = "V"+nodeCount;
			//newNode.renderer.enabled = false;
			CiDyNode cloneA = NewNode("V"+nodeCount2,origNodeA.position, nodeCount2);
			cloneA.name = "V"+nodeCount2;
			//CiDyNode cloneA = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
			//Update nxtNode and roadacces
			cloneA.succNode = origNodeA.succNode;
			cloneA.roadAccess = origNodeA.roadAccess;
			//Clone B
			//newNode = (GameObject)Instantiate(nodePrefab, origNodeB.position, Quaternion.identity);
			//newNode.transform.parent = nodeHolder.transform;
			//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
			//newNode.name = "V"+nodeCount;
			//newNode.renderer.enabled = false;
			CiDyNode cloneB = NewNode("V"+nodeCount2,origNodeB.position, nodeCount2);
			cloneB.name = "V"+nodeCount2;
			//CiDyNode cloneB = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
			//Update nxtNode 
			cloneB.succNode = origNodeB.succNode;
			cloneB.roadAccess = origNodeB.roadAccess;
			//Now update origNode A's to there non-roadAccess  bridge edges
			//Node A -> CloneB//No Road Access
			origNodeA.succNode = cloneB;
			origNodeA.roadAccess = false;
			//Node B -> CloneA // No Road Access
			origNodeB.succNode = cloneA;
			origNodeB.roadAccess = false;
			if(origNodeA.isCorner){
				cloneA.isCorner = true;
			}
			if(origNodeB.isCorner){
				cloneB.isCorner = true;
			}
		}
		//Create new lists of sub-Regions.
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		//Extract the New Regions from the Graph.
		foreach(CiDyNode createdEdge in intersectionNodes){
			List<CiDyNode> newRegion = new List<CiDyNode>();
			CiDyNode edge = createdEdge;
			bool skipDuplicate = false;
			do
			{
				//Skip duplicates
				if(edge.s > 0){
					skipDuplicate = true;
					break;
				}
				edge.s = 1;//Mark as visited
				edge = edge.succNode;//Advance to Nxt Stage
				newRegion.Add(edge);//Add to current region being toured.
			} while(edge != createdEdge);
			if(!skipDuplicate){
				//Output Region
				outputRegions.Add(newRegion);
			}
		}
		//Debug.Log("Split Regions into "+outputRegions.Count+" parts");
		return outputRegions;
	}

	//This function will divide a region into specified lotWidth/lotDepth regions.
	public List<List<CiDyNode>> SubdivideLots2(List<CiDyNode> blockRegion, float lotWidth, float lotDepth){
		//Pre-Set Nodes cyclic List
		for(int i = 0;i<blockRegion.Count;i++){
			CiDyNode curNode = blockRegion[i];
			if(i==blockRegion.Count-1){
				curNode.succNode = blockRegion[0];
			} else {
				curNode.succNode = blockRegion[i+1];
			}
			//Set Road Access
			curNode.roadAccess = true;
		}
		bool firstRegion = true;
		//Debug.Log ("SubdivideLots");
		//Make sure lotDeviance is not equal to or higher than lotWidth || lotDepth
		lotDeviance = Mathf.Clamp (lotDeviance,0.1f, 0.9f);
        //Initilize Queue
        List<List<CiDyNode>> regionQueue = new List<List<CiDyNode>>
        {
            blockRegion
        };
        //Initilize testing region
        List<CiDyNode> region = new List<CiDyNode> ();
		//Initilize testing variables
		float splitSize;
		//Initilize Output List
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		//Use while loop to iterate through queue until at desired lot sizes.
		while(regionQueue.Count>0){
			//Grab next region for division
			region = regionQueue[0];
			//Debug.Log("Cutting Region, Count = "+region.Count);
			//Calc the longest road edge and split size
			CiDyNode longestEdge = GetLongestRoad2(region);
            if (longestEdge == null)
            {
                Debug.LogError("Longest Edge is Null");
            }
            if (firstRegion && longestEdge.edgeLength <= lotWidth){
				lotWidth=longestEdge.edgeLength-0.1f;
				//Debug.LogWarning("Lot Width Changed "+lotWidth);
			}
			if(firstRegion){
				firstRegion = false;
			}
			//Debug.Log("LongestEdge with Access ="+longestEdge.edgeLength);
			if(longestEdge.edgeLength <= lotWidth){
				// calc the longest non-road edge and split size
				longestEdge = GetLongestNonRoad2(region);
				//Debug.Log("LongestEdge Without Access "+longestEdge.edgeLength);
				if(longestEdge.edgeLength <= lotDepth){
					// if lot is small enough, add completed region
					outputRegions.Add(region);
					regionQueue.RemoveAt(0);
					//Debug.Log("Completed Region");
					continue;
				} else {
					//Debug.Log("Non-Road Split");
					splitSize = lotDepth;
				} 
			} else {
				//Debug.Log("Road Split");
				splitSize = lotWidth;
			}
			
			// calculate the split line points.
			Vector3 sp1 = CalcSplitPoint2(longestEdge, splitSize, lotDeviance);
			Vector3 sp2 = sp1+longestEdge.perpendicular;
			
			// split and process the new regions and add ones with road access to queue list.
			List<List<CiDyNode>> newRegions = SplitRegion2(region, sp1, sp2);
			regionQueue.RemoveAt(0);
			//Debug.Log("Split Region into "+newRegions.Count+" sub-regions");
			//Iterate through
			for(int i = 0;i<newRegions.Count;i++){
				List<CiDyNode> curRegion = newRegions[i];
				if(HasRoadAccess(curRegion)){
					//Debug.Log("RegionQueued");
					regionQueue.Add(curRegion);//add to processing queue
				} else {
					DeleteRegion(curRegion);
				}
			}
		}
		//Debug.Log (outputRegions.Count+" Valid Regions, "+greenSpace.Count+" Green Regions");
		//Return Lists of Sub-Regions
		return outputRegions;
	}

	//This function will divide a region into specified lotWidth/lotDepth regions.
	public List<List<CiDyNode>> SubdivideLots2(List<CiDyNode> blockRegion, float lotWidth, float lotDepth, ref List<List<CiDyNode>> greenSpace){
		//Pre-Set Nodes cyclic List
		for(int i = 0;i<blockRegion.Count;i++){
			CiDyNode curNode = blockRegion[i];
			if(i==blockRegion.Count-1){
				curNode.succNode = blockRegion[0];
			} else {
				curNode.succNode = blockRegion[i+1];
			}
			//Set Road Access
			curNode.roadAccess = true;
		}
		bool firstRegion = true;
		//Debug.Log ("SubdivideLots");
		//Make sure lotDeviance is not equal to or higher than lotWidth || lotDepth
		lotDeviance = Mathf.Clamp (lotDeviance,0.1f, 0.9f);
        //Initilize Queue
        List<List<CiDyNode>> regionQueue = new List<List<CiDyNode>>
        {
            blockRegion
        };
        //Initilize testing region
        List<CiDyNode> region = new List<CiDyNode> ();
		//Initilize testing variables
		float splitSize;
		//Initilize Output List
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		greenSpace = new List<List<CiDyNode>> (0);
		//Use while loop to iterate through queue until at desired lot sizes.
		while(regionQueue.Count>0){
			//Grab next region for division
			region = regionQueue[0];
			//Debug.Log("Cutting Region, Count = "+region.Count);
			//Calc the longest road edge and split size
			CiDyNode longestEdge = GetLongestRoad2(region);
			if(longestEdge.edgeLength <= lotWidth && firstRegion){
				lotWidth=longestEdge.edgeLength-0.1f;
				//Debug.LogWarning("Lot Width Changed "+lotWidth);
			}
			if(firstRegion){
				firstRegion = false;
			}
			//Debug.Log("LongestEdge with Access ="+longestEdge.edgeLength);
			if(longestEdge.edgeLength <= lotWidth){
				// calc the longest non-road edge and split size
				longestEdge = GetLongestNonRoad2(region);
				//Debug.Log("LongestEdge Without Access "+longestEdge.edgeLength);
				if(longestEdge.edgeLength <= lotDepth){
					// if lot is small enough, add completed region
					outputRegions.Add(region);
					regionQueue.RemoveAt(0);
					//Debug.Log("Completed Region");
					continue;
				} else {
					//Debug.Log("Non-Road Split");
					splitSize = lotDepth;
				} 
			} else {
				//Debug.Log("Road Split");
				splitSize = lotWidth;
			}
			
			// calculate the split line points.
			Vector3 sp1 = CalcSplitPoint2(longestEdge, splitSize, lotDeviance);
			Vector3 sp2 = sp1+longestEdge.perpendicular;
			
			// split and process the new regions and add ones with road access to queue list.
			List<List<CiDyNode>> newRegions = SplitRegion2(region, sp1, sp2);
			regionQueue.RemoveAt(0);
			//Debug.Log("Split Region into "+newRegions.Count+" sub-regions");
			//Iterate through
			for(int i = 0;i<newRegions.Count;i++){
				List<CiDyNode> curRegion = newRegions[i];
				if(HasRoadAccess(curRegion)){
					//Debug.Log("RegionQueued");
					regionQueue.Add(curRegion);//add to processing queue
				} else {
					//Debug.Log("Region Deleted");
					greenSpace.Add(curRegion);//Store for greenSpace use
				}
			}
		}
		//Debug.Log (outputRegions.Count+" Valid Regions, "+greenSpace.Count+" Green Regions");
		//Return Lists of Sub-Regions
		return outputRegions;
	}
	
	//This function will find the longest Side with Road Access
	CiDyNode GetLongestRoad2(List<CiDyNode> region){
		//Initilize Variables
		CiDyNode longestEdge = null;//Longest Edge with Road Access
		float bestDist = 0.0f;//Longest Found
		//Iterate through region and look at only nodes with Road Access.
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			if(curNode.roadAccess){
				//Determine dist of this edge and find longest.
				float dist = Vector3.Distance(curNode.position, curNode.succNode.position);
				if(dist>bestDist){
					//This is our longest edge.
					longestEdge = curNode;
					bestDist = dist;
					curNode.edgeLength = Mathf.Round (dist * 100f) / 100f;
				}
			}
		}
		//Return the Longest Edge with Road Access.
		return longestEdge;
	}
	
	//This function will find the longest Side without Road Access
	CiDyNode GetLongestNonRoad2(List<CiDyNode> region){
		//Initilize Variables
		CiDyNode longestEdge = null;//Longest Edge with Road Access
		float bestDist = 0.0f;//Longest Found
		//Iterate through region and look at only nodes without Road Access.
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			if(!curNode.roadAccess){
				//Determine dist of this edge and find longest.
				float dist = Vector3.Distance(curNode.position, curNode.succNode.position);
				if(dist>bestDist){
					//This is our longest edge.
					longestEdge = curNode;
					bestDist = dist;
					curNode.edgeLength = Mathf.Round (dist * 100f) / 100f;
				}
			}
		}
		//Check for Degeneracy event where there are ONLY roads with access
		if(longestEdge == null){
			longestEdge = GetLongestRoad2(region);
		}
		//Return the Longest Edge with Road Access.
		return longestEdge;
	}
	
	//This function will calculate the split point.
	Vector3 CalcSplitPoint2(CiDyNode longestEdge, float splitSize, float lotDeviance){
		float factor = Mathf.Round(longestEdge.edgeLength/splitSize);
		float fraction = 1/factor;
		float midPosition = Mathf.Round(factor/2)*fraction;
		// calculate longest edge vector src → dst
		Vector3 longestEdgeVec = longestEdge.succNode.position - longestEdge.position;
		//Calculate Perpendicular and store into edge for later.
		longestEdge.perpendicular = Vector3.Cross (Vector3.up, longestEdgeVec);
		//Return calculated Split point.
		return longestEdge.position+longestEdgeVec*(midPosition+(lotDeviance*fraction));
		//return longestEdge.transform.position+longestEdgeVec*(midPosition+(lotDeviance*(Random.value - 0.5f)*fraction));
	}
	
	//This algorithm will be used to split the region input by the line A-B.
	List<List<CiDyNode>> SplitRegion2(List<CiDyNode> region, Vector3 a, Vector3 b){
		//List of intersection edges during split region process.
		List<CiDyNode> intersectionNodes = new List<CiDyNode>();
		//Variables used for split line perpedicular determination
		Vector3 ab = b - a;
		float lsq = ab.sqrMagnitude;
		//Set S Values of Edge Nodes from split line.
		for(int i = 0;i<region.Count;i+=2){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			//Set CurNode(SrcNode)
			float s = ((a.x-b.x) *(curNode.position.z-a.z)-(a.z-b.z)*(curNode.position.x-a.x))/lsq;
			s = Mathf.Round (s * 100f) / 100f;
			curNode.s = s;
			//set nxtNode(DstNode)
			s = ((a.x-b.x) *(nxtNode.position.z-a.z)-(a.z-b.z)*(nxtNode.position.x-a.x))/lsq;
			s = Mathf.Round (s * 100f) / 100f;
			nxtNode.s = s;
		}
		//Look at all edges that cross split line.
		for(int i=0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			//Make sure this edge intersects the split line.
			if(curNode.s > 0 && nxtNode.s <= 0 || curNode.s <= 0 && nxtNode.s > 0){
				//Debug.Log(curNode.gameObject.name+" s= "+curNode.s);
				//This edge intersects the split line.
				Vector3 cd = nxtNode.position - curNode.position;
				float denom = (ab.x * cd.z) - (ab.z * cd.x);
				Vector3 ca = a - curNode.position;
				float r = ((ca.z * cd.x) - (ca.x * cd.z))/denom; // loc on ab
				float s = ((ca.z * ab.x) - (ca.x * ab.z))/denom; // loc on cd
				CiDyNode intersectionNode;
				if(curNode.s == 0){
					//if split on src then the intersection node is already apart of the graph..
					intersectionNode = curNode;
					//Debug.Log("First Node "+curNode.gameObject.name+" s= "+curNode.s);
				} else if(nxtNode.s == 0){ 
					//if split on dst then the intersection node is already apart of the graph.
					intersectionNode = nxtNode;
					//Debug.Log("Second Node "+nxtNode.gameObject.name+" s= "+nxtNode.s);
				} else {
					//We need to make a new intersection node and insert it into the graph.
					// intersection point calc using cd, splitline ab is flat
					//first Node for the left of Line edge.
					//GameObject newNode = (GameObject)Instantiate(nodePrefab, curNode.transform.position+(s*cd), Quaternion.identity);
					//newNode.transform.parent = nodeHolder.transform;
					//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
					//newNode.name = "V"+nodeCount;
					//newNode.renderer.enabled = false;
					intersectionNode = NewNode("V"+nodeCount2,curNode.position+(s*cd), nodeCount2);
					intersectionNode.name = "V"+nodeCount2;
					//intersectionNode = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
					//intersectionNode.name = newNode.name;
					//Now we want to insert this edge into the list/
					//Set nodes connections
					curNode.succNode = intersectionNode;
					intersectionNode.succNode = nxtNode;
					//Now insert this Node into list at cur location.
					int insertPoint = i;
					region.Insert(insertPoint,intersectionNode);
					i++;
					//Debug.Log("New Node "+intersectionNode.gameObject.name+" s= "+intersectionNode.s);
				}
				//Debug.Log("R = "+r);
				//Update intersection Edges s value for sorting of its location along the split line from source split point.
				intersectionNode.r = r;
				//Update road access
				intersectionNode.roadAccess = curNode.roadAccess;
				//Add intersection Edge to intersectionEdge List.
				intersectionNodes.Add(intersectionNode);
			}
		}
		//Now sort intersection Nodes by S Value
		intersectionNodes = intersectionNodes.OrderBy(x => x.r).ToList();
		//Clear edges s values.
		for(int i = 0;i<region.Count;i++){
			CiDyNode curNode = region[i];
			CiDyNode nxtNode = curNode.succNode;
			curNode.s = 0;
			nxtNode.s = 0;
		}
		//Create Bridges for Intersection Pairs
		for(int i=0;i<intersectionNodes.Count;i+=2){
			CiDyNode origNodeA = intersectionNodes[i];
			CiDyNode origNodeB = intersectionNodes[i+1];
			//Create there clone nodes
			//Clone A
			//GameObject newNode = (GameObject)Instantiate(nodePrefab, origNodeA.position, Quaternion.identity);
			//newNode.transform.parent = nodeHolder.transform;
			//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
			//newNode.name = "V"+nodeCount;
			//newNode.renderer.enabled = false;
			CiDyNode cloneA = NewNode("V"+nodeCount2,origNodeA.position, nodeCount2);
			cloneA.name = "V"+nodeCount2;
			//CiDyNode cloneA = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
			//Update nxtNode and roadacces
			cloneA.succNode = origNodeA.succNode;
			cloneA.roadAccess = origNodeA.roadAccess;
			//Clone B
			//newNode = (GameObject)Instantiate(nodePrefab, origNodeB.position, Quaternion.identity);
			//newNode.transform.parent = nodeHolder.transform;
			//newNode.transform.localScale = new Vector3(nodeScale,nodeScale,nodeScale);
			//newNode.name = "V"+nodeCount;
			//newNode.renderer.enabled = false;
			CiDyNode cloneB = NewNode("V"+nodeCount2,origNodeB.position, nodeCount2);
			cloneB.name = "V"+nodeCount2;
			//CiDyNode cloneB = (CiDyNode)newNode.GetComponent(typeof(CiDyNode));
			//Update nxtNode 
			cloneB.succNode = origNodeB.succNode;
			cloneB.roadAccess = origNodeB.roadAccess;
			//Now update origNode A's to there non-roadAccess  bridge edges
			//Node A -> CloneB//No Road Access
			origNodeA.succNode = cloneB;
			origNodeA.roadAccess = false;
			//Node B -> CloneA // No Road Access
			origNodeB.succNode = cloneA;
			origNodeB.roadAccess = false;
		}
		//Create new lists of sub-Regions.
		List<List<CiDyNode>> outputRegions = new List<List<CiDyNode>> ();
		//Extract the New Regions from the Graph.
		foreach(CiDyNode createdEdge in intersectionNodes){
			List<CiDyNode> newRegion = new List<CiDyNode>();
			CiDyNode edge = createdEdge;
			bool skipDuplicate = false;
			do
			{
				//Skip duplicates
				if(edge.s > 0){
					skipDuplicate = true;
					break;
				}
				edge.s = 1;//Mark as visited
				edge = edge.succNode;//Advance to Nxt Stage
				newRegion.Add(edge);//Add to current region being toured.
			} while(edge != createdEdge);
			if(!skipDuplicate){
				//Output Region
				outputRegions.Add(newRegion);
			}
		}
		//Debug.Log("Split Regions into "+outputRegions.Count+" parts");
		return outputRegions;
	}

	//This function will say if this region has any road access
	bool HasRoadAccess(List<CiDyNode> region){
		for(int i = 0;i<region.Count;i++){
			if(region[i].roadAccess){
				//Yes it has road access
				return true;
			}
		}
		//No access found
		return false;
	}

	//This function will delete a regions nodes
	void DeleteRegion(List<CiDyNode> badRegion){
		//Debug.Log ("Destroyed Region");
		/*for(int i = 0;i<badRegion.Count;i++){
			CiDyNode curNode = badRegion[i];
			//Debug.Log("Destroyed "+curNode.gameObject.name);
			DestroyImmediate(curNode.gameObject);
		}*/
		badRegion.Clear ();
	}

	//This function will clear the Graph Data.
	public IEnumerator ClearGraph(){

        //Clearing Graph. Let User Know
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", "Preping ", 0f);
        #endif
        //Calculate Clearing Time
        CalculateBlendTime();
		//Debug.Log ("ClearGraph");
		if(masterGraph.Count > 0){
			for(int i = 0;i<masterGraph.Count;i++){
				DestroyMasterNode(masterGraph[i]);
				i--;
                curProblems--;
                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Clearing Intersections: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
			}
			masterGraph.Clear();
			nodeCount = 0;
			nodeCount2 = 0;
		}
        //Get Actual Cell Containers
        CiDyCell[] actualCellContainers =  cellsHolder.GetComponentsInChildren<CiDyCell>();
        /*//Get Actual Boundary Cells
        Transform[] actualBoundaryCells = boundaryHolder.GetComponentsInChildren<Transform>();//Grab the Buildings in this cell. (It doesnt have anything else).
        if(actualBoundaryCells.Length > 0)
        {
            //Clear each Object
            for (int i = 0; i < actualBoundaryCells.Length; i++)
            {
                if (actualBoundaryCells[i] != null)
                {
                    //Skip ourself
                    if (actualBoundaryCells[i].name == boundaryHolder.name)
                    {
                        continue;
                    }
                    DestroyImmediate(actualBoundaryCells[i].gameObject);
                    curProblems--;
                    #if UNITY_EDITOR
                        UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Clearing Boundary Cells: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                    #endif
                }
            }
            //actualBoundaryCells = new Transform[0];
        }*/
        //Debug.Log("Cell Count for Deletion: " + cells.Count + " Actual Transforms, " + actualCellContainers.Length);
        //Manual Clear Cells Logic for Display Purposes
        if (cells.Count > 0 || actualCellContainers.Length > 0){
            //Count how many Cells are present in the Cells Holder
            
            if (actualCellContainers.Length > cells.Count) {
                //We have lost a connection somewhere.
                Debug.LogError("Cell Lost Connection due to internal error: "+Mathf.Abs(actualCellContainers.Length-cells.Count));
                for (int i = 0; i < actualCellContainers.Length; i++) {
                    DestroyImmediate(actualCellContainers[i].gameObject);
                    curProblems--;
                    #if UNITY_EDITOR
                        UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Clearing Cells: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                    #endif
                }
            } else {
                //Debug.Log ("Clear Cells");
                for (int i = 0; i < cells.Count; i++)
                {
                    DestroyImmediate(cells[i].holder);
                    curProblems--;
                     #if UNITY_EDITOR
                        UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Clearing Cells: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                    #endif
                }
            }
            //Clear References so we do not leak
            cells.Clear();
            runtimeCycles.Clear();
            cycles.Clear();
        }
		if(roads.Count > 0){
			for(int i = 0;i<roads.Count;i++){
				DestroyImmediate(roads[i]);
                curProblems--;
                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Clearing Roads: ", (1.0f - ((float)curProblems / (float)totalProblems)));
                #endif
			}
			roads.Clear();
		}
		if(graphEdges.Count > 0){
			graphEdges.Clear();
		}

        //Clear User Defined Road Points.
        ClearUserDefinedRoadPoints();

        //Return Terrain Details
        if (terrain)
        {
            #if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("Clearing City:", " Resetting Terrain Data: ", (1.0f - ((float)curProblems / (float)totalProblems)));
            #endif
            ResetTerrain();
        }
        //Finish
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
        #endif

        yield return null;
    }

    void ResetTerrain() {
        //Debug.Log("Restore Heights");
        //Restore Terrain Heights
        RestoreOriginalTerrainHeights();
        //Debug.Log("Restore Vegitation");
        ReAddTrees(terrain);//ReAdd Trees
        ReAddGrass(terrain);//ReAdd Grass
        //Flush Terrain
        terrain.Flush();
    }
	//Undo has been performed in the Editor. Check for changes in CiDy Graph.
	/*public void UpdateGraphFromUndo(){
		Debug.Log ("Update Graph From Undo/Redo");
		//Determine if any nodes are destroyed?
		for(int i = 0;i<masterGraph.Count;i++){
			if(masterGraph[i]==null){
				masterGraph.RemoveAt(i);
				break;
			}
		}
		//Check if any roads are Destroyed
		for(int i = 0;i<roads.Count;i++){
			if(roads[i] == null){
				roads.RemoveAt(i);
				break;
			}
		}
		//check if any edges are removed
		for(int i = 0;i<graphEdges.Count;i++){
			if(graphEdges[i].v1 == null || graphEdges[i].v2 == null){
				graphEdges.RemoveAt(i);
				break;
			}
		}
		//Check if any cells are destroyed
		for(int i = 0;i<cells.Count;i++){
			if(cells[i] == null){
				cells.RemoveAt(i);
				break;
			}
		}
	}*/

	//This function will clear the cells that have been created by the Graph.
	public void ClearCells(){
		//Debug.Log ("Clear Cells");
		for(int i = 0;i<cells.Count;i++){
			DestroyImmediate(cells[i].holder);
		}
		//Clear References so we do not leak
		cells.Clear ();
		runtimeCycles.Clear ();
		cycles.Clear();
	}

	//This function will determine if we have any cells that need to be updated from this moved node
	public void UpdateNodesCell(CiDyNode movedNode){
		string name = movedNode.name;
		if(cells.Count > 0){
			for(int i = 0;i<cells.Count;i++){
				List<CiDyNode> cycle = cells[i].cycleNodes;
				for(int j = 0;j<cycle.Count;j++){
					if(name == cycle[j].name){
						//Update this Cell.
						cells[i].UpdateCell();
						break;
					}
				}
			}
		}
	}

	public void UpdateRoadCell(CiDyRoad movedRoad){
       // Debug.Log("Update Road Cell");
        string nameA = movedRoad.nodeA.name;
		string nameB = movedRoad.nodeB.name;
		//Find a Cell with Both Nodes in it and that cell needs to be updated.
		if(cells.Count > 0){
			for(int i = 0;i<cells.Count;i++){
                bool foundA = false;
                bool foundB = false;
                List<CiDyNode> cycle = cells[i].cycleNodes;
                //Debug.Log("Testing Cell Update based on Moved Road Node A: "+nameA+" Node B: "+nameB);
				for(int j = 0;j<cycle.Count;j++){
                    if (cycle[j].name == nameA) {
                        foundA = true;
                        //Debug.Log("NodeA @ "+j);
                    }
                    if (cycle[j].name == nameB) {
                        foundB = true;
                        //Debug.Log("NodeB @ "+j);
                    }
                    if (foundA && foundB) {
                        //Debug.Log("Update Cell: "+cells[i].name);
                        cells[i].UpdateCell();
                        break;
                    }
					/*if(cycle[j].name == nameA || cycle[j].name == nameB){
						cells[i].UpdateCell();
						break;
					}*/
				}
			}
		}
	}

	public void RegenerateCells(){
		//Debug.Log ("Regenerating Cells");
		for(int i = 0;i<cells.Count;i++){
			//Debug.Log(cells[i].holder.name);
			cells[i].UpdateCell();
		}
	}

    //CiDy Cell
    public void UpdateBoundaryCells(List<List<Vector3>> roadEdge) {
        //Clear any previously Exisiting Cells
        Debug.Log("Update Boundary Cells: " + roadEdge.Count);
        GameObject[] prefabBuildings = Resources.LoadAll("CiDyResources/DownTownPrefabs", typeof(GameObject)).Cast<GameObject>().ToArray();
        //This is a special Cell, The Graph Handles This Cell.
        int rng = 0;
        RaycastHit hit;
        //Move Along the Road Edge and Try to Place a Building from the Potential Selections of Buildings.
        for (int i = 0; i < roadEdge.Count; i++) {
            for (int j = 0; j < roadEdge[i].Count; j++) {
                //Check Raycast to terrain
                if (terrain != null) {
                    if (Physics.Raycast(roadEdge[i][j] + (Vector3.up * 1000), Vector3.down, out hit)) {
                        //We hit something.
                        if (hit.collider.tag == "Terrain") {
                            //This is our Ground.
                            //Place a Random Building from our Boundary Cell Selection.
                            rng = UnityEngine.Random.Range(0, prefabBuildings.Length - 1);
                            GameObject newBuilding = Instantiate(prefabBuildings[rng], roadEdge[i][j], Quaternion.identity);
                            newBuilding.transform.parent = boundaryHolder.transform;
                        }
                    }
                }
            }
        }
    }

    //CiDyCell Stuff
    public void UpdateCells(List<List<CiDyNode>> cyclesList){
		//Debug.Log ("Updated Cells");
		runtimeCycles = new List<List<CiDyNode>>(cyclesList);
        //Make sure to Reference the Actual Nodes in the Master Graph so we have access to all there data.
        //Debug.Log ("Update Cells "+cells.Count);
        //Iterate through the Cells that exist.
        /*for (int i = 0; i<cyclesList.Count; i++) {
			List<CiDyNode> newList = cyclesList[i];
			Debug.Log("Cell "+i);
			for(int j = 0;j<newList.Count;j++){
				Debug.Log(newList[j].name);
			}
		}*/
        List<CiDyCell> actualCells = cellsHolder.GetComponentsInChildren<CiDyCell>().ToList();
        if (actualCells.Count > cells.Count)
        {
            //Remove un-connected Cells
            if (cells.Count > 0)
            {
                for (int i = 0; i < actualCells.Count; i++)
                {
                    //Test to see if this cell is connected?
                    for (int j = 0; j < cells.Count; j++) {
                        //Remove all that are still connected
                        if (actualCells[i].name == cells[j].name)
                        {
                            //Remove from list.
                            cells.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
            //Now remove all that remain
            if (actualCells.Count > 0)
            {
                for (int i = 0; i < actualCells.Count; i++)
                {
                    //Debug.Log("Removed Cell "+cells[i].holder.name);
                    DestroyImmediate(actualCells[i].gameObject);
                }
                actualCells.Clear();
            }
        }
		//remove all duplicates in cycles list. and remove all in cells list that are not found in the cycles list.
		if(cells.Count > 0){
			//Debug.Log("Testing Existing Cells "+cells.Count);
			//We have Cells So Find All Cells that already exist in the cycles list and remove those from the list.
			for(int i = 0;i<cells.Count;i++){
				List<CiDyNode> curCell = cells[i].cycleNodes;
				//Debug.Log("Cell "+cells[i].holder.name);
				//If any of these cells are not in the list then they no longer exist, destroy them.
				if(!DuplicateCell(curCell, ref cyclesList)){
					//Debug.Log("Removed Cell "+cells[i].holder.name);
					DestroyImmediate(cells[i].holder);
					//Remove from list.
					cells.RemoveAt(i);
					i--;
				}
			}
			//now all the cell cycles left in the cyclesList are New Cells that need to be created
			if(cyclesList.Count > 0){
				//StartCoroutine(CreateCells(cyclesList));
                CreateCells(cyclesList);
			}
		} else {
            //Debug.Log("All New Cells in list");
            //StartCoroutine(CreateCells(cyclesList));
            CreateCells(cyclesList);
		}
	}

	//This function will determine if the node is apart of the cells boundary nodes.
	public bool IsBoundaryNode(CiDyCell curCell, CiDyNode curNode){
		//Grab Boundary Nodes from cell.
		List<CiDyNode> boundaryNodes = curCell.cycleNodes;
		//Iterate through nodes and tell me if any match the curNode
		for(int i = 0;i<boundaryNodes.Count;i++){
			if(curNode.name == boundaryNodes[i].name){
				//We have a match this cell needs to be updated.
				return true;
			}
		}
		//If we are here then the node is not apart of this cell.
		return false;
	}

	[HideInInspector]
	public List<List<CiDyNode>> runtimeCycles = new List<List<CiDyNode>>();
	//Create a Cell and place it into cells List.
	void CreateCells(List<List<CiDyNode>> cyclesList){
		//Debug.Log ("runtIem Updated ");
		//All new List are created
		for(int i = 0;i<cyclesList.Count;i++){
            //Debug.Log("Created new cell");
            GameObject cellHolder = new GameObject("Tmp")
            {
                layer = LayerMask.NameToLayer(cellTag),
                tag = cellTag
            };
            cellHolder.transform.parent = cellsHolder.transform;
			CiDyCell newCell = cellHolder.AddComponent<CiDyCell>();
			//Set Building Type
			newCell.buildingType = (CiDyCell.BuildingType)buildingType;
			newCell.holder = cellHolder;
			newCell.SetGraph(this);
			int newInt = runtimeCycles.IndexOf(cyclesList[i]);
			newCell.UpdateCellCycle(newInt);
			cells.Add (newCell);
            //Debug.Log("Created "+cellHolder.name);
            //Create Cells From List<CiDyNode> cycle
            //CreateCell(cycle);
            //Turn this list into a slav
            //graph.SetSlav(cycle,(roadWidth/2)+desiredInset);
            //yield return null;
		}
	}

	//Determine if this cell is held in cells list for this graph already?
	bool DuplicateCell(List<CiDyNode> newCycle){
		if(cells.Count==0){
			//Debug.Log("No Cells So this is a new One");
			return false;
		} else {
			//Iterate through Cells
			for(int i = 0;i<cells.Count;i++){
				//Look at the First Cells List of nodes and see if any or all of them are in our list?
				List<CiDyNode> tmpCell = cells[i].cycleNodes;
				if(tmpCell.Count == newCycle.Count){
					//Iterate through tmpCell and Determine if it has newCycles first Node.
					for(int j = 0;j<tmpCell.Count;j++){
						CiDyNode tmpNode = tmpCell[j];
						if(tmpNode.name == newCycle[0].name){
							//TmpCell has the first node.
							//Does tmpCell have the secondNode nxt in line?
							CiDyNode secNode;
							if(j<tmpCell.Count-1){
								secNode = tmpCell[j+1];
							} else {
								secNode = tmpCell[0];
							}
							if(secNode != null){
								//If the Second node is equal to the second node of list order then we have a duplicat
								if(secNode.name == newCycle[1].name){
									return true;
								}
							}
						}
					}
				} else {
					//These counts are different so they cannot be the same cycle
					continue;
				}
			}
		}
		//Debug.Log ("No Duplicate found");
		//We have made it here so its not a duplicate
		return false;
	}

	bool DuplicateCell(List<CiDyNode> newCell,  ref List<List<CiDyNode>> curList){
		//Determine if the newCell is found inside the CurList
		//Iterate through curList
		//Debug.Log ("Testing");
		for(int i = 0;i<curList.Count;i++){
			//Find Duplicates with new Cell.
			List<CiDyNode> newList = curList[i];
			//Cannot be a duplicate if the counts are different.
			if(newList.Count == newCell.Count){
				//Debug.Log("Same Count");
				//Potential duplicate test further
				//Needs exact sequence order to be proper duplicate find starting point.
				int n = 0;
				for(int j = 0;j<newList.Count;j++){
					bool started = false;
					if(!started){
						if(newList[j].name == newCell[0].name){
							//Debug.Log(j);
							//We found the start point,Check Sequence
							started = true;
							//Debug.Log("Found Start Point "+newList[j].name+" "+newCell[0].name+" J: "+j);
							n = j;
							//Make sure that all are the same from here to the end.
							for(int k = 0;k<newCell.Count;k++){
								//Debug.Log(newCell[k].name+" Cell/list "+newList[n].name);
								if(newCell[k].name != newList[n].name){
									//These are not the same sequences
									//Debug.Log(newCell[k].name+" Different Sequence "+newList[n].name);
									break;
								} else {
									//Debug.Log(newCell[k].name+" Same "+newList[n].name);
									if(k==newCell.Count-1){
										//This is the last one
										//Debug.Log("Last Node needed. Same Sequence");
										//If we are here then it is the same cell
										curList.RemoveAt(i);
										//Debug.Log("Same Sequence Found "+curList.Count);
										return true;
									}
								}
								if(n < newList.Count-1){
									n++;
								} else {
									//We are at the last point so restart.
									n=0;
								}
							}
						}
					}
				}
			}
		}
		//If we are here then no duplicate found.
		return false;
	}
	
	////USED FOR DRAWING GIZMOSE AND EDITOR RUNTIME STUFF
	/*void OnDrawGizmos()
	{
		Vector3 pos = Camera.current.transform.position;
		for (float y = pos.y - 800.0f; y < pos.y + 800.0f; y+= height)
		{
			Gizmos.DrawLine(new Vector3(-1000000.0f, Mathf.Floor(y/height) * height, 0.0f),
			                new Vector3(1000000.0f, Mathf.Floor(y/height) * height, 0.0f));
		}
		
		for (float x = pos.x - 1200.0f; x < pos.x + 1200.0f; x+= width)
		{
			Gizmos.DrawLine(new Vector3(Mathf.Floor(x/width) * width, -1000000.0f, 0.0f),
			                new Vector3(Mathf.Floor(x/width) * width, 1000000.0f, 0.0f));
		}
	}*/

	////////Traffic System///////////////
	//This function will determine the Graphs current traffic connections for AI Use.
	void BuildTrafficData(){
		//Lets iterate through the Roads.
		for(int i = 0;i<roads.Count;i++){
			CiDyRoad curRoad = roads[i].GetComponent<CiDyRoad>();
			switch(curRoad.laneType){
				case CiDyRoad.LaneType.OneLane:

				break;
				case CiDyRoad.LaneType.TwoLane:

				break;
				case CiDyRoad.LaneType.FourLane:

				break;
				case CiDyRoad.LaneType.SixLane:

				break;
			}
		}
	}

    //2019 CiDy 2.0 Re-Write of CiDy Graph-> Designer Connections.
    //Variables
    /*[HideInInspector]
    [SerializeField]
    public List<CiDySpline> allSplines;//Public Reference to All Splines in this Graph.

    public CiDySpline NewSpline(Vector3 point) {
        //Start a New Spline
        CiDySpline tmpSpline = new CiDySpline(point);
        allSplines.Add(tmpSpline);
        return tmpSpline;
    }

    void GeneratePopulation(ref List<Vector3> midPoints, Terrain t, Transform cell, Vector3[] sideWalkEdge, float sideWalkwidth, int layerMask, float sideWalkHeight)
    {
        midPoints.Clear();
        //Calculate Points to Blend that are in the middle of the sidewalk.
        for (int i = 0; i < sideWalkEdge.Length; i++)
        {
            Vector3 p0 = sideWalkEdge[i];
            Vector3 p1;
            if (i == sideWalkEdge.Length - 1)
            {
                p1 = sideWalkEdge[0];
            }
            else {
                p1 = sideWalkEdge[i + 1];
            }
            //Offset to proper world space
            p0 = p0 + cell.position;
            p1 = p1 + cell.position;
            if (UnityEngine.Vector3.Distance(p0, p1) <= 0.1618f) {
                continue;
            }
            //Calculate our current direction
            Vector3 fwd;
            fwd = (p1 - p0).normalized;
            if (i == sideWalkEdge.Length - 1)
            {
                fwd = -fwd;
            }
            //Now that we know our forward direction and World Up. Lets get cross for Left Direction.
            Vector3 left = -Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 finalPoint = p0 + (left * sideWalkwidth / 2);
            //Now that we have the Left Direction. Move the Points over to the Middle.
            midPoints.Add(finalPoint);
        }
        //Now that we have the Walking Source Paths. Lets Send This Information to the Population Pool Manager.(Sending Source Path(Bi Directional Path)

    }*/
}

[System.Serializable]
public class TerrainDetail
{
    [SerializeField]
    int[] terrainDetails;
    [SerializeField]
    int _Width = 0;
    [SerializeField]
    int _Height = 0;
    //Initialize
    public TerrainDetail(int[,] terrainMap, int width, int height) {
        _Width = width;
        _Height = height;

        terrainDetails = new int[width * height];
        
        for (int _X = 0; _X < width; _X++) {
            for (int _Y = 0; _Y < height; _Y++) {
                terrainDetails[_Y * width + _X] = terrainMap[_X,_Y];
            }
        }
    }

    public int Length() {
        if (terrainDetails == null) {
            return 0;
        }
        return terrainDetails.Length;
    }

    public int[,] ReturnMap() {

        //Debug.Log("Width: "+_Width+" Height: "+_Height);
        int[,] map = new int[_Width, _Height];
        for (int i = 0; i < _Width; i++)
        {
            for (int j = 0; j < _Height; j++)
            {
                map[i, j] = terrainDetails[j * _Height + i];
            }
        }

        return map;
    }
}

[System.Serializable]
public class SerializedTreeInstance {

    [SerializeField]
    Color32 _Color;
    [SerializeField]
    float _HeightScale;
    [SerializeField]
    Color32 _LightMapColor;
    [SerializeField]
    Vector3 _Position;
    [SerializeField]
    int _PrototypeIndex;
    [SerializeField]
    float _Rotation;
    [SerializeField]
    float _WidthScale;

    public SerializedTreeInstance(Color32 newColor, float newHeightScale, Color32 newLightMapColor, Vector3 newPos, int TypeIndex, float rot, float newWidthScale){
        _Color = newColor;
        _HeightScale = newHeightScale;
        _LightMapColor = newLightMapColor;
        _Position = newPos;
        _PrototypeIndex = TypeIndex;
        _Rotation = rot;
        _WidthScale = newWidthScale;
    }

    //Returns a TreeInstance from Data.
    public TreeInstance Instance() {
        TreeInstance newInstance = new TreeInstance
        {
            color = _Color,
            heightScale = _HeightScale,
            lightmapColor = _LightMapColor,
            position = _Position,
            prototypeIndex = _PrototypeIndex,
            rotation = _Rotation,
            widthScale = _WidthScale
        };

        return newInstance;
    }
}

//Thread Classes
[System.Serializable]
public class ThreadMesh {
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] verts;

    public ThreadMesh(int[] newTris, Vector2[] newUvs, Vector3[] newVerts) {
        triangles = newTris;
        uvs = newUvs;
        verts = newVerts;
    }

    public ThreadMesh(Mesh newMesh)
    {
        triangles = newMesh.triangles;
        uvs = newMesh.uv;
        verts = newMesh.vertices;
    }
}