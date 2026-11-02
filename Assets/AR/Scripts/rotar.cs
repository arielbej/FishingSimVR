using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Igual que manipular, pero solo con el giro de un dedo:
// el señuelo no se puede ampliar para que siempre se vea a tamaño real
public class rotar : MonoBehaviour
{
    public float rotSpeed = 0.4f;
    private Vector3 posInicial;
    private Quaternion rotInicial;
    private bool guardado = false;

    void Start()
    {
        Guardar();
    }

    // Los señuelos que aún no se han mostrado no han pasado por Start,
    // así que también se guarda su posición la primera vez que se reinician
    void Guardar()
    {
        if (guardado) return;
        posInicial = this.transform.localPosition;
        rotInicial = this.transform.localRotation;
        guardado = true;
    }

    void Update()
    {
        if (objIsTouched())
        {
            if (Input.touchCount == 1)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    // Gira según cómo mira la cámara: arrastrar a la derecha lo gira a la derecha
                    // y arrastrar hacia arriba lo vuelca hacia arriba, se mire desde donde se mire
                    Transform cam = Camera.main.transform;
                    this.transform.Rotate(cam.up, -Input.GetTouch(0).deltaPosition.x * rotSpeed, Space.World);
                    this.transform.Rotate(cam.right, Input.GetTouch(0).deltaPosition.y * rotSpeed, Space.World);
                }
            }
        }
    }

    // Deja el señuelo recto y en su sitio, como al empezar
    public void Reiniciar()
    {
        Guardar();
        this.transform.localPosition = posInicial;
        this.transform.localRotation = rotInicial;
    }

    private bool objIsTouched()
    {
        foreach (Touch t in Input.touches)
        {
            Ray m_ray = Camera.main.ScreenPointToRay(t.position);
            RaycastHit m_hit;
            if (Physics.Raycast(m_ray, out m_hit, 100))
            {
                if (m_hit.transform.name == this.gameObject.name)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
