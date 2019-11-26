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
    public const string MainMenuName = "MainMenu";
    public const string PanelsName = "Panels";
    public const string PanelManagerPath = "PanelManager";
    public const string InstructionsPanelPath = "Panels/Instructions";
    public const string MapPanelPath = "Panels/Map";
    public const string MenuScoreTextPath = "OpenMenuButton/ScoreText";
    public const string PanelCloseButtonPath = "Window/Close/Background/Label";
    public const string MapImagePath = "Window/MapPanel/MapBackground/MapImage";
    public const string LevelTextPath = "Window/MapPanel/PlayermarksPanel/Score/LevelText";
    public const string TotalScoreTextPath = "Window/MapPanel/PlayermarksPanel/Score/TotalScoreText";
    public const string PlayermarksPath = "Window/MapPanel/PlayermarksPanel/Playermarks";

    // tag names
    public const string StartTag = "Start";
    public const string MainMenuUITag = "MainMenuUI";
    public const string LandmarkTag = "Landmark";

    // level names
    public const string Springfield = "Springfield";

    // localizable strings

    // text field values
    public const string StartGame = "Start Game";
    public const string Back = "Back";
    public const string SaveAndContinue = "Save and Continue";
    public const string MoveToNextLevel = "Move To Next Level";
    public const string CheckScore = "Check Score";

    // format strings
    public const string ScoreTextFormat = "Score: {0:d3}";
    public const string LevelTextFormat = "Level: {0}";

}
