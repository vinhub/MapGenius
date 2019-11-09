using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayermarkHandler : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public enum PlayermarkState { Unvisited, CurrentlyVisiting, Visited };

    public PlayermarkState State { get; private set; } = PlayermarkState.Unvisited;

    private bool m_isDropped = false; // whether the marker has been drag/dropped at least once

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

        Image playermarkImage = this.GetComponent<Image>();
        Text playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkText).GetComponent<Text>();

        switch (state)
        {
            case PlayermarkState.Unvisited:
                playermarkImage.color = new Color32(0, 160, 0, 255);
                playermarkText.color = new Color32(0, 160, 0, 255);
                break;

            case PlayermarkState.CurrentlyVisiting:
                // highlight the current playermark and make it draggable
                // TODO: make image blink
                playermarkImage.color = new Color32(0, 255, 0, 255);
                playermarkText.color = new Color32(0, 255, 0, 255);
                break;

            case PlayermarkState.Visited:
                // if it was not drag-dropped at all, remove the draggable marker
                if (!m_isDropped)
                {
                    this.gameObject.SetActive(false);
                }
                else
                {
                    playermarkImage.color = new Color32(0, 160, 0, 255);
                }

                playermarkText.color = new Color32(160, 160, 160, 255);
                break;
        }
    }
}
