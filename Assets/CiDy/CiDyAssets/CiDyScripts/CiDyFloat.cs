using UnityEngine;
using System.Collections;

public class CiDyFloat {
	public float value;
	public int listLoc;
	
	//Initializer
	public CiDyFloat(float newValue){
		value = newValue;
		listLoc = 0;
	}

	//Initializer
	public CiDyFloat(float newValue, int newLoc){
		value = newValue;
		listLoc = newLoc;
	}
}
