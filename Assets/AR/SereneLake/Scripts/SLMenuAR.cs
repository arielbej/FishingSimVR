using UnityEngine;

/// <summary>
/// Controla el flujo de la app de RA:
///  1) Al arrancar se ve la cámara y el aviso "Apunta la cámara al marcador".
///  2) Cuando Vuforia detecta el marcador (evento OnTargetFound del Image Target)
///     se abre el menú principal.
/// </summary>
public class SLMenuAR : MonoBehaviour
{
    [Tooltip("Panel con el aviso para escanear el marcador")]
    public GameObject panelEscanear;

    [Tooltip("Panel del menú principal (fondo + botones)")]
    public GameObject panelMenu;

    [Tooltip("Si está activo, el menú se queda abierto aunque se pierda el marcador")]
    public bool mantenerMenuAbierto = true;

    void Awake()
    {
        MostrarEscaneo();
    }

    /// Llamado desde el Image Target (DefaultObserverEventHandler > On Target Found)
    public void MarcadorEncontrado()
    {
        AbrirMenu();
    }

    /// Llamado desde el Image Target (DefaultObserverEventHandler > On Target Lost)
    public void MarcadorPerdido()
    {
        if (!mantenerMenuAbierto) MostrarEscaneo();
    }

    public void AbrirMenu()
    {
        if (panelEscanear != null) panelEscanear.SetActive(false);
        if (panelMenu != null) panelMenu.SetActive(true);
    }

    public void MostrarEscaneo()
    {
        if (panelMenu != null) panelMenu.SetActive(false);
        if (panelEscanear != null) panelEscanear.SetActive(true);
    }
}
