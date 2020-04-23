using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkHandler : MonoBehaviour
{
    public bool IsVisited { get; set; } = false;

    private void OnTriggerEnter(Collider other)
    {
        // if the collider was any of the car colliders
        if (Array.FindIndex(Strings.CarColliderNames, s => s == other.name) >= 0)
        {
            // Debug.Log("Car crossed landmark " + this.name);

            if (!IsVisited) // notify only if this was the first visit to this landmark
            {
                IsVisited = true;

                GameSystem.Instance.LandmarkCrossed(this.name);
            }
        }
    }
}
