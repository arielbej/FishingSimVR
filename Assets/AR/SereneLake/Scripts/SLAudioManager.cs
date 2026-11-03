using UnityEngine;

/// <summary>
/// Gestor de audio global de Serene Lake Fishing.
/// Se crea solo al arrancar la app y permanece entre escenas.
/// No necesita referencias manuales en el Inspector.
/// </summary>
public class SLAudioManager : MonoBehaviour
{
    private static SLAudioManager instancia;

    private AudioSource sfxSource;
    private AudioSource ambientSource;

    private AudioClip uiClick;
    private AudioClip uiBack;
    private AudioClip uiOpen;
    private AudioClip movement;
    private AudioClip boatWood;
    private AudioClip boatMotor;
    private AudioClip trailer;
    private AudioClip minigame;
    private AudioClip instructions;
    private AudioClip markerFound;
    private AudioClip ambientLake;

    private const float VOLUMEN_SFX = 0.85f;
    private const float VOLUMEN_AMBIENTE = 0.10f;
    private const float VOLUMEN_AMBIENTE_TRAILER = 0.025f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearAutomaticamente()
    {
        AsegurarInstancia();
    }

    private static SLAudioManager AsegurarInstancia()
    {
        if (instancia != null)
            return instancia;

        instancia = FindFirstObjectByType<SLAudioManager>();

        if (instancia == null)
        {
            GameObject go = new GameObject("SL_AudioManager");
            instancia = go.AddComponent<SLAudioManager>();
        }

        return instancia;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        CrearFuentes();
        CargarClips();
        IniciarAmbiente();
    }

    private void CrearFuentes()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = VOLUMEN_SFX;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = VOLUMEN_AMBIENTE;
    }

    private void CargarClips()
    {
        uiClick = Resources.Load<AudioClip>("SereneLakeAudio/ui_click_water");
        uiBack = Resources.Load<AudioClip>("SereneLakeAudio/ui_back_bubble");
        uiOpen = Resources.Load<AudioClip>("SereneLakeAudio/ui_open_water_whoosh");
        movement = Resources.Load<AudioClip>("SereneLakeAudio/movement_water_swish");
        boatWood = Resources.Load<AudioClip>("SereneLakeAudio/boat_wood_creak");
        boatMotor = Resources.Load<AudioClip>("SereneLakeAudio/boat_motor_start_short");
        trailer = Resources.Load<AudioClip>("SereneLakeAudio/trailer_reel_splash");
        minigame = Resources.Load<AudioClip>("SereneLakeAudio/minigame_ding_splash");
        instructions = Resources.Load<AudioClip>("SereneLakeAudio/instructions_open_soft");
        markerFound = Resources.Load<AudioClip>("SereneLakeAudio/marker_found_chime");
        ambientLake = Resources.Load<AudioClip>("SereneLakeAudio/ambient_lake_loop");
    }

    private void IniciarAmbiente()
    {
        if (ambientSource == null || ambientLake == null)
            return;

        ambientSource.clip = ambientLake;
        ambientSource.volume = VOLUMEN_AMBIENTE;
        ambientSource.Play();
    }

    private void Reproducir(AudioClip clip, float volumen = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volumen);
    }

    public static void PlayClick()        { AsegurarInstancia().Reproducir(AsegurarInstancia().uiClick, 0.70f); }
    public static void PlayBack()         { AsegurarInstancia().Reproducir(AsegurarInstancia().uiBack, 0.75f); }
    public static void PlayOpen()         { AsegurarInstancia().Reproducir(AsegurarInstancia().uiOpen, 0.70f); }
    public static void PlayMovement()     { AsegurarInstancia().Reproducir(AsegurarInstancia().movement, 0.70f); }
    public static void PlayBoat3D()       { AsegurarInstancia().Reproducir(AsegurarInstancia().boatWood, 0.68f); }
    public static void PlayBoatMotor()    { AsegurarInstancia().Reproducir(AsegurarInstancia().boatMotor, 0.62f); }
    public static void PlayTrailer()      { AsegurarInstancia().Reproducir(AsegurarInstancia().trailer, 0.72f); }
    public static void PlayMinigame()     { AsegurarInstancia().Reproducir(AsegurarInstancia().minigame, 0.72f); }
    public static void PlayInstructions() { AsegurarInstancia().Reproducir(AsegurarInstancia().instructions, 0.70f); }
    public static void PlayMarkerFound()  { AsegurarInstancia().Reproducir(AsegurarInstancia().markerFound, 0.68f); }

    public static void ModoTrailer(bool activo)
    {
        SLAudioManager a = AsegurarInstancia();
        if (a.ambientSource == null)
            return;

        a.ambientSource.volume = activo
            ? VOLUMEN_AMBIENTE_TRAILER
            : VOLUMEN_AMBIENTE;
    }
}
