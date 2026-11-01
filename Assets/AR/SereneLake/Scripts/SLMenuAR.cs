using UnityEngine;

/// <summary>
/// Controla el flujo del menú principal.
/// Por defecto el menú se abre directamente al arrancar (sin escanear ningún marcador).
/// El marcador solo se usa ahora para ver el tráiler (SLTrailerController).
/// </summary>
public class SLMenuAR : MonoBehaviour
{
    [Tooltip("Panel con el aviso para escanear el marcador")]
    public GameObject panelEscanear;

    [Tooltip("Panel del menú principal (fondo + botones)")]
    public GameObject panelMenu;

    [Tooltip("Si está activo, el menú se queda abierto aunque se pierda el marcador")]
    public bool mantenerMenuAbierto = true;

    [Tooltip("Abrir el menú al arrancar, sin escanear el marcador")]
    public bool abrirMenuAlIniciar = true;

    void Awake()
    {
        if (abrirMenuAlIniciar) AbrirMenu();
        else MostrarEscaneo();
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
