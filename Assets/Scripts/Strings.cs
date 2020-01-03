using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Strings
{
    // non-localizable

    // general non-localizable strings
    public static readonly string[] CarColliderNames = { "ColliderBody" }; // TODO: Do we need them all? { "ColliderBody", "ColliderFront", "ColliderBottom" };
    public const string ShowInstructionsAtStart = "ShowInstructionsAtStart";

    // object names and paths
    public const string PlayermarkName = "Playermark";
    public const string PlayermarkTextName = "PlayermarkText";
    public const string EmptyPlayermarkName = "PlayermarkEmpty";
    public const string PlayermarkIndexEmptyPath = "PlayermarkEmpty/PlayermarkIndexEmpty";
    public const string PlayermarkIndexPath = "Playermark/PlayermarkIndex";
    public const string PlayermarkTextPath = "PlayermarkText";
    public const string MainMenuName = "MainMenu";
    public const string PanelsName = "Panels";
    public const string PanelManagerPath = "PanelManager";
    public const string InstructionsPanelPath = "Panels/Instructions";
    public const string MapPanelPath = "Panels/Map";
    public const string MenuScoreTextPath = "OpenMenuButton/ScoreText";
    public const string MenuTimeTextPath = "OpenMenuButton/TimeText";
    public const string PanelCloseButtonPath = "Window/Close/Background/Label";
    public const string MapImagePath = "Window/MapPanel/MapBackground/MapImage";
    public const string LevelTextPath = "Window/MapPanel/PlayermarksPanel/Score/LevelText";
    public const string TotalScoreTextPath = "Window/MapPanel/PlayermarksPanel/Score/TotalScoreText";
    public const string PlayermarksPath = "Window/MapPanel/PlayermarksPanel/Playermarks";
    public const string ShowLandmarksTogglePath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle";
    public const string ShowLandmarksBackgroundPath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle/Background";
    public const string ShowLandmarksLabelPath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle/Label";
    public const string PopupWindowPath = "Window";
    public const string PopupMessageTextPath = "Window/Popup/Background/MessageText";
    public const string LandmarkOnMap = "LandmarkOnMap";
    public const string ButtonClickAudioSourceName = "ButtonClickAudioSource";
    public const string LandmarksPath = "Landmarks";
    public const string LandmarkName = "Name";
    public const string LandmarkName2 = "Name2";
    public const string NodeHolderPath = "CiDyGraph/NodeHolder";
    public const string RoadHolderPath = "CiDyGraph/RoadHolder";

    // tag names
    public const string MainMenuUITag = "MainMenuUI";
    public const string LandmarkTag = "Landmark";
    public const string PlayermarkTag = "Playermark";

    // level names
    public const string Springfield = "Springfield";

    // landmark names
    public static readonly string[] LandmarkNames = { "School", "Library", "Museum", "City Hall", "Post Office" };

    // localizable strings

    // text field values
    public const string StartGame = "Start Game";
    public const string Back = "Back";
    public const string SaveAndContinue = "Continue Game";
    public const string MoveToNextLevel = "Play Again";
    public const string CheckScore = "Check Score";

    // format strings
    public const string ScoreTextFormat = "Score: {0:d3}";
    public const string TimeTextFormat = "Time: {0:d3}";
    public const string LevelTextFormat = "Level: {0}";
    public const string LandmarkCrossedMessageFormat = "You just crossed the \"{0}\" landmark!";
    public const string FirstLandmarkCrossedMessage = "\r\n\r\nWe will now demonstrate how to mark its position on the map.";
    public const string OtherLandmarkCrossedMessage = "\r\n\r\nDrag and drop the blinking marker to the appropriate position on the map.";
    public const string LevelCompleteMessageFormat = "Congratulations! Level completed!\r\n\r\nYou scored {0} points in {1} seconds!";
}
