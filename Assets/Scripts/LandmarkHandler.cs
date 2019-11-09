using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkHandler : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if the collider was any of the car colliders
        if (Array.FindIndex(Strings.CarColliderNames, s => s == other.name) >= 0)
        {
            // Debug.Log("Car crossed landmark " + this.name);
            GameSystem.Instance.LandmarkCrossed(this.name);
        }
    }
}
