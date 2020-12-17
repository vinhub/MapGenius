using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerState
{
    public static string PlayerName { get; private set; }
    public static GameLevel PlayerGameLevel { get; private set; }
    public static DrivingMode PlayerDrivingMode { get; private set; }
    public static float PlayerTotalScore { get; private set; } // player's total score so far

    public static void InitPlayerName()
    {
        string playerName = null;
        
        if (PlayerPrefs.HasKey(Strings.PlayerName))
            playerName = PlayerPrefs.GetString(Strings.PlayerName, null);

        PlayerState.SetPlayerName(playerName);
    }

    public static void SetPlayerName(string playerName)
    {
        if (String.IsNullOrWhiteSpace(playerName))
        {
            PlayerPrefs.DeleteKey(Strings.PlayerName);
            PlayerName = null;
        }
        else
        {
            PlayerName = playerName;
            PlayerPrefs.SetString(Strings.PlayerName, playerName);
        }
    }

    public static void InitPlayerGameLevel()
    {
        string playerGameLevel = null;

        if (PlayerPrefs.HasKey(Strings.PlayerGameLevel))
            playerGameLevel = PlayerPrefs.GetString(Strings.PlayerGameLevel, GameLevel.Downtown.ToString());

        PlayerState.SetPlayerGameLevel(playerGameLevel);
    }

    public static void SetPlayerGameLevel(GameLevel playerGameLevel)
    {
        SetPlayerGameLevel(playerGameLevel.ToString());
    }

    public static void SetPlayerGameLevel(string playerGameLevel)
    {
        if (String.IsNullOrWhiteSpace(playerGameLevel))
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

    public static void InitPlayerDrivingMode()
    {
        string playerDrivingMode = null;

        if (PlayerPrefs.HasKey(Strings.PlayerDrivingMode))
            playerDrivingMode = PlayerPrefs.GetString(Strings.PlayerDrivingMode, DrivingMode.Normal.ToString());

        PlayerState.SetPlayerDrivingMode(playerDrivingMode);
    }

    public static void SetPlayerDrivingMode(DrivingMode playerDrivingMode)
    {
        SetPlayerDrivingMode(playerDrivingMode.ToString());
    }

    public static void SetPlayerDrivingMode(string playerDrivingMode)
    {
        if (String.IsNullOrWhiteSpace(playerDrivingMode))
        {
            PlayerDrivingMode = DrivingMode.Normal;
            PlayerPrefs.DeleteKey(Strings.PlayerDrivingMode);
        }
        else
        {
            PlayerDrivingMode = (DrivingMode)Enum.Parse(typeof(DrivingMode), playerDrivingMode);
            PlayerPrefs.SetString(Strings.PlayerDrivingMode, playerDrivingMode);
        }
    }

    public static void InitPlayerTotalScore()
    {
        float score = 0f;

        if (PlayerPrefs.HasKey(Strings.PlayerTotalScore))
            score = PlayerPrefs.GetFloat(Strings.PlayerTotalScore, 0f);

        PlayerTotalScore = score;
    }

    public static void SetPlayerTotalScore(float score)
    {
        PlayerTotalScore = score;
        PlayerPrefs.SetFloat(Strings.PlayerTotalScore, PlayerTotalScore);
    }

    public static void IncrementPlayerTotalScore(float levelScore)
    {
        SetPlayerTotalScore(PlayerTotalScore + levelScore);
    }

    public static void Reset()
    {
        SetPlayerName(null);
        SetPlayerGameLevel(null);
        SetPlayerDrivingMode(null);
        SetPlayerTotalScore(0f);
    }
}
