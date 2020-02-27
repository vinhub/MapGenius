using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private Toggle m_toggle;
    private AudioSource m_buttonClickAudioSource;

    private void Awake()
    {
        m_toggle = GetComponent<Toggle>();
        m_buttonClickAudioSource = transform.parent.Find(Strings.ButtonClickAudioSourceName).GetComponent<AudioSource>();
	}

    private void OnDisable()
    {
        m_toggle.isOn = false;    
    }

    private void MenuOn()
    {
        m_buttonClickAudioSource.Play();
        GameSystem.Instance.PauseGame();
    }
    
    private void MenuOff()
    {
        GameSystem.Instance.ResumeGame(false);
    }
    
    public void OnMenuStatusChange()
    {
        if ((m_toggle == null) || (GameSystem.Instance == null))
            return;

        if (m_toggle.isOn && !GameSystem.Instance.IsGamePaused())
        {
            MenuOn();
        }
        else if (GameSystem.Instance.IsGamePaused())
        {
            if (m_toggle.isOn)
                m_toggle.isOn = false;

            MenuOff();
        }
    }


#if !MOBILE_INPUT
	private void Update()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
		    m_toggle.isOn = !m_toggle.isOn;
            Cursor.visible = m_toggle.isOn;//force the cursor visible if anythign had hidden it
		}
	}
#endif
}

