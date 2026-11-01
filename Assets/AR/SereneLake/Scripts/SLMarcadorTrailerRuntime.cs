using UnityEngine;
using Vuforia;

public class SLMarcadorTrailerRuntime : MonoBehaviour
{
    [Header("Marcador del tráiler")]
    public Texture2D imagenMarcador;

    [Tooltip("Ancho real del marcador, en metros")]
    public float anchoMetros = 0.2f;

    public string nombre = "marcador_trailer";

    [Header("Controlador")]
    public SLTrailerController trailerController;

    [Header("Contenido del tráiler")]
    public GameObject contenidoTrailerAR;

    [Header("Cámara AR")]
    public Camera arCamera;

    [Header("Detección")]
    public float tiempoParaEncontrar = 0.20f;

    [Tooltip("Tiempo que aguantamos la última posición si Vuforia pierde el marcador un instante")]
    public float tiempoGraciaPerdida = 0.60f;

    [Header("Estabilización")]
    [Tooltip("Más alto = sigue más rápido. Más bajo = más suave.")]
    public float suavizadoPosicion = 12f;

    [Tooltip("Más alto = gira más rápido. Más bajo = más suave.")]
    public float suavizadoRotacion = 10f;

    [Header("Ajuste sobre el marcador")]
    public Vector3 offsetLocal = Vector3.zero;

    [Tooltip("Corrección adicional de orientación")]
    public Vector3 rotacionLocal = Vector3.zero;

    private ObserverBehaviour objetivo;

    private bool marcadorVisto = false;
    private bool trailerIniciado = false;
    private bool poseInicializada = false;

    private float momentoEncontrado = 0f;
    private float momentoPerdido = 0f;

    private bool modoTrailerAnterior = false;

    void Start()
    {
        PrepararCanvas();

        if (VuforiaApplication.Instance.IsRunning)
        {
            CrearMarcador();
        }
        else
        {
            VuforiaApplication.Instance.OnVuforiaStarted += CrearMarcador;
        }
    }

    void PrepararCanvas()
    {
        if (contenidoTrailerAR == null)
            return;

        Canvas canvas =
            contenidoTrailerAR.GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;

            if (arCamera != null)
                canvas.worldCamera = arCamera;
        }

        contenidoTrailerAR.SetActive(false);
    }

    void CrearMarcador()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= CrearMarcador;

        if (objetivo != null)
            return;

        if (imagenMarcador == null)
        {
            Debug.LogError(
                "[SereneLake] Falta la imagen del marcador del tráiler"
            );

            return;
        }

        objetivo =
            VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(
                imagenMarcador,
                anchoMetros,
                nombre
            );

        if (objetivo == null)
        {
            Debug.LogError(
                "[SereneLake] No se pudo crear el marcador del tráiler"
            );

            return;
        }

        objetivo.OnTargetStatusChanged += AlCambiarEstado;

        Debug.Log(
            "[SereneLake] Marcador del tráiler creado"
        );
    }

    void AlCambiarEstado(
        ObserverBehaviour behaviour,
        TargetStatus status
    )
    {
        bool vistoAhora =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        if (vistoAhora && !marcadorVisto)
        {
            marcadorVisto = true;
            momentoEncontrado = Time.unscaledTime;
        }

        if (!vistoAhora && marcadorVisto)
        {
            marcadorVisto = false;
            momentoPerdido = Time.unscaledTime;
        }
    }

    void Update()
    {
        if (
            trailerController == null ||
            objetivo == null ||
            contenidoTrailerAR == null ||
            arCamera == null
        )
        {
            return;
        }

        bool modoTrailer =
            trailerController.ModoTrailerActivo;

        if (modoTrailer && !modoTrailerAnterior)
        {
            trailerIniciado = false;
            poseInicializada = false;
        }

        if (!modoTrailer)
        {
            trailerIniciado = false;
            poseInicializada = false;

            modoTrailerAnterior = false;
            return;
        }

        if (marcadorVisto)
        {
            if (
                Time.unscaledTime - momentoEncontrado
                >= tiempoParaEncontrar
            )
            {
                SeguirMarcadorSuavizado();

                if (!trailerIniciado)
                {
                    trailerIniciado = true;

                    trailerController
                        .MarcadorTrailerEncontrado();

                    Debug.Log(
                        "[SereneLake] Tráiler siguiendo marcador y mirando a cámara"
                    );
                }
            }
        }
        else if (trailerIniciado)
        {
            float tiempoPerdido =
                Time.unscaledTime - momentoPerdido;

            if (tiempoPerdido > tiempoGraciaPerdida)
            {
                // Mantiene la última posición.
                // Cuando vuelve el tracking continúa suavemente.
            }
        }

        modoTrailerAnterior = modoTrailer;
    }

    void SeguirMarcadorSuavizado()
    {
        if (
            objetivo == null ||
            contenidoTrailerAR == null ||
            arCamera == null
        )
        {
            return;
        }

        // POSICIÓN:
        // sigue al marcador.
        Vector3 posicionObjetivo =
            objetivo.transform.TransformPoint(
                offsetLocal
            );

        // ROTACIÓN:
        // siempre mira hacia la cámara.
        Vector3 direccionCamara =
            arCamera.transform.position -
            posicionObjetivo;

        Quaternion rotacionObjetivo;

        if (direccionCamara.sqrMagnitude > 0.0001f)
        {
            rotacionObjetivo =
                Quaternion.LookRotation(
                    -direccionCamara.normalized,
                    arCamera.transform.up
                );

            rotacionObjetivo *=
                Quaternion.Euler(rotacionLocal);
        }
        else
        {
            rotacionObjetivo =
                contenidoTrailerAR.transform.rotation;
        }

        // Primera detección
        if (!poseInicializada)
        {
            contenidoTrailerAR.transform.position =
                posicionObjetivo;

            contenidoTrailerAR.transform.rotation =
                rotacionObjetivo;

            poseInicializada = true;

            return;
        }

        float dt = Time.unscaledDeltaTime;

        float factorPosicion =
            1f - Mathf.Exp(
                -suavizadoPosicion * dt
            );

        float factorRotacion =
            1f - Mathf.Exp(
                -suavizadoRotacion * dt
            );

        contenidoTrailerAR.transform.position =
            Vector3.Lerp(
                contenidoTrailerAR.transform.position,
                posicionObjetivo,
                factorPosicion
            );

        contenidoTrailerAR.transform.rotation =
            Quaternion.Slerp(
                contenidoTrailerAR.transform.rotation,
                rotacionObjetivo,
                factorRotacion
            );
    }

    void OnDestroy()
    {
        if (objetivo != null)
        {
            objetivo.OnTargetStatusChanged -=
                AlCambiarEstado;
        }

        if (VuforiaApplication.Instance != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted -=
                CrearMarcador;
        }
    }
}