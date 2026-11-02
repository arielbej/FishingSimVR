using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Acciones de los botones del menú.
/// </summary>
public class SLBotonesMenu : MonoBehaviour
{
    public SLTrailerController trailerController;

    [Header("Instrucciones")]
    public GameObject panelMenu;
    public GameObject panelInstrucciones;

    void Awake()
    {
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
    }

    public void Minijuego()     { SceneManager.LoadScene("MinijuegoSenuelos"); }
    public void VisorBarco3D()  { SceneManager.LoadScene("VisorBarco3D"); }
    public void PilotarBarco()  { SceneManager.LoadScene("AR_Movimiento Prueba"); }
    public void VerTrailer()
    {
        if (trailerController != null)
        {
            trailerController.AbrirTrailer();
        }
    }

    public void AbrirInstrucciones()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelInstrucciones != null) panelInstrucciones.SetActive(true);
    }

    public void CerrarInstrucciones()
    {
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
