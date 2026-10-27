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

    [Header("Estabilidad del tracking")]
    [Tooltip("Tiempo que debe verse antes de considerarlo encontrado")]
    public float tiempoParaEncontrar = 0.25f;

    [Tooltip("Tiempo que debe estar realmente perdido antes de considerarlo perdido")]
    public float tiempoParaPerder = 1.5f;

    private ObserverBehaviour objetivo;

    // Estado instantáneo de Vuforia
    private bool vistoBruto = false;

    // Estado estable que utilizamos nosotros
    private bool vistoEstable = false;

    private float momentoUltimoCambio = 0f;

    private bool modoTrailerAnterior = false;

    void Start()
    {
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
            "[SereneLake] Marcador del tráiler creado correctamente"
        );
    }

    void AlCambiarEstado(
        ObserverBehaviour behaviour,
        TargetStatus status)
    {
        /*
         * LIMITED TAMBIÉN CUENTA COMO VISIBLE.
         *
         * Vuforia puede entrar momentáneamente en LIMITED
         * aunque sigamos enfocando el marcador.
         */
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
        if (trailerController == null)
            return;

        bool modoTrailerActual =
            trailerController.ModoTrailerActivo;

        // Entramos por primera vez al modo tráiler
        if (modoTrailerActual && !modoTrailerAnterior)
        {
            if (vistoEstable)
            {
                Debug.Log(
                    "[SereneLake] Target tráiler ENCONTRADO"
                );

                trailerController.MarcadorTrailerEncontrado();
            }
        }

        // Esperamos antes de confirmar cualquier cambio.
        if (vistoBruto != vistoEstable)
        {
            float tiempoNecesario =
                vistoBruto
                    ? tiempoParaEncontrar
                    : tiempoParaPerder;

            if (Time.unscaledTime - momentoUltimoCambio
                >= tiempoNecesario)
            {
                vistoEstable = vistoBruto;

                if (modoTrailerActual)
                {
                    if (vistoEstable)
                    {
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

        modoTrailerAnterior = modoTrailerActual;
    }

    void OnDestroy()
    {
        if (objetivo != null)
        {
            objetivo.OnTargetStatusChanged -= AlCambiarEstado;
        }

        VuforiaApplication.Instance.OnVuforiaStarted -= CrearMarcador;
    }
}