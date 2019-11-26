using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupMessage : MonoBehaviour
{
    // our singleton instance
    private static PopupMessage m_instance;
    public static PopupMessage Instance { get { return m_instance; } }

    private Text m_messageText;
    private GameObject m_popupWindow;

    private void Awake()
    {
        m_instance = this;

        m_messageText = transform.Find(Strings.PopupMessageTextPath).GetComponent<Text>();
        m_popupWindow = transform.Find(Strings.PopupWindowPath).gameObject;
    }

    private void _ShowMessage(string message)
    {
        m_messageText.text = message;
        m_popupWindow.SetActive(true);

        transform.SetAsLastSibling();
    }

    private void _HideMessage()
    { 
        m_popupWindow.SetActive(false);
    }

    public static void ShowMessage(string message)
    {
        Instance._ShowMessage(message);
    }

    public static void HideMessage()
    {
        Instance._HideMessage();
    }
}
