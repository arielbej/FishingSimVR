using UnityEngine;
using UnityEngine.Video;

public class SLTrailerController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelEscanearTrailer;
    public GameObject panelTrailer;

    [Header("Vídeo")]
    public VideoPlayer videoPlayer;
    public RenderTexture renderTexture;

    private bool modoTrailerActivo = false;
    private bool trailerYaIniciado = false;
    private bool trailerTerminado = false;

    public bool ModoTrailerActivo
    {
        get { return modoTrailerActivo; }
    }

    void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.Stop();

            videoPlayer.prepareCompleted += VideoPreparado;
            videoPlayer.loopPointReached += VideoTerminado;
        }

        if (panelTrailer != null)
            panelTrailer.SetActive(false);

        if (panelEscanearTrailer != null)
            panelEscanearTrailer.SetActive(false);

        LimpiarPantalla();
    }

    // =========================================================
    // BOTÓN VER TRÁILER
    // =========================================================

    public void AbrirTrailer()
    {
        modoTrailerActivo = true;

        trailerYaIniciado = false;
        trailerTerminado = false;

        if (panelMenu != null)
            panelMenu.SetActive(false);

        if (panelTrailer != null)
            panelTrailer.SetActive(false);

        if (panelEscanearTrailer != null)
            panelEscanearTrailer.SetActive(true);

        ResetearVideo();
    }

    // =========================================================
    // VUFORIA ENCUENTRA EL MARCADOR
    // =========================================================

    public void MarcadorTrailerEncontrado()
    {
        if (!modoTrailerActivo)
            return;

        if (panelEscanearTrailer != null)
            panelEscanearTrailer.SetActive(false);

        if (panelTrailer != null)
            panelTrailer.SetActive(true);

        // Solo la primera detección inicia el vídeo.
        if (!trailerYaIniciado)
        {
            trailerYaIniciado = true;
            trailerTerminado = false;

            ReproducirDesdeInicio();
        }
    }

    // =========================================================
    // VUFORIA PIERDE EL MARCADOR
    // =========================================================

    public void MarcadorTrailerPerdido()
    {
        if (!modoTrailerActivo)
            return;

        // No hacemos nada.
        // Una vez detectado el marcador,
        // el vídeo sigue aunque dejemos de enfocarlo.
    }

    // =========================================================
    // PLAY / PAUSA
    // =========================================================

    public void PlayPausa()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else if (videoPlayer.isPrepared && !trailerTerminado)
        {
            videoPlayer.Play();
        }
    }

    // =========================================================
    // REINICIAR
    // =========================================================

    public void ReiniciarTrailer()
    {
        if (!modoTrailerActivo)
            return;

        trailerYaIniciado = true;
        trailerTerminado = false;

        ReproducirDesdeInicio();
    }

    // =========================================================
    // VOLVER AL MENÚ
    // =========================================================

    public void CerrarTrailer()
    {
        modoTrailerActivo = false;
        trailerYaIniciado = false;
        trailerTerminado = false;

        ResetearVideo();

        if (panelTrailer != null)
            panelTrailer.SetActive(false);

        if (panelEscanearTrailer != null)
            panelEscanearTrailer.SetActive(false);

        if (panelMenu != null)
            panelMenu.SetActive(true);
    }

    // =========================================================
    // REPRODUCIR DESDE EL PRINCIPIO
    // =========================================================

    private void ReproducirDesdeInicio()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.Stop();

        LimpiarPantalla();

        videoPlayer.Prepare();
    }

    // =========================================================
    // VÍDEO PREPARADO
    // =========================================================

    private void VideoPreparado(VideoPlayer vp)
    {
        if (!modoTrailerActivo)
            return;

        if (vp.canSetTime)
            vp.time = 0;

        if (vp.canStep)
            vp.frame = 0;

        vp.Play();
    }

    // =========================================================
    // VÍDEO TERMINADO
    // =========================================================

    private void VideoTerminado(VideoPlayer vp)
    {
        trailerTerminado = true;

        vp.Pause();
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetearVideo()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.Stop();

        if (videoPlayer.isPrepared && videoPlayer.canSetTime)
            videoPlayer.time = 0;

        LimpiarPantalla();
    }

    // =========================================================
    // LIMPIAR RENDER TEXTURE
    // =========================================================

    private void LimpiarPantalla()
    {
        if (renderTexture == null)
            return;

        RenderTexture anterior = RenderTexture.active;

        RenderTexture.active = renderTexture;

        GL.Clear(true, true, Color.black);

        RenderTexture.active = anterior;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= VideoPreparado;
            videoPlayer.loopPointReached -= VideoTerminado;
        }
    }
}