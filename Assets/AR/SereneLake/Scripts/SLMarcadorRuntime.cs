using UnityEngine;
using Vuforia;

/// Crea el Image Target a partir de la imagen del marcador cuando Vuforia arranca
/// y avisa al menú cuando lo detecta.
public class SLMarcadorRuntime : MonoBehaviour
{
    [Tooltip("Imagen del marcador (legible y sin comprimir)")]
    public Texture2D imagenMarcador;
    [Tooltip("Ancho real del marcador impreso, en metros")]
    public float anchoMetros = 0.2f;
    public string nombre = "marcador_serenelake";
    public SLMenuAR menu;

    ObserverBehaviour objetivo;

    void Start()
    {
        if (VuforiaApplication.Instance.IsRunning) Crear();
        else VuforiaApplication.Instance.OnVuforiaStarted += Crear;
    }

    void Crear()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= Crear;
        if (objetivo != null) return;
        objetivo = VuforiaBehaviour.Instance.ObserverFactory.CreateImageTarget(imagenMarcador, anchoMetros, nombre);
        if (objetivo == null) { Debug.LogError("[SereneLake] No se pudo crear el marcador"); return; }
        objetivo.OnTargetStatusChanged += AlCambiarEstado;
    }

    void AlCambiarEstado(ObserverBehaviour b, TargetStatus s)
    {
        bool visto = s.Status == Status.TRACKED || s.Status == Status.EXTENDED_TRACKED || s.Status == Status.LIMITED;
        if (menu == null) return;
        if (visto) menu.MarcadorEncontrado(); else menu.MarcadorPerdido();
    }

    void OnDestroy()
    {
        if (objetivo != null) objetivo.OnTargetStatusChanged -= AlCambiarEstado;
    }
}
