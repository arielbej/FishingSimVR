using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Acciones de los botones del menú + efectos de sonido Serene Lake.
/// </summary>
public class SLBotonesMenu : MonoBehaviour
{
    public SLTrailerController trailerController;

    [Header("Instrucciones")]
    public GameObject panelMenu;
    public GameObject panelInstrucciones;

    void Awake()
    {
        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);
    }

    public void Minijuego()
    {
        SLAudioManager.PlayMinigame();
        SceneManager.LoadScene("MinijuegoSenuelos");
    }

    public void VisorBarco3D()
    {
        SLAudioManager.PlayBoat3D();
        SceneManager.LoadScene("VisorBarco3D");
    }

    public void PilotarBarco()
    {
        SLAudioManager.PlayBoatMotor();
        SceneManager.LoadScene("AR_Movimiento Prueba");
    }

    public void VerTrailer()
    {
        SLAudioManager.PlayTrailer();

        if (trailerController != null)
            trailerController.AbrirTrailer();
    }

    public void AbrirInstrucciones()
    {
        SLAudioManager.PlayInstructions();

        if (panelMenu != null)
            panelMenu.SetActive(false);

        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(true);
    }

    public void CerrarInstrucciones()
    {
        SLAudioManager.PlayBack();

        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }

    public void Salir()
    {
        SLAudioManager.PlayBack();
        StartCoroutine(SalirConRetardo());
    }

    private IEnumerator SalirConRetardo()
    {
        // Da tiempo a escuchar el pequeño efecto de salida.
        yield return new WaitForSecondsRealtime(0.22f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
