using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingMessage : MonoBehaviour
{
    // our singleton instance
    private static FloatingMessage m_instance;
    public static FloatingMessage Instance { get { return m_instance; } }

    private AudioSource m_audioSource;

    private bool m_isShowing = false;

    private void Awake()
    {
        m_instance = this;

        m_audioSource = GetComponent<AudioSource>();
    }

    private void _ShowMessage(Transform tTarget, string message, float duration)
    {
        if (m_isShowing)
            return;

        m_isShowing = true;

        transform.SetParent(tTarget, false);
        transform.localPosition = new Vector3(0f, 2.5f, -3f); // little bit above the car and towards the camera
        transform.localRotation = Quaternion.identity;

        transform.GetComponent<TMP_Text>().text = message;
        gameObject.SetActive(true);

        m_audioSource.Play();

        StartCoroutine(ShowHideMessage(tTarget, message, duration));
    }

    private IEnumerator ShowHideMessage(Transform tTarget, string message, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        _HideMessage();
    }

    private void _HideMessage()
    {
        gameObject.SetActive(false);

        m_isShowing = false;
    }

    public static void ShowMessage(Transform tTarget, string message, float duration)
    {
        Instance._ShowMessage(tTarget, message, duration);
    }

    public static void HideMessage()
    {
        Instance._HideMessage();
    }
}