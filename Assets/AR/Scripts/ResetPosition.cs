using UnityEngine;
using UnityEngine.UI;

public class ResetPosition : MonoBehaviour
{
    public Button button;              // el botón de reset
    public Transform barco;            // arrastra aquí BarcoGO

    Vector3 posInicial;

    void Start()
    {
        // Valor LOCAL, porque el barco es hijo del ImageTarget
        posInicial = barco.localPosition;

        if (button != null)
            button.onClick.AddListener(Resetear);
    }

    public void Resetear()
    {
        barco.localPosition = posInicial;
    }
}