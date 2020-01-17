using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// generic stuff
public enum GameLevel { Demo = 0, Beginner = 1, Intermediate = 2, Advanced = 3 };

// saved as a part of the game state
public struct SavedLandmark
{
    public string name;
    public Vector3 pos;
    public Quaternion rotation;
}

public static class StaticGlobals
{
    // general globals
    public static GameLevel CurGameLevel { get; set; } = GameLevel.Demo;

    // saved game state related items
    public static bool SavedInitStateExists { get; set; }
    public static List<SavedLandmark> SavedLandmarks { get; set; }
    public static Vector3 SavedCarPosStart { get; set; }
    public static Quaternion SavedCarRotationStart { get; set; }
}