using UnityEngine;

public class Vehicle_TwoWheelDifferentialDrive : MonoBehaviour
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
            if (wheel.tag == "right") wheel.wheelComponent.input = new Vector2(0, Input.GetAxis("Vertical") * 300f - Input.GetAxis("Horizontal") * 800f);
            if (wheel.tag == "left") wheel.wheelComponent.input = new Vector2(0, Input.GetAxis("Vertical") * 300f + Input.GetAxis("Horizontal") * 800f);
        }
    }
}