using System;
using UnityEngine;

public class MenuSceneLoader : MonoBehaviour
{
    public GameObject MainMenuUI;
    private GameObject m_goMainMenuUI;

	private void Awake()
	{
	    if (m_goMainMenuUI == null)
	    {
	        m_goMainMenuUI = Instantiate(this.MainMenuUI);
	    }
	}
}
