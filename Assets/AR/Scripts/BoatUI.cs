using UnityEngine;

public class BoatUI : MonoBehaviour
{
    public BarcoController barco;
    public JoystickBehavour joystick;

    void Start() => barco.useKeyboard = false;   // ahora manda el joystick

    void Update()
    {
        barco.SetThrottle(joystick.Value.y);
        barco.SetSteer(joystick.Value.x);
    }
}