using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SLTrailerController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelEscanearTrailer;
    public GameObject panelTrailer;

    [Header("Vídeo")]
    public VideoPlayer videoPlayer;

    [Header("Pantalla del tráiler")]
    public RawImage rawImageTrailer;

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
            // IMPORTANTE:
            // El vídeo ya no utiliza RenderTexture.
            // Obtenemos la imagen directamente con videoPlayer.texture.
            videoPlayer.renderMode = VideoRenderMode.APIOnly;

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;

            videoPlayer.Stop();

            videoPlayer.prepareCompleted += VideoPreparado;
            videoPlayer.loopPointReached += VideoTerminado;
        }

        if (rawImageTrailer != null)
        {
            rawImageTrailer.texture = null;
        }

        if (panelTrailer != null)
        {
            panelTrailer.SetActive(false);
        }

        if (panelEscanearTrailer != null)
        {
            panelEscanearTrailer.SetActive(false);
        }
    }

    void Update()
    {
        // Mientras el VideoPlayer tenga imagen,
        // la ponemos directamente en el RawImage.
        if (
            videoPlayer != null &&
            rawImageTrailer != null &&
            videoPlayer.texture != null
        )
        {
            if (rawImageTrailer.texture != videoPlayer.texture)
            {
                rawImageTrailer.texture = videoPlayer.texture;
            }
        }
    }

    // =========================================================
    // BOTÓN VER TRÁILER
    // =========================================================

    public void AbrirTrailer()
    {
        modoTrailerActivo = true;

        trailerYaIniciado = false;
        trailerTerminado = false;

        ResetearVideo();

        if (panelMenu != null)
        {
            panelMenu.SetActive(false);
        }

        if (panelTrailer != null)
        {
            panelTrailer.SetActive(false);
        }

        if (panelEscanearTrailer != null)
        {
            panelEscanearTrailer.SetActive(true);
        }
    }

    // =========================================================
    // VUFORIA ENCUENTRA EL MARCADOR
    // =========================================================

    public void MarcadorTrailerEncontrado()
    {
        if (!modoTrailerActivo)
        {
            return;
        }

        if (!trailerYaIniciado)
        {
            SLAudioManager.PlayMarkerFound();
            SLAudioManager.ModoTrailer(true);
        }

        if (panelEscanearTrailer != null)
        {
            panelEscanearTrailer.SetActive(false);
        }

        if (panelTrailer != null)
        {
            panelTrailer.SetActive(true);
        }

        // Solo la primera detección empieza desde 0.
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
        {
            return;
        }

        // Por ahora no hacemos nada.
        // Una vez encontrado el marcador,
        // el vídeo continúa reproduciéndose.
    }

    // =========================================================
    // PLAY / PAUSA
    // =========================================================

    public void PlayPausa()
    {
        SLAudioManager.PlayClick();

        if (videoPlayer == null)
        {
            return;
        }

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else if (
            videoPlayer.isPrepared &&
            !trailerTerminado
        )
        {
            videoPlayer.Play();
        }
    }

    // =========================================================
    // REINICIAR
    // =========================================================

    public void ReiniciarTrailer()
    {
        SLAudioManager.PlayOpen();

        if (!modoTrailerActivo)
        {
            return;
        }

        trailerYaIniciado = true;
        trailerTerminado = false;

        ReproducirDesdeInicio();
    }

    // =========================================================
    // VOLVER AL MENÚ
    // =========================================================

    public void CerrarTrailer()
    {
        SLAudioManager.PlayBack();
        SLAudioManager.ModoTrailer(false);

        modoTrailerActivo = false;

        trailerYaIniciado = false;
        trailerTerminado = false;

        ResetearVideo();

        if (panelTrailer != null)
        {
            panelTrailer.SetActive(false);
        }

        if (panelEscanearTrailer != null)
        {
            panelEscanearTrailer.SetActive(false);
        }

        if (panelMenu != null)
        {
            panelMenu.SetActive(true);
        }
    }

    // =========================================================
    // REPRODUCIR DESDE EL PRINCIPIO
    // =========================================================

    private void ReproducirDesdeInicio()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.Stop();

        if (rawImageTrailer != null)
        {
            rawImageTrailer.texture = null;
        }

        videoPlayer.Prepare();
    }

    // =========================================================
    // VÍDEO PREPARADO
    // =========================================================

    private void VideoPreparado(VideoPlayer vp)
    {
        if (!modoTrailerActivo)
        {
            return;
        }

        if (vp.canSetTime)
        {
            vp.time = 0;
        }

        if (vp.canStep)
        {
            vp.frame = 0;
        }

        // Si la textura ya está disponible,
        // la ponemos directamente en el RawImage.
        if (
            rawImageTrailer != null &&
            vp.texture != null
        )
        {
            rawImageTrailer.texture = vp.texture;
        }

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
        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (
                videoPlayer.isPrepared &&
                videoPlayer.canSetTime
            )
            {
                videoPlayer.time = 0;
            }
        }

        if (rawImageTrailer != null)
        {
            rawImageTrailer.texture = null;
        }
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