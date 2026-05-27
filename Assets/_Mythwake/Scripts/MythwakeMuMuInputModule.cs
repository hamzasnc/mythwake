#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MythwakeMuMuInputModule : PointerInputModule
{
    private const int MousePointerId = -100;
    private PointerEventData mousePointerData;

    public override void Process()
    {
        var hasMouseButton = Input.mousePresent && (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0));
        if (hasMouseButton)
        {
            ProcessMousePointer();
            return;
        }

        var touchCount = Input.touchCount;
        if (touchCount > 0)
        {
            ProcessTouches(touchCount);
            return;
        }

        if (!Input.mousePresent)
        {
            return;
        }

        ProcessMousePointer();
    }

    private void ProcessMousePointer()
    {
        var pointerData = GetMousePointerData();
        ProcessMove(pointerData);
        ProcessDrag(pointerData);

        if (Input.GetMouseButtonDown(0))
        {
            ProcessMousePress(pointerData);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ProcessMouseRelease(pointerData);
        }
    }

    private void ProcessTouches(int touchCount)
    {
        for (var i = 0; i < touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            var pointerData = GetTouchPointerData(touch, out var pressed, out var released);

            ProcessMove(pointerData);
            ProcessDrag(pointerData);

            if (pressed)
            {
                ProcessMousePress(pointerData);
            }

            if (released)
            {
                ProcessMouseRelease(pointerData);
                RemovePointerData(pointerData);
            }
        }
    }

    private PointerEventData GetMousePointerData()
    {
        GetPointerData(MousePointerId, out mousePointerData, true);

        var position = GetCorrectedPosition(Input.mousePosition);

        mousePointerData.pointerId = MousePointerId;
        mousePointerData.button = PointerEventData.InputButton.Left;
        mousePointerData.delta = position - mousePointerData.position;
        mousePointerData.position = position;
        mousePointerData.scrollDelta = Input.mouseScrollDelta;

        eventSystem.RaycastAll(mousePointerData, m_RaycastResultCache);
        mousePointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();
        return mousePointerData;
    }

    private PointerEventData GetTouchPointerData(Touch touch, out bool pressed, out bool released)
    {
        pressed = touch.phase == TouchPhase.Began;
        released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;

        GetPointerData(touch.fingerId, out var pointerData, true);

        var position = GetCorrectedPosition(touch.position);
        pointerData.pointerId = touch.fingerId;
        pointerData.button = PointerEventData.InputButton.Left;
        pointerData.delta = pressed ? Vector2.zero : position - pointerData.position;
        pointerData.position = position;
        pointerData.scrollDelta = Vector2.zero;

        eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
        pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();
        return pointerData;
    }

    private static Vector2 GetCorrectedPosition(Vector2 rawPosition)
    {
        // MuMu can surface desktop clicks as top-left pointer positions, even on the touch path.
        return new Vector2(rawPosition.x, Screen.height - rawPosition.y);
    }

    private void ProcessMousePress(PointerEventData pointerData)
    {
        var currentOverGo = pointerData.pointerCurrentRaycast.gameObject;

        pointerData.eligibleForClick = true;
        pointerData.delta = Vector2.zero;
        pointerData.dragging = false;
        pointerData.useDragThreshold = true;
        pointerData.pressPosition = pointerData.position;
        pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;

        DeselectIfSelectionChanged(currentOverGo, pointerData);

        var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerData, ExecuteEvents.pointerDownHandler);
        if (newPressed == null)
        {
            newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
        }

        var time = Time.unscaledTime;
        if (newPressed == pointerData.pointerPress && time - pointerData.clickTime < 0.3f)
        {
            pointerData.clickCount++;
        }
        else
        {
            pointerData.clickCount = 1;
        }

        pointerData.clickTime = time;
        pointerData.pointerPress = newPressed;
        pointerData.rawPointerPress = currentOverGo;
        pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

        if (pointerData.pointerDrag != null)
        {
            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.initializePotentialDrag);
        }
    }

    private void ProcessMouseRelease(PointerEventData pointerData)
    {
        ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);

        var currentOverGo = pointerData.pointerCurrentRaycast.gameObject;
        var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

        if (pointerData.pointerPress == pointerClickHandler && pointerData.eligibleForClick)
        {
            ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);
        }
        else if (pointerData.pointerDrag != null && pointerData.dragging)
        {
            ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerData, ExecuteEvents.dropHandler);
        }

        pointerData.eligibleForClick = false;
        pointerData.pointerPress = null;
        pointerData.rawPointerPress = null;

        if (pointerData.pointerDrag != null && pointerData.dragging)
        {
            ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.endDragHandler);
        }

        pointerData.dragging = false;
        pointerData.pointerDrag = null;
    }
}
#endif
