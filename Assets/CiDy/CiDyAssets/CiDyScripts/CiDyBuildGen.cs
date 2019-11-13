using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CiDyBuildGen : MonoBehaviour {

    public bool testFacade = false;
	public Transform[] userPoints;
    public int buildingMaterial = 0;
    private int maxBuildMaterial = 0;
	public float buildHeight = 0f;

	//This will Extrude a Building from the Set Lot Vectors
	List<Vector3> footPrint = new List<Vector3>(0);
	List<Vector3> roofPrint = new List<Vector3>(0);

	//Create Mesh Components
	//WallMesh
	MeshRenderer wallRenderer;
	MeshFilter wallFilter;
	MeshCollider wallCollider;
	Mesh wallMesh;
	//RoofMesh
	GameObject roof;
	MeshRenderer roofRenderer;
	MeshFilter roofFilter;
	Mesh roofMesh;

    //Grab Resource Materials
    public Material[] facadeMaterials = new Material[0];

    void Awake(){
        //Grab Facade Resources
        //facadeMaterials = Resources.LoadAll("CiDyResources/FacadeMaterials", typeof(Material)).Cast<Material>().ToArray();
        facadeMaterials = Resources.LoadAll<Material>("CiDyResources/FacadeMaterials");
        //Grab Roof Material
        roof = new GameObject ("Roof");
		roof.transform.parent = this.transform;
		//Set WallMesh
		wallRenderer = gameObject.AddComponent<MeshRenderer> ();
		wallFilter = gameObject.AddComponent<MeshFilter> ();
		wallCollider = gameObject.AddComponent<MeshCollider> ();

		buildingMaterial = Random.Range (0, 3);//Randomize Building Materail on Initialize
        maxBuildMaterial = 2;

		if(buildingMaterial == 0){
            wallRenderer.sharedMaterial = facadeMaterials[0];//(Material)Resources.Load ("CiDyResources/FacadeMaterials/Building0");
		} else if(buildingMaterial == 1){
            wallRenderer.sharedMaterial = facadeMaterials[1];//(Material)Resources.Load("CiDyResources/FacadeMaterials/Building1");
        } else {
            wallRenderer.sharedMaterial = facadeMaterials[2];//(Material)Resources.Load("CiDyResources/FacadeMaterials/Building2");
        }
		wallMesh = wallFilter.sharedMesh;
		//Set RoofMesh
		roofRenderer = roof.AddComponent<MeshRenderer> ();
		roofFilter = roof.AddComponent<MeshFilter> ();
		roofRenderer.sharedMaterial = (Material)Resources.Load ("CiDyResources/FacadeMaterials/Roofs/Roof0");
		roofMesh = roofFilter.sharedMesh;

        //Generate a Single Facade Side for Testing Facade Split Algorithm
        if (testFacade && userPoints.Length > 0) {
            //Generate a Four Points for Facade Generator from first two user points and build height
            List<Vector3> facadePrint = new List<Vector3>();
            for (int i = 0; i < userPoints.Length; i++) {
                facadePrint.Add(userPoints[i].position);
            }
            //GenerateFacade(facadePrint);
            gameObject.AddComponent<CiDyBuildingGen>();
        }
    }

    public bool Regenerate = false;//If true make false

    private void Update()
    {
        if (buildingMaterial < 0)
        {
            buildingMaterial = 0;
        }
        else if (buildingMaterial > maxBuildMaterial) {
            buildingMaterial = maxBuildMaterial;
        }
        if (Regenerate) {
            Regenerate = false;
            ExtrudePrint(footPrint, buildHeight);
        }    
    }

    public void ExtrudePrint(List<Vector3> newPrint, float newHeight){
        /*//Set Material 
        if (buildingMaterial == 0)
        {
            wallRenderer.sharedMaterial = facadeMaterials[0];//(Material)Resources.Load ("CiDyResources/FacadeMaterials/Building0");
        }
        else if (buildingMaterial == 1)
        {
            wallRenderer.sharedMaterial = facadeMaterials[1];//(Material)Resources.Load("CiDyResources/FacadeMaterials/Building1");
        }
        else
        {
            wallRenderer.sharedMaterial = facadeMaterials[2];//(Material)Resources.Load("CiDyResources/FacadeMaterials/Building2");
        }*/
        //Set lot Print.
        footPrint = newPrint;
		//Set Height
		buildHeight = newHeight;
		//Now lets Extrude flat Mesh from this print.
		roofPrint.Clear ();
		//Clear previous Data
		List<Vector3> verts = new List<Vector3> (0);
		float highestPoint = Mathf.NegativeInfinity;
		int place = 0;
		for(int i = 0;i<footPrint.Count;i++){
            footPrint[i] = footPrint[i] - transform.position;
            //CiDyUtils.MarkPoint(footPrint[i], i);
			if(footPrint[i].y > highestPoint){
				highestPoint = footPrint[i].y;
				place = i;
			}
		}
		float roofY = (footPrint[place]+(Vector3.up*buildHeight)).y;
		for(int i = 0;i<footPrint.Count;i++){
			float thisHeight = roofY-footPrint[i].y;
			roofPrint.Add(footPrint[i]+(Vector3.up*thisHeight));
			//Merge with vertList
			verts.Add(footPrint[i]);
			verts.Add(roofPrint[i]);
			//uvs.Add(new Vector2(footPrint[i].x,footPrint[i].y));
			//uvs.Add(new Vector2(roofPrint[i].x,roofPrint[i].y));
		}
		//Add LoopedPoints.
		verts.Add (footPrint [0]);
		verts.Add (roofPrint [0]);
		//uvs.Add(new Vector2(footPrint[0].x,footPrint[0].y));
		//uvs.Add(new Vector2(roofPrint[0].x,roofPrint[0].y));
		//Create Tris for Mesh
		//Set N
		int n = verts.Count;
		//Setup UVS
		Vector2[] uvs = new Vector2[n];
		int uvCount = 0;
		for(int i = 0;i<n;i++){
			//Do UVS
			if(uvCount == 0){
				uvs[i] = new Vector2(1,0);
			} else if(uvCount == 1){
				uvs[i] = new Vector2(1,1);
			} else if(uvCount == 2){
				uvs[i] = new Vector2(0,0);
			} else if(uvCount == 3){
				uvs[i] = new Vector2(0,1);
				uvCount = -1;
			}
			uvCount++;
		}
		//Set Tris for SideWall.
		List<int> tris = new List<int> (0);
		//Look at four points at a time
		for(int i = 0;i<n-2;i+=2){
			tris.Add(i);//1
			tris.Add(i+2);//2
			tris.Add (i+1);//0
			tris.Add (i+1);//1
			tris.Add (i+2);//2
			tris.Add (i+3);//3
		}
		//Debug.Log ("ExtrudePrint FootPrintCnt: "+footPrint.Count+" RoofPrintCnt: "+roofPrint.Count+" Verts.Cnt:"+verts.Count);
		//Now Set WallMesh
		wallMesh = new Mesh ();
		wallMesh.vertices = verts.ToArray ();
		wallMesh.triangles = tris.ToArray ();
		wallMesh.uv = uvs;
		wallMesh.RecalculateBounds ();
		wallMesh.RecalculateNormals ();
		wallFilter.sharedMesh = wallMesh;
		wallCollider.sharedMesh = wallMesh;
		//Now Create and Set Roof Mesh
		//Need CounterClockswise for Triangulation
		roofPrint.Reverse ();
		Vector2[] newUVs = new Vector2[roofPrint.Count];
		//Triangulate RoofPrint
		Vector2z[] vertices2D = new Vector2z[roofPrint.Count];
		for(int i = 0;i<vertices2D.Length;i++){
			vertices2D[i] = new Vector2z(roofPrint[i].x,roofPrint[i].z);
			//Add UVs while we are here as well.
			newUVs[i] = new Vector2(roofPrint[i].x,roofPrint[i].z);
		}
		int[] indices = EarClipper.Triangulate (vertices2D);
		//Create RoofMesh
		roofMesh = new Mesh();
		roofMesh.vertices = roofPrint.ToArray();
		roofMesh.triangles = indices;
		roofMesh.uv = newUVs;
		roofMesh.RecalculateBounds();
		roofMesh.RecalculateNormals ();
		roofFilter.sharedMesh = roofMesh;
	}

    //public float groundFloorHeight = 2.8f;//2.8 meters is About 9.2ft.
    public int buildingFloors = 4;//Ground is included
    public float floorHeight = 2.5f;//2.5 meters is About 8.2ft.
    public float tileSize = 4;//The Width of Tiles of X Splits
    //List<Vector3> xSplitLines = new List<Vector3>();
    List<Vector3> ySplitLines = new List<Vector3>();
    //Test Functions for Facade Generations
    public void GenerateFacade(List<Vector3> newPrint)
    {
        //Calculate Building Height based on FloorHeight * building Floors.
        //Set lot Print.
        footPrint = newPrint;
        //Set Height
        buildHeight = floorHeight * buildingFloors;
        Debug.Log("Building Floors: "+buildingFloors+" Floor Height: "+floorHeight+" Total Height: "+buildHeight);
        //int count = 0;
      
        //Split Y Axis of Facade
        SplitFacade("Y");

        //Now lets Extrude flat Mesh from this print.
        roofPrint.Clear();
        //Clear previous Data
        List<Vector3> verts = new List<Vector3>(0);
        float highestPoint = Mathf.NegativeInfinity;
        int place = 0;
        for (int i = 0; i < footPrint.Count; i++)
        {
            footPrint[i] = footPrint[i] - transform.position;
            //CiDyUtils.MarkPoint(footPrint[i], i);
            if (footPrint[i].y > highestPoint)
            {
                highestPoint = footPrint[i].y;
                place = i;
            }
        }
        float roofY = (footPrint[place] + (Vector3.up * buildHeight)).y;
        for (int i = 0; i < footPrint.Count; i++)
        {
            float thisHeight = roofY - footPrint[i].y;
            roofPrint.Add(footPrint[i] + (Vector3.up * thisHeight));
            //Merge with vertList
            verts.Add(footPrint[i]);
            verts.Add(roofPrint[i]);
            //uvs.Add(new Vector2(footPrint[i].x,footPrint[i].y));
            //uvs.Add(new Vector2(roofPrint[i].x,roofPrint[i].y));
        }
        //Add LoopedPoints.
        verts.Add(footPrint[0]);
        verts.Add(roofPrint[0]);
        //uvs.Add(new Vector2(footPrint[0].x,footPrint[0].y));
        //uvs.Add(new Vector2(roofPrint[0].x,roofPrint[0].y));
        //Create Tris for Mesh
        //Set N
        int n = verts.Count;
        //Setup UVS
        Vector2[] uvs = new Vector2[n];
        int uvCount = 0;
        for (int i = 0; i < n; i++)
        {
            //Do UVS
            if (uvCount == 0)
            {
                uvs[i] = new Vector2(1, 0);
            }
            else if (uvCount == 1)
            {
                uvs[i] = new Vector2(1, 1);
            }
            else if (uvCount == 2)
            {
                uvs[i] = new Vector2(0, 0);
            }
            else if (uvCount == 3)
            {
                uvs[i] = new Vector2(0, 1);
                uvCount = -1;
            }
            uvCount++;
        }
        //Set Tris for SideWall.
        List<int> tris = new List<int>(0);
        //Look at four points at a time
        for (int i = 0; i < n - 2; i += 2)
        {
            tris.Add(i);//1
            tris.Add(i + 2);//2
            tris.Add(i + 1);//0
            tris.Add(i + 1);//1
            tris.Add(i + 2);//2
            tris.Add(i + 3);//3
        }
        //Debug.Log ("ExtrudePrint FootPrintCnt: "+footPrint.Count+" RoofPrintCnt: "+roofPrint.Count+" Verts.Cnt:"+verts.Count);
        //Now Set WallMesh
        wallMesh = new Mesh();
        wallMesh.vertices = verts.ToArray();
        wallMesh.triangles = tris.ToArray();
        wallMesh.uv = uvs;
        wallMesh.RecalculateBounds();
        wallMesh.RecalculateNormals();
        wallFilter.sharedMesh = wallMesh;
        wallCollider.sharedMesh = wallMesh;
        //Now Create and Set Roof Mesh
        //Need CounterClockswise for Triangulation
        roofPrint.Reverse();
        Vector2[] newUVs = new Vector2[roofPrint.Count];
        //Triangulate RoofPrint
        Vector2z[] vertices2D = new Vector2z[roofPrint.Count];
        for (int i = 0; i < vertices2D.Length; i++)
        {
            vertices2D[i] = new Vector2z(roofPrint[i].x, roofPrint[i].z);
            //Add UVs while we are here as well.
            newUVs[i] = new Vector2(roofPrint[i].x, roofPrint[i].z);
        }
        int[] indices = EarClipper.Triangulate(vertices2D);
        //Create RoofMesh
        roofMesh = new Mesh();
        roofMesh.vertices = roofPrint.ToArray();
        roofMesh.triangles = indices;
        roofMesh.uv = newUVs;
        roofMesh.RecalculateBounds();
        roofMesh.RecalculateNormals();
        roofFilter.sharedMesh = roofMesh;
    }

    //Run a Split Algorithm
    void SplitFacade(string splitType) {
        //Switch Case
        switch (splitType) {
            case "Y":
                //Split up the Y Vector
                //for (int i = 0; i < footPrint.Count-1; i++) {
                    for (int h = 1; h < buildingFloors; h++) {
                        //First Point.
                        ySplitLines.Add(footPrint[0] + (Vector3.up * (h * floorHeight)));
                        //Second Point
                        ySplitLines.Add(footPrint[1] + (Vector3.up * (h * floorHeight)));
                    }
                //}
                break;
            case "X":
                float facadeLength = Vector3.Distance(footPrint[0], footPrint[1]);
                int splits = Mathf.RoundToInt(facadeLength / tileSize);
                Debug.Log("Facade Length: "+facadeLength/tileSize);

                //X Direction
                Vector3 xDir = (footPrint[1] - footPrint[0]).normalized;

                //Split Along X Vector->
                for (int h = 1; h < splits; h++)
                {
                    //First Point.
                    ySplitLines.Add(footPrint[0] + (xDir  * (h * tileSize)));
                    //Second Point
                    ySplitLines.Add(footPrint[1] + (xDir * (h * tileSize)));
                }
                break;
        }
    }

    //Draw Debug Lines
    /*private void OnDrawGizmos()
    {
        //visualize X Splits. Red
        if (xSplitLines.Count > 0) {
            for (int i = 0; i < xSplitLines.Count-1; i+=2) {
                Debug.DrawLine(xSplitLines[i], xSplitLines[i + 1], Color.red);
            }
        }
        //Visualize YSplits Yellow
        if (ySplitLines.Count > 0) {
            for (int i = 0; i < ySplitLines.Count - 1; i+=2)
            {
                Debug.DrawLine(ySplitLines[i], ySplitLines[i + 1], Color.yellow);
            }
        }
    }*/
}