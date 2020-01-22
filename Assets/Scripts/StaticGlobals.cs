using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// generic stuff
public enum GameLevel { Downtown = 0, Smalltown = 1, Oldtown = 2 };

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
    public static GameLevel CurGameLevel { get; set; } = GameLevel.Downtown;

    // saved game state related items
    public static bool SavedInitStateExists { get; set; }
    public static List<SavedLandmark> SavedLandmarks { get; set; }
    public static Vector3 SavedCarPosStart { get; set; }
    public static Quaternion SavedCarRotationStart { get; set; }
}