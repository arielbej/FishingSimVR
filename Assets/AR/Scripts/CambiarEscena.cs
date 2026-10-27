using UnityEngine;
using UnityEngine.SceneManagement;

/// Carga otra escena desde un botón (On Click > CambiarEscena.CargarEscena, escribiendo el nombre).
public class CambiarEscena : MonoBehaviour
{
    public void CargarEscena(string nombre)
    {
        SceneManager.LoadScene(nombre);
    }
}
