using UnityEngine;

// El desplegable llama a Elegir con el número de la opción elegida
public class ElegirSenuelo : MonoBehaviour
{
    public GameObject[] senuelos;

    void Start()
    {
        Elegir(0);
    }

    public void Elegir(int i)
    {
        for (int j = 0; j < senuelos.Length; j++)
        {
            senuelos[j].SetActive(j == i);
        }
    }
}
