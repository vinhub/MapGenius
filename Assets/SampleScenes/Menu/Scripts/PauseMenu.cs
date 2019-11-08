using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private Toggle m_toggle;
    
    void Awake()
    {
        m_toggle = GetComponent<Toggle>();
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
        if (m_toggle.isOn && !GameSystem.Instance.IsGamePaused())
        {
            MenuOn();
        }
        else if (!m_toggle.isOn && GameSystem.Instance.IsGamePaused())
        {
            MenuOff();
        }
    }


#if !MOBILE_INPUT
	void Update()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
		    m_toggle.isOn = !m_toggle.isOn;
            Cursor.visible = m_toggle.isOn;//force the cursor visible if anythign had hidden it
		}
	}
#endif
}
