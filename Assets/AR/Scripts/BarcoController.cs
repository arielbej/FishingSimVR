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

    [Header("Animación")]
    public Animator m_animator;
    public float deadZone = 0.1f;

    [Header("Efectos")]
    public ParticleSystem ondas;   // ondas en el agua cuando el barco está parado

    [Header("Límites del agua")]
    public float limite = 0.4f;    // el barco no sale del agua (en metros desde el centro del marcador)

    float throttle;      // -1 (marcha atrás) a 1 (adelante)
    float steer;         // -1 (izquierda) a 1 (derecha)
    float currentSpeed;
    float avance_anterior;
    float giro_anterior;

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
        float speedFactor = Mathf.Max(0.3f, Mathf.Abs(currentSpeed / maxSpeed));
        float turn = steer * turnSpeed * speedFactor * Time.deltaTime;

        transform.Rotate(0f, turn, 0f, Space.Self);
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);

        // Que no se salga del agua
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Clamp(pos.x, -limite, limite);
        pos.z = Mathf.Clamp(pos.z, -limite, limite);
        transform.localPosition = pos;

        // Animaciones: solo cambiamos de estado cuando cambia la dirección
        float avance = 0;
        float giro = 0;
        if (Mathf.Abs(throttle) > deadZone) avance = Mathf.Sign(throttle);
        if (Mathf.Abs(steer) > deadZone) giro = Mathf.Sign(steer);

        if (m_animator != null)
        {
            if (avance != avance_anterior)
            {
                m_animator.ResetTrigger("avanzar");
                m_animator.ResetTrigger("retroceder");
                m_animator.ResetTrigger("parar");
                switch (avance)
                {
                    case > 0:
                        m_animator.SetTrigger("avanzar");
                        if (ondas != null) ondas.Stop();
                        break;
                    case < 0:
                        m_animator.SetTrigger("retroceder");
                        if (ondas != null) ondas.Stop();
                        break;
                    default:
                        m_animator.SetTrigger("parar");
                        if (ondas != null) ondas.Play();
                        break;
                }
            }

            if (giro != giro_anterior)
            {
                m_animator.ResetTrigger("derecha");
                m_animator.ResetTrigger("izquierda");
                m_animator.ResetTrigger("recto");
                switch (giro)
                {
                    case > 0:
                        m_animator.SetTrigger("derecha");
                        break;
                    case < 0:
                        m_animator.SetTrigger("izquierda");
                        break;
                    default:
                        m_animator.SetTrigger("recto");
                        break;
                }
            }
        }
        avance_anterior = avance;
        giro_anterior = giro;
    }
}
