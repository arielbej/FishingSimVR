using UnityEngine;

public class BarcoController : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 2f;        // unidades por segundo
    public float acceleration = 1f;
    public float deceleration = 0.8f;
    public float turnSpeed = 45f;      // grados por segundo

    [Header("Pruebas")]
    public bool useKeyboard = true; // esto esta solo para probar purament el moviemiento

    float throttle;      // -1 (marcha atrás) a 1 (adelante)
    float steer;         // -1 (izquierda) a 1 (derecha)
    float currentSpeed;

    // La UI llamará a estos dos métodos más adelante
    public void SetThrottle(float value) => throttle = Mathf.Clamp(value, -1f, 1f);
    public void SetSteer(float value) => steer = Mathf.Clamp(value, -1f, 1f);

    void Update()
    {
        if (useKeyboard)
        {
            throttle = Input.GetAxis("Vertical");    // W/S o flechas
            steer = Input.GetAxis("Horizontal");     // A/D o flechas
        }

        // Aceleración suave hacia la velocidad objetivo
        float target = throttle * maxSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * Time.deltaTime);

        // Un barco solo gira si avanza
        float speedFactor = Mathf.Clamp(currentSpeed / maxSpeed, -1f, 1f);
        float turn = steer * turnSpeed * speedFactor * Time.deltaTime;

        transform.Rotate(0f, turn, 0f, Space.Self);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
    }
}
