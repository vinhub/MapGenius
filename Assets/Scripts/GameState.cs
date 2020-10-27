using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TriangleNet.Voronoi.Legacy;
using UnityEngine;

// generic stuff
public enum GameLevel { Downtown = 0, Smalltown = 1, Oldtown = 2, FutureTown = 3 };
public enum DrivingMode { Normal, Free, VictoryLap };

public class LevelInfo
{
    public GameLevel GameLevel { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsEnabled { get; set; }

    public LevelInfo(GameLevel gameLevel, bool isAvaliable, bool isEnabled)
    {
        GameLevel = gameLevel;
        IsAvailable = isAvaliable;
        IsEnabled = isEnabled;
    }

    public string getName() { return GameLevel.ToString(); }

    public static LevelInfo getLevelInfo(GameLevel gameLevel) { return GameState.LevelInfos[(int)gameLevel]; }
}

// saved as a part of the game state
public struct SavedLandmark
{
    public string name;
    public Vector3 pos;
    public Quaternion rotation;
}

public static class GameState
{
    // general globals
    public static bool IsGameStarting { get; set; } = true; // true only when the game is just starting, set to false when the instructions dialog is closed.
    public const int MaxLevelScore = 100; // max score for a level
    public static DrivingMode CurDrivingMode = DrivingMode.Normal;

    public static LevelInfo[] LevelInfos = new LevelInfo[]
    {
        new LevelInfo(GameLevel.Downtown, true, true), // we start with just the lowest level enabled
        new LevelInfo(GameLevel.Smalltown, true, false),
        new LevelInfo(GameLevel.Oldtown, false, false),
        new LevelInfo(GameLevel.FutureTown, false, false),
    };

    // saved game state related items
    public static bool RetryingGame { get; set; }
    public static bool SavedStateExists { get { return SavedLandmarks != null; } }
    public static List<SavedLandmark> SavedLandmarks { get; set; }
    public static Quaternion SavedRotationOnTrack  { get; set; }
    public static CiDyRoad SavedRoadOnTrack { get; set; }
    public static int SavedOrigPointIndexOnTrack { get; set; }
}