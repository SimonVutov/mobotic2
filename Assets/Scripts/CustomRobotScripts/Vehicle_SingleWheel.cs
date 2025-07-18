using UnityEngine;

public class Vehicle_SingleWheel : MonoBehaviour
{
    [HideInInspector] public vehicleControl vc;
    [HideInInspector] public Rigidbody rb;

    void Start()
    {
        vc = GetComponent<vehicleControl>();
        rb = GetComponent<Rigidbody>();
    }
    
    void Update() // in unique vehicle controller scripts, add inputs and wheel controls
    {
        foreach (WheelReference wheel in vc.wheels)
        { // wheelComponent.input is a vector2, x is the steering angle (in degrees), y is the torque (in Nm)
            if (wheel.tag == "power") wheel.wheelComponent.input = new Vector2(Input.GetAxis("Horizontal") * 45f, Input.GetAxis("Vertical") * 350f);
        }
    }
}