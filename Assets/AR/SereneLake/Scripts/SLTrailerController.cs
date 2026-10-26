using UnityEngine;
using UnityEngine.Video;

public class SLTrailerController : MonoBehaviour
{
    public GameObject panelMenu;
    public GameObject panelTrailer;
    public VideoPlayer videoPlayer;

    void Start()
    {
        // Al arrancar, el tráiler debe estar totalmente parado
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (panelTrailer != null)
            panelTrailer.SetActive(false);
    }

    public void AbrirTrailer()
    {
        panelMenu.SetActive(false);
        panelTrailer.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void CerrarTrailer()
    {
        videoPlayer.Stop();

        panelTrailer.SetActive(false);
        panelMenu.SetActive(true);
    }
}