using System;
using UnityEngine;

public class MenuSceneLoader : MonoBehaviour
{
    public GameObject MainMenuUI;
    private GameObject m_goMainMenuUI;

	void Awake ()
	{
	    if (m_goMainMenuUI == null)
	    {
	        m_goMainMenuUI = Instantiate(MainMenuUI);
	    }
	}
}
