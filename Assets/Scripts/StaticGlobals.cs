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
    public const int MaxLevelScore = 100; // max score for a level
    public static GameLevel CurGameLevel { get; set; } = GameLevel.Smalltown;
    public static int TotalNumGames { get; set; } = 1; // total number of games attempted by the player so far
    public static float TotalScore { get; set; } = 0f; // player's total score so far
    public static float TotalScoreMax = StaticGlobals.MaxLevelScore * StaticGlobals.TotalNumGames;

    // saved game state related items
    public static bool SavedInitStateExists { get; set; }
    public static List<SavedLandmark> SavedLandmarks { get; set; }
    public static Vector3 SavedCarPos  { get; set; }
    public static Quaternion SavedCarRotation  { get; set; }
}