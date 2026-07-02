using UnityEngine;
using UnityEngine.EventSystems;

public class TouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [HideInInspector] public Vector2 TouchDist;
    [HideInInspector] public bool Pressed;
    
    private Vector2 PointerOld;
    private int PointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == PointerId)
        {
            // Calculate how far the finger moved since the last frame
            TouchDist = eventData.position - PointerOld;
            PointerOld = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        TouchDist = Vector2.zero;
    }

    void Update()
    {
        // Safety reset if the touch drops
        if (!Pressed)
        {
            TouchDist = Vector2.zero;
        }
    }
}