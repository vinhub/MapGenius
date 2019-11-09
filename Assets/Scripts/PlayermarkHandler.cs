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

    internal void SetState(bool isLocked, bool isDropped)
    {
        this.IsLocked = isLocked;
        this.IsDropped = isDropped;

        Image playermarkImage = this.GetComponent<Image>();
        Text playermarkText = this.gameObject.transform.parent.Find(Strings.PlayermarkText).GetComponent<Text>();

        if (isLocked)
        {
            playermarkImage.color = new Color32(0, 160, 0, 255);
            playermarkText.color = isDropped ? new Color32(160, 160, 160, 255) : new Color32(0, 160, 0, 255);
        }
        else
        {
            playermarkImage.color = new Color32(0, 255, 0, 255);
            playermarkText.color = new Color32(0, 255, 0, 255);
        }
    }
}
