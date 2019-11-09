using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayermarkHandler : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public bool IsLocked { get; private set; } = true; // whether the marker is locked i.e. not draggable
    public bool IsDropped { get; private set; } = false; // whether the marker has been drag/dropped at least once

    public void OnDrag(PointerEventData eventData)
    {
        if (this.IsLocked)
            return;

        this.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this.IsLocked)
            return;

        this.IsDropped = true; // Just note that it was drag/dropped. They can still drag and drop it until they close the panel. Then it gets locked.
    }

    internal void SetHighlight(bool highlight)
    {
        Image playermarkImage = this.GetComponent<Image>();
        Text playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkText).GetComponent<Text>();

        if (highlight)
        {
            playermarkImage.color = new Color(0f, 255f, 0f);
            playermarkText.color = new Color(0f, 255f, 0f);
        }
        else
        {
            playermarkImage.color = new Color(0f, 150f, 0f);
            playermarkText.color = new Color(0f, 150f, 0f);
        }
    }

    internal void SetLock(bool isLocked)
    {
        Image playermarkImage = this.GetComponent<Image>();
        Text playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkText).GetComponent<Text>();

        this.IsLocked = isLocked;

        if (isLocked)
        {
            playermarkImage.color = new Color(0f, 150f, 0f);
            playermarkText.color = new Color(170f, 170f, 170f);
        }
        else
        {
            playermarkImage.color = new Color(0f, 255f, 0f);
            playermarkText.color = new Color(0f, 255f, 0f);
        }
    }

    internal void SetDropped(bool isDropped)
    {
        this.IsDropped = isDropped;
    }
}
