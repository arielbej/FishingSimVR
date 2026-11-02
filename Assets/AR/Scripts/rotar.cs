using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Igual que manipular, pero solo con el giro de un dedo:
// el señuelo no se puede ampliar para que siempre se vea a tamaño real
public class rotar : MonoBehaviour
{
    public float rotSpeed = 0.4f;

    void Update()
    {
        if (objIsTouched())
        {
            if (Input.touchCount == 1)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    this.transform.Rotate(Input.GetTouch(0).deltaPosition.y * rotSpeed, -Input.GetTouch(0).deltaPosition.x * rotSpeed, 0, Space.World);
                }
            }
        }
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
