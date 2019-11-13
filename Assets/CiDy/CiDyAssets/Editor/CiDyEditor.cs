using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Swing.Editor;
using System.Threading;

public class CiDyEditor : EditorWindow {
	//Window Creation Functions
	[MenuItem ("Window/CiDy/CiDyEditor")]
    public static void ShowWindow()
    {
        //2018 >
        #if UNITY_5
		EditorWindow.GetWindow(typeof(CiDyEditor));
        #else
        GetWindow<CiDyEditor>(false, "CiDy", true);
        #endif
    }

    //CiDy Graph
    [SerializeField]
	public CiDyGraph graph;
	//LayerMasks
	public LayerMask roadMask = -1;
	public LayerMask roadMask2;
	public LayerMask cellMask;
	public LayerMask nodeMask;
	//Tagging System(Naming)
	private readonly string terrainTag = "Terrain";
	private readonly string cellTag = "Cell";
	private readonly string roadTag = "Road";
	private readonly string nodeTag = "Node";

    private bool displayingProgressBar = false;

    //Grab or Create a CiDyGraph
    void OnEnable(){
        //Debug.Log ("OnEnable CiDyWindow");
        //Check for an existing graph
        //hideFlags = HideFlags.HideAndDontSave;
        graph = FindObjectOfType (typeof(CiDyGraph)) as CiDyGraph;
		if(graph == null){
			//Debug.Log("Made Graph");
			GameObject go = new GameObject ("CiDyGraph");
			go.transform.position = new Vector3 (0, 0, 0);
			graph = (CiDyGraph)go.AddComponent(typeof(CiDyGraph));
			graph.InitilizeGraph();
		}
		//Set Layer Masks
		//Set Road Searching Mask
		roadMask = 1 << LayerMask.NameToLayer (terrainTag);
		roadMask2 = 1 << LayerMask.NameToLayer (roadTag);
		cellMask = 1 << LayerMask.NameToLayer (cellTag);
		nodeMask = (1 << LayerMask.NameToLayer (nodeTag) | 1 << LayerMask.NameToLayer (terrainTag));
        if (showCells) {
            graph.EnableCellGraphics();
        }
        if (showNodes) {
            graph.EnableNodeGraphics();
        }
		/*//Reset Visuals
		graph.EnableNodeGraphics ();
		graph.EnableCellGraphics ();
		showCells = true;
		showNodes = true;*/
        //Grab Material Resources
        //Grab Node Material Resources
        nodeMaterial = Resources.Load("CiDyResources/NodeMaterial", typeof(Material)) as Material;
        //Grab Active Node Material Resource
        activeMaterial = Resources.Load("CiDyResources/ActiveMaterial", typeof(Material)) as Material;
        //Cell Selection Material
        cellSelectionMaterial = Resources.Load("CiDyResources/CellSelection", typeof(Material)) as Material;
        cellMaterial = Resources.Load("CiDyResources/CellTransparency", typeof(Material)) as Material;
        //Grab CiDyGraph Materials for the Intersection and Roads.
        //Intersection
        definedIntersectionMat = graph.intersectionMaterial;
        //Road
        definedRoadMaterial = graph.roadMaterial;
        //Update Graph Visual State and References to CurRoad etc
        UpdateState ();
	}
    //SceneView MovementToggle
    //[Range(0.1f, 2f), Tooltip("If Use Camera is On, CiDy scene view interaction will stop.")]
    public bool useSceneCamera = false;
	public bool usingSceneCam = false;//Dynamic State Tester
	//Visual Toggles
	public bool showCells = true;
	public bool showNodes = true;
    //Terrain Variables
    public bool grabVegitation = false;//
	public bool grabHeights = false;
	public bool resetHeights = false;
	public bool blendTerrain = false;
    public bool updateCityGraph = false;
    //public bool generateSideWalkPedestrians = false;//Weather or not we add Population Module.
    public bool generateRoad = false;
    public int userRoadSegmentLength = 6;
    //Clear Graph Bool
    public bool clearGraph = false;
    //Node Variables
    //Grab Node Material Resources
    public Material nodeMaterial;// = Resources.Load ("CiDyResources/NodeMaterial", typeof(Material)) as Material;
	//Grab Active Node Material Resource
	public Material activeMaterial;// = Resources.Load ("CiDyResources/ActiveMaterial", typeof(Material)) as Material;
	//Cell Selection Material
	public Material cellSelectionMaterial;// = Resources.Load ("CiDyResources/CellSelection", typeof(Material)) as Material;
    public Material cellMaterial;// = Resources.Load ("CiDyResources/CellTransparency", typeof(Material)) as Material;
    //Road
    public Material definedRoadMaterial;//The Material that the user wants to be put onto any created road meshes.
    public Material roadMaterial;//The Material that is on the Current Selected Road.
    //Intersection Material
    public Material definedIntersectionMat;//The Material that the user wants to be put onto any created Intersection meshes.
    public Material intersectionMaterial;//The Material that the user wants to be put onto any created intersection meshes.
    public bool replaceAllMaterials = false;//When this is true. We will Go through the CiDy System and replace the Materials applied to the Roads and Intersections.

    public int maxNodeScale = 200;
	public int nodeScale = 50;
	public int curNodeScale = 50;
	//Road Creation variables
	public float roadWidth = 12f;
	public int roadSegmentLength = 6;//The Mesh Resolution/Mesh Quadrilateral Segment Length.(Decreasing Length will increase GPU Cost)
	private int flattenAmount = 8;//How many points from the ends we will flatten for end Smoothing(Bezier)
    public bool uvsRoadSet = true;//If false then we will stretch Roads UV's to match a Up Right Road Texture Method
    //Road Edit Variables
    public GameObject roadStopSign;
	public bool regenerateRoad = false;
	//Cell Edit Variables
	public float sideWalkWidth = 8f;//The Side Walk Width(Inset from Roads);
	public float sideWalkHeight = 0.25f;//The Height of the SideWalk
	public float lotWidth = 50f;//Cell Building Lots
	public float lotDepth = 60f;//Cell Building Lots
	public float lotInset = 0f;//Amount the Cell Insets there Lots.
	public bool lotsUseRoadHeight = false;
	public bool autoFillBuildings = true;
    public bool huddleBuildings = true;
	public bool maximizeLotSpace = true;
    public bool createSideWalks = true;
	public bool contourSideWalkLights = false;
	public bool contourSideWalkClutter = false;
    public bool randomizeClutterPlacement = false;
    public bool regenerateCell = false;//Toggle used to Regenerate Cell Parameters
    public bool usePrefabBuildings = true;//Use Prefab Buildings for Cell Creation
	public float pathClutterSpacing;
	public float pathLightSpacing;
	public GameObject streetLight;
	//Current Selected Objects
	public CiDyNode curData;//Cur Node Being Edited.
	public CiDyRoad curRoad;//Cur Road that the user has Selected.
	public CiDyCell curCell;//Track What Cell if any we are working with.
	public float roadEditRadius = 20;
	public List<Vector3> roadLines = new List<Vector3> (0);
	public List<GameObject> roadPoints = new List<GameObject>(0);//The Visualized Road Control Points.
	public List<Vector3> cPoints = new List<Vector3>();

	public enum Options { 
		Node = 0, 
		Road = 1, 
		Cell = 2
	}
	public Options selected = Options.Node;
	public Options curSelected = Options.Node;
	public bool enterEditMode = false;
	public float m_time = 0.0f;
	//GUI ScrollPos
	public Vector2 scrollPos = Vector2.zero;

    void OnInspectorUpdate()
    {
        Repaint();
    }

    void OnGUI()
	{
        //Check for Undo or Redo
        /*Event e = Event.current;
		if(e.isKey){
			if(e.control){
				if(e.keyCode == KeyCode.Z){
					Debug.Log("Undo");
				} else if(e.keyCode == KeyCode.Y){
					Debug.Log("Redo");
				}
			} 
		}*/
        /*if (progress < secs)
            EditorUtility.DisplayProgressBar("Simple Progress Bar", "Shows a progress bar for the given seconds", progress / secs);
        else
            EditorUtility.ClearProgressBar();
        progress = (float)(EditorApplication.timeSinceStartup - startVal);*/

        //Handle Display Logic Changes
        if (displayingProgressBar)
        {
            //Update Display
            UpdateDisplay("Blending Terrain:", "Blending!", (1.0f - (graph.curProblems / graph.totalProblems)));

        }
        EditorGUI.BeginChangeCheck();
		//Integrete Builtin Undo System for Variable changes
		Undo.RecordObject (this, "Changed Settings");
		//EditorApplication.playmodeStateChanged += ModeChanged;
		if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying && !enterEditMode) {
			//Debug.Log("Exiting playmode.");
			enterEditMode = true;
		} else if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying){
			//Debug.Log("Entering PlayMode");
			UpdateState();
		}
		GUILayout.Label ("Visual Settings", EditorStyles.boldLabel);
		useSceneCamera = EditorGUILayout.Toggle ("Use Scene Camera", useSceneCamera);
		clearGraph = EditorGUILayout.Toggle ("Clear Graph", clearGraph);
		if(clearGraph && EditorUtility.DisplayDialog("Clear Graph?", "Are you sure you want to Clear the CiDyGraph?", "Yes","No")){
			clearGraph = false;
			UpdateState();
            EditorCoroutine.Start(graph.ClearGraph());
		} else if(clearGraph){
			clearGraph = false;
		}
		showCells = EditorGUILayout.Toggle ("Show Cells", showCells);
		showNodes = EditorGUILayout.Toggle ("Show Nodes", showNodes);
		GUILayout.BeginHorizontal();
		GUILayout.Label("MaxNodeScale:");
		maxNodeScale = EditorGUILayout.IntField (maxNodeScale, GUILayout.Width (50));
		GUILayout.EndHorizontal ();
		nodeScale = EditorGUILayout.IntSlider("NodeScale: ", nodeScale,1,maxNodeScale);//,GUILayout.Width(50));// (nodeScale,GUILayout.Width(50));
		GUILayout.Label ("Terrain Settings", EditorStyles.boldLabel);
        grabVegitation = EditorGUILayout.Toggle("Grab Vegitation", grabVegitation);
        if (grabVegitation && EditorUtility.DisplayDialog("Store Current Terrain Vegitation?", "Are you sure you want to Store current Terrain Data?", "Yes", "No"))
        {
            grabVegitation = false;
            graph.GrabTerrainVegitation();
        }
        else if (grabVegitation)
        {
            grabVegitation = false;
        }
        grabHeights = EditorGUILayout.Toggle ("Grab Heights", grabHeights);
		if(grabHeights && EditorUtility.DisplayDialog("Store Current TerrainHeights?", "Are you sure you want to Store current Terrain Data?", "Yes", "No")){
			grabHeights = false;
			graph.GrabOriginalHeights();
		} else if(grabHeights){
			grabHeights = false;
		}
		//Only Show Reset Heights When Cell Has Heights to Reset To
		if(graph.originalHeights.Length > 0){
			resetHeights = EditorGUILayout.Toggle ("Reset Terrain Height & Vegetation Data", resetHeights);
			if(resetHeights && EditorUtility.DisplayDialog("Reset Current Terrain To Stored Heights & Vegetation?", "Are you sure you want to Reset Terrain?","Yes","No")){
				resetHeights = false;
				graph.RestoreOriginalTerrainHeights();
			} else if(resetHeights){
				resetHeights = false;
			}
		}
		blendTerrain = EditorGUILayout.Toggle ("Blend Terrain", blendTerrain);
		if(blendTerrain && EditorUtility.DisplayDialog("Blend Terrain To CiDyGraph?","Are you sure you want to Blend Terrain to CiDyGraph?", "Yes","No")){
			blendTerrain = false;

            EditorCoroutine.Start(BlendTerrain());
		} else if(blendTerrain){
			blendTerrain = false;
		}
        //Allow the User to One Click to Update All the Roads to new terrain Heights.
        updateCityGraph = EditorGUILayout.Toggle("Update City to Terrain", blendTerrain);
        if (updateCityGraph && EditorUtility.DisplayDialog("Blend Terrain To CiDyGraph?", "Are you sure you want to Update City Graph to Terrain?", "Yes", "No"))
        {
            updateCityGraph = false;
            EditorCoroutine.Start(UpdateCityToTerrain());
        }
        else if (updateCityGraph)
        {
            updateCityGraph = false;
        }
        //Draw Population Module
        /*generateSideWalkPedestrians = EditorGUILayout.Toggle("Generate Pedestrians", generateSideWalkPedestrians);

        if (generateSideWalkPedestrians && EditorUtility.DisplayDialog("Generate Population for your CiDy's Cells?", "Are you sure you want to Generate all Population?", "Yes", "No"))
        {
            generateSideWalkPedestrians = false;

            EditorCoroutine.Start(GenerateSideWalkPopulation());
        }
        else if (generateSideWalkPedestrians)
        {
            generateSideWalkPedestrians = false;
        }*/

        GUILayout.Label ("Building Type", EditorStyles.boldLabel);
		graph.buildingType = (CiDyGraph.BuildingType)EditorGUILayout.EnumPopup("BuildingType: ",graph.buildingType);

        GUILayout.Label("Desired Intersection Material", EditorStyles.boldLabel);
        //EditorGUILayout.BeginHorizontal();
        definedIntersectionMat = (Material)EditorGUILayout.ObjectField(definedIntersectionMat, typeof(Material), false, GUILayout.Width(150));
        GUILayout.Label("Desired Road Material", EditorStyles.boldLabel);
        definedRoadMaterial = (Material)EditorGUILayout.ObjectField(definedRoadMaterial, typeof(Material), false, GUILayout.Width(150));
        //Allow the User to One Click to Update All the Roads to new terrain Heights.
        replaceAllMaterials = EditorGUILayout.Toggle("Replace All City Materials", replaceAllMaterials);
        if (replaceAllMaterials && EditorUtility.DisplayDialog("Replace All Materials Applied to CiDyGraph?", "Are you sure you want to Replace ALL Materials of City Graph?", "Yes", "No"))
        {
            replaceAllMaterials = false;
            EditorCoroutine.Start(ReplaceAllMaterials());
        }
        else if (replaceAllMaterials)
        {
            replaceAllMaterials = false;
        }
        //Check for Graph Change 
        if(graph != null)
        {
            if (definedIntersectionMat != graph.intersectionMaterial) {
                //User has changed Material
                graph.intersectionMaterial = definedIntersectionMat;
            }
            if (definedRoadMaterial != graph.roadMaterial) {
                graph.roadMaterial = definedRoadMaterial;
            }
        }
        //EditorGUILayout.EndHorizontal();
		GUILayout.Label ("Placement Settings", EditorStyles.boldLabel);
		//groupEnabled = EditorGUILayout.BeginToggleGroup ("Pattern Settings", groupEnabled);
		selected = (Options)EditorGUILayout.EnumPopup("Selection: ",selected);
		if(selected == Options.Node){
			//Node
			GUILayout.BeginHorizontal();
			GUILayout.Label("RoadWidth:");
			roadWidth = EditorGUILayout.FloatField(roadWidth,GUILayout.Width(50));
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("RoadSegmentLength:");
			roadSegmentLength = EditorGUILayout.IntField((int)Mathf.Clamp(roadSegmentLength,2,Mathf.Infinity),GUILayout.Width(50));
			GUILayout.EndHorizontal();
			/*GUILayout.BeginHorizontal();
			GUILayout.Label("FlattenAmount:");
			flattenAmount = EditorGUILayout.IntField(flattenAmount,GUILayout.Width(50));
			GUILayout.EndHorizontal();*/
		} else if(selected == Options.Road){
            //Road
            if (curRoad)
            {
                regenerateRoad = EditorGUILayout.Toggle("Regenerate Road", regenerateRoad);
                roadStopSign = (GameObject)EditorGUILayout.ObjectField(roadStopSign, typeof(GameObject), false, GUILayout.Width(150));
                //Show Road Material
                GUILayout.Label("Road Material", EditorStyles.boldLabel);
                roadMaterial = (Material)EditorGUILayout.ObjectField(roadMaterial, typeof(Material), false, GUILayout.Width(150));
                GUILayout.BeginHorizontal();
                GUILayout.Label("RoadWidth:");
                roadWidth = EditorGUILayout.FloatField(roadWidth, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("RoadSegmentLength:");
                roadSegmentLength = EditorGUILayout.IntField((int)Mathf.Clamp(roadSegmentLength, 2, Mathf.Infinity), GUILayout.Width(50));
                GUILayout.EndHorizontal();
                /*GUILayout.BeginHorizontal();
                GUILayout.Label("FlattenAmount:");
                flattenAmount = EditorGUILayout.IntField(flattenAmount, GUILayout.Width(50));
                GUILayout.EndHorizontal();*/
                GUILayout.BeginHorizontal();
                uvsRoadSet = EditorGUILayout.Toggle("Stretch UV's for Road Texture:", uvsRoadSet);
                GUILayout.EndHorizontal();
            }
            else {
                /*if (graph.userDefinedRoadPnts.Count > 0) {
                    //Allow the User to One Click to Update All the Roads to new terrain Heights.
                    generateRoad = EditorGUILayout.Toggle("Generate User Road", generateRoad);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Road Segment Length:");
                    userRoadSegmentLength = EditorGUILayout.IntField((int)Mathf.Clamp(userRoadSegmentLength, 2, Mathf.Infinity), GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                    if (generateRoad)
                    {
                        generateRoad = false;
                        //Turn User Defined Road Pnts. Into a Road. :)
                        graph.CreateRoadFromKnots(graph.userDefinedRoadPnts.ToArray(), roadWidth, nodeScale, userRoadSegmentLength, flattenAmount);
                        graph.ClearUserDefinedRoadPoints();
                    }
                }*/
            }
		} else if(selected == Options.Cell){
			//Cell
			if(curCell){
				//Expose Variables for this cell.GUILayout.Label ("Building Type", EditorStyles.boldLabel);
				curCell.buildingType = (CiDyCell.BuildingType)EditorGUILayout.EnumPopup("BuildingType: ",curCell.buildingType);
				//Regenerate Button
				regenerateCell = EditorGUILayout.Toggle("Regenerate Cell", regenerateCell);
                //Procedural Buildings are Not Ready for public use yet.
                usePrefabBuildings = EditorGUILayout.Toggle("Use Prefab Buildings", usePrefabBuildings);
                if (usePrefabBuildings)
                {
                    //AutoFillPrefabBuildings
                    autoFillBuildings = EditorGUILayout.Toggle("AutoFillBuildings", autoFillBuildings);
                    //Group Buildings in a Lot as tight as possible
                    huddleBuildings = EditorGUILayout.Toggle("Huddle Buildings", huddleBuildings);
                    if (!huddleBuildings)
                    {
                        //Match Lots with Buildings that Use maximum Space of Lot
                        maximizeLotSpace = EditorGUILayout.Toggle("MaximizeLotSpace", maximizeLotSpace);
                        //Group Buildings in a Lot as tight as possible
                        huddleBuildings = EditorGUILayout.Toggle("Huddle Buildings", huddleBuildings);
                    }
                }
                if (!huddleBuildings)
                {
                    //Match Lots with Buildings that Use maximum Space of Lot
                    maximizeLotSpace = EditorGUILayout.Toggle("MaximizeLotSpace", maximizeLotSpace);
                }
                createSideWalks = EditorGUILayout.Toggle("Create Side Walks", createSideWalks);
                if (createSideWalks){
                    //Editable Variables
                    //SideWalk
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("SideWalkWidth:");
                    sideWalkWidth = EditorGUILayout.FloatField(Mathf.Clamp(sideWalkWidth, 1, Mathf.Infinity), GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("SideWalkHeight:");
                    sideWalkHeight = EditorGUILayout.FloatField(Mathf.Clamp(sideWalkHeight, 0.1f, Mathf.Infinity), GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }
				//Lot Dimensions
				GUILayout.BeginHorizontal();
				GUILayout.Label("LotWidth:");
				lotWidth = EditorGUILayout.FloatField(lotWidth,GUILayout.Width(50));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("LotDepth:");
				lotDepth = EditorGUILayout.FloatField(lotDepth,GUILayout.Width(50));
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("LotInset:");
				lotInset = EditorGUILayout.FloatField(Mathf.Clamp(lotInset,0,Mathf.Infinity),GUILayout.Width(50));
				GUILayout.EndHorizontal();
				lotsUseRoadHeight = EditorGUILayout.Toggle ("LotsUseRoadHeight", lotsUseRoadHeight);
				//Street Lights/Clutter
				contourSideWalkLights = EditorGUILayout.Toggle ("ContourSideWalkLights", contourSideWalkLights);
				GUILayout.BeginHorizontal();
				GUILayout.Label("Light Spacing:");
				pathLightSpacing = EditorGUILayout.FloatField(pathLightSpacing,GUILayout.Width(50));
				GUILayout.EndHorizontal();
				streetLight = (GameObject)EditorGUILayout.ObjectField("Street Light", streetLight,typeof(GameObject), false, GUILayout.Width(275));
				contourSideWalkClutter = EditorGUILayout.Toggle ("Contour SideWalk Clutter", contourSideWalkClutter);
                randomizeClutterPlacement = EditorGUILayout.Toggle("Randomize Clutter Placement", randomizeClutterPlacement);
                GUILayout.BeginHorizontal();
				GUILayout.Label("Clutter Spacing:");
				pathClutterSpacing = EditorGUILayout.FloatField(pathClutterSpacing,GUILayout.Width(50));
				GUILayout.EndHorizontal();
				scrollPos = GUILayout.BeginScrollView(scrollPos,true,true,GUILayout.Width(325),GUILayout.Height(100));
				//Handle Street Clutter Array
				SerializedObject so = new SerializedObject(curCell);
				SerializedProperty clutter = so.FindProperty("clutterObjects");
				EditorGUILayout.PropertyField(clutter,true);
				//Handle Building Array
				SerializedProperty buildings = so.FindProperty("prefabBuildings");
				EditorGUILayout.PropertyField(buildings,true);
				//Apply Changes to SerializedObject
				so.ApplyModifiedProperties();
				//GUILayout.EndArea();
				GUILayout.EndScrollView();
			}
		}
		if (EditorGUI.EndChangeCheck ()) {
			//Debug.Log("Value Changed");
			EditorUtility.SetDirty(this);
		}
		//color field
		//color = EditorGUILayout.ColorField(color, GUILayout.Width(200));
		//Repaint Windows
		SceneView.RepaintAll ();
	}

    //This function is called by the Graph when there Is an Editor Utility Display
    public void UpdateDisplay(string title, string info, float progress) {
        
        if (!displayingProgressBar) {
            displayingProgressBar = true;
        }
        //Display Desired Progress Bar State
        EditorUtility.DisplayProgressBar(title, info, progress);
    }

    public void CloseDisplayProgress() {
        if (displayingProgressBar)
        {
            Debug.Log("Progress End");
            displayingProgressBar = false;
        }
        //End Progress
        EditorUtility.ClearProgressBar();
    }

    Thread blendThread;
	//float blendProgress = 0;
    //This will blend the Graph to its Terrain if Applicable.
    IEnumerator BlendTerrain(){
        if (graph.terrain == null) {
            Debug.LogError("No Activ Terrain to Blend referenced by CiDyGraph");
            yield break;
        }
        //Blend/Cut Grass and Trees
        //UpdateDisplay("Blending City With Terrain: ", ": Blending", 0);
        //Check to See if the User has stored the Original Heights in the Graph?
        if (graph.originalHeights.Length == 0){
			Debug.Log("Graph doesn't have any stored Heights. Storing Heights Before Blend");
			graph.GrabOriginalHeights();
		}
        //Restore Terrain Heights
        //graph.RestoreOriginalTerrainHeights();
        //graph.UpdateTerrainDetails();
        EditorCoroutine.Start(graph.UpdateTerrainDetails());
        //End Coroutine
        yield break;
    }

    //This Function will Replace all Materials of the Graph for Road and Intersections to the Current Graph Selected ones.
    IEnumerator ReplaceAllMaterials() {
        EditorCoroutine.Start(graph.UpdateAllMaterials());
        yield break;
    }

    //Function will Update all the Roads. Allowing them to Replot to the Stored Terrain Heights.
    IEnumerator UpdateCityToTerrain() {
        //Iterate through Roads and Replot each Road. This will also cascade down the line and Update everything else.
        EditorCoroutine.Start(graph.UpdateCityGraph());
        yield break;
    }

    //Generate SideWalk Population
    IEnumerator GenerateSideWalkPopulation() {
        Debug.Log("Generate SideWalk Population");
        EditorCoroutine.Start(graph.GenerateSideWalkPopulation());
        Debug.Log("Yielded SideWalk");
        yield break;
    }


    // Window has been selected
    void OnFocus() {
		// Remove delegate listener if it has previously
		// been assigned.
		SceneView.duringSceneGui -= this.OnSceneGUI;
		// Add (or re-add) the delegate.
		SceneView.duringSceneGui += this.OnSceneGUI;

		//Undo.undoRedoPerformed -= this.CiDyUndoRedoCallback;
		//Undo.undoRedoPerformed += this.CiDyUndoRedoCallback;
	}
	
	/*public void CiDyUndoRedoCallback()
	{
		Debug.Log ("Undo/Redo Performed");
		if(graph){
			graph.UpdateGraphFromUndo();
			//Now update Cycles
			UpdateSideWalk();
		}
		//Call to Graph to update information.
		//Repaint Scene View
		//SceneView.RepaintAll();
	}*/

	void OnDestroy() {
		UpdateState ();
		// When the window is destroyed, remove the delegate
		// so that it will no longer do any drawing.
		//Undo.undoRedoPerformed -= this.CiDyUndoRedoCallback;
		SceneView.duringSceneGui -= this.OnSceneGUI;
	}
	
	void OnSceneGUI(SceneView sceneView) {
        //Grab Current Event.
		Event e = Event.current;

        // Do your drawing here using Handles.
        // Do your drawing here using GUI.
        if (e.type == EventType.Repaint)
        {
            Handles.color = Color.yellow;
            //Draw Road Points
            if (roadPoints.Count > 0)
            {
                for (int i = 0; i < roadPoints.Count; i++)
                {
                    if (i == 0 || i == roadPoints.Count - 1)
                    {
                        continue;
                    }
                    //Create Shape around Point and Draw it.
                    List<Vector3> drawPoints = CiDyUtils.PlotCircle(roadPoints[i].transform.position, roadEditRadius, 3);
                    for (int j = 0; j < drawPoints.Count; j++)
                    {
                        Vector3 pointPos = drawPoints[j];
                        Vector3 nxtPoint;
                        if (j == drawPoints.Count - 1)
                        {
                            nxtPoint = drawPoints[0];
                        }
                        else
                        {
                            nxtPoint = drawPoints[j + 1];
                        }
                        //Debug.DrawLine(pointPos,nxtPoint,Color.yellow);
                        Handles.DrawLine(pointPos, nxtPoint);
                    }
                }
            }
            if (roadLines.Count > 0)
            {
                for (int i = 0; i < roadLines.Count - 1; i++)
                {
                    Vector3 pointPos = roadLines[i];
                    Vector3 nxtPoint = roadLines[i + 1];
                    //Debug.DrawLine(pointPos,nxtPoint,Color.yellow);
                    Handles.DrawLine(pointPos, nxtPoint);
                }
            }
            //Draw CurCell Lots if Applicable
            if (curCell)
            {
                //Draw SubLots
                for (int i = 0; i < curCell.lots.Count; i++)
                {
                    for (int j = 0; j < curCell.lots[i].vectorList.Count; j++)
                    {
                        Vector3 p0 = curCell.lots[i].vectorList[j];
                        Vector3 p1;
                        if (j == curCell.lots[i].vectorList.Count - 1)
                        {
                            p1 = curCell.lots[i].vectorList[0];
                        }
                        else
                        {
                            p1 = curCell.lots[i].vectorList[j + 1];
                        }
                        Handles.DrawLine(p0, p1);
                    }
                }
            }
            //Draw User Defined Road Points.
            /*if (graph.userDefinedRoadPnts.Count > 0)
            {
                Handles.color = Color.yellow;
                for (int i = 0; i < graph.userDefinedRoadPnts.Count - 1; i++)
                {
                    //Debug.DrawLine(pointPos,nxtPoint,Color.yellow);
                    Handles.DrawLine(graph.userDefinedRoadPnts[i], graph.userDefinedRoadPnts[i + 1]);
                }
                //Draw its Resulting Bezier
                Handles.color = Color.blue;
                for (int i = 0; i < graph.userDefinedRoad.Count - 1; i++)
                {
                    //Debug.DrawLine(pointPos,nxtPoint,Color.yellow);
                    Handles.DrawLine(graph.userDefinedRoad[i], graph.userDefinedRoad[i + 1]);
                }
            }*/
        }
        /*Handles.BeginGUI();
		Handles.EndGUI();*/

        //Draw Handles Etc before UseScene Camera Logic
        if (e.alt || useSceneCamera)
        {
			return;
		}
		int controlID = GUIUtility.GetControlID(0, FocusType.Passive);
		//Listen For Mouse Position in Scene View.
		if (e.isMouse)
		{
            Vector3 mousePos = Event.current.mousePosition;
            Rect rect = GetWindow<SceneView>().camera.pixelRect;

            if (mousePos.x > rect.x && mousePos.y > rect.y && mousePos.x < rect.width && mousePos.y < rect.height)
            {
                //Debug.Log(mousePos);
                //Inside Screen
                Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePos);
                RaycastHit hit;
                if (e.button == 1)
                {
                    //Take Control from Unity Standard SceneView
                    //GUIUtility.hotControl = controlID;
                    //If Node Interaction is Active
                    if (selected == Options.Node)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, nodeMask))
                            {
                                string hitTag = hit.collider.tag;
                                if (hitTag == terrainTag || hitTag == cellTag)
                                {
                                    CiDyNode newData = graph.NewMasterNode(hit.point, nodeScale);
                                    if (newData == null)
                                    {
                                        Debug.Log("Cannot Place New Node at that position.");
                                        return;
                                    }
                                    //This is an acceptable place for a new Node.
                                    if (curData != null)
                                    {
                                        //Connect Nodes creating an Edge in the Graph.
                                        if (graph.ConnectNodes(curData, newData, roadWidth, roadSegmentLength, flattenAmount))
                                        {
                                            //Debug.Log("Connecting Nodes");
                                            //Update curNode to new Node
                                            UpdateCurNode(newData);
                                            if (!showNodes)
                                            {
                                                ActivateVisuals();
                                            }
                                            if (graph.masterGraph.Count > 2)
                                            {
                                                UpdateCiDy();
                                            }
                                        }
                                        else
                                        {
                                            Debug.Log("Failed Graph Test Cannot Add Connection");
                                            //Destroy new Data
                                            graph.DestroyMasterNode(newData);
                                            //Special Case We know that we can wind back the count. Manually
                                            graph.nodeCount--;
                                        }
                                    }
                                    else
                                    {
                                        //Update CurNode to Equal New Node
                                        NewCurNode(newData);
                                        if (!showNodes)
                                        {
                                            ActivateVisuals();
                                        }
                                    }
                                }
                                else if (hitTag == nodeTag)
                                {
                                    //The user is trying to select or connect to an exsiting Node.
                                    //Grab this node Object
                                    GameObject tmpNode = hit.collider.transform.parent.gameObject;
                                    CiDyNode newData = graph.masterGraph.Find(x => x.name == tmpNode.name);

                                    //Debug.Log(newData.name);
                                    //Do they have a connecting node selected?
                                    if (curData != null)
                                    {
                                        if (tmpNode.name == curData.name)
                                        {
                                            //Reselected our CurNode do Nothing.
                                            return;
                                        }
                                        //Connect Nodes.
                                        if (!graph.ConnectNodes(curData, newData, roadWidth, roadSegmentLength, flattenAmount))
                                        {
                                            Debug.Log("Couldn't Make Connection In Graph");
                                        }
                                        else
                                        {
                                            //Debug.Log("Connected Nodes, Update Sidewalk");
                                            if (graph.masterGraph.Count > 2)
                                            {
                                                UpdateCiDy();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //We do not have a Node selected yet. Lets make this node our curNode.
                                        UpdateCurNode(newData);
                                    }
                                }
                            }
                        }
                    }
                    else if (selected == Options.Road)
                    {
                        //Drawing Visual Aids
                        if (curRoad)
                        {
                            Vector2 screenPos = Event.current.mousePosition;
                            userRect = new Rect(screenPos.x - userRect.width / 2, screenPos.y - userRect.height / 2, userRect.width, userRect.height);
                            if (e.type == EventType.MouseDown)
                            {
                                //Test area round mouse point. curEvent.mousePosition
                                FindSelectedObjects();
                            }
                            else if (e.type == EventType.MouseUp)
                            {
                                //User wants to select a Road CP Point.
                                //Clear selectedPoint if we have one
                                if (selectedPoint)
                                {
                                    //Debug.Log("Released MoustButton");
                                    curRoad.cpPoints[pointInt] = selectedPoint.transform.position;
                                    curRoad.ReplotRoad(curRoad.cpPoints);
                                    //curRoad.UpdateRoadNodes();
                                    selectedPoint = null;
                                    //Update Graph if needed
                                    if (graph.cells.Count > 0)
                                    {
                                        graph.UpdateRoadCell(curRoad);
                                    }
                                }
                            }
                            else if (e.type == EventType.MouseDrag)
                            {
                                //Move selected Point if not null
                                if (selectedPoint)
                                {
                                    //Debug.Log("Holding Mouse Button");
                                    if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, roadMask))
                                    {
                                        selectedPoint.transform.position = hit.point;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Switch to Node Selection
                            selected = Options.Node;
                            //If No Road is currently Selected. We Assume the User wants to Click out a road.
                            //Where did they Click?
                            /*if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, roadMask))
                            {
                                //Hit
                                Debug.Log("Hit: "+hit.collider.tag);

                                //Now we want to store a reference to the points. The User is creating.
                                //If not a duplicate. Add to list.
                                graph.AddUserRoadPoints(hit.point, userRoadSegmentLength);

                            }*/
                        }
                    }
                    else if (selected == Options.Cell)
                    {

                        if (curCell == null)
                        {
                            selected = Options.Node;
                        }
                    }
                }
                else if (e.button == 0)
                {
                    //Take Control from Unity Standard SceneView
                    //GUIUtility.hotControl = controlID;
                    //Take an Extra Step here to determine What the User has just Clicked On
                    if (e.type == EventType.MouseDown)
                    {
                        //Debug.Log("User Pressed Left MB");
                        if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, nodeMask) && hit.collider.tag == nodeTag)
                        {
                            //Debug.Log("Clicked on Node");
                            //Clicked on a Node?
                            selected = Options.Node;
                        }
                        else if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, roadMask2))
                        {
                            //Debug.Log("Clicked on Road");
                            //Clicked on Road?
                            selected = Options.Road;
                        }
                        else if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, cellMask))
                        {
                            //Debug.Log("Clicked on Cell");
                            //Clicked on Cell?
                            selected = Options.Cell;
                        }
                    }

                    if (selected == Options.Node)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            //Take Control from Unity Standard SceneView
                            GUIUtility.hotControl = controlID;
                            if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, nodeMask))
                            {
                                string hitTag = hit.collider.tag;
                                //Debug.Log("User Pressed LftMSBtn hit= "+hitTag);
                                if (hitTag == nodeTag)
                                {
                                    //We have hit a node.
                                    //if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift)){
                                    if (e.control)
                                    {
                                        //User Wishes to Destroy this Node and All its Edges.
                                        NewCurNode(graph.masterGraph.Find(x => x.name == hit.collider.transform.parent.name));
                                        //Does the User wish to Destroy the Node?
                                        graph.DestroyMasterNode(curData);
                                        curData = null;
                                        //Update Graph
                                        UpdateCiDy();
                                    }
                                    else
                                    {
                                        //The user wishes to select this node as the CurNode. or deselect it
                                        //User Wishes to Destroy this Node and All its Edges.
                                        if (curData != null)
                                        {
                                            if (curData.name == hit.collider.transform.parent.name)
                                            {
                                                //Release this node
                                                curData.ChangeMaterial(nodeMaterial);
                                                curData = null;
                                            }
                                            else
                                            {
                                                //curData = graph.masterGraph.Find(x=> x.name == hit.collider.transform.parent.name);
                                                UpdateCurNode(graph.masterGraph.Find(x => x.name == hit.collider.transform.parent.name));
                                            }
                                        }
                                        else
                                        {
                                            if (graph == null)
                                            {
                                                Debug.Log("No Graph");
                                            }
                                            UpdateCurNode(graph.masterGraph.Find(x => x.name == hit.collider.transform.parent.name));
                                        }
                                    }
                                }
                                else
                                {
                                    //If we have a curNode then Move it to the New Position.
                                    if (curData != null && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                                    {
                                        //Debug.Log("Trying to Move Node "+curData.name);
                                        //Move Node to New Position if graph will allow.
                                        if (!graph.MovedNode(ref curData, hit.point))
                                        {
                                            Debug.Log("Could Not Move Node New Position Conflicts Graph");
                                        }
                                    }
                                }
                            }
                        }
                        else if (e.type == EventType.MouseUp)
                        {
                            //Take Control from Unity Standard SceneView
                            GUIUtility.hotControl = 0;
                        }
                    }
                    else if (selected == Options.Road)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            //Take Control from Unity Standard SceneView
                            GUIUtility.hotControl = controlID;
                            //We are Only Looking for Roads
                            if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, roadMask2))
                            {
                                string hitTag = hit.collider.tag;
                                if (hitTag == roadTag)
                                {
                                    //Lets handle the situations that are needed to better handle Roads.
                                    if (e.control)
                                    {
                                        if (curRoad)
                                        {
                                            DeselectRoad();
                                        }
                                        //User wants to destroy this Road from the Graph.
                                        //Debug.Log("Destroying Road "+hit.collider.name);
                                        graph.DestroyRoad(hit.collider.gameObject.name);
                                        //Now Update Graph
                                        UpdateCiDy();
                                    }
                                    else
                                    {
                                        //User wants to select this road as our curRoad.
                                        CiDyRoad tmpRoad = (CiDyRoad)hit.collider.GetComponent<CiDyRoad>();
                                        //See if we are trying to deselect a road.
                                        if (curRoad)
                                        {
                                            if (curRoad.name == tmpRoad.name)
                                            {
                                                //We have a selected node. Change its material back to what it was.
                                                DeselectRoad();
                                            }
                                            else
                                            {
                                                //We are just changing our pick
                                                DeselectRoad();
                                                SelectRoad(tmpRoad);
                                            }
                                        }
                                        else
                                        {
                                            //We do not have one just grab it. :)
                                            SelectRoad(tmpRoad);
                                        }
                                    }
                                }
                            }
                        }
                        else if (e.type == EventType.MouseUp)
                        {
                            //Take Control from Unity Standard SceneView
                            GUIUtility.hotControl = 0;
                        }
                    }
                    else if (selected == Options.Cell)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            //Take Control from Unity Standard SceneView
                            GUIUtility.hotControl = controlID;
                            //we have to shoot for the scenario where two cells are competing for clicking space.
                            if (Physics.Raycast(worldRay, out hit, Mathf.Infinity, cellMask))
                            {
                                //We only have one hit
                                string hitTag = hit.collider.tag;
                                if (hitTag == cellTag)
                                {
                                    if (curCell != null)
                                    {
                                        if (hit.collider.name == curCell.name)
                                        {
                                            //Deselecting curCell
                                            //Debug.Log("De-Selecting Cell ");
                                            ReleaseCurCell();
                                            return;
                                        }
                                        else
                                        {
                                            //Debug.Log("Only One hitCell Selecting it ");
                                            ReleaseCurCell();
                                        }
                                    }
                                    //Grabbing a Cell. Lets turn on the CellCanvas
                                    SetCurCell(hit.collider.GetComponent<CiDyCell>());
                                    //Debug.Log("shortest cycle "+shortestCycle+" Cell "+curCell.name);
                                }
                            }
                        }
                    }
                }
            }
			e.Use();
		}
	}

	public Rect userRect= new Rect(0,0,50,50);
	public List<GameObject> selectedObjects = new List<GameObject>();//Dynamic list for the user interface.
	public GameObject selectedPoint;
	public int pointInt = 0;

	void FindSelectedObjects(){
		//Debug.Log ("Find Selected Objects() StoredTransform Cnt: "+roadPoints.Count);
		//Clear last Selected
		if(selectedObjects.Count > 0){
			selectedObjects.Clear();
		}
		for(int i = 0;i<roadPoints.Count;i++){
			//Skip first and last. We do not want these points manipulated
			if(i==0 || i == roadPoints.Count-1){
				continue;
			}
			//Is this transform inside the view frustrum the user created?
			//Vector3 screenPos = Camera.main.ViewportToScreenPoint(roadPoints[i].transform.position);
			//Vector3 screenPos = Camera.main.WorldToScreenPoint(roadPoints[i].transform.position);
			Vector2 screenPos = HandleUtility.WorldToGUIPoint(roadPoints[i].transform.position);
			//screenPos.y = Screen.height - screenPos.y;
			//Debug.Log("testing "+roadPoints[i].name+" ScreenPos: "+screenPos);
			if(userRect.Contains(screenPos)){
				//Debug.Log("Its inside User Rect");
				selectedObjects.Add(roadPoints[i]);
				pointInt = i;
			}
		}
		//Set selected Point to bottom of list.
		if(selectedObjects.Count > 0){
			selectedPoint = selectedObjects[0];
			//Debug.Log("Set SelectedObject");
		}
		//isSelecting = false;
	}
	
	//Update CurNode
	void NewCurNode(CiDyNode newNode){
		if(curData!=null){
			curData.ChangeMaterial(nodeMaterial);
		}
		//Debug.Log ("Designer Is Pushing Node to Graph " + newNode.name);
		curData = newNode;
		curData.ChangeMaterial (activeMaterial);
	}
	
	//Change CurData to the GameObject Node Referenced
	void UpdateCurNode(CiDyNode newNode){
		//Do we have a node still selected?
		if(curData != null){
			curData.ChangeMaterial(nodeMaterial);
		}
		//Select the newNode now. :)
		string desiredNode = newNode.name;
		curData = (graph.masterGraph.Find(x=> x.name == desiredNode));
		curData.ChangeMaterial (activeMaterial);
	}

	void SelectRoad(CiDyRoad newRoad){
		curRoad = newRoad;
		//Grab stop Sign if Applicable
		if(curRoad.stopSign){
			roadStopSign = curRoad.stopSign;
		}
		roadWidth = curRoad.width;
		roadSegmentLength = curRoad.segmentLength;
		flattenAmount = curRoad.flattenAmount;
        uvsRoadSet = curRoad.uvsRoadSet;
		curRoad.SelectRoad ();
		//Create the Interactive Points.
		//Grab the newly selected roads origPoints.
		cPoints = new List<Vector3> (curRoad.cpPoints);
		//Iterate through the list and create control points at those positions.
		for(int i=0;i<cPoints.Count;i++){
			CreatePoint(cPoints[i]);
			if(i==0 || i == cPoints.Count-1){
				//Deactive these points.
				roadPoints[roadPoints.Count-1].SetActive(false);
			}
		}
		Repaint ();
	}

	void DeselectRoad(){
		curRoad.DeselectRoad ();
		roadStopSign = null;
		curRoad = null;
		if(roadPoints.Count > 0){
			for(int i = 0;i<roadPoints.Count;i++){
				DestroyImmediate(roadPoints[i]);
			}
			roadPointCount = 0;
			roadPoints.Clear();
			roadLines.Clear();
		}
	}

	void RegenerateRoad(){
        //Debug.Log("Regen Road");
		//This will update the Variables on the Selected CiDyRoad
		//Change Road GameObject
		if(roadStopSign != curRoad.stopSign){
			curRoad.stopSign = roadStopSign;
		} else if(roadStopSign == null){
			curRoad.stopSign = null;
		}
        //Debug.Log("After Stop Sign Regen Road Editor");
        //Update Uvs
        curRoad.uvsRoadSet = uvsRoadSet;
		curRoad.InitilizeRoad (roadWidth, roadSegmentLength, flattenAmount);
		graph.UpdateRoadCell (curRoad);
	}

	//This function will set the new curCell
	void SetCurCell(CiDyCell newCell){
		if(curCell != null){
			ReleaseCurCell();
		}
		
		curCell = newCell;
		//selectionColor.a = 0.8f;
		//curCell.GetComponent<Renderer>().material.SetColor("_Color", selectionColor);
		curCell.GetComponent<Renderer> ().material = cellSelectionMaterial;
		//Update EditorWindow Variables to Reflect this cells cur Variables
		sideWalkWidth = curCell.sideWalkWidth;
		sideWalkHeight = curCell.sideWalkHeight;
		lotWidth = curCell.lotWidth;
		lotDepth = curCell.lotDepth;
		lotInset = curCell.lotInset;
		lotsUseRoadHeight = curCell.lotsUseRoadHeight;
		autoFillBuildings = curCell.autoFillBuildings;
		contourSideWalkLights = curCell.contourSideWalkLights;
		contourSideWalkClutter = curCell.contourSideWalkClutter;
        randomizeClutterPlacement = curCell.randomizeClutterPlacement;
        huddleBuildings = curCell.huddleBuildings;
        maximizeLotSpace = curCell.maximizeLotSpace;
        createSideWalks = curCell.createSideWalks;
		pathLightSpacing = curCell.pathLightSpacing;
		pathClutterSpacing = curCell.pathClutterSpacing;
		//Grab GameObjects if Applicable
		if(curCell.pathLight){
			streetLight = curCell.pathLight;
		}
		Repaint ();
	}
		
	void ReleaseCurCell(){
		//Release the last curCell
		//curCell.GetComponent<Renderer>().material.SetColor("_Color", curCell.RandomColor());
		curCell.GetComponent<Renderer> ().material = cellMaterial;
		curCell = null;
	}

	void RegenerateCell(){
        if (curCell == null) {
            regenerateCell = false;
            return;
        }
		//Update Variables to Match EditorWindow Variables
		curCell.sideWalkWidth = sideWalkWidth;
		curCell.sideWalkHeight = sideWalkHeight;
		curCell.lotWidth = lotWidth;
		curCell.lotDepth = lotDepth;
		curCell.lotInset = lotInset;
		curCell.lotsUseRoadHeight = lotsUseRoadHeight;
		curCell.autoFillBuildings = autoFillBuildings;
		curCell.contourSideWalkLights = contourSideWalkLights;
		curCell.contourSideWalkClutter = contourSideWalkClutter;
        curCell.randomizeClutterPlacement = randomizeClutterPlacement;
        curCell.huddleBuildings = huddleBuildings;
		curCell.maximizeLotSpace = maximizeLotSpace;
        curCell.createSideWalks = createSideWalks;
		curCell.pathLightSpacing = pathLightSpacing;
		curCell.pathClutterSpacing = pathClutterSpacing;
		curCell.pathLight = streetLight;
        curCell.usePrefabBuildings = usePrefabBuildings;
        //Update Cell
        curCell.UpdateCell ();
	}

	private int roadPointCount = 0;
	//This will Create a transform in world Space at MousePosition and add to world points
	void CreatePoint(Vector3 newPos){
		//Make a cube at the point and add its transform to the list of worldPoints.
		GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
		cube.name = "P"+roadPointCount;
		cube.transform.position = newPos;
		cube.layer = LayerMask.NameToLayer("Ignore Raycast");
		//Turn off Cubes Mesh Renderer
		cube.GetComponent<MeshRenderer> ().enabled = false;
		//AddProjector (cube);
		roadPoints.Add(cube);
		roadPointCount++;
	}

	public void UpdateCiDy(){
        //Debug.Log ("Check for Cycles");
        //Inset the Roads
        List<List<Vector3>> boundaryCell = new List<List<Vector3>>(0);
		List<List<CiDyNode>> newCycles = graph.FindCycles(ref boundaryCell);
		//Debug.Log("Final Cycles = "+newCycles.Count);
		if(newCycles.Count > 0){
			graph.UpdateCells(newCycles);
		} else {
			//All Cells Destroyed
			if(graph.cells.Count > 0){
				//Remove the Cells
				graph.ClearCells();
			}
		}
        //Add Boundary Cell
        /*if (boundaryCell.Count > 0) {
            //We have boundary Cells.
            graph.UpdateBoundaryCells(boundaryCell);
        }*/
    }

	//Change Control State(What we can interact with)
	void UpdateState(){
		//Debug.Log ("Change State");
		curSelected = selected;
		//Clear temp stored state info.
		if(curData != null){
			curData.ChangeMaterial(nodeMaterial);
			//Deselect node
			curData = null;
		}
		if(curRoad != null){
			DeselectRoad();
		}
		if(curCell){
			//Clear Cell Data
			ReleaseCurCell();
		}
	}

	//Keep Track Of Dynamic User Edited Variables
	void Update(){

        //Update Changed Values
        if (curNodeScale != nodeScale){
			curNodeScale = nodeScale;
			graph.ChangeNodeScale(nodeScale);
		}
		if(curSelected != selected){
			UpdateState();
		}
		if(usingSceneCam != useSceneCamera){
			UpdateState();
			usingSceneCam = useSceneCamera;
		}
		//Visual Toggle Logic
		if(showCells && !graph.activeCells){
			graph.EnableCellGraphics();
		}
		if(!showCells && graph.activeCells){
			graph.DisableCellGraphics();
		}
		if(showNodes && !graph.activeNodes){
			graph.EnableNodeGraphics();
		}
		if(!showNodes && graph.activeNodes){
			graph.DisableNodeGraphics();
		}
		//Cell Regenerate
		if(regenerateCell){
			RegenerateCell();
			regenerateCell = false;
		}
		//Road Regenerate 
		if(regenerateRoad){
			RegenerateRoad();
			regenerateRoad = false;
		}

		if(roadPoints.Count > 1){
			roadLines = new List<Vector3>();
			for(int i = 0;i<roadPoints.Count;i++){
				roadLines.Add(roadPoints[i].transform.position);
			}
			roadLines = CiDyUtils.CreateBezier(roadLines,roadSegmentLength);
		}

		if (enterEditMode) {
			m_time +=0.01f;
			if(m_time >= 0.75f){
				//Make sure you reset your time
				m_time = 0.0f;
				enterEditMode = false;
				OnEnable();
			}
		}
	}

	//This function will simply turn all all visuals.
	void ActivateVisuals(){
		showNodes = true;
		showCells = true;
	}
}
