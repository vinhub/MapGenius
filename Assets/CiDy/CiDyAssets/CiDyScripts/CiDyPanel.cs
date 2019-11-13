using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CiDyPanel {

	public enum PanelType {
		Door,
		Window,
		Wall,
	}
	public PanelType panelType = PanelType.Window;

    public enum WindowType
    {
        Clear,
        Single,
        Double,
        T
    }
    public WindowType windowType = WindowType.T;

    [HideInInspector]
	public Mesh doorMesh;
    [HideInInspector]
    public Mesh wallMesh;
    [HideInInspector]
    public Mesh balconyMesh;
    [HideInInspector]
    public Mesh windowBoardMesh;
    [HideInInspector]
    public Mesh windowMesh;
    [HideInInspector]
    public Transform transform;
	public bool groundPanel = false;//Lets us know if this panel can be door
	float panelWidth = 0;
	float panelHeight = 0;
	float spacing = 0.5f;//This value will make sure that we have a margin around panel.
	List<Vector3> wall = new List<Vector3> (0);
	//Editable Variables
	public float width = 2f;//Desired Width
	public float height = 3f;//Desired Height
	public float depth =  0.8f;//Desired Depth
	//WindowBoard Variables
	public float boardWidth = 0.16f;
    public float boardDepth = 0.16f;
    //Create Balcony?
    public bool hasBalcony = false;//If true we will create a balcony for door/window panels unless is a door and has ground access or
    public float balconySpacing = 0.6f;//The Spacing we have on each side of door/window of balcony
    public float balconyDepth = 3f;//The Amount the Balcony Protrudes for walking space
    public float balconyHeight = 0.6f;//The Amount the Balcony Height for its rails
    public float balconyThickness = 0.6f;//How thick the Wall Is for the Balcony
    //Window Ledges
    public bool topLedge = false;//If true, user wants the top of the window to have a decorative ledge.
    public bool bottomLedge = false;//If true, user wants the bottom of the window to have a decorative ledge.
    public float ledgeWidth = 0.6f;
    public float ledgeHeight = 0.32f;
    public float ledgeDepth = 0.16f;

	//Initilizer with only list.
	public CiDyPanel(List<Vector3> newWall, Transform worldPos, PanelType newType, bool groundAccess){

        //Random Balcony
        if (Random.Range(0, 9) == 0) {
            hasBalcony = true;
        }
        //Random Window type
        if (newType == PanelType.Window) {
            /*switch (Random.Range(0, 4)) {
                case 0:
                    windowType = WindowType.Clear;
                    break;
                case 1:
                    windowType = WindowType.Single;
                    break;
                case 2:
                    windowType = WindowType.Double;
                    break;
                case 3:
                    windowType = WindowType.T;
                    break;
            }*/
            topLedge = true;
        }
		//Debug.Log ("CreatedPanel");
		wall = newWall;
		panelWidth = Vector3.Distance(wall[0],wall[1]);
		panelHeight = Vector3.Distance(wall[1],wall[2]);
		transform = worldPos;
		groundPanel = groundAccess;
        //Make sure super small panels are just walls.
        if (panelWidth < width + (spacing * 2))
        {
            panelType = PanelType.Wall;
        }
        else
        {
            if (newType == PanelType.Door && !groundPanel)
            {
                panelType = PanelType.Window;
            }
            else if (newType == PanelType.Window && groundPanel)
            {
                panelType = PanelType.Wall;
            }
            else
            {
                //Set Type
                panelType = newType;
            }
        }
        
		//Calculate Vertices/Tris/Uvs.
		CalculateMesh();
	}
	
	void ClearMesh(){
		if(doorMesh)
			doorMesh = new Mesh();
		if(wallMesh)
			wallMesh = new Mesh();
		if(balconyMesh)
			balconyMesh = new Mesh();
		if(windowBoardMesh)
			windowBoardMesh = new Mesh();
		if(windowMesh)
			windowMesh = new Mesh();
	}
	//This function will Calculate the Mesh based on the Panels Editable Variables.
	public void CalculateMesh(){
		//Clear Any currentMeshs
		ClearMesh ();
		//Debug.Log ("CalulcateMesh");
		//Clear previous mesh
		Mesh curMesh = new Mesh ();
        //Clamp Values that must be clamped based on Panel Size Ect
        //Test for variables being over or equal to panelDimensions.
        if (width >= panelWidth)
        {
            width = panelWidth - spacing;
        }
        if (height >= panelHeight)
        {
            height = panelHeight - spacing;
        }
        //Clamp Ledge Values
        ledgeWidth = Mathf.Clamp(ledgeWidth, width + spacing, panelWidth - spacing);
        ledgeDepth = Mathf.Clamp(ledgeDepth, 0.01f, panelHeight);
        ledgeHeight = Mathf.Clamp(ledgeHeight, 0.01f, panelHeight - height);
        //Balcony Clamping
        balconySpacing = Mathf.Clamp(balconySpacing, 1f, (panelWidth - width)/2 - spacing);
        balconyThickness = Mathf.Clamp(balconyThickness, 0.01f, 2);
        balconyHeight = Mathf.Clamp(balconyHeight, 0.01f, height-spacing);
        balconyDepth = Mathf.Clamp(balconyDepth, 1f, panelWidth);
        //Base Directions Of Panel
        Vector3 rightDir = (wall[1] - wall[0]).normalized;
        Vector3 depthDir = Vector3.Cross(rightDir, Vector3.up).normalized;
        //balconyDepth
        //Add Panel Points as these are always going to be vertices regardless off Panel Type
        List<Vector3> verts = new List<Vector3> (0);
		//Setup initial Verts
		for(int i = 0;i<wall.Count;i++){
			verts.Add(wall[i]);
		}
		//Based on Enum
		if(panelType == PanelType.Door){
			//Calculate Verts based on Editable Variables
			//Calculate center of bottom Edge
			Vector3 cntr = (wall[0]+wall[1])/2;
			//Project Left and Right Points from center based on (Width/2)
			Vector3 leftFPoint = cntr+(-rightDir*width/2);
			verts.Add(leftFPoint);
			Vector3 rightFPoint = cntr+(rightDir*width/2);
			verts.Add(rightFPoint);
			//Project Top Points
			Vector3 rightTopFPoint = rightFPoint+(Vector3.up*height);
			verts.Add(rightTopFPoint);
			Vector3 leftTopFPoint = leftFPoint+(Vector3.up*height);
			verts.Add(leftTopFPoint);
			//Duplicat these for Proper Uving later
			verts.Add(leftFPoint);
			verts.Add(rightFPoint);
			//Project Top Points
			verts.Add(rightTopFPoint);
			verts.Add(leftTopFPoint);
			//Project Door Points
			Vector3 leftDPoint = leftFPoint+(depthDir*depth);
			verts.Add(leftDPoint);
			Vector3 rightDPoint = rightFPoint+(depthDir*depth);
			verts.Add(rightDPoint);
			Vector3 rightTopDPoint = rightTopFPoint+(depthDir*depth);
			verts.Add(rightTopDPoint);
			Vector3 leftTopDPoint = leftTopFPoint+(depthDir*depth);
			verts.Add(leftTopDPoint);
			for(int i = 0;i<verts.Count;i++){
				verts[i]-=transform.position;
			}
			//Add Vertices to Cur Mesh
			curMesh.vertices = verts.ToArray();
			//Now Enter the Tris
			int[] tris = new int[42]{
			0,7,4,
			0,3,7,
			3,6,7,
			3,2,6,
			2,1,6,
			1,5,6,
			8,12,9,
			12,13,9,
			8,11,15,
			15,12,8,
			11,10,15,
			15,10,14,
			14,10,13,
			13,10,9};
			curMesh.triangles = tris;
			float leftX = Vector3.Distance(verts[0],verts[4]);
			float rightX = Vector3.Distance(verts[0],verts[5]);
			float leftY = Vector3.Distance(verts[4],verts[7]);
			float rightY = Vector3.Distance(verts[5],verts[6]);
			//Calculate Wall UVS
			Vector2[] uvs = new Vector2[16]{
				new Vector2(0,0),
				new Vector2(panelWidth,0),
				new Vector2(panelWidth,panelHeight),
				new Vector2(0,panelHeight),
				new Vector2(leftX,0),
				new Vector2(rightX,0),
				new Vector2(rightX,rightY),
				new Vector2(leftX,leftY),
				//Depth Uvs
				new Vector2(0f,panelHeight/2),
				new Vector2(0,0),
				new Vector2(0,panelHeight/2),
				new Vector2(0,panelHeight),
				new Vector2(depth,panelHeight/2),
				new Vector2(depth,0f),
				new Vector2(depth,panelHeight/2),
				new Vector2(depth,panelHeight),
			};
			curMesh.uv = uvs;
			curMesh.RecalculateBounds();
			curMesh.RecalculateNormals();
			//Set Wall Mesh
			wallMesh = curMesh;
			//Clear Values and Make Door Mesh 
			curMesh = new Mesh();
			verts = new List<Vector3>(0);
			//Project Door Points
			/*verts.Add(leftDPoint);
			verts.Add(rightDPoint);
			verts.Add(rightTopDPoint);
			verts.Add(leftTopDPoint);*/
			verts.Add(leftDPoint+(-rightDir*0.01f)+(Vector3.down*0.01f));
			verts.Add(rightDPoint+(rightDir*0.01f)+(Vector3.down*0.01f));
			verts.Add(rightTopDPoint+(rightDir*0.01f)+(Vector3.up*0.01f));
			verts.Add(leftTopDPoint+(-rightDir*0.01f)+(Vector3.up*0.01f));
			for(int i = 0;i<verts.Count;i++){
				verts[i]-=transform.position;
			}
			//Add to CurMesh
			curMesh.vertices = verts.ToArray();
			//Calculate Triangles for Door
			tris = new int[6]{
				0,3,1,
				3,2,1
			};
			curMesh.triangles = tris;
			//Calculate Uvs for Door.
			uvs = new Vector2[4]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
			curMesh.uv = uvs;
			curMesh.RecalculateBounds();
			curMesh.RecalculateNormals();
			//Set Door Mesh
			doorMesh = curMesh;
		} else if(panelType == PanelType.Window){
			//Calculate Verts based on Editable Variables
			//Calculate Wall Mesh.
			Vector3 cntr = (wall[0]+wall[1]+wall[2]+wall[3])/4;
			Vector3 leftBPoint = cntr+(-rightDir*(width/2))+(-Vector3.up*(height/2));
			Vector3 rightBPoint = cntr+(rightDir*(width/2))+(-Vector3.up*(height/2));
			Vector3 rightTopPoint =  cntr+(rightDir*(width/2))+(Vector3.up*(height/2));
			Vector3 leftTopPoint = cntr+(-rightDir*(width/2))+(Vector3.up*(height/2));
			verts.Add(leftBPoint);
			verts.Add(rightBPoint);
			verts.Add(rightTopPoint);
			verts.Add(leftTopPoint);
			verts.Add(leftBPoint);
			verts.Add(rightBPoint);
			verts.Add(rightTopPoint);
			verts.Add(leftTopPoint);
			//Plot Depth Points
			Vector3 leftDPoint = leftBPoint+(depthDir*depth);
			Vector3 rightDPoint = rightBPoint+(depthDir*depth);
			Vector3 rightTopDPoint = rightTopPoint+(depthDir*depth);
			Vector3 leftTopDPoint = leftTopPoint+(depthDir*depth);
			verts.Add(leftDPoint);
			verts.Add(rightDPoint);
			verts.Add(rightTopDPoint);
			verts.Add(leftTopDPoint);
			for(int i = 0;i<verts.Count;i++){
				verts[i]-=transform.position;
			}
            //Add Vertices to Cur Mesh
            curMesh.vertices = verts.ToArray();
			//Now Enter the Tris
			int[] tris = new int[48]{
				0,7,4,
				0,3,7,
				3,6,7,
				3,2,6,
				2,5,6,
				2,1,5,
				1,4,5,
				1,0,4,
				8,11,15,
				15,12,8,
				11,10,14,
				14,15,11,
				14,10,13,
				13,10,9,
				9,12,13,
				9,8,12};
			curMesh.triangles = tris;
			float cntrX = Vector3.Distance(verts[0],((verts[0]+verts[1])/2));
			float cntrY = Vector3.Distance(verts[1],((verts[1]+verts[2])/2));
			float leftX = (cntrX-(width/2));
			float rightX = (cntrX+(width/2));
			float bottomY = (cntrY-(height/2));
			float upperY = (cntrY+(height/2));
			//Calculate Wall UVS
			Vector2[] uvs = new Vector2[16]{
				new Vector2(0,0),
				new Vector2(panelWidth,0),
				new Vector2(panelWidth,panelHeight),
				new Vector2(0,panelHeight),
				new Vector2(leftX,bottomY),
				new Vector2(rightX,bottomY),
				new Vector2(rightX,upperY),
				new Vector2(leftX,upperY),
				//Depth Uvs
				new Vector2(0f,panelHeight/2),
				new Vector2(0,0),
				new Vector2(0,panelHeight/2),
				new Vector2(0,panelHeight),
				new Vector2(depth,panelHeight/2),
				new Vector2(depth,0f),
				new Vector2(depth,panelHeight/2),
				new Vector2(depth,panelHeight),
			};
			curMesh.uv = uvs;
			curMesh.RecalculateBounds();
			curMesh.RecalculateNormals();
			//Set Wall Mesh
			wallMesh = curMesh;
			//Clear Values and Make Window Mesh 
			curMesh = new Mesh();
			verts = new List<Vector3>(0);
            //Project Window Points
            verts.Add(leftDPoint+(-rightDir*0.01f)+(Vector3.down*0.01f));
			verts.Add(rightDPoint+(rightDir*0.01f)+(Vector3.down*0.01f));
			verts.Add(rightTopDPoint+(rightDir*0.01f)+(Vector3.up*0.01f));
			verts.Add(leftTopDPoint+(-rightDir*0.01f)+(Vector3.up*0.01f));
			for(int i = 0;i<verts.Count;i++){
				verts[i]-=transform.position;
			}
			//Add to CurMesh
			curMesh.vertices = verts.ToArray();
            //Calculate Triangles for Window
            tris = new int[6]{
				0,3,1,
				3,2,1
			};
			curMesh.triangles = tris;
            //Calculate Uvs for Window.
            uvs = new Vector2[4]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
			curMesh.uv = uvs;
			curMesh.RecalculateBounds();
			curMesh.RecalculateNormals();
			//Set Door Mesh
			windowMesh = curMesh;
            //Create OutlineMesh for Window boards
            curMesh = new Mesh();
            Vector3[] windowBoardEdge = new Vector3[4];
            windowBoardEdge[3] = leftDPoint - transform.position;
            windowBoardEdge[2] = rightDPoint - transform.position;
            windowBoardEdge[1] = rightTopDPoint - transform.position;
            windowBoardEdge[0] = leftTopDPoint - transform.position;

            windowBoardMesh = CiDyUtils.ExtrudeInset(windowBoardEdge, boardWidth, boardDepth, transform);

            if (windowType != WindowType.Clear)
            {
                //Clear Values and Make WindowBoards
                curMesh = new Mesh();
                verts = new List<Vector3>(0);
                Vector3 point0 = cntr + (depthDir * depth) + (Vector3.down * (height / 2) + (-rightDir * (boardWidth / 2)));
                Vector3 point1 = cntr + (depthDir * depth) + (Vector3.down * (height / 2) + (rightDir * (boardWidth / 2)));
                Vector3 point2 = cntr + (depthDir * depth) + (Vector3.down * (boardWidth / 2) + rightDir * (boardWidth / 2));
                Vector3 point3 = cntr + (depthDir * depth) + (Vector3.down * (boardWidth / 2) + rightDir * (width / 2));
                Vector3 point4 = cntr + (depthDir * depth) + (Vector3.up * (boardWidth / 2) + rightDir * (width / 2));
                Vector3 point5 = cntr + (depthDir * depth) + (Vector3.up * (boardWidth / 2) + rightDir * (boardWidth / 2));
                Vector3 point6 = cntr + (depthDir * depth) + (Vector3.up * (height / 2) + rightDir * (boardWidth / 2));
                Vector3 point7 = cntr + (depthDir * depth) + (Vector3.up * (height / 2) + (-rightDir * (boardWidth / 2)));
                Vector3 point8 = cntr + (depthDir * depth) + (Vector3.up * (boardWidth / 2) + (-rightDir * (boardWidth / 2)));
                Vector3 point9 = cntr + (depthDir * depth) + (Vector3.up * (boardWidth / 2) + (-rightDir * (width / 2)));
                Vector3 point10 = cntr + (depthDir * depth) + (Vector3.down * (boardWidth / 2) + (-rightDir * (width / 2)));
                Vector3 point11 = cntr + (depthDir * depth) + (Vector3.down * (boardWidth / 2) + (-rightDir * (boardWidth / 2)));

                switch (windowType)
                {
                    case WindowType.Single:
                        verts.Add(point10);
                        verts.Add(point3);
                        verts.Add(point4);
                        verts.Add(point9);
                        //Update Positions for Panels Location
                        for (int i = 0; i < verts.Count; i++)
                        {
                            verts[i] -= transform.position;
                        }
                        curMesh = CiDyUtils.ExtrudePrint(verts.ToArray(), boardDepth, transform, true);
                        
                        break;
                    case WindowType.Double:
                        verts.Add(point0);
                        verts.Add(point1);
                        verts.Add(point6);
                        verts.Add(point7);
                        //Update Positions for Panels Location
                        for (int i = 0; i < verts.Count; i++)
                        {
                            verts[i] -= transform.position;
                        }
                        /*//Calculate Triangles
                        tris = new int[6]{
                            2,1,0,
                            2,0,3,
                        };
                        //Calculate Window Board UVS
                        leftX = Vector3.Distance(verts[0], verts[1]) / width;
                        //rightX = (leftX + (boardWidth / 2)) / width;
                        bottomY = Vector3.Distance(verts[0], verts[3]) / height;
                        //upperY = (bottomY + (boardWidth / 2)) / height;
                        uvs = new Vector2[4]{
                            new Vector2(0,0),
                            new Vector2(leftX,0),
                            new Vector2(leftX,bottomY),
                            new Vector2(0,bottomY),
                        };*/
                        curMesh = CiDyUtils.ExtrudePrint(verts.ToArray(), boardDepth, transform, true);
                        break;
                    case WindowType.T:
                        verts.Add(point0);
                        verts.Add(point1);
                        verts.Add(point2);
                        verts.Add(point3);
                        verts.Add(point4);
                        verts.Add(point5);
                        verts.Add(point6);
                        verts.Add(point7);
                        verts.Add(point8);
                        verts.Add(point9);
                        verts.Add(point10);
                        verts.Add(point11);
                        //Update Positions for Panels Location
                        for (int i = 0; i < verts.Count; i++)
                        {
                            verts[i] -= transform.position;
                        }
                        curMesh = CiDyUtils.ExtrudePrint(verts.ToArray(), boardDepth, transform, true);
                        /*//Calculate Triangles
                        //Now Enter the Tris
                        tris = new int[30]{
                            0,11,2,
                            0,2,1,
                            2,5,3,
                            5,4,3,
                            5,8,6,
                            8,7,6,
                            8,11,9,
                            9,11,10,
                            11,8,2,
                            8,5,2
                        };
                        //Calculate Window Board UVS
                        leftX = Vector3.Distance(verts[10], verts[11]) / width;
                        rightX = (leftX + (boardWidth / 2)) / width;
                        bottomY = Vector3.Distance(verts[0], verts[11]) / height;
                        upperY = (bottomY + (boardWidth / 2)) / height;
                        uvs = new Vector2[12]{
                            new Vector2(leftX,0),
                            new Vector2(rightX,0),
                            new Vector2(rightX,bottomY),
                            new Vector2(1,bottomY),
                            new Vector2(1,upperY),
                            new Vector2(rightX,upperY),
                            new Vector2(rightX,1),
                            new Vector2(leftX,1),
                            new Vector2(leftX,upperY),
                            new Vector2(0,upperY),
                            new Vector2(0,bottomY),
                            new Vector2(leftX,bottomY),
                        };*/
                        break;
                }
                //Update Mesh with its vertices and Triangles
                /*curMesh.vertices = verts.ToArray();
                curMesh.triangles = tris;
                curMesh.uv = uvs;
                curMesh.RecalculateBounds();
                curMesh.RecalculateNormals();*/
                if (curMesh != null)
                {
                    //Debug.Log("Combine");
                    //Add Window Board Mesh
                    CombineInstance[] combines = new CombineInstance[2];
                    combines[0].mesh = curMesh;
                    combines[1].mesh = windowBoardMesh;
                    //Initialize Doors
                    Mesh finalMesh = new Mesh();
                    //Combine
                    finalMesh.CombineMeshes(combines, true, false);
                    /*for (int i = 0; i < finalMesh.vertexCount; i++) {
                        CiDyUtils.MarkPoint(finalMesh.vertices[i], i);
                    }*/
                    windowBoardMesh = finalMesh;
                }
            }
		} else if(panelType == PanelType.Wall){
			for(int i = 0;i<verts.Count;i++){
				verts[i]-=transform.position;
			}
			//Calculate Plane Wall Mesh
			curMesh.vertices = verts.ToArray();
			//Now Enter the Tris
			int[] tris = new int[6]{
			0,3,1,
			3,2,1};
			curMesh.triangles = tris;
			//Calculate Wall UVS
			Vector2[] uvs = new Vector2[4]{
				new Vector2(0,0),
				new Vector2(panelWidth,0),
				new Vector2(panelWidth,panelHeight),
				new Vector2(0,panelHeight),
			};
			curMesh.uv = uvs;
			curMesh.RecalculateBounds();
			curMesh.RecalculateNormals();
			wallMesh = curMesh;
		}
        //Handle Balcony Logic
        if (hasBalcony && panelType != PanelType.Wall && !groundPanel) {
            //Clear Values and Make Balconies
            curMesh = new Mesh();
            //Calculate Width
            float balconyWidth = width + (spacing * 2);
            //Clamp Value to Panel Width(Max)
            Mathf.Clamp(balconyWidth, balconyWidth, panelWidth);
            //Grab Panel Points and Directions
            //Vector3 cntr = (wall[0] + wall[1] + wall[2] + wall[3]) / 4;
            //Calculate Right Direction of Bottom Edge
            //Vector3 leftBPoint = cntr + (-rightDir * (width / 2)) + (-Vector3.up * (height / 2));
            //Vector3 rightBPoint = cntr + (rightDir * (width / 2)) + (-Vector3.up * (height / 2));
            //Vector3 rightTopPoint = cntr + (rightDir * (width / 2)) + (Vector3.up * (height / 2));
            //Vector3 leftTopPoint = cntr + (-rightDir * (width / 2)) + (Vector3.up * (height / 2));
            //Calculate Verts
            verts = new List<Vector3>(0);
            //For Inset Thickness
            List<Vector3> balconyFootPrint = new List<Vector3>(0);//Feed it clockwise for PolygonInset orientation results in inset or outset/ (Clockwise = inset, counterClockwise = outset)
            List<Vector3> balconyInsetPrint = new List<Vector3>(0);
            //Determine Balcony Location based on Panel Type
            if (panelType == PanelType.Window)
            {
                //Create Balcony Floor Print
                balconyFootPrint.Add(windowMesh.vertices[1] + (rightDir * balconySpacing));
                balconyFootPrint.Add(balconyFootPrint[0] + (-depthDir * balconyDepth));
                balconyFootPrint.Add(windowMesh.vertices[0] + (-depthDir * balconyDepth) + (-rightDir * balconySpacing));
                balconyFootPrint.Add(windowMesh.vertices[0] + (-rightDir * balconySpacing));
            }
            else if (panelType == PanelType.Door) {
                //Create Balcony Floor Print
                balconyFootPrint.Add(doorMesh.vertices[1] + (rightDir * balconySpacing));
                balconyFootPrint.Add(balconyFootPrint[0] + (-depthDir * balconyDepth));
                balconyFootPrint.Add(doorMesh.vertices[0] + (-depthDir * balconyDepth) + (-rightDir * balconySpacing));
                balconyFootPrint.Add(doorMesh.vertices[0] + (-rightDir * balconySpacing));
            }
            //Foot Print is currently Clockwise Oriented for the InsetPolygon Algorithm to work properly
            //Inset FootPrint of Balcony for Thickness Positions
            balconyInsetPrint = CiDyUtils.PolygonInset(balconyFootPrint, balconyThickness);
            balconyInsetPrint.Reverse();
            balconyFootPrint.Reverse();
          
            List<Vector2> newUVs = new List<Vector2> (0);
            //Create RoofWall
            for (int i = 0; i < balconyFootPrint.Count-1; i++)
            {
                Vector3 v0 = balconyFootPrint[i];
                Vector3 v1 = balconyFootPrint[i] + (Vector3.up * balconyHeight);
                Vector3 v2 = balconyInsetPrint[i] + (Vector3.up * balconyHeight);
                Vector3 v3 = balconyInsetPrint[i];
                //Add to Verts List
                verts.Add(v0);
                verts.Add(v1);
                verts.Add(v1);
                verts.Add(v2);
                verts.Add(v2);
                verts.Add(v3);
                //Now Create the Other End of this Sides Wall.
                Vector3 v0b;
                Vector3 v1b;
                Vector3 v2b;
                Vector3 v3b;
                
                v0b = balconyFootPrint[i + 1];
                v1b = balconyFootPrint[i + 1] + (Vector3.up * balconyHeight);
                v2b = balconyInsetPrint[i + 1] + (Vector3.up * balconyHeight);
                v3b = balconyInsetPrint[i + 1];
                verts.Add(v0b);
                verts.Add(v1b);
                verts.Add(v1b);
                verts.Add(v2b);
                verts.Add(v2b);
                verts.Add(v3b);
                //Setup Uvs
                float xDist = Vector3.Distance(v0, v0b);
                newUVs.Add(new Vector2(0, 0));
                newUVs.Add(new Vector2(0, balconyHeight));
                newUVs.Add(new Vector2(0, 0));
                newUVs.Add(new Vector2(0, balconyHeight));
                newUVs.Add(new Vector2(0, 0));
                newUVs.Add(new Vector2(0, balconyHeight));

                newUVs.Add(new Vector2(xDist, 0));
                newUVs.Add(new Vector2(xDist, balconyHeight));
                newUVs.Add(new Vector2(xDist, 0));
                newUVs.Add(new Vector2(xDist, balconyHeight));
                newUVs.Add(new Vector2(xDist, 0));
                newUVs.Add(new Vector2(xDist, balconyHeight));
            }
            
            List<int> tris = new List<int>(0);
            for (int i = 0; i < verts.Count - 6; i += 6)
            {
                
                    //Beginning or Middle
                    tris.Add(i);
                    tris.Add(i + 1);
                    tris.Add(i + 6);

                    tris.Add(i + 1);
                    tris.Add(i + 7);
                    tris.Add(i + 6);

                    tris.Add(i + 2);
                    tris.Add(i + 3);
                    tris.Add(i + 8);

                    tris.Add(i + 3);
                    tris.Add(i + 9);
                    tris.Add(i + 8);

                    tris.Add(i + 4);
                    tris.Add(i + 5);
                    tris.Add(i + 10);

                    tris.Add(i + 5);
                    tris.Add(i + 11);
                    tris.Add(i + 10);
            }

            //Add Bottom of Balcony Verts/UVS and Tris
            //At End of Iteration
            //Add bottom Floor Verts and Tris for Proper Normal Calculations
            verts.Add(balconyFootPrint[0]);
            verts.Add(balconyFootPrint[1]);
            verts.Add(balconyFootPrint[2]);
            verts.Add(balconyFootPrint[3]);
            float xdist = Vector3.Distance(balconyFootPrint[0], balconyFootPrint[3]);
            float ydist = Vector3.Distance(balconyFootPrint[0], balconyFootPrint[1]);
            //Add UVS for Bottom of Balcony
            newUVs.Add(new Vector2(0, 0));
            newUVs.Add(new Vector2(0, ydist));
            newUVs.Add(new Vector2(xdist, ydist));
            newUVs.Add(new Vector2(xdist, 0));

            tris.Add(37);
            tris.Add(38);
            tris.Add(39);
            tris.Add(39);
            tris.Add(36);
            tris.Add(37);
            //Add Top View using Inset Values
            verts.Add(balconyInsetPrint[0]);
            verts.Add(balconyInsetPrint[1]);
            verts.Add(balconyInsetPrint[2]);
            verts.Add(balconyInsetPrint[3]);
            xdist = Vector3.Distance(balconyInsetPrint[0], balconyInsetPrint[3]);
            ydist = Vector3.Distance(balconyInsetPrint[0], balconyInsetPrint[1]);
            //Add UVS for Bottom of Balcony
            newUVs.Add(new Vector2(xdist, 0));
            newUVs.Add(new Vector2(xdist, ydist));
            newUVs.Add(new Vector2(0, ydist));
            newUVs.Add(new Vector2(0, 0));

            tris.Add(40);
            tris.Add(43);
            tris.Add(41);
            tris.Add(43);
            tris.Add(42);
            tris.Add(41);
            //Update Mesh
            //Calculate Plane Wall Mesh
            curMesh.vertices = verts.ToArray();
            curMesh.triangles = tris.ToArray();
            curMesh.uv = newUVs.ToArray();
            curMesh.RecalculateNormals();
            curMesh.RecalculateBounds();
            balconyMesh = curMesh;
        }
        //Handle Ledges Logic
        //Ledges are for windows only
        if (panelType == PanelType.Window && !groundPanel)
        {
            if (topLedge || bottomLedge)
            {
                //Lets handle bottom ledge first. If Balcony we will not create a bottom ledge
                if (bottomLedge && !hasBalcony)
                {
                    //Debug.Log("Creating Bottom Ledge");
                    //Create bottom Ledge Decorative Graphic
                    //Place Vertices
                    //Calculate Center of Window for Orientation of Bottom Ledge
                    Vector3 centr = windowMesh.vertices[0] + rightDir * (width / 2) + (-depthDir * depth);
                    List<Vector3> ledgeFootprint = new List<Vector3>(0);
                    ledgeFootprint.Add(centr + (-rightDir * (ledgeWidth / 2)));
                    ledgeFootprint.Add(centr + (-rightDir * (ledgeWidth / 2)) + Vector3.down * ledgeHeight);
                    ledgeFootprint.Add(centr + (rightDir * (ledgeWidth / 2)) + Vector3.down * ledgeHeight);
                    ledgeFootprint.Add(centr + (rightDir * (ledgeWidth / 2)));

                    balconyMesh = CiDyUtils.ExtrudePrint(ledgeFootprint.ToArray(), ledgeDepth, transform, true);
                }
                if (topLedge)
                {
                    //Create bottom Ledge Decorative Graphic
                    curMesh = new Mesh();
                    verts = new List<Vector3>(0);
                    //Place Vertices
                    //Calculate Center of Window for Orientation of Bottom Ledge
                    Vector3 centr = windowMesh.vertices[3] + rightDir * (width / 2) + (-depthDir * depth);
                    List<Vector3> ledgeFootprint = new List<Vector3>(0);
                    ledgeFootprint.Add(centr + (rightDir * (ledgeWidth / 2)));
                    ledgeFootprint.Add(centr + (rightDir * (ledgeWidth / 2)) + Vector3.up * ledgeHeight);
                    ledgeFootprint.Add(centr + (-rightDir * (ledgeWidth / 2)) + Vector3.up * ledgeHeight);
                    ledgeFootprint.Add(centr + (-rightDir * (ledgeWidth / 2)));

                    //Create Ledge Mesh
                    curMesh = CiDyUtils.ExtrudePrint(ledgeFootprint.ToArray(), ledgeDepth, transform, true);
                }

                //If we already have a balcony Mesh.(bottom Ledge or Balcony) lets combine the meshes for the final result.
                if (balconyMesh && topLedge)
                {
                    Mesh tmpMesh = new Mesh();
                    //Combine Top Ledge
                    CombineInstance[] combine = new CombineInstance[2];
                    combine[0].mesh = balconyMesh;//Bottom Ledge Mesh
                    combine[1].mesh = curMesh;
                    combine[0].transform = transform.localToWorldMatrix;
                    combine[1].transform = transform.localToWorldMatrix;
                    //Combine
                    tmpMesh.CombineMeshes(combine, true, false);
                    balconyMesh = tmpMesh;
                }
                else if (topLedge)
                {
                    //There is only a top ledge and no bottom
                    balconyMesh = curMesh;//(TopLedge)
                }
            }
        }
    }
}
