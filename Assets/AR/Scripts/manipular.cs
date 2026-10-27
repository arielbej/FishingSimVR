using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class manipular : MonoBehaviour
{
    public float rotSpeed = 0.4f;
    private float initialDistance;
    private Vector3 initialScale;

    void Update()
    {
        if (objIsTouched())
        {
            if (Input.touchCount == 1)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    Quaternion initRotation = this.transform.rotation;
                    this.transform.Rotate(Input.GetTouch(0).deltaPosition.y * rotSpeed, -Input.GetTouch(0).deltaPosition.x * rotSpeed, 0, Space.World);
                }
            }

            if (Input.touchCount == 2)
            {
                var touchZero = Input.GetTouch(0);
                var touchOne = Input.GetTouch(1);

                if (touchZero.phase == TouchPhase.Ended || touchZero.phase == TouchPhase.Canceled
                   || touchOne.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Canceled)
                {
                    return;
                }

                if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
                {
                    initialDistance = Vector2.Distance(touchZero.position, touchOne.position);
                    initialScale = this.transform.localScale;
                }
                else
                {
                    var currentDistance = Vector2.Distance(touchZero.position, touchOne.position);
                    if (Mathf.Approximately(initialDistance, 0)) return;
                    var factor = currentDistance / initialDistance;
                    this.transform.localScale = initialScale * factor;
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
