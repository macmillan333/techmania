﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteInEditor : MonoBehaviour
{
    public Image selectionOverlay;
    public RectTransform noteImage;
    public RectTransform pathToPreviousNote;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;
    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged += SetKeysoundVisibility;
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -= SetKeysoundVisibility;
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        if (selectionOverlay == null) return;
        selectionOverlay.enabled = selection.Contains(gameObject);
    }

    public void SetKeysoundVisibility(bool visible)
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .gameObject.SetActive(visible);
    }

    public void SetKeysoundText()
    {
        NoteObject noteObject = GetComponent<NoteObject>();
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = UIUtils.StripExtension(noteObject.sound);
    }

    #region Event Relay
    public void OnPointerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.dragging) return;

        switch (pointerData.button)
        {
            case PointerEventData.InputButton.Left:
                LeftClicked?.Invoke(gameObject);
                break;
            case PointerEventData.InputButton.Right:
                RightClicked?.Invoke(gameObject);
                break;
        }
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        BeginDrag?.Invoke(gameObject);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        Drag?.Invoke(pointerData.delta);
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        EndDrag?.Invoke();
    }
    #endregion

    #region Note Attachments
    public void PointPathToward(GameObject target)
    {
        float distance = 0f;
        float angleInRadian = 0f;
        if (target != null)
        {
            Vector2 targetPos = target.GetComponent<RectTransform>()
            .anchoredPosition;
            Vector2 selfPos = GetComponent<RectTransform>().anchoredPosition;
            distance = Vector2.Distance(targetPos, selfPos);
            angleInRadian = Mathf.Atan2(selfPos.y - targetPos.y,
                selfPos.x - targetPos.x);
        }

        pathToPreviousNote.sizeDelta = new Vector2(distance, 0f);
        pathToPreviousNote.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);

        if (target != null &&
            target.GetComponent<NoteObject>().note.type == NoteType.ChainHead)
        {
            target.GetComponent<NoteInEditor>().noteImage.localRotation =
                Quaternion.Euler(0f, 0f, angleInRadian * Mathf.Rad2Deg);
        }
    }
    #endregion
}
