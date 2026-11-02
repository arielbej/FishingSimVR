using UnityEngine;
using UnityEngine.UI;

// Muestra u oculta la ficha técnica del señuelo elegido en el desplegable
public class FichaTecnica : MonoBehaviour
{
    public GameObject panel;
    public Text texto;

    [TextArea(6, 12)]
    public string[] fichas;

    void Start()
    {
        panel.SetActive(false);
        Mostrar(0);
    }

    public void Alternar()
    {
        panel.SetActive(!panel.activeSelf);
    }

    public void Mostrar(int i)
    {
        texto.text = fichas[i];
    }
}
