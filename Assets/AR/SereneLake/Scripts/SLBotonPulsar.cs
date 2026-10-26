using UnityEngine;
using UnityEngine.EventSystems;

/// Pequeño efecto de "hundir" el botón al pulsarlo.
public class SLBotonPulsar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float escalaPulsado = 0.95f;
    public float velocidad = 18f;

    Vector3 escalaBase;
    float objetivo = 1f;

    void Awake() { escalaBase = transform.localScale; }

    void Update()
    {
        float actual = transform.localScale.x / escalaBase.x;
        float nueva = Mathf.Lerp(actual, objetivo, Time.unscaledDeltaTime * velocidad);
        transform.localScale = escalaBase * nueva;
    }

    public void OnPointerDown(PointerEventData e) { objetivo = escalaPulsado; }
    public void OnPointerUp(PointerEventData e) { objetivo = 1f; }
    public void OnPointerExit(PointerEventData e) { objetivo = 1f; }
}
