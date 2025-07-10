using UnityEngine;

public class Vehicle_DifferentialDriveWithCastorWheel : MonoBehaviour
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
            if (wheel.tag == "poweredRight") wheel.wheelComponent.input = new Vector2(0, Input.GetAxis("Vertical") * 1000f - Input.GetAxis("Horizontal") * 3000f);
            if (wheel.tag == "poweredLeft") wheel.wheelComponent.input = new Vector2(0, Input.GetAxis("Vertical") * 1000f + Input.GetAxis("Horizontal") * 3000f);

            //reduce rotation
            Vector3 localAngularVelocity = transform.InverseTransformVector(rb.angularVelocity);
            // if (wheel.tag == "poweredRight") wheel.wheelComponent.input += new Vector2(0, -localAngularVelocity.z * 2000f);
            // if (wheel.tag == "poweredLeft") wheel.wheelComponent.input += new Vector2(0, localAngularVelocity.z * 2000f);
            Debug.Log(localAngularVelocity);
        }
    }
}
