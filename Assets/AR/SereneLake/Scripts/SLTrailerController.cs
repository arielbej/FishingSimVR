using UnityEngine;
using UnityEngine.Video;

public class SLTrailerController : MonoBehaviour
{
    public GameObject panelMenu;
    public GameObject panelEscanearTrailer;
    public GameObject panelTrailer;
    public VideoPlayer videoPlayer;

    private bool modoTrailerActivo = false;

    public bool ModoTrailerActivo
    {
        get { return modoTrailerActivo; }
    }

    public void AbrirTrailer()
    {
        modoTrailerActivo = true;

        panelMenu.SetActive(false);
        panelTrailer.SetActive(false);
        panelEscanearTrailer.SetActive(true);

        videoPlayer.Stop();
    }

    public void MarcadorTrailerEncontrado()
    {
        if (!modoTrailerActivo)
            return;

        panelEscanearTrailer.SetActive(false);
        panelTrailer.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void MarcadorTrailerPerdido()
    {
        if (!modoTrailerActivo)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    public void CerrarTrailer()
    {
        modoTrailerActivo = false;

        videoPlayer.Stop();

        panelTrailer.SetActive(false);
        panelEscanearTrailer.SetActive(false);
        panelMenu.SetActive(true);
    }
}