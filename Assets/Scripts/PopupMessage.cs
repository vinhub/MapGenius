﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum PopupMessageType { FirstLandmarkCrossed, OtherLandmarkCrossed, LevelLost, LevelWon };

public class PopupMessage : MonoBehaviour
{
    // our singleton instance
    private static PopupMessage m_instance;
    public static PopupMessage Instance { get { return m_instance; } }

    private AudioSource[] m_audioSources;

    private TMP_Text m_messageText;
    private GameObject m_popupWindow;

    private void Awake()
    {
        m_instance = this;

        m_messageText = transform.Find(Strings.PopupMessageTextPath).GetComponent<TMP_Text>();
        m_popupWindow = transform.Find(Strings.WindowPath).gameObject;

        m_audioSources = GetComponents<AudioSource>();
    }

    private void _ShowMessage(PopupMessageType type, string message)
    {
        int iAudioSource;

        switch (type)
        {
            case PopupMessageType.LevelLost:
                iAudioSource = 1;
                break;

            case PopupMessageType.LevelWon:
                iAudioSource = 2;
                break;

            default:
                iAudioSource = 0;
                break;
        }

        m_messageText.text = message;

        m_popupWindow.SetActive(true);
        transform.SetAsLastSibling();

        m_popupWindow.transform.DOScale(1f, 0.6f).SetUpdate(true);

        m_audioSources[iAudioSource].Play();
    }

    private void _HideMessage()
    {
        m_popupWindow.transform.DOScale(0f, 0.25f).SetUpdate(true).OnComplete(() => m_popupWindow.SetActive(false));
    }

    public static void ShowMessage(PopupMessageType type, string message)
    {
        Instance._ShowMessage(type, message);
    }

    public static void HideMessage()
    {
        Instance._HideMessage();
    }
}
