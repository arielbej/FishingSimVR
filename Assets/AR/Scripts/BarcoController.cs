using UnityEngine;

public class BarcoController : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 2f;        // unidades por segundo
    public float acceleration = 1f;
    public float deceleration = 0.8f;
    public float turnSpeed = 45f;      // grados por segundo

    [Header("Control por dirección")]
    public Transform cam;                 // la ARCamera (si está vacío usa Camera.main)
    public bool controlPorDireccion = true;

    Vector2 stick;
    public void SetStick(Vector2 v) => stick = Vector2.ClampMagnitude(v, 1f);

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
    Vector3 posInicial;

    // La UI llama a estos dos métodos más adelante
    public void SetThrottle(float value) => throttle = Mathf.Clamp(value, -1f, 1f);
    public void SetSteer(float value) => steer = Mathf.Clamp(value, -1f, 1f);

    void Start()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        posInicial = transform.localPosition;
    }

    void Update()
    {
        if (useKeyboard)
        {
            throttle = Input.GetAxis("Vertical");    // W/S o flechas
            steer = Input.GetAxis("Horizontal");     // A/D o flechas
        }

        // ================================ MOVIMIENTO BASICO===========================================
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        if (controlPorDireccion && !useKeyboard)
        {
            // Dirección deseada: el stick relativo a la cámara, sobre el plano del agua
            Vector3 camF = Vector3.ProjectOnPlane(cam.forward, up).normalized;
            Vector3 camR = Vector3.ProjectOnPlane(cam.right, up).normalized;
            Vector3 dir = camF * stick.y + camR * stick.x;

            if (dir.magnitude > deadZone)
            {
                float angulo = Vector3.SignedAngle(transform.forward, dir, up);
                float giroFrame = Mathf.Clamp(angulo, -turnSpeed * Time.deltaTime, turnSpeed * Time.deltaTime);
                transform.Rotate(up, giroFrame, Space.World);

                // Solo avanza cuanto más alineado esté con la dirección pedida
                float alineacion = Mathf.Clamp01(Vector3.Dot(transform.forward, dir.normalized));
                throttle = Mathf.Min(dir.magnitude, 1f) * alineacion;
                steer = Mathf.Clamp(angulo / 45f, -1f, 1f);   // para las animaciones
            }
            else { throttle = 0f; steer = 0f; }
        }

        float target = throttle * maxSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * Time.deltaTime);

        if (!controlPorDireccion || useKeyboard)
        {
            float speedFactor = Mathf.Max(0.3f, Mathf.Abs(currentSpeed / maxSpeed));
            transform.Rotate(0f, steer * turnSpeed * speedFactor * Time.deltaTime, 0f, Space.Self);
        }
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);

        // ================================ MOVIMIENTO BASICO===========================================



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
    public void ResetPosition()
    {
        Debug.Log("Reset pulsado");
        transform.localPosition = posInicial;
    }
}
