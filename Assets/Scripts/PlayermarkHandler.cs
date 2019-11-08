using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayermarkHandler : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public bool IsLocked = false; // whether the marker is locked i.e. not draggable
    public bool IsDropped = false; // whether the marker has been drag/dropped at least once

    public void OnDrag(PointerEventData eventData)
    {
        if (IsLocked)
            return;

        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsLocked)
            return;

        IsDropped = true; // Just note that it was drag/dropped. They can still drag and drop it until they close the panel. Then it gets locked.
    }
}
