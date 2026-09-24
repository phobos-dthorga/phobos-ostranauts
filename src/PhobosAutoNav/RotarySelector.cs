using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhobosAutoNav;

// UI interaction only. Every detent delegates to the checked navigation service.
public sealed class RotarySelector : Selectable, IPointerClickHandler, IScrollHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, ISubmitHandler
{
    private const float DragPixelsPerDetent = 18;
    internal Action<int>? Changed;
    internal RectTransform Pointer = null!;
    private Vector2 previous;
    private float accumulated;
    private bool dragged;
    internal void SetAngle(float angle) { if (Pointer != null) Pointer.localRotation = Quaternion.Euler(0, 0, angle); }
    private void Step(int direction)
    {
        if (!IsActive() || !IsInteractable() || direction == 0) return;
        CrewSim.bJustClickedInput = true; Changed?.Invoke(Math.Sign(direction));
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (dragged) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.position, data.pressEventCamera, out var point)) return;
        Step(data.button == PointerEventData.InputButton.Right || point.x < 0 ? -1 : 1);
    }
    public override void OnPointerDown(PointerEventData data) { dragged = false; base.OnPointerDown(data); }
    public void OnScroll(PointerEventData data) => Step(Math.Sign(data.scrollDelta.y));
    public void OnBeginDrag(PointerEventData data)
    {
        dragged = true; accumulated = 0;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out previous);
    }
    public void OnDrag(PointerEventData data)
    {
        if (!IsInteractable()) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
            data.position, data.pressEventCamera, out var current)) return;
        accumulated += current.y - previous.y; previous = current;
        while (Math.Abs(accumulated) >= DragPixelsPerDetent)
        { int direction = Math.Sign(accumulated); accumulated -= direction * DragPixelsPerDetent; Step(direction); }
    }
    public void OnEndDrag(PointerEventData data) { accumulated = 0; }
    public void OnSubmit(BaseEventData data) => Step(1);
    public override void OnMove(AxisEventData data)
    {
        if (data.moveDir == MoveDirection.Left || data.moveDir == MoveDirection.Down) Step(-1);
        else if (data.moveDir == MoveDirection.Right || data.moveDir == MoveDirection.Up) Step(1);
    }
}
