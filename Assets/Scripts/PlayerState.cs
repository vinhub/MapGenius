using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerState
{
    public static GameLevel CurGameLevel { get; set; } = GameLevel.Downtown;
    public static float TotalScore { get; private set; } = 0f; // player's total score so far

    public static void IncrementScore(float levelScore)
    {
        PlayerState.TotalScore += levelScore;
    }
}
