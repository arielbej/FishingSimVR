using System.ComponentModel;
using UnityEngine;

[Description("Script de Debugging para saber el shadow distance de la camara AR")]

public class SD_Calculator : MonoBehaviour
{
    public Transform arcamera;   // ARCamera
    void Update()
    {
        Debug.Log("Distancia cámara-target: " + Vector3.Distance(transform.position, arcamera.position));
    }
}
