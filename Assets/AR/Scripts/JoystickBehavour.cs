using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickBehavour : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float radius = 100f;      // recorrido del handle en píxeles del canvas
    public float deadZone = 0.1f;    // ignora movimientos muy pequeños

    public Vector2 Value { get; private set; }   // x = timón, y = acelerador (-1 a 1)

    RectTransform area;

    void Awake() => area = (RectTransform)transform;

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, e.position, e.pressEventCamera, out Vector2 local);

        Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
        handle.anchoredPosition = v * radius;
        Value = v.magnitude < deadZone ? Vector2.zero : v;
    }

    public void OnPointerUp(PointerEventData e)
    {
        handle.anchoredPosition = Vector2.zero;
        Value = Vector2.zero;
    }
}