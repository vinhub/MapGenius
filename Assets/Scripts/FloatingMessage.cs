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

    private bool m_isAlreadyShowing = false;

    private void Awake()
    {
        m_instance = this;

        m_audioSource = GetComponent<AudioSource>();
    }

    private void _ShowMessage(Transform tTarget, string message, float duration)
    {
        if (m_isAlreadyShowing)
            return;

        m_isAlreadyShowing = true;

        m_audioSource.Play();

        transform.parent = tTarget;
        transform.localPosition = new Vector3(0f, 2.5f, -3f); // little bit above the car and towards the camera
        transform.localRotation = Quaternion.identity;

        transform.GetComponent<TMP_Text>().text = message;
        gameObject.SetActive(true);

        StartCoroutine(ShowHideMessage(tTarget, message, duration));
    }

    private IEnumerator ShowHideMessage(Transform tTarget, string message, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        gameObject.SetActive(false);

        m_isAlreadyShowing = false;
    }

    public static void ShowMessage(Transform tTarget, string message, float duration)
    {
        Instance._ShowMessage(tTarget, message, duration);
    }
}