using UnityEngine;
using UnityEngine.UI;

// Amplía a la vez el señuelo y una regla virtual para no perder la proporción.
// Al volver a tamaño real todo queda como al principio.
public class AmpliarContenido : MonoBehaviour
{
    public Transform contenido;
    public GameObject reglaVirtual;
    public Text textoBoton;
    public float factor = 2f;

    private bool ampliado = false;

    void Start()
    {
        reglaVirtual.SetActive(false);
        textoBoton.text = "Ampliar x" + factor;
    }

    public void Alternar()
    {
        ampliado = !ampliado;

        if (ampliado)
        {
            contenido.localScale = Vector3.one * factor;
            reglaVirtual.SetActive(true);
            textoBoton.text = "Tamaño real";
        }
        else
        {
            contenido.localScale = Vector3.one;
            reglaVirtual.SetActive(false);
            textoBoton.text = "Ampliar x" + factor;

            foreach (rotar r in contenido.GetComponentsInChildren<rotar>(true))
            {
                r.Reiniciar();
            }
        }
    }
}
