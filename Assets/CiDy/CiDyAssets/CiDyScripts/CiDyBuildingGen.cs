using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CiDyBuildingGen : MonoBehaviour {

    //Create Multiples
    public bool instanceArray = false;//Use the Compute Shader Draw Mesh Test

    public Transform[] userPoints;
    public int seed = 0;//The Seed to create a deterministic Building from Seed Value
    [HideInInspector]
    public int curSeed = 0;
	public bool randomize = false;
    public bool recalculate = false;
    public bool clearMesh = false;
    public float allWindowHeight = 4f;
    public float allWindowWidth = 4f;
	public float buildHeight = 25f;
	public float roofWallHeight = 1.6f;
	public float roofWallWidth = 1.6f;
	public float groundFloorHeight = 4f;//2.8 meters is About 9.2ft.
    public float columnWidth = 10;//4 Meters is the desired columnWidth
	public int columns = 3;//How many splits vertically along the length.
	public int floors = 3;//How many splits horizontally along the Width.
    public float edgeColumnWidth = 1.0f;//The Space split in a facade for the Decorative Edges
    public float edgeColumnDepth = 0.6f;//The Depth the column protrudes
	public List<CiDyPanel> fascadePanels = new List<CiDyPanel> (0);
	List<Vector3> buildPrint = new List<Vector3> (0);
	List<Vector3> roofPrint = new List<Vector3>(0);
    public Mesh columnMesh;//The Edge Columns
    public Mesh walls;
    public Mesh doors;
    public Mesh windows;
    public Mesh balconies;
    public Mesh windowBoards;
    public Mesh roofMesh;
	public Material[] materials;//User Knows material[0]=walls,1==roof,2==doors,3==windows, 4==windowBoards,5==balconies, 6 = Edge Columns,
	public GameObject roofHolder;
    public GameObject columnHolder;
	public GameObject wallsHolder;
	public GameObject doorsHolder;
	public GameObject windowsHolder;
	public GameObject balconiesHolder;
	public GameObject windowBoardsHolder;

    void Awake() {
        curSeed = seed;//Update user Set Seed

        if (userPoints != null) {
            for (int i = 0; i < userPoints.Length; i++) {
                buildPrint.Add(userPoints[i].position);
            }
            //Create a Print
            ExtrudePrint(buildPrint, buildHeight, true);
        }
    }

    void SetLOD() {
        // Programmatically create a LOD group and add LOD levels.
        // Create a GUI that allows for forcing a specific LOD level.
        LODGroup group = gameObject.GetComponent<LODGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<LODGroup>();
        }

        group.fadeMode = LODFadeMode.CrossFade;


        //Determine All Holders of this Building
        // Add 4 LOD levels (0 = All Renderers, 1 = All Renderers but Balconies and Boards, 2 = Roof,Wall,Windows, 3 = Culled)
        LOD[] lods = new LOD[4];
        for (int i = 0; i < 4; i++)
        {
            //Has to be a dynamic list as we do not know which meshes this building has generated yet.
            List<Renderer> renderers = new List<Renderer>(0);

            switch (i)
            {
                case 0:
                    //All Renderers
                    if (roofHolder) {
                        renderers.Add(roofHolder.GetComponent<MeshRenderer>());
                    }
                    if (wallsHolder) {
                        renderers.Add(wallsHolder.GetComponent<MeshRenderer>());
                    }
                    if (doorsHolder) {
                        renderers.Add(doorsHolder.GetComponent<MeshRenderer>());
                    }
                    if (windowsHolder) {
                        renderers.Add(windowsHolder.GetComponent<MeshRenderer>());
                    }
                    if (balconiesHolder) {
                        renderers.Add(balconiesHolder.GetComponent<MeshRenderer>());
                    }
                    if (windowBoardsHolder) {
                        renderers.Add(windowBoardsHolder.GetComponent<MeshRenderer>());
                    }
                    if (columnHolder) {
                        renderers.Add(columnHolder.GetComponent<MeshRenderer>());
                    }
                    break;
                case 1:
                    //All Renderers but Balconies and Boards
                    if (roofHolder)
                    {
                        renderers.Add(roofHolder.GetComponent<MeshRenderer>());
                    }
                    if (wallsHolder)
                    {
                        renderers.Add(wallsHolder.GetComponent<MeshRenderer>());
                    }
                    if (doorsHolder)
                    {
                        renderers.Add(doorsHolder.GetComponent<MeshRenderer>());
                    }
                    if (windowsHolder)
                    {
                        renderers.Add(windowsHolder.GetComponent<MeshRenderer>());
                    }
                    if (balconiesHolder)
                    {
                        renderers.Add(balconiesHolder.GetComponent<MeshRenderer>());
                    }
                    if (columnHolder)
                    {
                        renderers.Add(columnHolder.GetComponent<MeshRenderer>());
                    }
                    break;
                case 2:
                    //Roof,Wall,Windows,Doors
                    if (roofHolder)
                    {
                        renderers.Add(roofHolder.GetComponent<MeshRenderer>());
                    }
                    if (wallsHolder)
                    {
                        renderers.Add(wallsHolder.GetComponent<MeshRenderer>());
                    }
                    if (doorsHolder)
                    {
                        renderers.Add(doorsHolder.GetComponent<MeshRenderer>());
                    }
                    if (windowsHolder)
                    {
                        renderers.Add(windowsHolder.GetComponent<MeshRenderer>());
                    }
                    if (columnHolder)
                    {
                        renderers.Add(columnHolder.GetComponent<MeshRenderer>());
                    }
                    break;
                case 3:
                    //Roof,Wall,Windows,Doors
                    if (roofHolder)
                    {
                        renderers.Add(roofHolder.GetComponent<MeshRenderer>());
                    }
                    if (wallsHolder)
                    {
                        renderers.Add(wallsHolder.GetComponent<MeshRenderer>());
                    }
                    if (doorsHolder)
                    {
                        renderers.Add(doorsHolder.GetComponent<MeshRenderer>());
                    }
                    if (windowsHolder)
                    {
                        renderers.Add(windowsHolder.GetComponent<MeshRenderer>());
                    }
                    if (columnHolder)
                    {
                        renderers.Add(columnHolder.GetComponent<MeshRenderer>());
                    }
                    break;
            }
            lods[i] = new LOD(1.0F / (i + 1), renderers.ToArray());
        }
        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    public void ExtrudePrint(List<Vector3> newPrint, float newHeight, bool isNew) {

        //Random if columnWidth will be used.
        switch (Random.Range(0, 2)) {
            case 0:
                edgeColumnWidth = 1;
                break;
            case 1:
                edgeColumnWidth = 0;
                break;
        }
        //Debug.Log("Extrude Print");
        if (materials == null || isNew) {
            GetMaterials();
        }
        //Debug.Log("ExtrudePrint " + newPrint.Count + " NewHeight: " + newHeight);
        //Set Random.Range Seed
        //Random.InitState(seed);
        //Extrude Foot Print
        buildPrint = new List<Vector3>(newPrint);
		newPrint.Add (newPrint[0]);
		newPrint.Reverse ();
		buildHeight = newHeight;
		//Translate User Created Quad into Panel
		roofPrint = new List<Vector3> (0);//Initilize Roof
        //Create new Panels
        if (isNew)
        {
            fascadePanels = new List<CiDyPanel>(0);//Initialize Panels
        }
        //Turn UserPlaced Polygon FootPrint into Quads for Panel Creation
        for (int i = 0;i<newPrint.Count;i++){
			Vector3 v0 = newPrint[i];
			Vector3 v1;
			if(i == newPrint.Count-1){
				v1 = newPrint[0];
			} else {
				v1 = newPrint[i+1];
			}
			//Project Up By buildHeight
			Vector3 v2 = v1+Vector3.up*buildHeight;
			Vector3 v3 = v0+Vector3.up*buildHeight;
			roofPrint.Add(v3);
            if (i == newPrint.Count - 1)
            {
                continue;
            }
            List<Vector3> quad = new List<Vector3>(0);
			quad.Add(v0);
			quad.Add(v1);
			quad.Add(v2);
			quad.Add(v3);
            
            //Divide Building Floors by Height and floor height available.
            floors = Mathf.RoundToInt(buildHeight / groundFloorHeight);
            //Divide Columns by 
            float facadeWidth = Vector3.Distance(v0, v1);
            columns = Mathf.RoundToInt(facadeWidth / columnWidth);
            //Debug.Log("Columns: "+facadeWidth / columnWidth);
            //Debug.Log("Floors: "+ floors);
            if (isNew)
            {
                List<CiDyPanel> newPanels = new List<CiDyPanel>(0);
                int random = Random.Range(0, 3);

                switch (random) {
                    case 0:
                        newPanels = SplitFascade(quad, groundFloorHeight, columns, floors, CiDyPanel.PanelType.Door);
                        break;
                    case 1:
                        newPanels = SplitFascade(quad, groundFloorHeight, columns, floors, CiDyPanel.PanelType.Window);
                        break;
                    case 2:
                        newPanels = SplitFascade(quad, groundFloorHeight, columns, floors, CiDyPanel.PanelType.Window);
                        break;
                }
                
                //Add to FascadePanels
                for (int j = 0; j < newPanels.Count; j++)
                {
                    fascadePanels.Add(newPanels[j]);
                }
            }
		}

        CombineInstance[] columnCombine = new CombineInstance[columnMeshes.Count];
        //Combine Edge Column Meshes into a Single Mesh for the building.
        for (int i = 0; i < columnMeshes.Count; i++) {
            columnCombine[i].mesh = columnMeshes[i];
            //columnCombine[i].transform = transform.localToWorldMatrix;
        }

        columnMesh = new Mesh();
        //Run Combining Function
        if (columnHolder == null && !instanceArray)
        {
            columnHolder = CreateMeshHolder("ColumnHolder", materials[6]);
            //Add Collision To Walls
            columnHolder.AddComponent<MeshCollider>();
        }
        else {
            columnHolder.GetComponent<MeshRenderer>().material = materials[6];
        }

        if (!instanceArray)
        {
            //Set Updated Mesh to Filter and Collider
            columnHolder.GetComponent<MeshFilter>().sharedMesh = columnMesh;
            columnHolder.GetComponent<MeshCollider>().sharedMesh = columnMesh;
        }
        //Combine into final Mesh
        columnMesh.CombineMeshes(columnCombine, true,false);
        //Clear Storage of Previous Meshes
        columnMeshes.Clear();
        //Reverse Roof Back and remove last added for PolygonInset Algorithm
        roofPrint.RemoveAt(0);
        roofPrint.Reverse();
        //Inset by RoofWallWidth
        List<Vector3> insetRoof = CiDyUtils.PolygonInset (roofPrint, roofWallWidth);
        //Reverse Back
        roofPrint.Reverse();
        insetRoof.Reverse();
		//Project Roof Walls.
		List<Vector3> verts = new List<Vector3>(0);
		List<Vector2> newUVs = new List<Vector2> (0);
		//Create RoofWall
		for(int i = 0;i<roofPrint.Count;i++){
			Vector3 v0 = roofPrint[i];
			Vector3 v1 = roofPrint[i]+(Vector3.up*roofWallHeight);
			Vector3 v2 = insetRoof[i]+(Vector3.up*roofWallHeight);
			Vector3 v3 = insetRoof[i];
			//Add to Verts List
			verts.Add(v0-transform.position*2);
			verts.Add(v1-transform.position*2);
			verts.Add(v1-transform.position*2);
			verts.Add(v2-transform.position*2);
			verts.Add(v2-transform.position*2);
			verts.Add(v3-transform.position*2);
			//Now Create the Other End of this Sides Wall.
			Vector3 v0b;
			Vector3 v1b;
			Vector3 v2b;
			Vector3 v3b;
			if(i==roofPrint.Count-1){
				v0b = roofPrint[0];
				v1b = roofPrint[0]+(Vector3.up*roofWallHeight);
				v2b = insetRoof[0]+(Vector3.up*roofWallHeight);
				v3b = insetRoof[0];
			} else {
				v0b = roofPrint[i+1];
				v1b = roofPrint[i+1]+(Vector3.up*roofWallHeight);
				v2b = insetRoof[i+1]+(Vector3.up*roofWallHeight);
				v3b = insetRoof[i+1];
			}
			verts.Add(v0b-transform.position*2);
			verts.Add(v1b-transform.position*2);
			verts.Add(v1b-transform.position*2);
			verts.Add(v2b-transform.position*2);
			verts.Add(v2b-transform.position*2);
			verts.Add(v3b-transform.position*2);
			//Setup Uvs
			float xDist = Vector3.Distance(v0,v0b);
			newUVs.Add(new Vector2(0,0));
			newUVs.Add(new Vector2(0,roofWallHeight));
			newUVs.Add(new Vector2(0,0));
			newUVs.Add(new Vector2(0,roofWallHeight));
			newUVs.Add(new Vector2(0,0));
			newUVs.Add(new Vector2(0,roofWallHeight));

			newUVs.Add(new Vector2(xDist,0));
			newUVs.Add(new Vector2(xDist,roofWallHeight));
			newUVs.Add(new Vector2(xDist,0));
			newUVs.Add(new Vector2(xDist,roofWallHeight));
			newUVs.Add(new Vector2(xDist,0));
			newUVs.Add(new Vector2(xDist,roofWallHeight));
		}
		List<int> tris = new List<int> (0);
		for(int i = 0;i<verts.Count-5;i+=6){
			//Setup Triangles
			if(i==verts.Count-6){
				//Last Points
				tris.Add(i);
				tris.Add(i+1);
				tris.Add(0);
				
				tris.Add(i+1);
				tris.Add(1);
				tris.Add(0);
				
				tris.Add(i+2);
				tris.Add(i+3);
				tris.Add(2);
				
				tris.Add(i+3);
				tris.Add(3);
				tris.Add(2);
				
				tris.Add(i+4);
				tris.Add(i+5);
				tris.Add(4);
				
				tris.Add(i+5);
				tris.Add(5);
				tris.Add(4);
			} else {
				//Beginning or Middle
				tris.Add(i);
				tris.Add(i+1);
				tris.Add(i+6);

				tris.Add(i+1);
				tris.Add(i+7);
				tris.Add(i+6);

				tris.Add(i+2);
				tris.Add(i+3);
				tris.Add(i+8);

				tris.Add(i+3);
				tris.Add(i+9);
				tris.Add(i+8);

				tris.Add(i+4);
				tris.Add(i+5);
				tris.Add(i+10);

				tris.Add(i+5);
				tris.Add(i+11);
				tris.Add(i+10);
			}
		}

		//Now Create Panels
		List<Mesh> wallMeshs = new List<Mesh> (0);
		List<Mesh> doorMeshs = new List<Mesh> (0);
		List<Mesh> windowMeshs = new List<Mesh> (0);
		List<Mesh> balconyMeshs = new List<Mesh> (0);
		List<Mesh> windowBoardsMeshs = new List<Mesh> (0);

		//Assemble RoofWallMesh
		Mesh roofWallMesh = new Mesh();
		roofWallMesh.vertices = verts.ToArray();
		roofWallMesh.triangles = tris.ToArray();
		roofWallMesh.uv = newUVs.ToArray();
		roofWallMesh.RecalculateBounds ();
		roofWallMesh.RecalculateNormals ();
		wallMeshs.Add (roofWallMesh);

		for(int i = 0;i<fascadePanels.Count;i++){
			CiDyPanel newPanel = fascadePanels[i];
			//CheckForWallMesh
			if(newPanel.wallMesh){
				Vector3[] tmpVerts = newPanel.wallMesh.vertices;
				for(int j = 0;j<tmpVerts.Length;j++){
					tmpVerts[j]-=transform.position;
				}
				newPanel.wallMesh.vertices = tmpVerts;
				wallMeshs.Add(newPanel.wallMesh);
			}
			//Check for Door
			if(newPanel.doorMesh){
				Vector3[] tmpVerts = newPanel.doorMesh.vertices;
				for(int j = 0;j<tmpVerts.Length;j++){
					tmpVerts[j]-=transform.position;
				}
				newPanel.doorMesh.vertices = tmpVerts;
				doorMeshs.Add(newPanel.doorMesh);
			}
			//window
			if(newPanel.windowMesh){
				Vector3[] tmpVerts = newPanel.windowMesh.vertices;
				for(int j = 0;j<tmpVerts.Length;j++){
					tmpVerts[j]-=transform.position;
				}
				newPanel.windowMesh.vertices = tmpVerts;
				windowMeshs.Add(newPanel.windowMesh);
			}
			//balcony
			if(newPanel.balconyMesh){
				Vector3[] tmpVerts = newPanel.balconyMesh.vertices;
				for(int j = 0;j<tmpVerts.Length;j++){
					tmpVerts[j]-=transform.position;
				}
				newPanel.balconyMesh.vertices = tmpVerts;
				balconyMeshs.Add(newPanel.balconyMesh);
			}
			//WindowBoards
			if(newPanel.windowBoardMesh){
				Vector3[] tmpVerts = newPanel.windowBoardMesh.vertices;
				for(int j = 0;j<tmpVerts.Length;j++){
					tmpVerts[j]-=transform.position;
				}
				newPanel.windowBoardMesh.vertices = tmpVerts;
				windowBoardsMeshs.Add(newPanel.windowBoardMesh);
			}
		}

		//Now Combine Meshes into Groups. WallMesh First
		//Walls
		if(wallMeshs.Count > 0){
			CombineInstance[] combine = new CombineInstance[wallMeshs.Count];
            //Combine All wall Meshes into a single Wall Mesh
			for(int i = 0;i<wallMeshs.Count;i++){
				combine[i].mesh = wallMeshs[i];
				combine[i].transform = transform.localToWorldMatrix;
			}
            //Initialize Walls
            walls = new Mesh();
            walls.CombineMeshes(combine);
            /*walls.RecalculateTangents();
            walls.RecalculateBounds();*/
            //NormalSolver.RecalculateNormals(walls, 90);
            //Run Combining Function
            if (wallsHolder == null && !instanceArray)
            {
                wallsHolder = CreateMeshHolder("WallsHolder", materials[0]);
                //Add Collision To Walls
                wallsHolder.AddComponent<MeshCollider>();
            }
            else
            {
                wallsHolder.GetComponent<MeshRenderer>().material = materials[0];
            }

            if (!instanceArray)
            {
                //Set Updated Mesh to Filter and Collider
                wallsHolder.GetComponent<MeshFilter>().sharedMesh = walls;
                wallsHolder.GetComponent<MeshCollider>().sharedMesh = walls;
            }
			//Now Create Roof For Walls.
			for(int i = 0;i<roofPrint.Count;i++){
				roofPrint[i]-=transform.position;
			}
			Vector2[] roofUVs = new Vector2[roofPrint.Count];
			//Triangulate RoofPrint
			Vector2z[] vertices2D = new Vector2z[roofPrint.Count];
			for(int i = 0;i<vertices2D.Length;i++){
				vertices2D[i] = new Vector2z(roofPrint[i].x,roofPrint[i].z);
				//Add UVs while we are here as well.
				roofUVs[i] = new Vector2(roofPrint[i].x,roofPrint[i].z);
			}
			int[] indices = EarClipper.Triangulate (vertices2D);
			//Create RoofMesh and its Holder
			roofMesh = new Mesh();
			roofMesh.vertices = roofPrint.ToArray();
			roofMesh.triangles = indices;
			roofMesh.uv = roofUVs;
			roofMesh.RecalculateBounds();
			roofMesh.RecalculateNormals ();
            if (roofHolder == null && !instanceArray)
            {
                roofHolder = CreateMeshHolder("RoofHolder", materials[1]);
            }
            else {
                roofHolder.GetComponent<MeshRenderer>().material = materials[1];
            }
            if (!instanceArray)
            {
                roofHolder.GetComponent<MeshFilter>().sharedMesh = roofMesh;
            }
		}
		//Doors
		if(doorMeshs.Count > 0){
			CombineInstance[] combine = new CombineInstance[doorMeshs.Count];
			for(int i = 0;i<doorMeshs.Count;i++){
				combine[i].mesh = doorMeshs[i];
				combine[i].transform = this.transform.localToWorldMatrix;
			}
            //Initialize Doors
            doors = new Mesh();
            //Combine
            doors.CombineMeshes(combine);
            //Create Holder if Needed
            if (doorsHolder == null && !instanceArray)
            {
                doorsHolder = CreateMeshHolder("DoorsHolder", materials[2]);
            }
            else
            {
                doorsHolder.GetComponent<MeshRenderer>().material = materials[2];
            }
            //Turn off ShadowCalculations for Doors
            //doorsHolder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            //Update Doors Mesh to new doors mesh
            if (!instanceArray)
            {
                doorsHolder.GetComponent<MeshFilter>().sharedMesh = doors;
            }
        }
		//Windows
		if(windowMeshs.Count > 0){
			CombineInstance[] combine = new CombineInstance[windowMeshs.Count];
			for(int i = 0;i<windowMeshs.Count;i++){
				combine[i].mesh =windowMeshs[i];
				combine[i].transform = this.transform.localToWorldMatrix;
			}
            //Initialize Windows Mesh
            windows = new Mesh();
            windows.CombineMeshes(combine);
            if (windowsHolder == null && !instanceArray)
            {
                windowsHolder = CreateMeshHolder("WindowsHolder", materials[3]);
            }
            else
            {
                windowsHolder.GetComponent<MeshRenderer>().material = materials[3];
            }
            //Turn off ShadowCalculations for Windows
            //windowsHolder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            //Update windows to New Mesh
            if (!instanceArray)
            {
                windowsHolder.GetComponent<MeshFilter>().sharedMesh = windows;
            }
        }
		//WindowBoards
		if(windowBoardsMeshs.Count > 0){
			CombineInstance[] combine = new CombineInstance[windowBoardsMeshs.Count];
			for(int i = 0;i<windowBoardsMeshs.Count;i++){
				combine[i].mesh = windowBoardsMeshs[i];
				combine[i].transform = this.transform.localToWorldMatrix;
			}
            //Initialize 
            windowBoards = new Mesh();
            //Combine
            windowBoards.CombineMeshes(combine);

            //WindowBoards Holder creation if needed
            if (windowBoardsHolder == null && !instanceArray)
            {
                windowBoardsHolder = CreateMeshHolder("WindowsBoardsHolder", materials[4]);
            }
            else
            {
                windowBoardsHolder.GetComponent<MeshRenderer>().material = materials[4];
            }
            if (!instanceArray)
            {
                //Turn off ShadowCalculations for Window Boards
                windowBoardsHolder.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //Update windows Boards Data
                windowBoardsHolder.GetComponent<MeshFilter>().sharedMesh = windowBoards;
            }
        }
		//Balconies
		if(balconyMeshs.Count > 0){
			CombineInstance[] combine = new CombineInstance[balconyMeshs.Count];
			for(int i = 0;i<balconyMeshs.Count;i++){
				combine[i].mesh = balconyMeshs[i];
				combine[i].transform = this.transform.localToWorldMatrix;
			}
            //Initialize
            balconies = new Mesh();
            //Combine
            balconies.CombineMeshes(combine);
            if (balconiesHolder == null && !instanceArray)
            {
                balconiesHolder = CreateMeshHolder("BalconiesHolder", materials[5]);
            }
            else
            {
                balconiesHolder.GetComponent<MeshRenderer>().material = materials[5];
            }

            if (!instanceArray)
            {
                //Update Mesh
                balconiesHolder.GetComponent<MeshFilter>().sharedMesh = balconies;
            }
        }

        //Setup LOD Groups for the Procedural Building
        if (!instanceArray)
        {
            SetLOD();
        }
    }

    void GetMaterials() {
        materials = new Material[7];
        int random = Random.Range(0, 3);
        //Debug.Log("Get Materials: " + random);
        switch (random) {
            case 0:
                //Fill Materials with Preset Resource Materials
                materials[0] = (Material)Resources.Load("CiDyPanel/Building1/Wall");
                materials[1] = (Material)Resources.Load("CiDyPanel/Building1/Roof");
                materials[2] = (Material)Resources.Load("CiDyPanel/Building1/Door");
                materials[3] = (Material)Resources.Load("CiDyPanel/Building1/Window");
                materials[4] = (Material)Resources.Load("CiDyPanel/Building1/Boards");
                materials[5] = (Material)Resources.Load("CiDyPanel/Building1/Balcony");
                materials[6] = (Material)Resources.Load("CiDyPanel/Building1/Columns");
                break;
            case 1:
                //Fill Materials with Preset Resource Materials
                materials[0] = (Material)Resources.Load("CiDyPanel/Building2/Wall");
                materials[1] = (Material)Resources.Load("CiDyPanel/Building2/Roof");
                materials[2] = (Material)Resources.Load("CiDyPanel/Building2/Door");
                materials[3] = (Material)Resources.Load("CiDyPanel/Building2/Window");
                materials[4] = (Material)Resources.Load("CiDyPanel/Building2/Boards");
                materials[5] = (Material)Resources.Load("CiDyPanel/Building2/Balcony");
                materials[6] = (Material)Resources.Load("CiDyPanel/Building2/Columns");
                break;
            case 2:
                //Fill Materials with Preset Resource Materials
                materials[0] = (Material)Resources.Load("CiDyPanel/Building3/Wall");
                materials[1] = (Material)Resources.Load("CiDyPanel/Building3/Roof");
                materials[2] = (Material)Resources.Load("CiDyPanel/Building3/Door");
                materials[3] = (Material)Resources.Load("CiDyPanel/Building3/Window");
                materials[4] = (Material)Resources.Load("CiDyPanel/Building3/Boards");
                materials[5] = (Material)Resources.Load("CiDyPanel/Building3/Balcony");
                materials[6] = (Material)Resources.Load("CiDyPanel/Building3/Columns");
                break;
        }
    }

	GameObject CreateMeshHolder(string name, Material newMaterial){
		GameObject newGameObject = new GameObject (name);
		newGameObject.transform.position = this.transform.position;
		newGameObject.transform.parent = this.transform;
		newGameObject.AddComponent<MeshFilter> ();
		MeshRenderer mRenderer = newGameObject.AddComponent<MeshRenderer> ();
		mRenderer.material = newMaterial;
		//Return Create Object
		return newGameObject;
	}

    //Store Reference too all the Edge Columns Created
    List<Mesh> columnMeshes = new List<Mesh>(0);

	List<CiDyPanel> SplitFascade(List<Vector3> wall, float groundFHeight, int columns, int floors, CiDyPanel.PanelType newType){
		//Create Panels
		List<CiDyPanel> panels = new List<CiDyPanel> (0);
		//Take initial Wall and Split into panels.
		//Calculate Dist from 0-1. WallLength && Dist from 1-2 WallHeight.
		//float length = Vector3.Distance (wall [0], wall [1]);
		float height = Vector3.Distance (wall [1], wall [2]);
		//Take care of situation where there is only one desired floor.
		if(floors <= 1){
			groundFHeight = height;
		}
		//Take care of Situation where GroundFloorHeight is same or Greater than Height
		if(groundFHeight >= height){
			//Max it out then
			groundFHeight = height;
		}
		if(groundFHeight <= 0){
			//Split it equally then
			groundFHeight = (height/floors);
		}
		//Minimum Allowed is 1.
		if(columns < 1){
			columns = 1;
		}
        //Split length by RowWidth
        //float panelWidth = (length/columns);
        //Debug.Log("WallLength: "+length+" WallHegith: "+height+" PanelWidth: "+panelWidth+ "Rows: "+columns);
        Vector3 topA;
        Vector3 topB;
        Vector3 bottomA;
        Vector3 bottomB;
        if (edgeColumnWidth > 0)
        {
            Vector3 right = (wall[2] - wall[3]).normalized;
            //Interpolate Top Line
            topA = wall[3] + (right * edgeColumnWidth);//Top Facade Point Left inset by EdgeColumnWidth
            topB = wall[2] + (-right * edgeColumnWidth);//Inset Left by edgeColumnWidth
                                                                //Interpolate Bottom Line
            bottomA = wall[0] + (right * edgeColumnWidth);//Top Facade Point Left inset by EdgeColumnWidth;
            bottomB = wall[1] + (-right * edgeColumnWidth);//Inset Left by edgeColumnWidth;
        } else
        {
            //Interpolate Top Line
            topA = wall[3];//Top Facade Point Left inset by EdgeColumnWidth
            topB = wall[2];//Inset Left by edgeColumnWidth
                                                        //Interpolate Bottom Line
            bottomA = wall[0];//Top Facade Point Left inset by EdgeColumnWidth;
            bottomB = wall[1];//Inset Left by edgeColumnWidth;
        }

        //Create EdgeColumns
        //Add BottomLeft Point to inset point same for top but in reverse so total point list is counter clockwise
        Vector3[] leftColumn = new Vector3[4];
        leftColumn[0] = wall[0] - transform.position;//Bottom Left
        leftColumn[1] = bottomA - transform.position;//Bottom Right
        leftColumn[2] = topA - transform.position;//Top Right
        leftColumn[3] = wall[3] - transform.position;//Top Left
        //Make Right Column
        Vector3[] rightColumn = new Vector3[4];
        rightColumn[0] = bottomB - transform.position;//Bottom Left
        rightColumn[1] = wall[1] - transform.position;//Bottom Right
        rightColumn[2] = wall[2] - transform.position;//Top Right
        rightColumn[3] = topB - transform.position;//Top Left
        //Create Mesh for this facades left Column
        Mesh leftMesh = CiDyUtils.ExtrudePrint(leftColumn, edgeColumnDepth, transform, true);
        columnMeshes.Add(leftMesh);//Store for not until we combine them
        //Create Right Edge Mesh
        Mesh rightMesh = CiDyUtils.ExtrudePrint(rightColumn, edgeColumnDepth, transform , true);
        columnMeshes.Add(rightMesh);//Store for now until we combine them
        //Dynamic Reference Points for Panel Creation
        Vector3 lastTop = topA;
		Vector3 lastBottom = bottomA;
		//Split Fascade Long Ways first.
		float curInterpolation = 0;
		float interpolation = (1.0f/columns);
		//Store TmpColumns
		List<List<Vector3>> tmpColumns = new List<List<Vector3>>(0);
		for(int i = 0;i<columns;i++){
            curInterpolation += interpolation;
            //Debug.Log(curInterpolation);
            List<Vector3> panel = new List<Vector3>(0);
			Vector3 topP = Vector3.Lerp(topA, topB,curInterpolation);
			//Now find Bottom P
			Vector3 bottomP = Vector3.Lerp(bottomA, bottomB,curInterpolation);
			//Create Panel from This Information.
			panel.Add(lastBottom);
			panel.Add(bottomP);
			panel.Add(topP);
			panel.Add(lastTop);
			tmpColumns.Add(panel);
			//Update Last Points to current Points
			lastTop = topP;
			lastBottom = bottomP;
        }
        
		//Now Split Columns into Floors using groundFHeight && floorHeight
		float topHeight = (height - groundFHeight);//Removed GroundFloorHeight.
		float floorHeight = (topHeight/(floors-1));
		//Debug.Log ("TopHeight: "+topHeight+" FloorHeight: "+floorHeight);
		if(floorHeight != 0){
			//Iterate through the panels and split them vertically
			for(int n = 0;n<tmpColumns.Count;n++){
				for(int i = 0;i<tmpColumns[n].Count-3;i+=4){
					List<Vector3> panel = new List<Vector3>(0);
					Vector3 leftB = tmpColumns[n][i];
					Vector3 rightB = tmpColumns[n][i+1];
					//Vector3 rightT = tmpColumns[n][i+2];
					//Vector3 leftT = tmpColumns[n][i+3];
					//Right Dir
					//Vector3 rightDir = (rightB-leftB).normalized;
					//Up Dir is same as World Up.
					panel.Add(leftB);
					panel.Add(rightB);
					//Project top Right Point
					Vector3 lastRight = rightB+(Vector3.up*groundFHeight);
					Vector3 lastLeft = leftB+(Vector3.up*groundFHeight);
					panel.Add(lastRight);
					panel.Add(lastLeft);
					CiDyPanel newPanel = new CiDyPanel(panel,this.transform,newType, true);
					newPanel.groundPanel = true;
					panels.Add(newPanel);
					//Now create the Final Floor Points.
					for(int k = 0;k<floors-1;k++){
						panel = new List<Vector3>(0);
						//Add Bottom two points.
						panel.Add(lastLeft);
						panel.Add(lastRight);
						//Calculate top new Points.
						lastRight = lastRight+(Vector3.up*floorHeight);
						lastLeft = lastLeft+(Vector3.up*floorHeight);
						panel.Add(lastRight);
						panel.Add(lastLeft);
						newPanel = new CiDyPanel(panel,this.transform,newType, false);
						panels.Add(newPanel);
					}
				}
			}
		} else {
			//Iterat through the Current Columns and turn into Panels
			for(int i = 0;i<tmpColumns.Count;i++){
				CiDyPanel newPanel = new CiDyPanel(tmpColumns[i],this.transform,newType,true);
				panels.Add(newPanel);
			}
		}
		//Return Final Panels List in CounterClockwise Quad Format
		return panels;
	}

    /*void LateUpdate(){

        if (curSeed != seed) {
            curSeed = seed;
            randomize = true;
        }
		if(randomize){
            randomize = false;
			ExtrudePrint(buildPrint,buildHeight, true);
		}
        if (recalculate) {
            recalculate = false;
            ExtrudePrint(buildPrint,buildHeight, false);
        }
        if (clearMesh) {
            clearMesh = false;
            ClearMeshes();
        }

        if (!instanceArray)
        {
            return;
        }
    }*/

    //Test Clearing
    void ClearMeshes() {
        //Walls
        if (wallsHolder) {
            wallsHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
        //Windows
        if (windowsHolder) {
            windowsHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
        //Doors
        if (doorsHolder) {
            doorsHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
        //Boards
        if (windowBoardsHolder)
        {
            windowBoardsHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
        //Balconies
        if (balconiesHolder) {
            balconiesHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
        //Roof
        if (roofHolder) {
            roofHolder.GetComponent<MeshFilter>().sharedMesh = null;
        }
    }
}
