using UnityEngine;
using Vuforia;

public class SLMarcadorTrailerRuntime : MonoBehaviour
{
    [Header("Marcador del tráiler")]
    public Texture2D imagenMarcador;

    [Tooltip("Ancho real del marcador impreso, en metros")]
    public float anchoMetros = 0.2f;

    public string nombre = "marcador_trailer";

    [Header("Controlador del tráiler")]
    public SLTrailerController trailerController;

    [Header("Contenido AR")]
    public GameObject contenidoTrailerAR;

    [Header("Estabilidad del tracking")]
    [Tooltip("Tiempo que debe verse antes de considerarlo encontrado")]
    public float tiempoParaEncontrar = 0.25f;

    [Tooltip("Tiempo que debe estar perdido antes de considerarlo perdido")]
    public float tiempoParaPerder = 1.5f;

    [Header("Suavizado del movimiento")]
    [Tooltip("Velocidad con la que el panel sigue la posición del marcador")]
    [Range(1f, 20f)]
    public float suavizadoPosicion = 4f;

    [Tooltip("Velocidad con la que el panel sigue la rotación del marcador")]
    [Range(1f, 20f)]
    public float suavizadoRotacion = 3f;

    [Header("Zona muerta")]
    [Tooltip("Movimientos menores que esta distancia se ignoran")]
    public float distanciaMinima = 0.015f;

    [Tooltip("Giros menores que este ángulo se ignoran")]
    public float anguloMinimo = 3f;

    private ObserverBehaviour objetivo;

    private bool vistoBruto = false;
    private bool vistoEstable = false;

    private float momentoUltimoCambio = 0f;

    // Transformación del contenido respecto al marcador
    private Vector3 posicionRelativa;
    private Quaternion rotacionRelativa;
    private Vector3 escalaOriginal;

    private bool poseInicializada = false;

    void Start()
    {
        // Guardamos la configuración que ya tienes hecha
        // en Canvas_TrailerAR.
        if (contenidoTrailerAR != null)
        {
            posicionRelativa =
                contenidoTrailerAR.transform.localPosition;

            rotacionRelativa =
                contenidoTrailerAR.transform.localRotation;

            escalaOriginal =
                contenidoTrailerAR.transform.localScale;

            contenidoTrailerAR.SetActive(false);
        }

        if (VuforiaApplication.Instance.IsRunning)
        {
            CrearMarcador();
        }
        else
        {
            VuforiaApplication.Instance.OnVuforiaStarted += CrearMarcador;
        }
    }

    void CrearMarcador()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= CrearMarcador;

        if (objetivo != null)
            return;

        if (imagenMarcador == null)
        {
            Debug.LogError(
                "[SereneLake] No hay imagen asignada al marcador del tráiler"
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

        /*
         * IMPORTANTE:
         *
         * No hacemos Canvas_TrailerAR hijo directo del ImageTarget.
         *
         * En vez de eso seguimos la posición del marcador manualmente.
         * Así podemos suavizar los movimientos y eliminar el temblor.
         */
        if (contenidoTrailerAR != null)
        {
            contenidoTrailerAR.transform.SetParent(
                null,
                true
            );

            contenidoTrailerAR.transform.localScale =
                escalaOriginal;

            contenidoTrailerAR.SetActive(false);
        }

        objetivo.OnTargetStatusChanged += AlCambiarEstado;

        Debug.Log(
            "[SereneLake] Marcador del tráiler creado correctamente"
        );
    }

    void AlCambiarEstado(
        ObserverBehaviour behaviour,
        TargetStatus status
    )
    {
        bool nuevoVisto =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED ||
            status.Status == Status.LIMITED;

        if (nuevoVisto != vistoBruto)
        {
            vistoBruto = nuevoVisto;
            momentoUltimoCambio = Time.unscaledTime;
        }
    }

    void Update()
    {
        if (objetivo == null)
            return;

        // =====================================================
        // MOVIMIENTO SUAVIZADO
        // =====================================================

        if (vistoBruto || vistoEstable)
        {
            ActualizarPoseSuavizada();
        }

        if (trailerController == null)
            return;

        // Si no estamos en modo tráiler, esperamos.
        if (!trailerController.ModoTrailerActivo)
        {
            vistoEstable = false;
            poseInicializada = false;

            return;
        }

        // =====================================================
        // ESTABILIDAD DEL TRACKING
        // =====================================================

        if (vistoBruto != vistoEstable)
        {
            float tiempoNecesario =
                vistoBruto
                ? tiempoParaEncontrar
                : tiempoParaPerder;

            if (
                Time.unscaledTime - momentoUltimoCambio
                >= tiempoNecesario
            )
            {
                vistoEstable = vistoBruto;

                if (vistoEstable)
                {
                    // Antes de mostrarlo lo colocamos
                    // exactamente en la posición correcta.
                    ActualizarPoseInstantanea();

                    Debug.Log(
                        "[SereneLake] Target tráiler ENCONTRADO"
                    );

                    trailerController
                        .MarcadorTrailerEncontrado();
                }
                else
                {
                    Debug.Log(
                        "[SereneLake] Target tráiler PERDIDO"
                    );

                    trailerController
                        .MarcadorTrailerPerdido();
                }
            }
        }
    }

    // =========================================================
    // COLOCACIÓN INSTANTÁNEA
    // =========================================================

    void ActualizarPoseInstantanea()
    {
        if (
            objetivo == null ||
            contenidoTrailerAR == null
        )
        {
            return;
        }

        Vector3 posicionObjetivo =
            objetivo.transform.TransformPoint(
                posicionRelativa
            );

        Quaternion rotacionObjetivo =
            objetivo.transform.rotation *
            rotacionRelativa;

        contenidoTrailerAR.transform.position =
            posicionObjetivo;

        contenidoTrailerAR.transform.rotation =
            rotacionObjetivo;

        contenidoTrailerAR.transform.localScale =
            escalaOriginal;

        poseInicializada = true;
    }

    // =========================================================
    // SEGUIMIENTO SUAVIZADO + ZONA MUERTA
    // =========================================================

    void ActualizarPoseSuavizada()
    {
        if (
            objetivo == null ||
            contenidoTrailerAR == null
        )
        {
            return;
        }

        Vector3 posicionObjetivo =
            objetivo.transform.TransformPoint(
                posicionRelativa
            );

        Quaternion rotacionObjetivo =
            objetivo.transform.rotation *
            rotacionRelativa;

        // Primera vez:
        // lo colocamos directamente.
        if (!poseInicializada)
        {
            contenidoTrailerAR.transform.position =
                posicionObjetivo;

            contenidoTrailerAR.transform.rotation =
                rotacionObjetivo;

            poseInicializada = true;

            return;
        }

        // =====================================================
        // CALCULAR CUÁNTO SE HA MOVIDO
        // =====================================================

        float distancia =
            Vector3.Distance(
                contenidoTrailerAR.transform.position,
                posicionObjetivo
            );

        float angulo =
            Quaternion.Angle(
                contenidoTrailerAR.transform.rotation,
                rotacionObjetivo
            );

        // =====================================================
        // POSICIÓN
        //
        // Si el movimiento es muy pequeño, NO lo hacemos.
        // Esto elimina el temblor de la mano.
        // =====================================================

        if (distancia > distanciaMinima)
        {
            float factorPosicion =
                1f -
                Mathf.Exp(
                    -suavizadoPosicion *
                    Time.unscaledDeltaTime
                );

            contenidoTrailerAR.transform.position =
                Vector3.Lerp(
                    contenidoTrailerAR.transform.position,
                    posicionObjetivo,
                    factorPosicion
                );
        }

        // =====================================================
        // ROTACIÓN
        //
        // También ignoramos pequeños giros.
        // =====================================================

        if (angulo > anguloMinimo)
        {
            float factorRotacion =
                1f -
                Mathf.Exp(
                    -suavizadoRotacion *
                    Time.unscaledDeltaTime
                );

            contenidoTrailerAR.transform.rotation =
                Quaternion.Slerp(
                    contenidoTrailerAR.transform.rotation,
                    rotacionObjetivo,
                    factorRotacion
                );
        }
    }

    void OnDestroy()
    {
        if (objetivo != null)
        {
            objetivo.OnTargetStatusChanged -= AlCambiarEstado;
        }

        if (VuforiaApplication.Instance != null)
        {
            VuforiaApplication.Instance.OnVuforiaStarted -= CrearMarcador;
        }
    }
}