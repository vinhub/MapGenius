using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private Toggle m_MenuToggle;
    
    void Awake()
    {
        m_MenuToggle = GetComponent <Toggle> ();
	}
    
    private void MenuOn()
    {
        GameSystem.Instance.PauseGame();
    }
    
    public void MenuOff()
    {
        GameSystem.Instance.ResumeGame();
    }
    
    public void OnMenuStatusChange()
    {
        if (m_MenuToggle.isOn && !GameSystem.Instance.IsGamePaused())
        {
            MenuOn();
        }
        else if (!m_MenuToggle.isOn && GameSystem.Instance.IsGamePaused())
        {
            MenuOff();
        }
    }


#if !MOBILE_INPUT
	void Update()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
		    m_MenuToggle.isOn = !m_MenuToggle.isOn;
            Cursor.visible = m_MenuToggle.isOn;//force the cursor visible if anythign had hidden it
		}
	}
#endif
}
