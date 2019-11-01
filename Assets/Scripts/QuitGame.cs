using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
        Debug.Log("User quit the game.");
        Application.Quit();
    }
}
