using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class PromptMessage : MonoBehaviour
{
    // our singleton instance
    private static PromptMessage m_instance;
    public static PromptMessage Instance { get { return m_instance; } }

    private GameObject m_prompt;
    private TMP_Text m_messageText, m_action1Text, m_action2Text;

    public Action<bool> PromptCallback;

    private void Awake()
    {
        m_instance = this;

        m_prompt = transform.Find(Strings.PromptPath).gameObject;
        m_messageText = transform.Find(Strings.PromptMessageTextPath).GetComponent<TMP_Text>();
        m_action1Text = transform.Find(Strings.Action1ButtonLabelPath).GetComponent<TMP_Text>();
        m_action2Text = transform.Find(Strings.Action2ButtonLabelPath).GetComponent<TMP_Text>();
    }

    private void _ShowMessage(string message, string action1Text, string action2Text, Action<bool> promptCallback)
    {
        m_messageText.text = message;
        m_action1Text.text = action1Text;
        m_action2Text.text = action2Text;
        PromptCallback = promptCallback;

        m_prompt.SetActive(true);
        transform.SetAsLastSibling();

        m_prompt.transform.DOScale(1f, 0.6f).SetUpdate(true);
    }

    private void _HideMessage()
    {
        if (!m_prompt.activeInHierarchy)
            return;

        m_prompt.transform.DOScale(0f, 0.25f).SetUpdate(true).OnComplete(() => m_prompt.SetActive(false));
    }

    public void OnClickAction1()
    {
        _HideMessage();
        PromptCallback(true);
    }

    public void OnClickAction2()
    {
        _HideMessage();
        PromptCallback(false);
    }

    public static void ShowMessage(string message, string action1Text, string action2Text, Action<bool> promptCallback)
    {
        Instance._ShowMessage(message, action1Text, action2Text, promptCallback);
    }
}
