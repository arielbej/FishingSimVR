using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Acciones de los botones del menú.
/// </summary>
public class SLBotonesMenu : MonoBehaviour
{
    public SLTrailerController trailerController;

    public void VisorBarco3D()  { SceneManager.LoadScene("VisorBarco3D"); }
    public void PilotarBarco()  { SceneManager.LoadScene("AR_Movimiento Prueba"); }
    public void VerTrailer()
    {
        if (trailerController != null)
        {
            trailerController.AbrirTrailer();
        }
    }
}
