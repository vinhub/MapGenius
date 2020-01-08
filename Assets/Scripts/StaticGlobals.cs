using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SavedLandmark
{
    public string name;
    public Vector3 pos;
    public Quaternion rotation;
}

public static class StaticGlobals
{
    public static bool SavedInitStateExists { get; set; }
    public static List<SavedLandmark> SavedLandmarks { get; set; }
    public static Vector3 SavedCarPosStart { get; set; }
    public static Quaternion SavedCarRotationStart { get; set; }
}