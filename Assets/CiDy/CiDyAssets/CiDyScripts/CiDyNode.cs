using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

//The node class holds the adjacency list. Each Node points to all its adjacent nodes.
// Custom serializable class
[System.Serializable]
public class CiDyNode : ScriptableObject {

    [HideInInspector]
    [SerializeField]
    public Hierarchy hierarchy = Hierarchy.Major;//The Road Connection Type this edge is.
    [SerializeField]
    public enum Hierarchy
    {
        Major,
        Minor
    }
    [HideInInspector]
    [SerializeField]
    public Status status = Status.Planned;//If this road is full of traffic or not.
    [SerializeField]
    public enum Status
    {
        Planned,
        Built
    }
    //float cornerSpacing = 1.1f;
    //Name and world position are used for edge creation sorting and Primitive Detection ( Cycle);
    [SerializeField]
    public int nodeNumber;//Used for Edge Naming/Comparison Testing
	new public string name = "";
	public Vector3 position;
	//For Testing
	public float Angle;
	//Marked (This is used when checking for Cycles(MCB)
	public bool marked = false;
	//Our adjacent nodes list.
	[SerializeField]
	public List<CiDyNode> adjacentNodes;
	//For Intersectoin Mesh
	[SerializeField]
	public List<CiDyListWrapper> cornerPoints;
	public List<Vector3> culDeSacPoints;
    //For Detail Flattening/Blending
    [SerializeField]
    public int maxRadius = 0;
	//For cell graphs
	public float r = 0f;
	public float s = 0f;
	public float distToP = 0f;
	//For Striaght Skeleton
	public Vector3 bisector;
	public CiDyNode predNode;
	public CiDyNode succNode;
	public CiDyEdge eA;
	public CiDyEdge eB;
	public CiDyNode reflexNode;
	public bool reflex = false;
	public bool isCorner = false;
	public bool isCuldesac = false;
    public int polyPos;
	public Vector3 edgeDir;
	public CiDyNode origNode = null;
	public CiDyNode origEvent = null;
	//Used for subdivision
	public bool roadAccess = false;
	public float edgeLength;
	public Vector3 perpendicular;
	public GameObject nodeObject;//Our gameObject for this node IF there is one.
    //Used for Editor Material setup etc
    public Material intersectionMaterial;//The Material we apply to our Intersection Meshes.
    public Material roadMaterial;//The Material we use for Road Connector Pieces.
	// Static singleton property
	public CiDyGraph Graph {get; private set;}
	public CiDyCell Cell {get; private set;}

	public CiDyNode Init(string newName,Vector3 newPosition, CiDyGraph newGraph, int nodeCount){
		Graph = newGraph;
		name = newName;
		nodeNumber = nodeCount;
		position = newPosition;
		//Clear List
		adjacentNodes = new List<CiDyNode>(0);
		connectedRoads = new List<CiDyRoad> (0);
		return this;
	}

    public CiDyNode Init(string newName,Vector3 newPosition, CiDyGraph newGraph, int nodeCount, GameObject position3D){
		Graph = newGraph;
		name = newName;
		position = newPosition;
		//string tmpName = name;
		nodeNumber = nodeCount;
		//Debug.Log (name + " 1 " + nodeNumber);
		//nodeNumber = nodeCount;
		//Clear List
		adjacentNodes = new List<CiDyNode>(0);
		connectedRoads = new List<CiDyRoad> (0);
		nodeObject = position3D;
        //Grab Material Info
        //Apply Intersection Texture
        if (Graph == null || Graph.intersectionMaterial == null)
        {
            intersectionMaterial = (Material)Resources.Load("CiDyResources/Intersection");
        }
        else
        {
            intersectionMaterial = Graph.intersectionMaterial;
        }
        //Apply Road Texture
        if (Graph == null || Graph.roadMaterial == null)
        {
            roadMaterial = (Material)Resources.Load("CiDyResources/Road", typeof(Material));
        }
        else
        {
            roadMaterial = Graph.roadMaterial;
        }
        //Create Intersection Mesh
        CreateIntersection();
		return this;
	}

    public CiDyNode Init(string newName, Vector3 newPosition, CiDyGraph newGraph, int nodeCount, GameObject position3D, CiDyNode.Hierarchy newHiearchy)
    {
        Graph = newGraph;
        name = newName;
        position = newPosition;
        //string tmpName = name;
        nodeNumber = nodeCount;
        //Debug.Log (name + " 1 " + nodeNumber);
        //nodeNumber = nodeCount;
        //Clear List
        adjacentNodes = new List<CiDyNode>(0);
        connectedRoads = new List<CiDyRoad>(0);
        nodeObject = position3D;
        //Set Hiearchy Type.
        hierarchy = newHiearchy;
        //Grab Material Info
        //Apply Intersection Texture
        if (Graph == null || Graph.intersectionMaterial == null)
        {
            intersectionMaterial = (Material)Resources.Load("CiDyResources/Intersection");
        }
        else
        {
            intersectionMaterial = Graph.intersectionMaterial;
        }
        //Apply Road Texture
        if (Graph == null || Graph.roadMaterial == null)
        {
            roadMaterial = (Material)Resources.Load("CiDyResources/Road", typeof(Material));
        }
        else
        {
            roadMaterial = Graph.roadMaterial;
        }
        //Create Intersection Mesh
        CreateIntersection();
        return this;
    }

    //Initializer
    public CiDyNode (string newName,Vector3 newPosition, int nodeCount){
		name = newName;
		position = newPosition;
		nodeNumber = nodeCount;
		//Debug.Log (name + " 2 " + nodeNumber);
		//nodeNumber = nodeCount;
		//Clear List
		adjacentNodes = new List<CiDyNode>(0);
		connectedRoads = new List<CiDyRoad> (0);
	}
	//Initializer
	public CiDyNode Init(string newName,Vector3 newPosition, int nodeCount){
		name = newName;
		position = newPosition;
		nodeNumber = nodeCount;
		//Debug.Log (name + " 2 " + nodeNumber);
		//nodeNumber = nodeCount;
		//Clear List
		adjacentNodes = new List<CiDyNode>(0);
		connectedRoads = new List<CiDyRoad> (0);
		return this;
	}
    //Initializer
    public CiDyNode Init(string newName, Vector3 newPosition, int nodeCount, CiDyNode.Hierarchy newHiearchy)
    {
        name = newName;
        position = newPosition;
        nodeNumber = nodeCount;
        //Debug.Log (name + " 2 " + nodeNumber);
        //nodeNumber = nodeCount;
        //Clear List
        adjacentNodes = new List<CiDyNode>(0);
        connectedRoads = new List<CiDyRoad>(0);
        //Set Hiearchy
        hierarchy = newHiearchy;
        return this;
    }

    public CiDyNode Init(string newName,Vector3 newPosition, CiDyCell newCell, int nodeCount, GameObject position3D){
		Cell = newCell;
		name = newName;
		position = newPosition;
		nodeNumber = nodeCount;
		//Debug.Log (name + " 3 " + nodeNumber);
		//nodeNumber = nodeCount;
		//Clear List
		adjacentNodes = new List<CiDyNode>(0);
		connectedRoads = new List<CiDyRoad> (0);
		nodeObject = position3D;
		CreateIntersection();
		return this;
	}

	//Use this function so the proper moves are made
	public void MoveNode(Vector3 newPosition){
		if(nodeObject != null){
			nodeObject.transform.position = newPosition;
		}
		position = newPosition;
		//Debug.Log (name+" Changed Pos: "+position);
	}

	//This function will change the material
	public void ChangeMaterial(Material newMaterial){
		//Debug.Log ("Ran it "+newMaterial.name+" on "+name);
		//Change the GameObjects sphere material
		if(nodeObject!=null){
			nodeObject.transform.Find("Graphic").GetComponent<Renderer>().material = newMaterial;
			//curNode.transform.FindChild ("Sphere").GetComponent<Renderer>().material = activeMaterial;
		}
	}

    //This function is called when we want to change the Current Material.
    public void ReplaceMaterials() {
        roadMaterial = Graph.roadMaterial;
        intersectionMaterial = Graph.intersectionMaterial;
        //Check if its a Intersection/Culdesac or a Connector Piece.
        switch (type) {
            case IntersectionType.continuedSection:
                if (intersection != null)
                {
                    mRenderer.sharedMaterial = roadMaterial;
                }
                break;
            default:
                //Everything Else
                if (intersection != null) {
                    mRenderer.sharedMaterial = intersectionMaterial;
                }
                break;
        }
    }

	public Transform graphicTransform;
	//This will update the transform scale of node graphic.
	public void UpdateGraphicScale(float newScale){
		if(graphicTransform == null && nodeObject != null){
			graphicTransform = nodeObject.transform.Find("Graphic").transform;
		}
		if(graphicTransform == null){
			Debug.LogWarning("Update Graphic Scale Did Not Return a Sphere Transform from Node: "+name);
			return;
		}
		//If the code is here then we have met all requirments for the scale update. :)
		graphicTransform.localScale = new Vector3 (newScale, newScale, newScale);
	}

	public void EnableGraphic(){
		if(graphicTransform == null && nodeObject != null){
			graphicTransform = nodeObject.transform.Find("Graphic").transform;
		}
		if(graphicTransform == null){
			Debug.LogWarning("Enable Graphic Did Not Return a Sphere Transform from Node: "+name);
			return;
		}
		//If the code is here then we have met all requirments for the scale update. :)
		graphicTransform.gameObject.SetActive (true);
	}

	public void DisableGraphic(){
		if(graphicTransform == null && nodeObject != null){
			graphicTransform = nodeObject.transform.Find("Graphic").transform;
		}
		if(graphicTransform == null){
			Debug.LogWarning("Disable Graphic Did Not Return a Sphere Transform from Node: "+name);
			return;
		}
		//If the code is here then we have met all requirments for the scale update. :)
		graphicTransform.gameObject.SetActive (false);
	}

	void UpdateScale(){
		intersection.transform.localScale = new Vector3((intersection.transform.localScale.x/intersection.transform.lossyScale.x),(intersection.transform.localScale.y/intersection.transform.lossyScale.y),(intersection.transform.localScale.z/intersection.transform.lossyScale.z));
	}

	//We have a new Node Connection
	public void AddNode(CiDyNode newNode){
		//Debug.Log ("AddNode " + newNode.name);
		if(newNode == null){
			Debug.LogError (name+" Trying to Add(null)");
			return;
		}
		//Debug.Log (name+" Trying to Add("+newNode.name+")");
		//CiDyNode testNode = adjacentNodes.Find(x=> x.name == newNode.name);
		//Do not add a node that we already have.
		if(!Duplicate(newNode.name, adjacentNodes)){
			//Debug.Log("Not duplicate");
			//Debug.Log(name+" Is Adding New Adjacent Node "+newNode.name+" AdjacentNode: "+adjacentNodes.Count);
			adjacentNodes.Add(newNode);
			//New Node added clockwise sort nodes.
			if(adjacentNodes.Count > 1){
				List<CiDyNode> clonedNodes = new List<CiDyNode>(adjacentNodes);
				adjacentNodes = ClockwiseSort(clonedNodes, position);
			}
			//Debug.Log("Sorted Nodes");
		} else {
			//Debug.Log("Duplicate Node "+newNode.name+" Trying to add To: "+name);
		}
	}

	//This will remove a node from adjacentList
	public void RemoveNode(CiDyNode oldNode){
		//Debug.Log ("Remove Node " + oldNode.name + " Called On "+name);
		for(int i = 0;i<adjacentNodes.Count;i++){
			CiDyNode newNode = adjacentNodes[i];
			if(newNode.name == oldNode.name){
				//We found the node remove it.
				adjacentNodes.RemoveAt(i);
				//End For Loop there is only one. :)
				//Debug.Log ("Removed Node " + oldNode.name);
				break;
			}
		}
		//Debug.Log(adjacentNodes.Count);
		UpdateRoadList ();
	}

	//This will sort the Adjaceny Nodes based on there X,Z Values.
	void SortNodes(){
		//Sort Adjaceny Nodes by there X,Z Values.
		adjacentNodes = adjacentNodes.OrderBy(x => x.position.x).ThenBy(x => x.position.z).ToList();
	}

	//This function will return a clockwise ordered list of the Nodes
	List<CiDyNode> ClockwiseSort(List<CiDyNode> newNodes, Vector3 pos){
		//Debug.Log ("Sort Nodes "+newNodes.Count+" Adj: "+adjacentNodes.Count);
		//To perform the sort we will run a while loop.
		List<CiDyNode> untested = new List<CiDyNode>(newNodes);
		//Debug.Log ("Untested "+untested.Count+" Adj: "+adjacentNodes.Count);
		//Clear new roads after its cloned
		newNodes = new List<CiDyNode>(0);
		//Debug.Log ("Untested "+untested.Count+" Adj: "+adjacentNodes.Count);
		//Start direction and start while loop
		CiDyNode curNode = untested[0];
		untested.RemoveAt(0);
		newNodes.Add (curNode);
		//Normalized direction vector to start the clockwise testing with.
		Vector3 startDir = (curNode.position-pos).normalized;
		
		while(untested.Count > 0){
			if(curNode != null){
				//This Node is being tested. Add it too the ordered list run clockwise test on all remaining roads in the Untested list.
				//Find next clockwise node using startDir.
				CiDyNode nxtNode = GetClockWiseNode(startDir, untested);
				
				if(nxtNode == null){
					Debug.Log("Nothing Found");
					return newNodes;
				} else {
					curNode = nxtNode;
					startDir = (curNode.position-pos).normalized;
					newNodes.Add(curNode);
				}
			} else {
				Debug.LogError("Cannot run clockwise sort no transform selected");
			}
		}
		//Somthing went wrong.
		return newNodes;
	}
	
	//This function returns the next clockwise road from referenced directional plane
	CiDyNode GetClockWiseNode(Vector3 dir, List<CiDyNode> untested){
		float currentDirection = Mathf.Infinity;
		int bestNode = -1;
		
		// the vector that we want to measure an angle from
		Vector3 referenceForward = dir;// some vector that is not Vector3.up
		
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, referenceForward);
		
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<untested.Count;i++){
			CiDyNode tmpNode = untested[i];
			//Grab new Direction
			Vector3 newDirection = (tmpNode.position - position).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, referenceForward);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			//Determine if we need to subtract 360.
			if(sign < 0){
				//NEgative
				angle-=360;
				angle *=-1;
			}
			//print (angle);
			if(angle < currentDirection){
				bestNode = i;
				currentDirection = angle;
			}
		}
		//Did we find a new node?
		if(bestNode != -1){
			CiDyNode finalNode = untested[bestNode];
			untested.RemoveAt(bestNode);
			//We have selected a Node
			return finalNode;
		}
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}

	bool Duplicate(string nodeName, List<CiDyNode> Nodes){
		//Debug.Log (nodeName+" "+Nodes.Count);
		for(int i = 0;i<Nodes.Count;i++){
			string origName = Nodes[i].name;
			if(nodeName == origName){
				//This is a duplicate
				return true;
			}
		}
		//No duplicate found
		return false;
	}

	//This function will clear gameObject Data from the node so when its removedFromMemory it will be clean.
	public void DestroyNode(){
		//Debug.Log ("DestoryNode: " + name);
		if(nodeObject){
			GameObject.DestroyImmediate (nodeObject);
		}
	}

	////////////////////////////////////////////////////////////////////////Road Mesh INTERSECTIONS///////////////////////////////////
	//public List<Vector3> connectedRoads = new List<Vector3> ();//Connected Roads and connectedWidths
	//List<MeshFilter> connectedFilters = new List<MeshFilter>();
	[SerializeField]
	public List<CiDyRoad> connectedRoads;//Connected Roads and connectedWidths
	[SerializeField]
	float[] connectedRadius = new float[0];//Connected Roads Radius
	[SerializeField]
	List<Vector3> corners = new List<Vector3>(0);
	List<Vector3> untested = new List<Vector3>(0);
	List<GameObject> untested2 = new List<GameObject>(0);
	//What part of the system is the user currently able to manipulate?
	public enum IntersectionType {
		blank,
		culDeSac,
		continuedSection,
		tConnect,
	}
	
	public IntersectionType type = IntersectionType.culDeSac;
	//Mesh Variables
	public Vector2[] newUV;
	public int[] newTriangles;
	public MeshFilter mFilter;
	public MeshRenderer mRenderer;
	public MeshCollider mCollider;
	public bool processing = false;

	public GameObject intersection;//This is the holder for the intersection mesh data.

	RaycastHit hit;
	//Creates an Intersection Mesh for this node.
	void CreateIntersection(){
		//Create Intersection Object and Place as child of Node.(If we haven't already)
		if(!intersection){
			GameObject newInterMesh = new GameObject(name+" Intersection");
			//Move to us and lock in as child of node.
			newInterMesh.transform.position = nodeObject.transform.position;
			//Make child of node.
			newInterMesh.transform.parent = nodeObject.transform;
			//Set intersection gameobject
			intersection = newInterMesh;
			//Setup Mesh Rendering for Intersection
			mFilter = (MeshFilter)intersection.AddComponent<MeshFilter>();
			//Add Mesh Renderer
			mRenderer = (MeshRenderer)intersection.AddComponent<MeshRenderer>();
			//Turn OFF cast Shadows
			mRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			//Add collider for Mesh Collision Changes
			mCollider = (MeshCollider)intersection.AddComponent<MeshCollider>();
            //Make Object Static
            newInterMesh.isStatic = true;
		} else {
			Debug.LogError("This should only be called when this node was initilized");
		}
	}
	
	public void UpdateRoadList(){
		//Debug.Log ("Checking for Nulls "+name+" in RoadList "+connectedRoads.Count);
		//Our connected Roads Gameobject list has nulls in it.
		for(int i = 0;i<connectedRoads.Count;i++){
			if(connectedRoads[i] == null){
				connectedRoads.RemoveAt(i);
				i--;
			}
		}
		if(!processing){
			ProcessIntersection();
		}
	}

	//One Of our Roads has been Modified
	public void UpdatedRoad(){
		//Debug.Log ("Updated Road "+name);
		if(!processing){
			ProcessIntersection();
		}
	}

	//This function will Add Roads to the Connected list and update the Intersection as needed. 
	public void AddRoad(CiDyRoad newRoad){
		//Debug.Log ("Adding CiDyRoad: "+newRoad.name+" to Node: "+name);
		//Take the GameObject Road.
		connectedRoads.Add(newRoad);
		//Debug.Log ("connected Roads: " + connectedRoads.Count);
		if(!processing){
			ProcessIntersection();
		}
	}

	//string[] stringSeparators = new string[] {"V"};
	//string[] result;

	public List<CiDyRdLine> rdLines = new List<CiDyRdLine>(0);
	//if(test%2==0) // Is even
	//if(test%2==1) // Is odd
	public List<Vector3> circlePath = new List<Vector3> (0);
	public int circleSegments = 360;

	void SortConnectedRoads(){
		//connectedFilters.Clear ();
		//Take the connected Roads incoming direction to intersection Center and use this as its line to create its edge lines.(Left/Right)
		//Create left line and Right line by simply creating left and right points from intersection and connect point based on its direction to cen.
		//iterate through the Adj Nodes we have. they are clockwise sorted :)
		List<CiDyRoad> sortedRoads = new List<CiDyRoad> ();
        //Debug.Log ("Cloned Connected Roads: "+storedRoads.Count);
        for (int i = 0;i<adjacentNodes.Count;i++){
			//Get the Name of this node.
			string nodeName = adjacentNodes[i].name;
            
			//Iterate through the Connected CiDyRoad Components.
			for(int n = 0;n<connectedRoads.Count;n++){
				//Now we know if this road has the desired nodeName
				if(connectedRoads[n].nodeA.name == nodeName || connectedRoads[n].nodeB.name == nodeName){
					//This is the Next Road. :)
					sortedRoads.Add(connectedRoads[n]);
				}
			}
		}
		//Update nodes List to Sorted :)
		connectedRoads = sortedRoads;
		//Now Setup Radius List.
		connectedRadius = new float[connectedRoads.Count];
		rdLines = new List<CiDyRdLine>();

		//Iterate through all connected Roads.
		for(int i = 0;i<connectedRoads.Count;i++){
			List<Vector3> leftSide = new List<Vector3>();
			List<Vector3> rightSide = new List<Vector3>();
			//We need to go through the Roads and orient there side vectors as left line and right line based on intersection as starting point.
			//Grab first roads Road Data
			CiDyRoad tmpRoad = connectedRoads[i];
			List<Vector3> centerPath = new List<Vector3>(tmpRoad.origPoints);
			//Find out which side is closest/starting side.
			Vector3 end1 = centerPath[0];
			Vector3 end2 = centerPath.Last();
			float dist1 = Vector3.Distance(position,end1);
			float dist2 = Vector3.Distance(position,end2);
			float curWidth = tmpRoad.width;

			//Calculate the Left and Right Vertices and put in expected lists.
			for(int j = 0;j<centerPath.Count;j++){
				Vector3 center = centerPath[j];
				Vector3 nxt;
				Vector3 dir;
				if(j!= centerPath.Count-1){
					nxt = centerPath[j+1];
					dir = (nxt-center).normalized;
				} else {
					nxt = centerPath[j-1];
					dir = (center-nxt).normalized;
				}
				Vector3 cross = Vector3.Cross(dir,Vector3.up);
				//now determine left and right using cross
				Vector3 leftVector = center+(cross*(curWidth/2));
				Vector3 rightVector = center+(-cross*(curWidth/2));
				//Add lefts to left and right to right.
				leftSide.Add(leftVector);
				//leftSide.Add(leftVector2);
				rightSide.Add(rightVector);
				//rightSide.Add(rightVector2);
			}
			//Flip if needed.
			if(dist2<dist1){
				List<Vector3> tmpList = leftSide;
				leftSide = rightSide;
				rightSide = tmpList;
				leftSide.Reverse();
				rightSide.Reverse();
			}

			//Default set the ConnectedRoads Radius
			connectedRadius[i] = tmpRoad.width;//Dynamically Updated when making intersectionMeshs.
            //Create Rdlines using the New List Left then Right.
            bool isMajor = true;
            if (connectedRoads[i].nodeA.hierarchy != CiDyNode.Hierarchy.Major || connectedRoads[i].nodeB.hierarchy != CiDyNode.Hierarchy.Major) {
                isMajor = false;
            }
			rdLines.Add(new CiDyRdLine(leftSide,rightSide, tmpRoad.width, connectedRoads[i].name, isMajor));
			//rdLines.Add(new CiDyRdLine(rightEndPoint,rightCenPoint, leftEndPoint, leftCenPoint, roadWidth));
		}
        //Sort Biggest
        int large = 0;
        for (int i = 0; i < connectedRadius.Length; i++) {
            if (connectedRadius[i] > large) {
                large = (int)connectedRadius[i];
            }
        }
        //Store Max Radius for Blending/Removing Terrain Details
        maxRadius = large;
	}

    List<GameObject> visualNodes = new List<GameObject>(0);

	//This function will use line intersection testing to create the needed polygon for the intersection Mesh.
	void CreateIntersectionMesh(){
        if (visualNodes != null && visualNodes.Count > 0) {
            for (int i = 0; i < visualNodes.Count; i++) {
                DestroyImmediate(visualNodes[i]);
            }
        }
        visualNodes = new List<GameObject>(0);
		//Debug.Log("Intersection Processing Section "+name);
		processing = true;
		List<Vector3> pointList = new List<Vector3> (0);
		//Stored CornerPoints for Cell Interiors// Exposed for Later Use
		cornerPoints = new List<CiDyListWrapper>(0);

        /*bool skipNonMajorRd = false;
        if (hierarchy == CiDyNode.Hierarchy.Major) {
            skipNonMajorRd = true;
        }*/
		for(int i = 0;i<rdLines.Count;i++){
            /*if (skipNonMajorRd && !rdLines[i].isMajor) {
                continue;
            }*/
			float farthestPoint = 0;//Update as needed.
			//Debug.Log("Testing Road "+connectedRoads[i].name);
			Vector3 newIntersection = new Vector3(0,position.y,0);
			//Grab test Line to Compare
			CiDyRdLine testLine = rdLines[i];
			CiDyRdLine adjLine;//Line to our right
			CiDyRdLine adjLine2;//Line to our Left
			if(i==0){
				//At Beginning
				adjLine = rdLines[i+1];
				adjLine2 = rdLines[rdLines.Count-1];
			} else if(i<rdLines.Count-1){
				//In the Middle
				//Grab nxt In line
				adjLine = rdLines[i+1];
				adjLine2 = rdLines[i-1];
			} else {
				//At End
				//Grab first
				adjLine = rdLines[0];
				adjLine2 = rdLines[i-1];
			}
			//Debug.Log("Testing "+testLine.name+" VS "+adjLine.name+" adjLine2 "+adjLine2.name); 
			//We need to test the Line arrays of testline and Adjline against eachother.
			bool foundIntersection = false;
			for(int j = 0;j<testLine.rightLine.Length-1;j++){
                //We need to test the lines against adjLines rightLines.
                Vector3 tLineA = testLine.rightLine[j];
				Vector3 tLineB = testLine.rightLine[j+1];
				for(int h = 0;h<adjLine.leftLine.Length - 1;h++){
                    Vector3 aLineA = adjLine.leftLine[h];
					Vector3 aLineB = adjLine.leftLine[h+1];
                    //Is there an intersection between these edge lines?
                    if (CiDyUtils.LineIntersection(tLineA,tLineB,aLineA,aLineB, ref newIntersection)){
                        //CiDyUtils.MarkPoint(newIntersection, i);
                        float dist = Vector3.Distance(newIntersection,position);
						//Debug.Log(dist);
						if(dist>farthestPoint){
							farthestPoint = dist;
						}
						testLine.UpdateRightIntersection(newIntersection);
						adjLine.UpdateLeftIntersection(newIntersection);
						
						pointList.Add(newIntersection);
						//Debug.Log(newIntersection+" RightLine-LeftLine intersection Added ");
						foundIntersection = true;
						break;
					}
				}
				if(foundIntersection){
					//Move on to the next testLine
					break;
				}
			}
			//Special Infinite Line Test if no answer was found before.
			if(!foundIntersection){
				//Debug.Log("Performing Special Test for RightLine to Adj Left Line");
				//Test The Next RdLine in the list and this time use infinite lines from the [0] dir[0]->[1]Infinity
				//Determine Direction for Left line
				Vector3 infinDir = (adjLine.leftLine[adjLine.leftLine.Length - 1] - adjLine.leftLine[0]).normalized;
                //Create Points for testing
                Vector3 leftLineStr = adjLine.leftLine[0] + (-infinDir*100);
                Vector3 leftLineEnd = adjLine.leftLine[0] + (infinDir*100);
				//Extend Right Line
				infinDir = (testLine.rightLine[testLine.rightLine.Length - 1] - testLine.rightLine[0]).normalized;
				//Create points for TestingLine
				Vector3 rightLineStr = testLine.rightLine[0] + (-infinDir*100);
                Vector3 rightLineEnd = testLine.rightLine[0] + (infinDir * 100);
                /*CiDyUtils.MarkPoint(leftLineStr, i);
                CiDyUtils.MarkPoint(leftLineEnd, i);
                CiDyUtils.MarkPoint(rightLineStr, i+1);
                CiDyUtils.MarkPoint(rightLineEnd, i+1);*/
                //Test Lines Now.
                if (CiDyUtils.LineIntersection(rightLineStr,rightLineEnd,leftLineStr,leftLineEnd, ref newIntersection)){
					float dist = Vector3.Distance(newIntersection,position);
					//Debug.Log(dist);
					if(dist>farthestPoint){
						farthestPoint = dist;
					}
					//CiDyUtils.MarkPoint(newIntersection,i);
					//Store Point
					testLine.UpdateRightIntersection(newIntersection);
					//Debug.Log(newIntersection+" Right Line infinite to leftLine infinite ");
					pointList.Add(newIntersection);
				} else {
					pointList.Add(testLine.rightLine[0]);
					testLine.UpdateRightIntersection(testLine.rightLine[0]);
                    //CiDyUtils.MarkPoint(testLine.rightLine[0], i);
                    //Debug.Log(testLine.rightLine[0]+" !testLine.rightIntersect Added rightLine[0] ");
                }
            }
			foundIntersection = false;
			//Now Test LeftLine to previous Right Line
			for(int j = 0;j<testLine.leftLine.Length - 1;j++){
                //We need to test the lines against adjLines rightLines.
                Vector3 tLineA = testLine.leftLine[j];
				Vector3 tLineB = testLine.leftLine[j+1];
				for(int h = 0;h<adjLine2.rightLine.Length - 1;h++){
					Vector3 aLineA = adjLine2.rightLine[h];
					Vector3 aLineB = adjLine2.rightLine[h+1];
					//Is there an intersection between these edge lines?
					if(CiDyUtils.LineIntersection(tLineA,tLineB,aLineA,aLineB, ref newIntersection)){
						float dist = Vector3.Distance(newIntersection,position);
						//Debug.Log(dist);
						if(dist>farthestPoint){
							farthestPoint = dist;
						}
						//CiDyUtils.MarkPoint(newIntersection,1);
						//Debug.Log(newIntersection+" LeftLine-RightLine intersection Added ");
						foundIntersection = true;
						break;
					}
				}
				if(foundIntersection){
					//Move on to the next testLine
					break;
				}
			}
			//Special Infinite Line Test if no answer was found before.
			if(!foundIntersection){
				//Debug.Log("Performing Special Test for Left Line to previous Right Line");
				//Test The Next RdLine in the list and this time use infinite lines from the [0] dir[0]->[1]Infinity
				//Determine Direction for Left line
				Vector3 infinDir = (adjLine2.rightLine[0]-adjLine2.rightLine[1]).normalized;
				//Create Points for testing
				Vector3 leftLineStr = adjLine2.rightLine[0]+(-infinDir*100);
				Vector3 leftLineEnd = adjLine2.rightLine[0]+(infinDir*100);
				//Extend Right Line
				infinDir = (testLine.leftLine[0]-testLine.leftLine[1]).normalized;
				//Create points for TestingLine
				Vector3 rightLineStr = testLine.leftLine[0]+(-infinDir*100);
				Vector3 rightLineEnd = testLine.leftLine[0]+(infinDir*100);
				//Test Lines Now.
				if(CiDyUtils.LineIntersection(rightLineStr,rightLineEnd,leftLineStr,leftLineEnd, ref newIntersection)){
					float dist = Vector3.Distance(newIntersection,position);
					//Debug.Log(dist);
					if(dist>farthestPoint){
						farthestPoint = dist;
					}
					//Debug.Log(newIntersection+"Infinite Line Intersection Found");
					//CiDyUtils.MarkPoint(newIntersection,1);
				}
			}
			//Debug.Log("FarthestPoint: "+farthestPoint);
			if(farthestPoint > connectedRoads[i].width){
				//Debug.Log("Farthest Point Farther Than ConnectedRoads[i].width: "+farthestPoint+" ConnectRoads[i].width: "+connectedRoads[i].width);
                //connectedRadius[i]
				float newRadius = farthestPoint+1.1f;
                connectedRadius[i] = newRadius;
				//Debug.Log("ConnectedRadius: "+connectedRadius[i]);
			}

			//Circle Test Left Line
			for(int j = 0;j<testLine.leftLine.Length - 1;j++){
				//Find Lines 
				Vector3 p0 = testLine.leftLine[j];
				Vector3 p1;
				if(j==testLine.leftLine.Length - 1){
					p1 = testLine.leftLine[0];
				} else {
					p1 = testLine.leftLine[j+1];
				}
				//Now test this line to circle for Intersection
				if(CiDyUtils.CircleIntersectsLine(position,connectedRadius[i],360,p0,p1, ref newIntersection)){
					//CiDyUtils.MarkPoint(newIntersection,2);
					//Debug.Log(newIntersection+"Added for Left Line-Circle Test Line ");
					corners.Add(newIntersection);
					break;
				}
			}

			//Now Circle Test Right Line
			for(int j = 0;j<testLine.rightLine.Length - 1;j++){
				//Find Lines 
				Vector3 p0 = testLine.rightLine[j];
				Vector3 p1;
				if(j==testLine.rightLine.Length - 1){
					p1 = testLine.rightLine[0];
				} else {
					p1 = testLine.rightLine[j+1];
				}

				//Now test this line to circle for Intersection
				if(CiDyUtils.CircleIntersectsLine(position,connectedRadius[i],360,p0,p1, ref newIntersection)){
					//Debug.Log("Added for Right Line-Circle Test Line "+newIntersection);
					//CiDyUtils.MarkPoint(newIntersection,3);
					corners.Add(newIntersection);
					break;
				}
			}
			//Add line/Line point
			corners.Add(pointList.Last());
		}

		//Create curves
		List<Vector3> curvedPoints = new List<Vector3> (0);
		List<Vector3> finalPoints = new List<Vector3> (0);
		List<Vector3> tmpCurve = new List<Vector3> (0);

		for(int i = 1;i<corners.Count;i++){
			curvedPoints.Add(corners[i]);
			if(i==corners.Count-1){
				curvedPoints.Add(corners[0]);
			}
			if(curvedPoints.Count == 3){
				int curveDetail = Mathf.RoundToInt(Vector3.Distance(curvedPoints[0],curvedPoints[2]));
				if(curveDetail > 1){
					tmpCurve = CiDyUtils.CreateBezier(curvedPoints,1);
				} else {
					tmpCurve = new List<Vector3>(curvedPoints);
				}
				for(int j = 0;j<tmpCurve.Count;j++){
					finalPoints.Add(tmpCurve[j]);
				}
				
				//Add to Corners List
				//cornerPoints.Add(new List<Vector3>(curvedPoints));
				CiDyListWrapper newWrapper = new CiDyListWrapper(tmpCurve);
				cornerPoints.Add(newWrapper);
				//Now clear curvedList
				curvedPoints.Clear();
			}
		}
        corners = finalPoints;
		Vector2[] vertices2D = new Vector2[corners.Count];
		newUV = new Vector2[corners.Count];
		for(int i =0 ;i<vertices2D.Length;i++){
			vertices2D[i] = new Vector2(corners[i].x,corners[i].z);
			//newUV[i] = new Vector2(vertices2D[i].x*0.1f, vertices2D[i].y*0.1f);
		}
		// Use the triangulator to get indices for creating triangles
		Triangulator tr = new Triangulator(vertices2D);
		int[] indices = tr.Triangulate();
		// Create the Vector3 vertices
		Vector3[] vertices = new Vector3[vertices2D.Length];
		for (int i=0; i<vertices2D.Length; i++) {
			vertices[i] = new Vector3(vertices2D[i].x,position.y,vertices2D[i].y);
			Vector3 relativePos = (vertices[i]-intersection.transform.position);
			newUV[i] = new Vector2(relativePos.x*0.1f,relativePos.z*0.1f);
		}
		//Orient Points to our current place in World Space.
		for(int i = 0;i<vertices.Length;i++){
			vertices[i] = (vertices[i]-position);
		}

		Mesh mesh = new Mesh();
		mFilter.sharedMesh = mesh;
		mesh.vertices = vertices;
		mesh.uv = newUV;
		mesh.triangles = indices;
        //Apply Material
        mRenderer.sharedMaterial = intersectionMaterial;
        //Calculate Normals
        mesh.RecalculateNormals();
		mesh.RecalculateBounds ();
		//Update collider
		mCollider.sharedMesh = mesh;
		//Calculate Tangents for bumped shaders
		//TangentSolver (mesh);
		processing = false;
		//Debug.Log("Intersection Processed"+nodeObject.name+" CornerPoints: "+cornerPoints.Count);
	}

    void CreateConnector() {
        //Debug.Log("Creating Connector Road Piece " + name);
        if (visualNodes != null && visualNodes.Count > 0)
        {
            for (int i = 0; i < visualNodes.Count; i++)
            {
                DestroyImmediate(visualNodes[i]);
            }
        }
        visualNodes = new List<GameObject>(0);
        processing = true;
        List<Vector3> pointList = new List<Vector3>(0);
        cornerPoints = new List<CiDyListWrapper>(0);
        //Debug.Log("Connector Radius: " + connectedRadius[0]);
        //Project a Circle from Center point of Node by Largest Connected Road Radius and Grab the Intersection of the CenterLines to the Circle
        Vector3 newIntersection = new Vector3(0, position.y, 0);//Used for Line Testing
        List<Vector3> leftPoints = new List<Vector3>();
        List<Vector3> rightPoints = new List<Vector3>();

        bool skipNonMajor = false;
        if (hierarchy == CiDyNode.Hierarchy.Major) {
            skipNonMajor = true;
        }
        //Test Line Intersection of Circle Line to Center Line of Adj Rds
        for (int j = 0; j < rdLines.Count; j++) {
            if (skipNonMajor && !rdLines[j].isMajor) {
                continue;
            }
            //Debug.Log("Testing Road "+connectedRoads[j].name);
            //Grab test Line to Compare
            CiDyRdLine testLine = rdLines[j];
            for (int h = 0; h < testLine.leftLine.Length - 1; h++)
            {
                //Left line and Right Line
                Vector3 leftA = testLine.leftLine[h];
                Vector3 leftB = testLine.leftLine[h + 1];
                Vector3 rightA = testLine.rightLine[h];
                Vector3 rightB = testLine.rightLine[h + 1];

                if (CiDyUtils.CircleIntersectsLine(position, connectedRadius[0], 360, leftA, leftB, ref newIntersection))
                {
                    //Get Dist between the Two Left Points.
                    /*float totalDist = Vector3.Distance(leftA, leftB);
                    //Determine curDist from LeftA - intersection
                    float curDist = Vector3.Distance(leftA, newIntersection);
                    float t = curDist/totalDist;
                    //Liner Interpolate between LeftA and LeftB to find Y Value
                    Vector3 lerpPos = Vector3.Lerp(leftA, leftB, t);
                    newIntersection.y = lerpPos.y;*/
                    leftPoints.Add(newIntersection);
                }
                //Test Right Line
                if (CiDyUtils.CircleIntersectsLine(position, connectedRadius[0], 360, rightA, rightB, ref newIntersection))
                {
                    /*//Get Dist between the Two Right Points.
                    float totalDist = Vector3.Distance(rightA, rightB);
                    //Determine curDist from RightA - intersection
                    float curDist = Vector3.Distance(rightA, newIntersection);
                    float t = curDist / totalDist;
                    //Liner Interpolate between rightA and rightB to find Y Value
                    Vector3 lerpPos = Vector3.Lerp(rightA, rightB, t);
                    newIntersection.y = lerpPos.y;*/
                    rightPoints.Add(newIntersection);
                }
                if (rightPoints.Count >= 2 && leftPoints.Count >= 2)
                {
                    break;
                }
            }
        }

        //Show Left Points
        /*for (int i = 0; i < leftPoints.Count; i++) {
            GameObject sphere = CiDyUtils.MarkPoint(leftPoints[i], i);
            sphere.name = sphere.name + name;
        }
        //Show Right Points.
        for (int i = 0; i < rightPoints.Count; i++) {
            GameObject sphere = CiDyUtils.MarkPoint(rightPoints[i], i);
            sphere.name = sphere.name + name;
        }*/
        //Debug.Log("After Rd Line Test, RightPoints: "+rightPoints.Count+" Left Points: "+leftPoints.Count);
        //Now we have the Points. Lets Create a Road Spline from this Raw Data After Putting Middle Point in middle position of list.
        if (leftPoints.Count == 2 && rightPoints.Count == 2)
        {
            //Combine to find Middle Point
            Vector3 combined = (leftPoints[0] + rightPoints[0]) / 2;
            pointList.Add(combined);
            //Insert Middle Point.
            pointList.Insert(1, position);
            combined = (leftPoints[1] + rightPoints[1]) / 2;
            pointList.Add(combined);
            //Now that we have the Points. Create a Bezier Curve for them.
            pointList = CiDyUtils.CreateBezier(pointList, connectedRoads[0].segmentLength/2);
            //float totalDist = CiDyUtils.FindTotalDistOfPoints(pointList);
            //Graph.ContourPathToTerrain(ref pointList);
           /* float startY = pointList[0].y;
            float endY = pointList[pointList.Count - 1].y;
            //Blend The Points Averages along the Line.
            float curDist = 0;
            for (int i = 0; i < pointList.Count-1; i++) {
                float t = curDist / totalDist;
                pointList[i] = new Vector3(pointList[i].x, Mathf.Lerp(pointList[i].y, pointList[pointList.Count - 1].y, t), pointList[i].x);
                //Distance Between these two points.
                curDist = Vector3.Distance(pointList[i], pointList[i + 1]);
            }*/
            float width = connectedRoads[0].width;
            //Iterate throught Center Points and Project Left / Right Points of Road Mesh
            //int totalPoints = (endPlace - startPlace)*2;
            //Corner Points
            List<Vector3> leftCorners = new List<Vector3>();
            List<Vector3> rightCorners = new List<Vector3>();
            List<Vector3> newVerts = new List<Vector3>();
            //Debug.Log ("StartPlace: "+startPlace + " EndPlace: " + endPlace);
            //float totalDist = 0;
            for (int i = 0; i < pointList.Count; i++)
            {
                //Test Left of Line.//Determine Pos
                Vector3 vector = pointList[i] - position;
                Vector3 vector2;
                //Dir based on next in line.
                Vector3 vectorDir;
                if (i != pointList.Count - 1)
                {
                    vector2 = pointList[i + 1] - position;
                    vectorDir = (vector2 - vector);
                }
                else
                {
                    vector2 = pointList[i - 1] - position;
                    vectorDir = (vector - vector2);
                }
                //Calculate Cross Product and place points.
                Vector3 cross = Vector3.Cross(Vector3.up, vectorDir).normalized;
                //Calculate Four Points creating left Line and Right Line.
                Vector3 leftVector = vector + (-cross) * (width / 2);
                Vector3 rightVector = vector + cross * (width / 2);
                newVerts.Add(leftVector);
                newVerts.Add(rightVector);
                leftCorners.Add(leftVector + position);
                rightCorners.Add(rightVector + position);
            }
            //Left Verts = Right Saved Point/Right Vert = Left Saved Point
            newVerts[0] = rightPoints[0] - position;
            newVerts[1] = leftPoints[0] - position;
            //Same for End Verts
            newVerts[newVerts.Count - 1] = rightPoints[1] - position;
            newVerts[newVerts.Count - 2] = leftPoints[1] - position;
            //Changes Update End Points to Match Actual Intersection points of Meshes.
            leftCorners[0] = rightPoints[0];
            rightCorners[0] = leftPoints[0];
            leftCorners[leftCorners.Count-1] = leftPoints[leftPoints.Count - 1];
            rightCorners[rightCorners.Count-1] = rightPoints[rightPoints.Count - 1];
            cornerPoints.Add(new CiDyListWrapper(leftCorners));
            rightCorners.Reverse();
            cornerPoints.Add(new CiDyListWrapper(rightCorners));
            //Create Mesh Triangles/Uvs for new Verts
            //Set N
            List<int> tris = new List<int>(0);
            List<Vector2> uvs = new List<Vector2>(0);
            //Look at four points at a time
            for (int i = 0; i < newVerts.Count - 2; i += 2)
            {
                tris.Add(i);//0
                tris.Add(i + 2);//2
                tris.Add(i + 1);//1

                tris.Add(i + 1);//1
                tris.Add(i + 2);//2
                tris.Add(i + 3);//3
            }
            //Setup UVs
            float uvDist = 0;
            if (connectedRoads[0].uvsRoadSet)
            {
                //Special Case where there is only one Segment
                if (newVerts.Count == 4)
                {
                    //We are at the Beginning//Get Vertical Distance
                    Vector3 midPointA = (newVerts[0] + newVerts[1]) / 2;
                    Vector3 midPointB = (newVerts[2] + newVerts[3]) / 2;
                    //Get Vertical Distance
                    float zDist = Vector3.Distance(midPointA, midPointB) / 1;
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, zDist));
                    uvs.Add(new Vector2(1, zDist));
                }
                else if (newVerts.Count == 6)
                {
                    //We are at the Beginning//Get Vertical Distance
                    Vector3 midPointA = (newVerts[0] + newVerts[1]) / 2;
                    Vector3 midPointB = (newVerts[2] + newVerts[3]) / 2;
                    //Get Vertical Distance
                    float zDist = Vector3.Distance(midPointA, midPointB) / 1;
                    //Special Case of only Two Segments.
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(0, zDist));
                    uvs.Add(new Vector2(1, zDist));
                    uvs.Add(new Vector2(0, (zDist * 2)));
                    uvs.Add(new Vector2(1, (zDist * 2)));
                }
                else
                {
                    float zDist = 0;
                    //Set up UVs for Three Segments and Up.
                    for (int i = 0; i < newVerts.Count - 2; i += 2)
                    {
                        //Handle All Four Points of UV with mounting Values.
                        uvs.Add(new Vector2(0, uvDist));
                        uvs.Add(new Vector2(1, uvDist));
                        //We are at the Beginning//Get Vertical Distance
                        Vector2 midPointA = (new Vector2(newVerts[i].x, newVerts[i].z) + new Vector2(newVerts[i + 1].x, newVerts[i + 1].z)) / 2;
                        Vector2 midPointB = (new Vector2(newVerts[i + 2].x, newVerts[i + 2].z) + new Vector2(newVerts[i + 3].x, newVerts[i + 3].z)) / 2;
                        //Get Vertical Distance
                        zDist = (Vector2.Distance(midPointA, midPointB) / 60);
                        uvDist = uvDist + zDist;
                    }
                    //Add Last Two Points
                    uvs.Add(new Vector2(0, uvDist));
                    uvs.Add(new Vector2(1, uvDist));
                }
            }
            else
            {
                //Set Uvs based on X/Z Values
                for (int i = 0; i < newVerts.Count; i++)
                {
                    //CiDyUtils.MarkPoint(newVerts[i], i);
                    uvs.Add(new Vector2(newVerts[i].x, newVerts[i].z));
                }
            }
            //Set Triangles and 
            Mesh mesh = new Mesh
            {
                vertices = newVerts.ToArray(),
                triangles = tris.ToArray(),
                uv = uvs.ToArray()
            };
            Mesh roadMesh = mesh;
            roadMesh.RecalculateBounds();
            roadMesh.RecalculateNormals();
            //Set Mesh to filter
            mFilter.sharedMesh = roadMesh;
            //Update Collider Mesh value as well.
            mCollider = intersection.GetComponent<MeshCollider>();
            mCollider.sharedMesh.Clear();
            mCollider.sharedMesh = roadMesh;
            //Set Road Material
            mRenderer.sharedMaterial = roadMaterial;
        }
        //Debug.Log("Connector Mesh Before: "+name+" Count: "+cornerPoints.Count);
        //End Processing
        processing = false;
    }

    // Use this for initialization
    void ProcessIntersection(){
		//Debug.Log ("ProcessIntersection "+name+" Adj: "+adjacentNodes.Count);
		processing = true;
        int adjNodeCount = adjacentNodes.Count;
        /*bool onlyConnectToSameHiearchy = false;
        if (hierarchy == CiDyNode.Hierarchy.Major)
        {
            onlyConnectToSameHiearchy = true;
        }

        if (onlyConnectToSameHiearchy) {
            //Special Case for Any Minor Connected AdjNodes.
            //Only count the Major Adj Nodes.
            int trueCount = 0;
            for (int i = 0; i < adjacentNodes.Count; i++) {
                if (adjacentNodes[i].hierarchy == this.hierarchy) {
                    trueCount++;
                }
            }
            adjNodeCount = trueCount;
        }*/
		//Determine what type of intersection we will create based on the current Connected Roads.
		switch(adjNodeCount){
		case 0:
			//Debug.Log("No Intersection Mesh Needed There are no Connected Roads");
			type = IntersectionType.blank;
			break;
		case 1:
			type = IntersectionType.culDeSac;
			break;
		case 2:
			type = IntersectionType.continuedSection;
			break;
		default:
			type = IntersectionType.tConnect;
			break;
		}

		//Clear Mesh
		SortConnectedRoads ();
		corners = new List<Vector3>(0);
        //Do we need a Dynamic Intersection Mesh?
        switch (type) {
            case IntersectionType.blank:
                //Debug.Log("Blank");
                //We need to clear the interesection mesh. There is no intersection.
                mFilter.sharedMesh.Clear();
                break;
            case IntersectionType.culDeSac:
                //Debug.Log("CulDeSac");
                //We need a CulDeSac Piece
                CreateCulDeSac();
                break;
            case IntersectionType.continuedSection:
                //Debug.Log("Connector");
                //Just Create the Connecting Road Piece.
                //Degeneracy Check. If Road is less than 45 Degrees. It is a Stop point. 
                Vector3 lineA = (((rdLines[0].leftLine[1] + rdLines[0].rightLine[1]) / 2) - position);
                Vector3 lineB = (rdLines[1].leftLine[1] + rdLines[1].rightLine[1]) / 2 - position;
                float angle = Vector3.Angle(lineB, lineA);
                if (angle <= 89)
                {
                    //This has to be a Intersection. The Turn is too Sharp.
                    type = IntersectionType.tConnect;
                    CreateIntersectionMesh();
                } else {
                    //Until I determin the Proper Corners Method this process is in efficent
                    //CreateIntersectionMesh();
                    CreateConnector();
                }
                break;
            case IntersectionType.tConnect:
                //Debug.Log("Intersection");
                //This handles three or more roads.
                CreateIntersectionMesh();
                break;
        }
		//Debug.Log ("connectedRoads "+connectedRoads.Count);
		if(connectedRoads.Count > 0){
			//Debug.Log("Sending Radius Data");
			for(int i = 0;i<connectedRoads.Count;i++){
				//Debug.Log(i+" ConnectedRoads: "+connectedRoads[i].name+" Radius: "+connectedRadius[i]);
				connectedRoads[i].NodeDone(this,connectedRadius[i], true);
			}
		}
		//Debug.Log ("Finished Intersection");
		//Finish Process
		processing = false;
	}

	//Create A Cul De Sac
	void CreateCulDeSac(){
		//Debug.Log ("Create CulDeSac " + name+" CircleRadius: "+connectedRadius[0]);
		//Determine Local Fwrd Direction based on Projected Circle points.
		//Iterate through the pre Sorted RdLines and Test Left VS Circle && Right VS Circle.
		culDeSacPoints = new List<Vector3> (0);
		//Vector3 newIntersection = Vector3.zero;
		if(corners!=null){
			corners.Clear ();
		}
		CiDyRdLine ourRd = rdLines [0];
		//Create a Circle of Line Segment points :)
		List<Vector3> ourCircle = CiDyUtils.PlotCircle (position, connectedRadius[0], 90);
		//Now test Left-Right. :) any points AFTER left(first) are Going to be under the road. so Remove them. ;)//
		//Any points BEFORE Right are gone and this is the End of the Change to Circle Point List. :)
		Vector3 intersectPoint = Vector3.zero;//Default == null
		bool resetI = false;
		bool endTest = false;
		int strt = 0;
		int end = 0;
		//Iterate through Circle List.
		for(int i = 0;i<ourCircle.Count;i++){
			if(endTest){
				break;
			}
			Vector3 p0 = ourCircle[i];
			Vector3 p1;
			if(i==ourCircle.Count-1){
				p1 = ourCircle[0];
			} else {
				p1 = ourCircle[i+1];
			}
			//Find Left of RdLines intersection point into Circle List.
			//Now Iterate through and do Left
			if(!resetI){
				for(int j = 0;j<ourRd.leftLine.Length - 1;j++){
					//Find Lines 
					Vector3 p2 = ourRd.leftLine[j];
					Vector3 p3 = ourRd.leftLine[j+1];
					//Find intersection point between line and ourCircle.
					if(CiDyUtils.LineIntersection(p0,p1,p2,p3,ref intersectPoint)){
						//Any Point After p0 can be removed from the circleList.  :)
						corners.Add(intersectPoint);//point for Left. :) Used later for updated mesh point.
						//Which Point is closer to the intersection Point
						float dist = Vector3.Distance(intersectPoint,p0);
						float dist2 = Vector3.Distance(intersectPoint,p1);
						//Debug.Log("L : "+i+" dist: "+dist+" dist2: "+dist2);
						if(dist < dist2){
							//Start is i
							strt = i;
						} else {
							//Start is i+1/0
							if(i!=ourCircle.Count-1){
								strt = i+1;
							} else {
								strt = 0;
							}
						}
						Vector3 tmpPoint = intersectPoint;
						ourCircle[strt] = new Vector3(tmpPoint.x,intersection.transform.position.y,tmpPoint.z);
						resetI = true;//Change now and find right 
						i=-1;//Reset
						break;
					}
				}
			} else {
				//Find Right Intersection Point Now. :)
				for(int j = 0;j<ourRd.rightLine.Length - 1;j++){
					//Find Lines 
					Vector3 p2 = ourRd.rightLine[j];
					Vector3 p3 = ourRd.rightLine[j+1];
					//Find intersection point between line and ourCircle.
					if(CiDyUtils.LineIntersection(p0,p1,p2,p3,ref intersectPoint)){
						//Any Point BEFORE p0(P0 Has to Stay) can be removed from the circleList.  :)
						corners.Add(intersectPoint);
						//Which Point is closer to the intersection Point
						float dist = Vector3.Distance(intersectPoint,p0);
						float dist2 = Vector3.Distance(intersectPoint,p1);
						//Debug.Log("R : dist "+dist+" dist2: "+dist2);
						if(dist < dist2){
							//Start is i
							end = i;
						} else {
							//Start is i+1/0
							if(i!=ourCircle.Count-1){
								end = i+1;
							} else {
								end = 0;
							}
						}

						Vector3 tmpPoint = intersectPoint;
						ourCircle[end] = new Vector3(tmpPoint.x,intersection.transform.position.y,tmpPoint.z);
						endTest = true;
						break;
					}
				}
			}
		}
		//Debug.Log ("Strt: " + strt + " End: " + end);
		//Now we know what points in the circle List can be removed.
		//Update Closest circleList point to intersection point.(equalize positions)
		if(end < strt){
			//Debug.Log("Degeneracy End<strt");
			//Degeneracy line perfectly splits list circle[count]/circle[0].
			//Clear all points to end of list.
			int count = (ourCircle.Count-strt);
			//Debug.Log("Deg: End<Strt "+(count));
			if(strt == ourCircle.Count-1){
				//this is the end push to beginning
				strt = 0;
			} else {
				strt+=1;
			}
			ourCircle.RemoveRange(strt,count-1);
			//Now clear points from beginning to end if end is != [0]
			if(end != 0){
				ourCircle.RemoveRange(0,end);
			}
			for(int i = 0;i<ourCircle.Count;i++){
				culDeSacPoints.Add(ourCircle[i]);
			}
		} else {
			//Debug.Log("end > strt");
			//int count = (end-strt)-1;
			int count = (Mathf.Max(end,strt)-Mathf.Min(end,strt));
			//Debug.Log("end>Strt "+(count));
			//Remove area between start and end :)
			if(count != 0){
				count-=1;
			}
			ourCircle.RemoveRange(strt+1,count);
			for(int i = strt+1;i<ourCircle.Count;i++){
				culDeSacPoints.Add(ourCircle[i]);
			}
			for(int i = 0;i<strt+1;i++){
				culDeSacPoints.Add(ourCircle[i]);
			}
		}
		//CiDyUtils.MarkPoint (ourCircle [strt], 0);
		//CiDyUtils.MarkPoint (ourCircle [end], 1);
		//Debug.Log ("Made It");
		List<Vector2> uvs = new List<Vector2> ();
		//Find Centroid
		//Vector3 center = intersection.transform.position;
		/*//Find Middle and Add it.
		Vector3 middle = (corners[0]+corners[1])/2;
		Vector3 cenDir = (center - middle);
		Vector3 newPoint = (middle + cenDir * 0.25f);
		ourCircle.Add (middle);
		ourCircle.Add (newPoint);
		newPoint += cenDir * 0.25f;;
		ourCircle.Add (newPoint);*/
		//Add Center
		//ourCircle.Add(center);
		//Now propogate points from middle to center
		//Orient Points to our current place in World Space.
		for(int i = 0;i<ourCircle.Count;i++){
			ourCircle[i] = (ourCircle[i]-position);
		}
		//Now handle the Proper intersection of the two meshes using CSG.
		//////// Create the Mesh ///////////
		//Clear vertices
		Vector3[] newVertices = new Vector3[0];
		//Add verts to Builtin for Mesh.
		newVertices = ourCircle.ToArray();
		//Reference Center vert to minimize length checks
		//int cntTri = newVertices.Length-1;

		Vector2[] vertices2D = new Vector2[newVertices.Length];
		newUV = new Vector2[newVertices.Length];
		for(int i =0 ;i<vertices2D.Length;i++){
			vertices2D[i] = new Vector2(newVertices[i].x,newVertices[i].z);
			uvs.Add(new Vector2(vertices2D[i].x*0.1f, vertices2D[i].y*0.1f));
		}
		// Use the triangulator to get indices for creating triangles
		Triangulator tr = new Triangulator(vertices2D);
		int[] indices = tr.Triangulate();
		//Add triangles to Builtin for Mesh
		newTriangles = indices.ToArray ();
		//Add UVS to Builtin for mesh
		newUV = uvs.ToArray();
		Mesh mesh = new Mesh();
		mFilter.sharedMesh = mesh;
		mesh.vertices = newVertices;
		mesh.uv = newUV;
		mesh.triangles = newTriangles;
		//Calculate Normals
		mesh.RecalculateNormals();
		mesh.RecalculateBounds ();
		//Update collider
		mCollider.sharedMesh = mesh;
        //Calculate Tangents for bumped shaders
        //TangentSolver (mesh);
        //Set Material
        if (intersectionMaterial == null)
        {
            intersectionMaterial = (Material)Resources.Load("CiDyResources/Intersection");
        }
        mRenderer.sharedMaterial = intersectionMaterial;
    }	
	
	//This function will return a clockwise ordered list of the connected roads for this intersections creation.
	List<CiDyRoad> ClockwiseSort(List<CiDyRoad> newRoads, Vector3 pos){
		Debug.Log ("List<CiDyRoad> Clockwise Sort "+name);
		List<CiDyRoad> testingRoads = new List<CiDyRoad> (newRoads);
		//Find the
		//Clear new roads after its cloned
		newRoads = new List<CiDyRoad>(0);
		//Start direction and start while loop
		CiDyRoad curRoad = testingRoads[0];
		testingRoads.RemoveAt(0);
		newRoads.Add (curRoad);
		Debug.Log ("CURRENT ROAD "+curRoad.name);
		Vector3 dir;
		while(testingRoads.Count > 0){
			//Find the Point farthest
			float dist = Vector3.Distance(curRoad.nodeA.position,pos);
			float dist2 = Vector3.Distance (curRoad.nodeB.position, pos);
			Vector3 dirPos;
			if(dist<dist2){
				//EndA
				dirPos = curRoad.nodeA.position;
			} else {
				//EndB
				dirPos = curRoad.nodeB.position;
			}
			//Normalized direction vector to start the clockwise testing with.
			dir = (pos-dirPos).normalized;
			//This road is being tested. Add it too the ordered list run clockwise test on all remaining roads in the Untested list.
			//Find next clockwise node using startDir.
			CiDyRoad nxtRoad = GetClockWiseRoad(testingRoads,dir,pos);
			Debug.Log("Nxt Road: "+nxtRoad);
			curRoad = nxtRoad;
			testingRoads.RemoveAt(0);
			newRoads.Add(curRoad);
		}
		Debug.Log ("List<CidyRoad> Clockwise Sort "+newRoads.Count);
		//Somthing went wrong.
		return newRoads;
	}

	//This function will return a clockwise ordered list of the connected roads for this intersections creation.
	List<GameObject> ClockwiseSort(List<GameObject> newRoads, Vector3 pos){
		Debug.Log ("List<GameObject> Clockwise Sort "+name);
		//To perform the sort we will run a while loop.
		untested2 = new List<GameObject>(newRoads);
		//Clear new roads after its cloned
		newRoads = new List<GameObject>(0);
		//Start direction and start while loop
		GameObject curRoad = untested2[0];
		untested2.RemoveAt(0);
		newRoads.Add (curRoad);
		//Grab curRoads Road Data and find closest vs.
		CiDyRoad tmpRoad = curRoad.GetComponent (typeof(CiDyRoad)) as CiDyRoad;
		Vector3 end1 = tmpRoad.nodeA.position;
		end1 -= pos;
		Vector3 end2 = tmpRoad.nodeB.position;
		end2 -= pos;
		
		float dist1 = Vector3.Distance (pos, end1);
		float dist2 = Vector3.Distance (pos, end2);
		//Normalized direction vector to start the clockwise testing with.
		Vector3 startDir;// = (curRoad.transform.position-transform.position).normalized;
		if(dist1<dist2){
			//Normalized direction vector to start the clockwise testing with.
			//We need to find the inbetween vector of end1 and the next in line
			startDir = (end1-pos).normalized;
		} else {
			//Normalized direction vector to start the clockwise testing with.
			//We need to find the inbetween vector of end2 and the next in line
			startDir = (end2-pos).normalized;
		}
		
		while(untested2.Count > 0){
			if(curRoad != null){
				//This road is being tested. Add it too the ordered list run clockwise test on all remaining roads in the Untested list.
				//Find next clockwise node using startDir.
				GameObject nxtRoad = GetClockWiseRoad(startDir,0, pos);
				
				if(nxtRoad == null){
					Debug.Log("Nothing Found");
					return newRoads;
				} else {
					curRoad = nxtRoad;
					tmpRoad = curRoad.GetComponent (typeof(CiDyRoad)) as CiDyRoad;
					end1 = tmpRoad.nodeA.position;
					end2 = tmpRoad.nodeB.position;
					end1-=pos;
					end2-=pos;
					dist1 = Vector3.Distance (pos, end1);
					dist2 = Vector3.Distance (pos, end2);
					//Normalized direction vector to start the clockwise testing with.
					if(dist1<dist2){
						startDir = (end1-pos).normalized;
					} else {
						startDir = (end2-pos).normalized;
					}
					newRoads.Add(curRoad);
				}
			} else {
				Debug.LogError("Cannot run clockwise sort no transform selected");
			}
		}
		//Somthing went wrong.
		return newRoads;
	}

	//This function returns the next clockwise road from referenced directional plane
	Vector3 GetClockWiseRoad(Vector3 dir, Vector3 pos){
		float currentDirection = Mathf.Infinity;
		int bestRd = -1;
		
		// the vector that we want to measure an angle from is the Sent Dir vector
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, dir);
		
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<untested.Count;i++){
			Vector3 tmpRd = untested[i];//new Vector3(untested[i].x,transform.position.y,untested[i].z);
			//Grab new Direction
			Vector3 newDirection = (tmpRd-pos).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, dir);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			//Determine if we need to subtract 360.
			if(sign < 0){
				//NEgative
				angle-=360;
				angle *=-1;
			}
			if(angle < currentDirection){
				bestRd = i;
				currentDirection = angle;
			}
		}
		//Did we find a new node?
		if(bestRd != -1){
			Vector3 finalRoad = untested[bestRd];
			untested.RemoveAt(bestRd);
			//We have selected a Node
			return finalRoad;
		}
		//Didn't Find a new Node return null so we know this may be a filament.
		return new Vector3(0,0,0);
	}

	//This function returns the next clockwise road from referenced directional plane
	CiDyRoad GetClockWiseRoad(List<CiDyRoad> untested,Vector3 dir, Vector3 pos){
		float currentDirection = Mathf.Infinity;
		int bestRd = -1;
		
		// the vector that we want to measure an angle from is the Sent Dir vector
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, dir);
		
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<untested.Count;i++){
			//Find the Point farthes
			float dist = Vector3.Distance(untested[i].nodeA.position,pos);
			float dist2 = Vector3.Distance (untested[i].nodeB.position, pos);
			Vector3 dirPos;
			if(dist<dist2){
				//EndA
				dirPos = untested[i].nodeA.position;
			} else {
				//EndB
				dirPos = untested[i].nodeB.position;
			}
			//Grab new Direction
			Vector3 newDirection = (dirPos-pos).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, dir);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			//Determine if we need to subtract 360.
			if(sign < 0){
				//NEgative
				angle-=360;
				angle *=-1;
			}
			if(angle < currentDirection){
				bestRd = i;
				currentDirection = angle;
			}
		}
		//Did we find a new node?
		if(bestRd != -1){
			CiDyRoad finalRoad = untested[bestRd];
			//We have selected a Node
			return finalRoad;
		}
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}

	//This function returns the next clockwise road from referenced directional plane(null int is just so we can have an overload with the same paramater)
	GameObject GetClockWiseRoad(Vector3 dir, int nullInt, Vector3 pos){
		float currentDirection = Mathf.Infinity;
		int bestRd = -1;
		
		// the vector that we want to measure an angle from is the Sent Dir vector
		// the vector perpendicular to referenceForward (90 degrees clockwise)
		// (used to determine if angle is positive or negative)
		Vector3 referenceRight = Vector3.Cross(Vector3.up, dir);
		
		// the vector of interest
		//Itearate through adjacent Nodes
		for(int i = 0;i<untested2.Count;i++){
			CiDyRoad tmpRoad = untested2[i].GetComponent (typeof(CiDyRoad)) as CiDyRoad;
			Vector3 end1 = tmpRoad.nodeA.position;
			Vector3 end2 = tmpRoad.nodeB.position;
			end1-=pos;
			end2-=pos;
			float dist1 = Vector3.Distance (pos, end1);
			float dist2 = Vector3.Distance (pos, end2);
			Vector3 tmpRd;
			if(dist1<dist2){
				tmpRd = end1;//new Vector3(end1.x,transform.position.y,end1.z);
			} else {
				tmpRd = end2;//new Vector3(end2.x,transform.position.y,end2.z);
			}
			//Grab new Direction
			Vector3 newDirection = (tmpRd-pos).normalized;// some vector that we're interested in 
			// Get the angle in degrees between 0 and 180
			float angle = Vector3.Angle(newDirection, dir);
			// Determine if the degree value should be negative.  Here, a positive value
			// from the dot product means that our vector is on the right of the reference vector   
			// whereas a negative value means we're on the left.
			float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));
			//Determine if we need to subtract 360.
			if(sign < 0){
				//NEgative
				angle-=360;
				angle *=-1;
			}
			if(angle < currentDirection){
				bestRd = i;
				currentDirection = angle;
			}
		}
		//Did we find a new node?
		if(bestRd != -1){
			GameObject finalRoad = untested2[bestRd];
			untested2.RemoveAt(bestRd);
			//We have selected a Node
			return finalRoad;
		}
		//Didn't Find a new Node return null so we know this may be a filament.
		return null;
	}
	
	//Compares if the vectors are the same or so close they might as well be.
	bool SameVector3s(Vector3 v1, Vector3 v2){
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
	
	//Returns List<Vector3> from Segments from ordered List of Vector3s using linear Interpolation
	List<Vector3> FindP(List<Vector3> points, float t){
		//Copy Points
		List<Vector3> secPoints = new List<Vector3>(points);
		//Clear for new Points
		points = new List<Vector3>();
		//Iterate through clone array of old tmp control points.
		for(int i=0;i<secPoints.Count-1;i++){
			//Add new Points to List.
			Vector3 p = (1-t)*secPoints[i]+t*secPoints[i+1];
			points.Add(p);
		}
		return points;
	}
	
	//Special Function that will remove lines based on Name
	void RemoveLine(CiDyRdLine newLine){
		for(int i = 0;i<rdLines.Count;i++){
			CiDyRdLine testLine = rdLines[i];
			if(newLine.name == testLine.name){
				rdLines.RemoveAt(i);
				break;
			}
		}
	}
}