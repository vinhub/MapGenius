using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CiDyRoad : MonoBehaviour {

    //Public varaibles for Editor Use etc.
    public Material roadMaterial;//The Material we want to be applied to this road when its created or updated.
    public bool uvsRoadSet = true;
	public bool oneWay = false;//Always true if LaneType is OneLane. If Lane Type is anything else it may be twoWay or OneWay based on Boolean.
	public enum LaneType {
		OneLane,
		TwoLane,
		FourLane,
		SixLane
	}
	public LaneType laneType = LaneType.TwoLane;
	//Nodes that this road Connects.
	public CiDyNode nodeA;
	public CiDyNode nodeB;
	float nodeARadius;
	float nodeBRadius;
	//Road
	public float width = 0.0f;
	public Mesh roadMesh;
	public MeshFilter mFilter;
	[SerializeField]
	MeshRenderer mRender;
	[SerializeField]
	MeshCollider mCollider;
    //Guard Rail Materials
    public float leftRailOffset = 0f;
    public float rightRailOffset = 0f;
    public Material railMat;
    public Material postMat;
    public Transform leftRail;
    public Transform rightRail;
	// public to help debug in editor
	public int n = 0;
    [HideInInspector]
    public Vector3[] cpPoints = new Vector3[0];
	public Vector3[] origPoints = new Vector3[0];
	public List<Vector3> vs = new List<Vector3>(0);
	//private List<Vector2> uvs = new List<Vector2>(0);
	//private Vector3 endA = new Vector3(0,0,0);
	//private Vector3 endB = new Vector3(0,0,0);
	public int updateCall = 0;//Reset after equals 2;
	//Secondary Roads
	public Vector3 growthPoint;
	public Vector3 growthDir;
	public bool selected = false;//Weather this road is being modified in real-time or not. :)
	public int segmentLength = 4;//Default segment length for Bezier Algos
	public int flattenAmount = 12;//Determines how many points we flatten near the ends of the Bezier Curve
	public GameObject parent;
	//Stop Sign Variables
	public GameObject stopSign;
	[SerializeField]
    private GameObject[] spawnedSigns = new GameObject[2];
	//Graph
	[SerializeField]
	CiDyGraph graph;

    [HideInInspector]
    public Vector3 position;
    //Used to Prep for Threaded functions
    public void StorePos() {
        position = transform.position;
    }

	void CreateRenderer(){
		//First time this component as been created.
		mRender = (MeshRenderer)gameObject.AddComponent<MeshRenderer>();
		mRender.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mFilter = (MeshFilter)gameObject.AddComponent<MeshFilter>();
        if (graph.roadMaterial == null)
        {
            roadMaterial = (Material)Resources.Load("CiDyResources/Road", typeof(Material));
        }
        else {
            roadMaterial = graph.roadMaterial;
        }
        //Set Road Material
        mRender.sharedMaterial = roadMaterial;
        //Add Collider for later
        mCollider = (MeshCollider)gameObject.AddComponent<MeshCollider> ();
		mCollider.sharedMesh = new Mesh ();
		if(stopSign==null){
			stopSign = (GameObject)Resources.Load("CiDyResources/StopSign", typeof(GameObject));
		}
        //Grab Guard Rail Material if Applciable
        railMat = (Material)Resources.Load("CiDyResources/RailMat", typeof(Material));
        postMat = (Material)Resources.Load("CiDyResources/PostMat", typeof(Material));
    }

	//Raw Path That needs to be Bezier/BSpline
	public void InitilizeRoad(Vector3[] rawPath, float newWidth, int newSegmentLength, int newFlatAmount, CiDyNode newA, CiDyNode newB, GameObject holder, CiDyGraph newGraph, bool normalizePath){
		//Debug.Log ("Initilized "+name+" with "+rawPath.Length+" points and width of "+newWidth+"NodeA: "+newA.name+" NewB: "+newB.name+" RoadMaterial: "+newGraph.roadMaterial);
        graph = newGraph;//Set Graph Reference
        if (mRender == null){
			CreateRenderer();
		}
		width = newWidth;
		segmentLength = newSegmentLength;//Segment Length Must Not Be Large than Half RoadWidth(This is for the Terrain Blending)
        segmentLength = Mathf.Clamp(segmentLength, 0, (int)(width / 2));
		flattenAmount = newFlatAmount;
		nodeA = newA;
		nodeB = newB;
		parent = holder;

        if (normalizePath)
        {
            cpPoints = CiDyUtils.CreateBezier(rawPath);
        }
        else {
            cpPoints = rawPath;
        }


        origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        //Now Lets Contour these Points to the Terrain.
        graph.ContourPathToTerrain(ref origPoints);
        //origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        origPoints = FlattenRoadPath();

        //Convert Input Path to a Specificed 4 Control Point Path
        /*if (rawPath.Length == 4) {
            cpPoints = rawPath;
        } else {
            //If its not. Then Convert it to Four through Tessellation.
            cpPoints = rawPath;
        }*/
        //origPoints = CiDyUtils.CreateBSpline (rawPath, segmentLength);
        //origPoints = CiDyUtils.CreateBezier (rawPath, (float)segmentLength);
        /*int flattenInt;
        if (origPoints.Length <= flattenAmount)
        {
            flattenInt = origPoints.Length / 2;
        }
        else
        {
            flattenInt = flattenAmount;
        }
        //Iterate through and Update the Slopes
        for (int i = 1; i < origPoints.Length - 1; i++)
        {
            Vector3 v0 = origPoints[i];
            Vector3 v1 = origPoints[origPoints.Length - 1];
            if (i < flattenInt)
            {
                origPoints[i] = new Vector3(v0.x, origPoints[0].y, v0.z);
            }
            else if (i > origPoints.Length - flattenInt)
            {
                origPoints[i] = new Vector3(v0.x, v1.y, v0.z);
            }
            else
            {
                continue;
            }
        }
        origPoints = CiDyUtils.CreateBezier(origPoints, segmentLength);*/

        /*float startY = origPoints[0].y;
        float endY = origPoints[origPoints.Length - 1].y;
        //Blend The Points Averages along the Line.
        float totalDist = CiDyUtils.FindTotalDistOfPoints(origPoints);
        float curDist = 0;
        for (int i = 0; i < origPoints.Length-1; i++) {
            float t = curDist / totalDist;
            origPoints[i].y = Mathf.Lerp(origPoints[i].y, origPoints[origPoints.Length - 1].y, t);
            //Distance Between these two points.
            curDist = Vector3.Distance(origPoints[i], origPoints[i + 1]);
        }*/
        //Tell Nodes Road is Attached and Needs to Be Accounted For.
        nodeA.AddRoad (this);
		nodeB.AddRoad (this);
	}
    
    //Update RoadWidth,SegmentLength,FlattenAmount
    public void InitilizeRoad(float newWidth, int newSegmentLength, int newFlatAmount){
        //Debug.Log("Initialize Road: "+name+" Width: "+newWidth+" New Seg Lenght: "+newSegmentLength+" New Flat Amount: "+newFlatAmount);
		//Debug.Log ("Initilized "+name+" with "+rawPath.Count+" points and width of "+newWidth+"NodeA: "+newA.name+" NewB: "+newB.name);
		width = newWidth;
		segmentLength = newSegmentLength;
		flattenAmount = newFlatAmount;
        //cpPoints = CiDyUtils.CreateBezier(cpPoints);
        origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        //Now Lets Contour these Points to the Terrain.
        graph.ContourPathToTerrain(ref origPoints);
        //origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        origPoints = FlattenRoadPath();
        /*int flattenInt;
		if(origPoints.Length <= flattenAmount){
			flattenInt = origPoints.Length / 2;
		} else {
			flattenInt = flattenAmount;
		}
		//Iterate through and Update the Slopes
		for(int i = 1;i<origPoints.Length - 1;i++){
			Vector3 v0 = origPoints[i];
			Vector3 v1 = origPoints[origPoints.Length - 1];
			if(i<flattenInt){
				origPoints[i] = new Vector3(v0.x,origPoints[0].y,v0.z);
			} else if(i>origPoints.Length - flattenInt){
				origPoints[i] = new Vector3(v0.x,v1.y,v0.z);
			} else {
				continue;
			}
		}*/

        //Tell Nodes Road is Attached and Needs to Be Accounted For.
        nodeA.UpdatedRoad ();
		nodeB.UpdatedRoad ();
        //Debug.Log("Done Initializing Road");
	}

	//Raw Path That needs to be Bezier/BSpline
	/*public void InitilizeBSpline(Vector3[] rawPath, float newWidth, int newSegmentLength, int newFlatAmount, CiDyNode newA, CiDyNode newB, GameObject holder, CiDyGraph newGraph){
		Debug.Log ("Initilized "+name+" with "+rawPath.Length+" points and width of "+newWidth);
		width = newWidth;
		segmentLength = newSegmentLength;
		flattenAmount = newFlatAmount;
		nodeA = newA;
		nodeB = newB;
		parent = holder;
		graph = newGraph;
		cpPoints = rawPath;
		//origPoints = CiDyUtils.CreateBSpline (rawPath, segmentLength);
		origPoints = CiDyUtils.CreateBezier (rawPath, (float)segmentLength);
		//Now Lets Contour these Points to the Terrain.
		graph.ContourPathToTerrain(ref origPoints);
		int flattenInt;
		if(origPoints.Length <= flattenAmount){
			flattenInt = origPoints.Length / 2;
		} else {
			flattenInt = flattenAmount;
		}
		//Iterate through and Update the Slopes
		for(int i = 1;i<origPoints.Length - 1;i++){
			Vector3 v0 = origPoints[i];
			Vector3 v1 = origPoints[origPoints.Length - 1];
			if(i<flattenInt){
				origPoints[i] = new Vector3(v0.x,origPoints[0].y,v0.z);
			} else if(i>origPoints.Length - flattenInt){
				origPoints[i] = new Vector3(v0.x,v1.y,v0.z);
			} else {
				continue;
			}
		}
		origPoints = CiDyUtils.CreateBezier (origPoints, segmentLength);
		//Tell Nodes Road is Attached and Needs to Be Accounted For.
		nodeA.AddRoad (this);
		nodeB.AddRoad (this);
	}
	*/
	//Replot Road Based on New Path And cur RoadWidth and Nodes A & B.
	public void ReplotRoad(Vector3[] rawPath){
		//Debug.Log ("ReplotRoad "+name+" RawPath: "+rawPath.Length);
		cpPoints = rawPath;
        //Replot Path to Nodes.
        origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        //Now Lets Contour these Points to the Terrain.
        graph.ContourPathToTerrain(ref origPoints);
        //origPoints = CiDyUtils.CreateBezier(cpPoints, (float)segmentLength);
        origPoints = FlattenRoadPath();
        //Update Connected Nodes and allow the Road Change to Take Full Effect
        UpdateRoadNodes();
	}

    Vector3[] FlattenRoadPath() {
        float totalDist = CiDyUtils.FindTotalDistOfPoints(origPoints);
        float flatDist = (width*3.2f) + (segmentLength * 2);
        if (flatDist > (totalDist/2)) {
            flatDist = totalDist / 2;
        }
        //Debug.Log("FlattenRoadPath: "+ flatDist);
        //Project a Point 12 Meters towards B end from A End to visualize the are a of flattening.
        Vector3 strPos = nodeA.position;
        Vector3 endPos = nodeB.position;
        strPos.y = 0;
        endPos.y = 0;
        //Pre calculate Flattend Ends
        for (int i = 0; i < origPoints.Length; i++)
        {
            Vector3 v0 = origPoints[i];
            //CiDyUtils.MarkPoint(v0,i);
            v0.y = 0;

            //Calculate Distance
            float distA = Vector3.Distance(v0, strPos);
            float distB = Vector3.Distance(v0, endPos);

            if (i == 1 || i == origPoints.Length - 2) {
                if (distA < distB) {
                    v0.y = nodeA.position.y;
                    origPoints[i] = v0;
                } else
                {
                    v0.y = nodeB.position.y;
                    origPoints[i] = v0;
                }
                //CiDyUtils.MarkPoint(origPoints[i], i+999);
            }
            if (distA <= flatDist)//flatDist)
            {
                v0.y = nodeA.position.y;
                origPoints[i] = v0;
                //CiDyUtils.MarkPoint(v0, i);
            }
            else if (distB <= flatDist)//(nodeBRadius + (segmentLength * 3)))
            {
                v0.y = nodeB.position.y;
                origPoints[i] = v0;
                //CiDyUtils.MarkPoint(v0, 200 + i);
            }
        }

        return CiDyUtils.CreateBezier(origPoints, (float)segmentLength);
    }

	public void UpdateRoadNodes(){
		nodeA.UpdatedRoad ();
		nodeB.UpdatedRoad ();
	}

	int count = 0;
	public void NodeDone(CiDyNode doneNode, float newRadius, bool updateMesh){
		//Debug.Log ("Road: "+name+" Node Done " + doneNode.name+" newRadius: "+newRadius);
        if (count >= 2) {
            count = 0;
        }
		if(doneNode.name == nodeA.name){
			nodeARadius = newRadius;
            count++;
		} else if(doneNode.name == nodeB.name){
			nodeBRadius = newRadius;
            count++;
		}

        if(count >= 2 || updateMesh) {
            if (nodeARadius > 0 && nodeBRadius > 0)
            {
                SnipRoadMesh();
            }
        }
	}

    //This will cut a the Road Mesh At its Node Ends (ORIGINAL SNIP MESH FUNCTION)
    public void SnipRoadMesh()
    {
        //Debug.Log("Snip");
        //Is this a Special Case of Minor Road Intersecting with a Major Road?
        bool minorRoadMerge = false;
        /*if (nodeA.hierarchy != nodeB.hierarchy)
        {
            //This is a Minor Road merging into a Major Road. or visa versa
            minorRoadMerge = true;
            //Debug.Log("This is Minor Road Merge");
        }*/
        //Update Our GameObject position to be in the middle of our nodes.
        //Debug.Log ("Creating RoadMesh "+gameObject.name+" RadiusA: "+nodeARadius+" RadiusB: "+nodeBRadius);
        //Grab the Circle Radius for the line Testing that needs to happen on both nodeA and nodeB.
        //Grab roads OrigPoints
        //Iterate through the Vertices and determine all the Points that are to the Right of the line and Remove them.Then push the p0andp1 as the v0 and v1 of the list.
        //Determine which end we are snipping
        if (origPoints.Length <= 0)
        {
            return;
        }
        List<Vector3> tmpPoints = new List<Vector3>(origPoints);
        //float totalDist = CiDyUtils.FindTotalDistOfPoints(origPoints);
        Vector3[] endPoints = new Vector3[4];
        List<int> tris = new List<int>(0);
        List<Vector2> uvs = new List<Vector2>(0);
        Vector3 tmpIntersection = Vector3.zero;
        bool hasLeft = false;
        bool hasRight = false;
        //Using the OrigPionts from the Road we will calculate dynamic lines based on roadWidth From Each End that the nodes in testing are on.
        int startPlace = 0;//Start of Mesh Points
        int endPlace = 0;//End of Mesh Points
        int startPlaceR = 0;//Special MinorRoadMerge Snip Logic
        int endPlaceR = 0;//Special MinorRoadMerge Snip Logic
        float dist1 = Vector3.Distance(tmpPoints[0], nodeA.position);
        float dist2 = Vector3.Distance(tmpPoints[0], nodeB.position);
        if (dist2 < dist1)
        {
            tmpPoints.Reverse();
        }
        //Debug.Log("Start Check: " + name);
        //Calculate the Snip points this road will have with its intersection nodes circles
        //Node A is at the start of list.//And Node B is at the End.
        //Now lets calculate the lines from the start to end based on the origPoints.
        float closestL = Mathf.Infinity;
        float closestR = Mathf.Infinity;
        //Iterate through center line of road
        for (int i = 0; i < tmpPoints.Count; i++)
        {
            //Test Left of Line.//Determine Pos
            Vector3 vector = tmpPoints[i];
            Vector3 vector2;//Next Point
            Vector3 vector3;//thrid point.If Applicable
            //Dir based on next in line.And Direction Based on Second Line.
            Vector3 vectorDir;//Direction from cur to Next
            Vector3 vectorDir2;//Direction from nxt to third. If Applicable
            if (i < tmpPoints.Count - 2)
            {
                //Beginning or Middle
                vector2 = tmpPoints[i + 1];
                vectorDir = (vector2 - vector);
                vector3 = tmpPoints[i + 2];
                vectorDir2 = (vector3 - vector2);
            }
            else
            {
                //At End
                vector2 = tmpPoints[i - 1];
                vectorDir = (vector - vector2);
                vectorDir2 = Vector3.zero;
            }
            //Calculate Cross Product and place points.
            Vector3 cross = Vector3.Cross(Vector3.up, vectorDir).normalized;
            //Calculate Four Points creating left Line and Right Line.
            Vector3 leftVector = vector + (-cross) * (width / 2);
            Vector3 rightVector = vector + cross * (width / 2);
            Vector3 leftVector2;
            Vector3 rightVector2;
            if (i < tmpPoints.Count - 2)
            {
                Vector3 cross2 = Vector3.Cross(Vector3.up, vectorDir2).normalized;
                leftVector2 = vector2 + (-cross2) * (width / 2);
                rightVector2 = vector2 + cross2 * (width / 2);
            }
            else
            {
                leftVector2 = vector2 + (-cross) * (width / 2);
                rightVector2 = vector2 + cross * (width / 2);
            }
            //Are we testing against a Circle Radius(Standard Intersection)
            //OR are we testing against the Node RdLines?
            if (minorRoadMerge && nodeA.hierarchy == CiDyNode.Hierarchy.Major)
            {
                //Run Test Against Road Lines. Not A Radius Circle.
                //Test Intersections Road Lines. Find the Intersection Points for Left and Right points.
                for (int j = 0; j < nodeA.rdLines.Count; j++)
                {
                    //LEFT VECTORS
                    //Test Extended Line First
                    //First Test Extended Line.
                    Vector3 extA = nodeA.rdLines[j].leftLine[0];
                    Vector3 extDir = nodeA.rdLines[j].leftLine[1] - extA;
                    Vector3 extB = nodeA.rdLines[j].leftLine[0] + (extDir * 1000);
                    //Left Extended Line Test
                    if (CiDyUtils.LineIntersection(leftVector, leftVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[0] = tmpIntersection;
                            //hasLeft = true;
                            startPlace = i + 1;//originally just i
                            //This line is an Intersection. So where does the Line Exit? Through the Top Line or the bottom?
                            //Check top line
                            if (CiDyUtils.LineIntersection(leftVector2, rightVector2, extA, extB, ref tmpIntersection))
                            {
                                //Top Line is the Exit Point.

                            }
                        }
                    }
                    //Right Extended Line Test.
                    extA = nodeA.rdLines[j].rightLine[0];
                    extDir = nodeA.rdLines[j].rightLine[1] - extA;
                    extB = nodeA.rdLines[j].rightLine[0] + (extDir * 1000);
                    if (CiDyUtils.LineIntersection(leftVector, leftVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[0] = tmpIntersection;
                            //hasLeft = true;
                            startPlace = i + 1;//originally just i
                        }
                    }
                    //RIGHT VECTORS
                    extA = nodeA.rdLines[j].leftLine[0];
                    extDir = nodeA.rdLines[j].leftLine[1] - extA;
                    extB = nodeA.rdLines[j].leftLine[0] + (extDir * 1000);
                    //Left Extended Line Test
                    if (CiDyUtils.LineIntersection(rightVector, rightVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[1] = tmpIntersection;
                            //hasLeft = true;
                            startPlaceR = i + 1;//originally just i
                        }
                    }
                    //Right Extended Line Test.
                    extA = nodeA.rdLines[j].rightLine[0];
                    extDir = nodeA.rdLines[j].rightLine[1] - extA;
                    extB = nodeA.rdLines[j].rightLine[0] + (extDir * 1000);
                    if (CiDyUtils.LineIntersection(rightVector, rightVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[1] = tmpIntersection;
                            //hasLeft = true;
                            startPlaceR = i + 1;//originally just i
                        }
                    }
                    //Were the Extended Lines all we needed?
                    if (startPlace != 0 && startPlaceR != 0)
                    {
                        //If both Starts are filled. Then move on to the Next Rd Line for Testing.
                        continue;
                    }

                    //The EXTENDED LINES of this Rd Line has been Tested. Now we need to test the Segments of the Lines.
                    //If here then the Extended Line of this Rd Line was a fail. Test every segment of Rd Left Line
                    if (CiDyUtils.IntersectsList(leftVector, leftVector2, nodeA.rdLines[j].leftLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[0] = tmpIntersection;
                            //hasLeft = true;
                            startPlace = i + 1;//originally just i
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                    //Test this lines Right lines to our left Lines.
                    ///Debug.Log("NodeA Radius: " + nodeARadius);
                    if (CiDyUtils.IntersectsList(leftVector, leftVector2, nodeA.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[0] = tmpIntersection;
                            //hasLeft = true;
                            startPlace = i + 1;
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }

                    //Now Test Right Lines
                    //If here then the Extended Line of this Rd Line was a fail. Test every segment of Rd Left Line
                    if (CiDyUtils.IntersectsList(rightVector, rightVector2, nodeA.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[1] = tmpIntersection;
                            //hasLeft = true;
                            startPlaceR = i + 1;//originally just i
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                    //Test this lines Right lines to our left Lines.
                    ///Debug.Log("NodeA Radius: " + nodeARadius);
                    if (CiDyUtils.IntersectsList(rightVector, rightVector2, nodeA.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[tmpPoints.Count - 1]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeA.position.y;

                            endPoints[1] = tmpIntersection;
                            //hasLeft = true;
                            startPlaceR = i + 1;
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                }
            }
            else
            {
                //Debug.Log("Standard Circle A: "+this.name);
                //Standard Circle Test of NodeA
                if (!hasLeft)
                {
                    if (CiDyUtils.CircleIntersectsLine(nodeA.position, nodeARadius, 360, leftVector, leftVector2, ref tmpIntersection))
                    {
                        //Found Left :)
                        tmpIntersection.y = nodeA.position.y;

                        endPoints[0] = tmpIntersection;
                        hasLeft = true;
                        startPlace = i + 1;//originally just i
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                }
                if (!hasRight)
                {
                    if (CiDyUtils.CircleIntersectsLine(nodeA.position, nodeARadius, 360, rightVector, rightVector2, ref tmpIntersection))
                    {
                        //Found Right :)
                        tmpIntersection.y = nodeA.position.y;
                        //Debug.Log("Second : "+tmpIntersection);
                        endPoints[1] = tmpIntersection;
                        hasRight = true;
                        startPlaceR = i + 1;
                        //CiDyUtils.MarkPoint(tmpIntersection,1);
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                }
                if (hasLeft && hasRight)
                {
                    //Debug.Log("Found First Points");
                    //We are Done with this side now find the Other sides.
                    break;
                }
            }
        }
        //Debug.Log("EndOf First Cycle: "+name);
        hasLeft = false;
        hasRight = false;
        closestR = Mathf.Infinity;
        closestL = Mathf.Infinity;
        for (int i = tmpPoints.Count - 1; i > 0; i--)
        {
            //Test Left of Line.//Determine Pos
            Vector3 vector = tmpPoints[i];
            Vector3 vector2;
            Vector3 vector3;//thrid point.If Applicable
            //Dir based on next in line.And Direction Based on Second Line.
            Vector3 vectorDir;//Direction from cur to Next
            Vector3 vectorDir2;//Direction from nxt to third. If Applicable
            if (i > 1)
            {
                //At End or Middle
                vector2 = tmpPoints[i - 1];
                vectorDir = (vector2 - vector);
                vector3 = tmpPoints[i - 2];
                vectorDir2 = (vector3 - vector2);
            }
            else if (i == 1)
            {
                vector2 = tmpPoints[i - 1];
                vectorDir = (vector2 - vector);
                vectorDir2 = Vector3.zero;
            }
            else
            {
                vector2 = tmpPoints[i + 1];
                vectorDir = (vector - vector2);
                vectorDir2 = Vector3.zero;
            }
            //Calculate Cross Product and place points.
            Vector3 cross = Vector3.Cross(Vector3.up, vectorDir).normalized;
            //Calculate Four Points creating left Line and Right Line.
            Vector3 leftVector = vector + (-cross) * (width / 2);
            Vector3 rightVector = vector + cross * (width / 2);
            Vector3 leftVector2;
            Vector3 rightVector2;
            if (i > 1)
            {
                Vector3 cross2 = Vector3.Cross(Vector3.up, vectorDir2).normalized;
                leftVector2 = vector2 + (-cross2) * (width / 2);
                rightVector2 = vector2 + cross2 * (width / 2);
            }
            else
            {
                leftVector2 = vector2 + (-cross) * (width / 2);
                rightVector2 = vector2 + cross * (width / 2);
            }
            if (minorRoadMerge && nodeB.hierarchy == CiDyNode.Hierarchy.Major)
            {
                for (int j = 0; j < nodeB.rdLines.Count; j++)
                {
                    //LEFT VECTORS
                    //Test Extended Line First
                    //First Test Extended Line.
                    Vector3 extA = nodeB.rdLines[j].leftLine[0];
                    Vector3 extDir = nodeB.rdLines[j].leftLine[1] - extA;
                    Vector3 extB = nodeB.rdLines[j].leftLine[0] + (extDir * 1000);
                    //Left Extended Line Test
                    if (CiDyUtils.LineIntersection(leftVector, leftVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[3] = tmpIntersection;
                            endPlace = i;
                        }
                    }
                    //Right Extended Line Test.
                    extA = nodeB.rdLines[j].rightLine[0];
                    extDir = nodeB.rdLines[j].rightLine[1] - extA;
                    extB = nodeB.rdLines[j].rightLine[0] + (extDir * 1000);
                    if (CiDyUtils.LineIntersection(leftVector, leftVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[3] = tmpIntersection;
                            //hasLeft = true;
                            endPlace = i;//originally just i
                        }
                    }
                    //RIGHT VECTORS
                    extA = nodeB.rdLines[j].leftLine[0];
                    extDir = nodeB.rdLines[j].leftLine[1] - extA;
                    extB = nodeB.rdLines[j].leftLine[0] + (extDir * 1000);
                    //Left Extended Line Test
                    if (CiDyUtils.LineIntersection(rightVector, rightVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[2] = tmpIntersection;
                            endPlaceR = i;
                        }
                    }
                    //Right Extended Line Test.
                    extA = nodeB.rdLines[j].rightLine[0];
                    extDir = nodeB.rdLines[j].rightLine[1] - extA;
                    extB = nodeB.rdLines[j].rightLine[0] + (extDir * 1000);
                    if (CiDyUtils.LineIntersection(rightVector, rightVector2, extA, extB, ref tmpIntersection))
                    {
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[2] = tmpIntersection;
                            endPlaceR = i;
                        }
                    }
                    //Were the Extended Lines all we needed?
                    if (endPlace != 0 && endPlaceR != 0)
                    {
                        //If both Starts are filled. Then move on to the Next Rd Line for Testing.
                        continue;
                    }

                    //The EXTENDED LINES of this Rd Line has been Tested. Now we need to test the Segments of the Lines.
                    //If here then the Extended Line of this Rd Line was a fail. Test every segment of Rd Left Line
                    if (CiDyUtils.IntersectsList(leftVector, leftVector2, nodeB.rdLines[j].leftLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[3] = tmpIntersection;
                            //hasLeft = true;
                            endPlace = i;//originally just i
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                    //Test this lines Right lines to our left Lines.
                    ///Debug.Log("NodeA Radius: " + nodeARadius);
                    if (CiDyUtils.IntersectsList(leftVector, leftVector2, nodeB.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestL)
                        {
                            closestL = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[3] = tmpIntersection;
                            //hasLeft = true;
                            endPlace = i;
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }

                    //Now Test Right Lines
                    //If here then the Extended Line of this Rd Line was a fail. Test every segment of Rd Left Line
                    if (CiDyUtils.IntersectsList(rightVector, rightVector2, nodeB.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[2] = tmpIntersection;
                            //hasLeft = true;
                            endPlaceR = i;//originally just i
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                    //Test this lines Right lines to our left Lines.
                    ///Debug.Log("NodeA Radius: " + nodeARadius);
                    if (CiDyUtils.IntersectsList(rightVector, rightVector2, nodeB.rdLines[j].rightLine.ToList(), ref tmpIntersection, false))
                    {
                        //Found Left :)
                        //Debug.Log("First : "+tmpIntersection);
                        //Get dist
                        float distance = Vector3.Distance(tmpIntersection, tmpPoints[0]);
                        if (distance < closestR)
                        {
                            closestR = distance;
                            //Update Y Value as it is not set  by the 2D Line Intersection Test but by the Interpolated Value
                            tmpIntersection.y = nodeB.position.y;

                            endPoints[2] = tmpIntersection;
                            //hasLeft = true;
                            endPlaceR = i;
                        }
                        //Debug.Log("Found First Left foundPoints: "+foundPoints);
                    }
                }
            }
            else
            {
                //Debug.Log("Standard Circle B: " + this.name);
                if (!hasLeft)
                {
                    //Debug.Log("Node B Radius Left: " + nodeBRadius);
                    if (CiDyUtils.CircleIntersectsLine(nodeB.position, nodeBRadius, 360, rightVector, rightVector2, ref tmpIntersection))
                    {
                        //Found Left :)
                        tmpIntersection.y = nodeB.intersection.transform.position.y;
                        endPoints[2] = tmpIntersection;
                        hasLeft = true;
                        endPlace = i;
                    }
                }
                if (!hasRight)
                {
                    //Debug.Log("Node B Radius Right: " + nodeBRadius);
                    if (CiDyUtils.CircleIntersectsLine(nodeB.position, nodeBRadius, 360, leftVector, leftVector2, ref tmpIntersection))
                    {
                        //Found Left :)
                        tmpIntersection.y = nodeB.intersection.transform.position.y;
                        endPoints[3] = tmpIntersection;
                        hasRight = true;
                        endPlaceR = i;
                    }
                }
                if (hasLeft && hasRight)
                {
                    //We are Done with this side now find the Other sides.
                    break;
                }
            }
        }
        if (endPoints.Length < 4)
        {
            Debug.Log("Not Enough Points: " + name);
            return;
        }

        //List<Vector3> leftEdge = new List<Vector3>(0);
        //List<Vector3> rightEdge = new List<Vector3>(0);
        //Debug.Log("EndOf Second Cycle: " + name);
        //int totalPoints = (endPlace - startPlace)*2;
        List<Vector3> newVerts = new List<Vector3>();
            //float totalDist = 0;
            for (int i = startPlace; i < endPlace; i++)
            {
                //Test Left of Line.//Determine Pos
                Vector3 vector = tmpPoints[i];
                Vector3 vector2;
                //Dir based on next in line.
                Vector3 vectorDir;
                if (i != tmpPoints.Count - 1)
                {
                    vector2 = tmpPoints[i + 1];
                    vectorDir = (vector2 - vector);
                    //totalDist+= Vector3.Distance(vector,vector2);
                    //CiDyUtils.MarkPoint(vector,i);
                    //CiDyUtils.MarkPoint(vector2,i);
                }
                else
                {
                    vector2 = tmpPoints[i - 1];
                    vectorDir = (vector - vector2);
                }
                //Calculate Cross Product and place points.
                Vector3 cross = Vector3.Cross(Vector3.up, vectorDir).normalized;
                //Calculate Four Points creating left Line and Right Line.
                Vector3 leftVector = vector + (-cross) * (width / 2);
                Vector3 rightVector = vector + cross * (width / 2);
                newVerts.Add(leftVector);
                newVerts.Add(rightVector);
                /*//Move Left Vector according to Offset
                if(leftRailOffset != 0) {
                    leftVector += ((-cross) * (leftRailOffset));
                }
                if(rightRailOffset != 0) {
                    rightVector += (cross * rightRailOffset);
                }
                //Left and Right Edges for Procedural Guard Railing logic
                leftEdge.Add(leftVector);
                rightEdge.Add(rightVector);*/
            }
        if (newVerts.Count < 4)
        {
            return;
        }
        //Debug.Log("EndOf Last Cycle: " + name);
        newVerts[0] = endPoints[0];
        newVerts[1] = endPoints[1];
        //leftEdge[0] = endPoints[0];
        //rightEdge[0] = endPoints[1];
        newVerts[newVerts.Count - 2] = endPoints[2];
        newVerts[newVerts.Count - 1] = endPoints[3];
        //leftEdge[leftEdge.Count - 1] = endPoints[2];
        //rightEdge[rightEdge.Count-1] = endPoints[3];

        tris = new List<int>();
        uvs = new List<Vector2>();
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
        if (uvsRoadSet)
        {
            float uvDist = 0;
            float zDist = 0;
            //Set up UVs for Three Segments and Up.
            for (int i = 0; i < newVerts.Count - 2; i += 2)
            {
                //Handle All Four Points of UV with mounting Values.
                uvs.Add(new Vector2(0, uvDist));
                uvs.Add(new Vector2(1, uvDist));
                //We are at the Beginning//Get Vertical Distance
                //Vector2 midPointA = (new Vector2(newVerts[i].x, newVerts[i].z) + new Vector2(newVerts[i + 1].x, newVerts[i + 1].z)) / 2;
                //Vector2 midPointB = (new Vector2(newVerts[i + 2].x, newVerts[i + 2].z) + new Vector2(newVerts[i + 3].x, newVerts[i + 3].z)) / 2;
                //Get Vertical Distance
                //zDist = (Vector2.Distance(midPointA, midPointB) / totalDist);
                Vector3 midPointA = (newVerts[i] + newVerts[i + 1]) / 2;
                Vector3 midPointB = (newVerts[i + 2] + newVerts[i + 3]) / 2;
                //Get Vertical Distance
                zDist = (Vector3.Distance(midPointA, midPointB)) / 60;
                uvDist += zDist;
            }
            //Add Last Two Points
            uvs.Add(new Vector2(0, uvDist));
            uvs.Add(new Vector2(1, uvDist));
        }
        else
        {
            //Set Uvs based on X/Z Values
            for (int i = 0; i < newVerts.Count; i++)
            {
                uvs.Add(new Vector2(newVerts[i].x, newVerts[i].z));
            }
        }
        //Debug.Log("Verts: " + newVerts.Count+" UVs: "+uvs.Count+" Road: "+name);
        //Set Triangles and 
        Mesh roadMesh = new Mesh
        {
            vertices = newVerts.ToArray(),
            triangles = tris.ToArray(),
            uv = uvs.ToArray()
        };
        roadMesh.RecalculateBounds();
        roadMesh.RecalculateNormals();
        //Set Mesh to filter
        mFilter.sharedMesh = roadMesh;
        //Update Collider Mesh value as well.
        mCollider = gameObject.GetComponent<MeshCollider>();
        mCollider.sharedMesh.Clear();
        mCollider.sharedMesh = roadMesh;
        //Set Stop Sign Spawn Points
        //spawnPoints [0] = roadMesh.vertices[0];
        //spawnPoints [1] = roadMesh.vertices[roadMesh.vertices.Length - 1];
        //Spawn Sign if it exist
        if (spawnedSigns[0] != null)
        {
            DestroyImmediate(spawnedSigns[0]);
        }
        if (spawnedSigns[1] != null)
        {
            DestroyImmediate(spawnedSigns[1]);
        }
        //Debug.Log("Finalizing Stop Sign: "+name);
        if (stopSign != null)
        {
            if (roadMesh.vertices.Length >= 4)
            {
                GameObject sign = null;
                Vector3 fwd = Vector3.zero;
                Vector3 nxt = Vector3.zero;
                //Check this Nodes end.
                if (nodeA.type == CiDyNode.IntersectionType.tConnect)
                {
                    //Create First
                    sign = (GameObject)Instantiate(stopSign, roadMesh.vertices[0], Quaternion.identity);
                    sign.transform.parent = transform;
                    fwd = (roadMesh.vertices[2] - roadMesh.vertices[0]).normalized;
                    nxt = (roadMesh.vertices[0] + (fwd * 2));
                    sign.transform.LookAt(nxt);
                    spawnedSigns[0] = sign;
                }
                //Check this Nodes end.
                if (nodeB.type == CiDyNode.IntersectionType.tConnect)
                {
                    //Create Second
                    sign = (GameObject)Instantiate(stopSign, roadMesh.vertices[roadMesh.vertices.Length - 1], Quaternion.identity);
                    sign.transform.parent = transform;

                    fwd = (roadMesh.vertices[roadMesh.vertices.Length - 3] - roadMesh.vertices[roadMesh.vertices.Length - 1]).normalized;
                    nxt = (roadMesh.vertices[roadMesh.vertices.Length - 1] + (fwd * 2));
                    sign.transform.LookAt(nxt);
                    spawnedSigns[1] = sign;
                }
            }
        }

        /*//Calculate Left Rail Line and Right Rail Line
        //Now generate Rails down the road.(RailRoad Meshes)
        GameObject leftRailing = CiDyUtils.GenerateGuardRailandPost(leftEdge.ToArray(), true, railMat, postMat);
        GameObject rightRailing = CiDyUtils.GenerateGuardRailandPost(rightEdge.ToArray(), false, railMat, postMat);
        leftRailing.transform.parent = transform;
        rightRailing.transform.parent = transform;
        if (leftRail != null) {
            DestroyImmediate(leftRail.gameObject);
        }
        //Set Left
        leftRail = leftRailing.transform;
        if (rightRail != null) {
            DestroyImmediate(rightRail.gameObject);
        }
        rightRail = rightRailing.transform;*/
    }

    //This function is called when we want to change the applied Material to the Road.
    public void ChangeRoadMaterial() {
        mRender.sharedMaterial = graph.roadMaterial;
    }

    //Functions used to Select and Deselect the Road(IE. Change Material)
    [SerializeField]
	Material lastMaterial;
	[SerializeField]
	Material selectedMaterial;

	public void SelectRoad(){
		//Debug.Log ("Select Road "+selectedMaterial.name);
		//Change Material
		if(selectedMaterial == null){
			selectedMaterial = (Material)Resources.Load("CiDyResources/ActiveRoadMaterial", typeof(Material));
		}
		lastMaterial = mRender.sharedMaterial;
		mRender.sharedMaterial = selectedMaterial;
		selected = true;
	}

	public void DeselectRoad(){
		//Debug.Log ("Deselected Road "+lastMaterial.name);
		mRender.sharedMaterial = lastMaterial;
		selected = false;
	}
}

//This class is used To Rebuild a Road Mesh after Intersections have determined there Shapes. (Doesnt need to be serialized)
public class TmpVert {
    //Needs to hold its Position and If its On or Off.
    public bool state = true;
    public Vector3 pos;//The Position its at.

    public TmpVert(Vector3 newPos, bool isOn) {

        state = isOn;
        pos = newPos;
    }
}



