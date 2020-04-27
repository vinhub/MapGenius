﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayermarkHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image m_playermarkImage, m_emptyPlayermarkImage;
    private TMP_Text m_playermarkText, m_playermarkIndex, m_emptyPlayermarkText;

    public enum PlayermarkState { Unvisited, CurrentlyVisiting, Visited };

    public PlayermarkState State { get; private set; } = PlayermarkState.Unvisited;

    private bool m_isDropped = false; // whether the marker has been drag/dropped at least once
    public float ScoreFactor { get; private set; } = 100f; // percentage to multiply the score by

    private Color32 m_unvisitedColor = new Color32(0, 255, 255, 255);
    private Color32 m_currentlyVisitingColor = new Color32(255, 255, 255, 255);
    private Color32 m_visitedColor = new Color32(160, 160, 160, 255);
    private Color32 m_droppedColor = new Color32(0, 255, 255, 255);
    private Color32 m_red = new Color32(0xff, 0x57, 0x33, 0xff);
    private Color32 m_green = new Color32(0, 255, 0, 255);

    private bool m_isMoving = false;
    private Vector3 m_newPosition;

    private void Awake()
    {
        m_playermarkImage = this.GetComponent<Image>();
        m_playermarkIndex = this.transform.Find(Strings.PlayermarkIndexPath2).GetComponent<TMP_Text>();
        m_playermarkText = this.transform.parent.Find(Strings.PlayermarkTextName).GetComponent<TMP_Text>();
        m_emptyPlayermarkImage = this.transform.parent.Find(Strings.EmptyPlayermarkName).GetComponent<Image>();
        m_emptyPlayermarkText = this.transform.parent.Find(Strings.PlayermarkIndexEmptyPath).GetComponent<TMP_Text>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // we can drag-drop only while currently visiting
        if (this.State != PlayermarkState.CurrentlyVisiting)
            return;

        m_playermarkText.color = m_playermarkImage.color = m_playermarkIndex.color = m_visitedColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // we can drag-drop only while currently visiting
        if (this.State != PlayermarkState.CurrentlyVisiting)
            return;

        this.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // we can drag-drop only while currently visiting
        if (this.State != PlayermarkState.CurrentlyVisiting)
            return;

        this.m_isDropped = true; // Just note that it was drag/dropped. They can still drag and drop it until they close the panel. Then it gets locked.
    }

    void Update()
    {
        if (m_isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, m_newPosition, 1.5f * Time.unscaledDeltaTime);

            if (Vector3.Distance(transform.position, m_newPosition) < 0.1) // stop animation when close enough
            {
                DoneMoving();
            }
        }
    }

    public void DoneMoving()
    {
        m_isMoving = false;
        m_isDropped = true;
        SetState(PlayermarkState.Visited);
    }

    // artificially move the playermark to given position as if it was drag-dropped
    public void Move(Vector3 position)
    {
        // set up the animation which will occur during update
        m_newPosition = position;
        m_isMoving = true;
    }

    public void SetState(PlayermarkState state)
    {
        // Debug.Log(m_playermarkText.text + ": Setstate: " + state);

        this.State = state;

        switch (state)
        {
            case PlayermarkState.Unvisited:
                m_playermarkImage.color = m_playermarkIndex.color = m_playermarkText.color = m_unvisitedColor;
                break;

            case PlayermarkState.CurrentlyVisiting:
                // highlight the current playermark and make it draggable
                m_playermarkImage.color = m_playermarkIndex.color = m_playermarkText.color = m_currentlyVisitingColor;

                // make image blink
                StartCoroutine(Blink(1000));
                break;

            case PlayermarkState.Visited:
                StopAllCoroutines();

                // if it was not drag-dropped at all, remove the draggable marker
                if (!m_isDropped)
                {
                    this.gameObject.SetActive(false);
                }
                else
                {
                    m_playermarkImage.color = m_playermarkIndex.color = m_droppedColor;
                }

                m_playermarkText.color = m_visitedColor;
                break;
        }
    }

    public void SetScoreFactor(float factor)
    {
        ScoreFactor = factor;
    }

    private IEnumerator Blink(int blinkCount)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            m_playermarkImage.color = m_playermarkIndex.color = m_playermarkText.color = m_unvisitedColor;
            yield return new WaitForSecondsRealtime(0.5f);

            m_playermarkImage.color = m_playermarkIndex.color = m_playermarkText.color = m_currentlyVisitingColor;
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    // update appearance based on score.
    public void OnUpdateScore(float score)
    {
        m_playermarkImage.color = m_playermarkIndex.color = m_emptyPlayermarkImage.color = m_playermarkText.color = m_emptyPlayermarkText.color = (score > 0.01) ? m_green : m_red;
    }
}
