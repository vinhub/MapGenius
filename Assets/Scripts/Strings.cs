public static class Strings
{
    // non-localizable

    // general non-localizable strings
    public static readonly string[] CarColliderNames = { "ColliderBody" }; // TODO: Do we need them all? { "ColliderBody", "ColliderFront", "ColliderBottom" };
    public const string HideInstructionsAtStart = "HideInstructionsAtStart";

    // panel names
    public const string MapPanelName = "Map";
    public const string InstructionsPanelName = "Instructions";
    public const string AboutPanelName = "About";

    // object names and paths
    public const string PlayermarkName = "Playermark";
    public const string PlayermarkTextName = "PlayermarkText";
    public const string EmptyPlayermarkName = "PlayermarkEmpty";
    public const string PlayermarkIndexEmptyPath = "PlayermarkEmpty/PlayermarkIndexEmpty";
    public const string PlayermarkIndexPath = "Playermark/PlayermarkIndex";
    public const string PlayermarkIndexPath2 = "PlayermarkIndex";
    public const string PlayermarkTextPath = "PlayermarkText";
    public const string MainMenuName = "MainMenu";
    public const string ContinueGameButtonPath = "MainMenu/Window/ContinueGame";
    public const string PanelsName = "Panels";
    public const string PanelManagerPath = "PanelManager";
    public const string OpenMenuButtonPath = "OpenMenuButton";
    public const string InstructionsPanelPath = "Panels/Instructions";
    public const string InstructionSteps = "Window/Instructions/Background";
    public const string AboutPanelPath = "Panels/About";
    public const string MapPanelPath = "Panels/Map";
    public const string StatusBarScoreTextPath = "ScoreStatus/Window/ScoreText";
    public const string StatusBarTimeTextPath = "TimeStatus/Window/TimeText";
    public const string StatusBarSpeedTextPath = "SpeedStatus/Window/SpeedText";
    public const string StatusBarRevsTextPath = "RevsStatus/Window/RevsText";
    public const string InfoMessagePath = "InfoMessage";
    public const string InfoMessageTextPath = "Window/InfoMessageText";
    //public const string OnTrackrLocatorPath = "UI/OnTrackLocator";
    public const string CloseButtonPath = "Window/ButtonBar/CloseButton";
    public const string ActionButtonPath = "Window/ButtonBar/ActionButton";
    public const string CloseButtonLabelPath = "Background/Label";
    public const string ActionButtonLabelPath = "Window/ButtonBar/ActionButton/Background/Label";
    public const string ButtonBarTogglePath = "Window/ButtonBar/Toggle";
    public const string MapImagePath = "Window/MapPanel/MapBackground/MapImage";
    public const string LevelTextPath = "Window/MapPanel/PlayermarksPanel/Score/LevelText";
    public const string TotalScoreTextPath = "Window/MapPanel/PlayermarksPanel/Score/TotalScoreText";
    public const string PlayermarksPath = "Window/MapPanel/PlayermarksPanel/Playermarks";
    public const string HintPath = "Window/MapPanel/PlayermarksPanel/Hint";
    public const string ShowLandmarksTogglePath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle";
    public const string ShowLandmarksBackgroundPath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle/Background";
    public const string ShowLandmarksLabelPath = "Window/MapPanel/PlayermarksPanel/Hint/Toggle/Label";
    public const string WindowPath = "Window";
    public const string PopupPath = "Popup";
    public const string PopupMessageTextPath = "Popup/Background/MessageText";
    public const string FloatingMessageTextPath = "FloatingMessage";
    public const string LandmarkOnMap = "LandmarkOnMap";
    public const string ButtonClickAudioSourceName = "ButtonClickAudioSource";
    public const string LandmarksPath = "Landmarks";
    public const string LandmarkName = "Name";
    public const string LandmarkName2 = "Name2";
    public const string GraphPath = "CiDyGraph";
    public const string NodeHolderPath = "NodeHolder";
    public const string RoadHolderPath = "RoadHolder";
    public const string DemoGraphName = "DemoGraph";
    public const string BeginnerGraphName = "BeginnerGraph";
    public const string IntermediateGraphName = "IntermediateGraph";
    public const string AdvancedGraphName = "AdvancedGraph";

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
    public const string ContinueGame = "Continue Game";
    public const string NewGame = "New Game";
    public const string RetryGame = "Try Again";
    public const string VictoryLap = "Take a Victory Lap!";
    public const string CheckScore = "Check Score";
    public const string Next = "Next";

    // format strings
    public const string ScoreTextFormat = "Score: {0:d4}";
    public const string TimeTextFormat = "Time: {0:d4}";
    public const string LevelTextFormat = "Level: {0}";
    public const string FirstLandmarkCrossedMessageFormat = "You just crossed the  <b><color=#00ffffff>\"{0}\"</color></b>  landmark!\r\n\r\nWe will now demonstrate how to mark its position on the map.";
    public const string OtherLandmarkCrossedMessageFormat = "You just crossed the  <b><color=#00ffffff>\"{0}\"</color></b>  landmark!\r\n\r\nDrag and drop the blinking marker to the appropriate position on the map.";
    public const string LevelWonMessageFormat = "<b><color=#00ff00ff>Congratulations! You won!</color></b>\r\n\r\nYou scored {0} points in {1} seconds!";
    public const string LevelLostMessageFormat = "<b><color=#ff5733ff>Map completed, but with some mistakes.</color></b>\r\n\r\nYou scored {0} points in {1} seconds.";
    public const string CarSpeedStatusFormat = "Speed: {0:F0}";
    public const string CarRevsStatusFormat = "Revs: {0:d3}";
    public const string GetBackOnTrackMessage = "Car is stuck? Press T to get it back on track.";
    public const string StartingInstructionsMessage = "Drive the car using the W, A, S, D or arrow keys (or standard joystick controls).";
    public const string VictoryLapMessage = "Drive freely anywhere you want to. You can even go offroad.";
}
