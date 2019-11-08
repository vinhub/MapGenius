using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkHandler : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "ColliderBody" || other.name == "ColliderFront" || other.name == "ColliderBottom")
        {
            Debug.Log("Car crossed landmark " + this.name);
        }
    }
}
