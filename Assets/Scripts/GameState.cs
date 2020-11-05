using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TriangleNet.Voronoi.Legacy;
using UnityEngine;

// generic stuff
public enum GameLevel { Downtown = 0, Smalltown = 1, Oldtown = 2, FutureTown = 3, Exit = 4 }; // Exit is a placeholder for game exit, not a real level
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

// remembered as a part of the init game state
public struct LandmarkParams
{
    public string name;
    public Vector3 pos;
    public Quaternion rotation;
}

public static class GameState
{
    // general globals
    public static bool IsGameStarting { get; set; } // true only when the game is just starting at the lowest level, so instructions dialog can be show. Set to false when the instructions dialog is closed.
    public const int MaxLevelScore = 100; // max score for a level

    public static LevelInfo[] LevelInfos = new LevelInfo[]
    {
        new LevelInfo(GameLevel.Downtown, true, true), // we start with just the lowest level enabled
        new LevelInfo(GameLevel.Smalltown, true, false),
        new LevelInfo(GameLevel.Oldtown, false, false),
        new LevelInfo(GameLevel.FutureTown, false, false),
        new LevelInfo(GameLevel.Exit, false, false), // placeholder
    };

    // remember initial game state so the same game state can be reinitialized for retrying the level
    public static bool RetryingLevel { get; set; }
    public static bool InitStateExists { get { return InitLandmarks != null; } }
    public static List<LandmarkParams> InitLandmarks { get; set; }
    public static Quaternion InitRotationOnTrack  { get; set; }
    public static CiDyRoad InitRoadOnTrack { get; set; }
    public static int InitOrigPointIndexOnTrack { get; set; }
}