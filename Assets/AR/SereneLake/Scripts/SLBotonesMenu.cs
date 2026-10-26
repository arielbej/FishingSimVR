using UnityEngine;

/// <summary>
/// Acciones de los botones del menú. De momento están vacías:
/// aquí se conectarán las escenas más adelante.
/// </summary>
public class SLBotonesMenu : MonoBehaviour
{
    public SLTrailerController trailerController;

    public void Minijuego()     { /* TODO: cargar el minijuego */ }
    public void VisorBarco3D()  { /* TODO: visor del modelo (girar el barco) */ }
    public void PilotarBarco()  { /* TODO: movimiento del vehículo */ }
    public void VerTrailer()
    {
        if (trailerController != null)
        {
            trailerController.AbrirTrailer();
        }
    }
}
