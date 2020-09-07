using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterHandler : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if car enters water, take them back to track
        GameSystem.Instance.GetBackOnTrack();
    }
}
