using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayermarkHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image m_playermarkImage, m_emptyPlayermarkImage;
    private Text m_playermarkText;

    public enum PlayermarkState { Unvisited, CurrentlyVisiting, Visited };

    public PlayermarkState State { get; private set; } = PlayermarkState.Unvisited;

    private bool m_isDropped = false; // whether the marker has been drag/dropped at least once
    public float ScoreFactor { get; private set; } = 100f; // percentage to multiply the score by

    private Color32 m_unvisitedColor = new Color32(0, 160, 0, 255);
    private Color32 m_currentlyVisitingColor = new Color32(0, 255, 0, 255);
    private Color32 m_visitedColor = new Color32(160, 160, 160, 255);
    private Color32 m_droppedColor = new Color32(0, 160, 0, 255);
    private Color32 m_red = new Color32(255, 0, 0, 255);
    private Color32 m_green = new Color32(0, 255, 0, 255);

    private bool m_isMoving = false;
    private Vector3 m_newPosition;

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
        InitMembers();

        // Debug.Log(m_playermarkText.text + ": Setstate: " + state);

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
                m_playermarkText.color = m_currentlyVisitingColor;

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

    public void SetScoreFactor(float factor)
    {
        ScoreFactor = factor;
    }

    private void InitMembers()
    {
        if (m_playermarkImage == null)
        {
            m_playermarkImage = this.GetComponent<Image>();
            m_playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkTextName).GetComponent<Text>();
            m_emptyPlayermarkImage = this.gameObject.transform.parent.Find(Strings.EmptyPlayermarkName).GetComponent<Image>();
        }
    }

    private IEnumerator Blink(int blinkCount)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            m_playermarkImage.color = m_unvisitedColor;
            yield return new WaitForSecondsRealtime(0.5f);

            m_playermarkImage.color = m_currentlyVisitingColor;
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    // update appearance based on score.
    public void OnUpdateScore(int score)
    {
        InitMembers();

        m_playermarkImage.color = m_emptyPlayermarkImage.color = m_playermarkText.color = (score > 0) ? m_green : m_red;
    }
}
