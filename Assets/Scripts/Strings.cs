using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Strings
{
    // non-localizable
    public const string MapPanelName = "MapPanel";
    public static readonly string[] CarColliderNames = { "ColliderBody" }; // TODO: Do we need them all? { "ColliderBody", "ColliderFront", "ColliderBottom" };
    public const string PlayermarkText = "PlayermarkText";
    public const string PanelCloseButtonPath = "Window/Close/Background/Label";
    public const string MapImagePath = "Window/MapPanel/MapBackground/MapImage";
    public const string StartTag = "Start";
    public const string ShowInstructionsAtStart = "ShowInstructionsAtStart";

    // localizable
    public const string StartGame = "Start Game";
    public const string Back = "Back";
    public const string SaveAndContinue = "Save and Continue";
    public const string MoveToNextLevel = "Move To Next Level";
    public const string CheckScore = "Check Score";

    public const string Springfield = "Springfield";

    // format strings
    public const string ScoreTextFormat = "Score: {0:d3}";
    public const string LevelTextFormat = "Level: {0}";
}
