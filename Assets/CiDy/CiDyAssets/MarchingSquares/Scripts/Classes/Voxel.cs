﻿using UnityEngine;
using System;

[Serializable]
public class Voxel {

	public bool state;

    public int vertexIndex = -1;
    public int polyIndex = -1;
	public Vector3 position;

	public float xEdge, yEdge;

    public Voxel upperVoxel;
    public Voxel rightVoxel;

	public Voxel (int x, int y, float size) {
		position.x = (x + 0.5f) * size;
		position.y = (y + 0.5f) * size;

		xEdge = float.MinValue;
		yEdge = float.MinValue;
	}

    public Voxel(Vector3 newPos, float size)
    {
        //position.x = (x + 0.5f) * size;
        //position.y = (y + 0.5f) * size;
        position = newPos;

        xEdge = float.MinValue;
        yEdge = float.MinValue;
    }

    public Voxel () {}

	public void BecomeXDummyOf (Voxel voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.x += offset;
		xEdge = voxel.xEdge + offset;
		yEdge = voxel.yEdge;
	}

	public void BecomeYDummyOf (Voxel voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.y += offset;
		xEdge = voxel.xEdge;
		yEdge = voxel.yEdge + offset;
	}

	public void BecomeXYDummyOf (Voxel voxel, float offset) {
		state = voxel.state;
		position = voxel.position;
		position.x += offset;
		position.y += offset;
		xEdge = voxel.xEdge + offset;
		yEdge = voxel.yEdge + offset;
	}
}


[Serializable]
public class VoxelSquare {

    public Voxel[] exteriorPoints;

    public Voxel topLeft, topRight, bottomLeft, bottomRight;
    public Voxel centreTop, centreRight, centreBottom, centreLeft, centre;
    public int configuration;

    public VoxelSquare(Voxel _TopLeft, Voxel _TopRight, Voxel _BottomRight, Voxel _BottomLeft) {
        topLeft = _TopLeft;
        topRight = _TopRight;
        bottomLeft = _BottomLeft;
        bottomRight = _BottomRight;

        if (topLeft.state) {
            configuration += 8;
        }
        if (topRight.state) {
            configuration += 4;
        }
        if (bottomRight.state) {
            configuration += 2;
        }
        if (bottomLeft.state) {
            configuration += 1;
        }

        //Create Intermediate Voxels
        centreTop = new Voxel((topLeft.position + topRight.position) / 2, 0.5f);
        //Create CenterRight
        centreRight = new Voxel((topRight.position + bottomRight.position) / 2, 0.5f);
        //Create CenterBottom
        centreBottom = new Voxel((bottomLeft.position + bottomRight.position) / 2, 0.5f);
        //Create Centre Left
        centreLeft = new Voxel((topLeft.position + bottomLeft.position) / 2, 0.5f);
        //Create Centre Point
        centre = new Voxel((topLeft.position + topRight.position + bottomLeft.position + bottomRight.position) /4, 0.5f);
        //Initialize Exterior Points
        exteriorPoints = new Voxel[0];
    }

    //Returns Array of Square Points in counterclockwise order, starting from bottom left point.
    public Vector3[] SquareOutline() {
        Vector3[] outline = new Vector3[4];

        outline[0] = bottomLeft.position;
        outline[1] = bottomRight.position;
        outline[2] = topRight.position;
        outline[3] = topLeft.position;

        //Return final Counterclockwise Positions List.
        return outline;
    }

    public void AddExteriorPoint(Voxel newPoint) {
        //Adding One Point
        if (exteriorPoints.Length == 0) {
            exteriorPoints = new Voxel[1];
            exteriorPoints[0] = newPoint;
        } else
        {
            //Adding another Point
            Voxel[] newArray =  new Voxel[exteriorPoints.Length+1];
            for (int i = 0; i < exteriorPoints.Length; i++) {
                newArray[i] = exteriorPoints[i];
            }
            newArray[exteriorPoints.Length] = newPoint;

            exteriorPoints = newArray;
        }
    }
}