using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerState
{
    public static string PlayerName { get; private set; }
    public static GameLevel PlayerGameLevel { get; private set; }
    public static float TotalScore { get; private set; } = 0f; // player's total score so far

    public static void InitPlayerName()
    {
        string playerName = null;
        
        if (PlayerPrefs.HasKey(Strings.PlayerName))
            playerName = PlayerPrefs.GetString(Strings.PlayerName, null);

        PlayerState.SetPlayerName(playerName);
    }

    public static void InitPlayerGameLevel()
    {
        string playerGameLevel = null;
        
        if (PlayerPrefs.HasKey(Strings.PlayerGameLevel))
            playerGameLevel = PlayerPrefs.GetString(Strings.PlayerGameLevel, GameLevel.Downtown.ToString());
        
        PlayerState.SetPlayerGameLevel(playerGameLevel);
    }

    public static void SetPlayerName(string playerName)
    {
        if (String.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.DeleteKey(Strings.PlayerName);
        }
        else
        {
            PlayerName = playerName;
            PlayerPrefs.SetString(Strings.PlayerName, playerName);
        }
    }

    public static void SetPlayerGameLevel(string playerGameLevel)
    {
        if (String.IsNullOrEmpty(playerGameLevel))
        {
            PlayerGameLevel = GameLevel.Downtown;
            PlayerPrefs.DeleteKey(Strings.PlayerGameLevel);
        }
        else
        {
            PlayerGameLevel = (GameLevel)Enum.Parse(typeof(GameLevel), playerGameLevel);
            PlayerPrefs.SetString(Strings.PlayerGameLevel, playerGameLevel);
        }
    }

    public static void SetPlayerGameLevel(GameLevel playerGameLevel)
    {
        SetPlayerGameLevel(playerGameLevel.ToString());
    }

    public static void IncrementScore(float levelScore)
    {
        TotalScore += levelScore;
        PlayerPrefs.SetFloat(Strings.PlayerScore, TotalScore);
    }
}
