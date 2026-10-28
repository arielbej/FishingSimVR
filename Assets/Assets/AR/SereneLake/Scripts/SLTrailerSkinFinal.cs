using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SLTrailerSkinFinal : MonoBehaviour
{
    [Header("Se buscan automáticamente por nombre si están vacíos")]
    public RawImage rawImageTrailer;
    public Button botonPlayPausa;
    public Button botonReiniciar;
    public Button botonVolver;

    private Image marcoFinal;

    void Start()
    {
        AplicarSkin();
    }

    [ContextMenu("Aplicar Skin Trailer Final")]
    public void AplicarSkin()
    {
        RectTransform panel = GetComponent<RectTransform>();

        // Panel a pantalla completa
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        panel.localScale = Vector3.one;

        // Quitamos cualquier fondo negro/gris del Panel_Trailer
        Image imagenPanel = GetComponent<Image>();
        if (imagenPanel != null)
        {
            Color c = imagenPanel.color;
            c.a = 0f;
            imagenPanel.color = c;
            imagenPanel.raycastTarget = false;
        }

        // Buscar objetos existentes
        if (rawImageTrailer == null)
            rawImageTrailer = BuscarComponentePorNombre<RawImage>("RawImage_Trailer");

        if (botonPlayPausa == null)
            botonPlayPausa = BuscarComponentePorNombre<Button>("Boton_PlayPausa");

        if (botonReiniciar == null)
            botonReiniciar = BuscarComponentePorNombre<Button>("Boton_Reiniciar");

        if (botonVolver == null)
            botonVolver = BuscarComponentePorNombre<Button>("Boton_Volver");

        // Desactivar la antigua Barra_Controles después de sacar los botones
        Transform barra = BuscarTransformPorNombre("Barra_Controles");

        PrepararBoton(botonPlayPausa, panel, 
            new Vector2(0.13f, 0.065f), 
            new Vector2(0.38f, 0.205f));

        PrepararBoton(botonReiniciar, panel, 
            new Vector2(0.375f, 0.065f), 
            new Vector2(0.625f, 0.205f));

        PrepararBoton(botonVolver, panel, 
            new Vector2(0.62f, 0.065f), 
            new Vector2(0.875f, 0.205f));

        if (barra != null)
        {
            Image barraImage = barra.GetComponent<Image>();
            if (barraImage != null)
                barraImage.enabled = false;

            HorizontalLayoutGroup layout =
                barra.GetComponent<HorizontalLayoutGroup>();

            if (layout != null)
                layout.enabled = false;

            barra.gameObject.SetActive(false);
        }

        // Vídeo colocado exactamente en el hueco del marco
        if (rawImageTrailer != null)
        {
            RectTransform rtVideo =
                rawImageTrailer.GetComponent<RectTransform>();

            rtVideo.SetParent(panel, false);

            rtVideo.anchorMin = new Vector2(0.1226f, 0.2508f);
            rtVideo.anchorMax = new Vector2(0.8762f, 0.8406f);
            rtVideo.offsetMin = Vector2.zero;
            rtVideo.offsetMax = Vector2.zero;
            rtVideo.localScale = Vector3.one;

            // Recorte vertical para mantener proporción del tráiler sin deformarlo.
            rawImageTrailer.uvRect =
                new Rect(0f, 0.1085f, 1f, 0.783f);

            rawImageTrailer.raycastTarget = false;
            rawImageTrailer.transform.SetAsFirstSibling();
        }

        CrearMarco(panel);

        // Los botones deben quedar siempre por encima.
        if (botonPlayPausa != null)
            botonPlayPausa.transform.SetAsLastSibling();

        if (botonReiniciar != null)
            botonReiniciar.transform.SetAsLastSibling();

        if (botonVolver != null)
            botonVolver.transform.SetAsLastSibling();
    }

    private void CrearMarco(RectTransform panel)
    {
        Transform existente = panel.Find("Marco_Trailer_Final");

        if (existente != null)
        {
            marcoFinal = existente.GetComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject(
                "Marco_Trailer_Final",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            go.transform.SetParent(panel, false);
            marcoFinal = go.GetComponent<Image>();
        }

        Texture2D textura =
            Resources.Load<Texture2D>("MarcoTrailerFinal");

        if (textura == null)
        {
            Debug.LogError(
                "[SereneLake] No se encuentra Resources/MarcoTrailerFinal.png"
            );
            return;
        }

        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0, 0, textura.width, textura.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        marcoFinal.sprite = sprite;
        marcoFinal.color = Color.white;
        marcoFinal.preserveAspect = false;
        marcoFinal.raycastTarget = false;

        RectTransform rt =
            marcoFinal.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        // Encima del vídeo, debajo de los botones
        if (rawImageTrailer != null)
            marcoFinal.transform.SetSiblingIndex(1);
    }

    private void PrepararBoton(
        Button boton,
        RectTransform panel,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        if (boton == null)
            return;

        RectTransform rt =
            boton.GetComponent<RectTransform>();

        rt.SetParent(panel, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        // El dibujo del botón ya está en el PNG.
        // El botón de Unity queda invisible pero sigue siendo pulsable.
        Image img = boton.GetComponent<Image>();

        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = true;
        }

        // Ocultar texto antiguo del botón.
        TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);

        if (texto != null)
            texto.gameObject.SetActive(false);
    }

    private T BuscarComponentePorNombre<T>(string nombre)
        where T : Component
    {
        T[] componentes =
            GetComponentsInChildren<T>(true);

        foreach (T componente in componentes)
        {
            if (componente.gameObject.name == nombre)
                return componente;
        }

        return null;
    }

    private Transform BuscarTransformPorNombre(string nombre)
    {
        Transform[] objetos =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform objeto in objetos)
        {
            if (objeto.name == nombre)
                return objeto;
        }

        return null;
    }
}
