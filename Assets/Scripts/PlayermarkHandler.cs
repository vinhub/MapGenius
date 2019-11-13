using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayermarkHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image m_playermarkImage;
    private Text m_playermarkText;

    public enum PlayermarkState { Unvisited, CurrentlyVisiting, Visited };

    public PlayermarkState State { get; private set; } = PlayermarkState.Unvisited;

    private bool m_isDropped = false; // whether the marker has been drag/dropped at least once

    private Color32 m_unvisitedColor = new Color32(255, 255, 0, 255);
    private Color32 m_currentlyVisitingColor = new Color32(0, 255, 0, 255);
    private Color32 m_visitedColor = new Color32(160, 160, 160, 255);
    private Color32 m_droppedColor = new Color32(255, 255, 0, 255);

    public void Awake()
    {
        m_playermarkImage = this.GetComponent<Image>();
        m_playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkText).GetComponent<Text>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // we can drag-drop only while currently visiting
        if (this.State != PlayermarkState.CurrentlyVisiting)
            return;

        m_playermarkText.color = m_visitedColor;
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

    internal void SetState(PlayermarkState state)
    {
        this.State = state;

        switch (state)
        {
            case PlayermarkState.Unvisited:
                m_playermarkImage.color = m_unvisitedColor;
                m_playermarkText.color = m_unvisitedColor;
                break;

            case PlayermarkState.CurrentlyVisiting:
                // highlight the current playermark and make it draggable
                m_playermarkImage.color = m_currentlyVisitingColor;
                m_playermarkText.color = m_unvisitedColor;

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
                    m_playermarkImage.color = m_droppedColor;
                }

                m_playermarkText.color = m_visitedColor;
                break;
        }
    }

    IEnumerator Blink(int blinkCount)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            m_playermarkImage.color = m_unvisitedColor;
            yield return new WaitForSeconds(1);
            m_playermarkImage.color = m_currentlyVisitingColor;
            yield return new WaitForSeconds(1);
        }
    }
}
