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

    private ObserverBehaviour objetivo;

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
        bool visto =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED ||
            status.Status == Status.LIMITED;

        if (trailerController == null)
            return;

        if (!trailerController.ModoTrailerActivo)
            return;

        if (visto)
        {
            trailerController.MarcadorTrailerEncontrado();
        }
        else
        {
            trailerController.MarcadorTrailerPerdido();
        }
    }

    void OnDestroy()
    {
        if (objetivo != null)
        {
            objetivo.OnTargetStatusChanged -= AlCambiarEstado;
        }
    }
}