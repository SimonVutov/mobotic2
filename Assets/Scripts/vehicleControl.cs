// VEHICLECONTROL.CS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WheelReference
{
    // set defaults button, to setup this wheel

    public Transform wheel;
    public KeyCode forwardsKey = KeyCode.W;
    public KeyCode backwardsKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    public float turnAngle = 45;
    public float torque = 200;
}

public class vehicleControl : MonoBehaviour
{
    public Vector3 centerOfMass = new Vector3(0, 0, 0);
    public WheelReference[] wheels;
    // button to auto search wheels, looking for WheelComponent in children
    public bool autoSearchWheels = false; // set to true to auto search wheels, treat as a button
    public void AutoSearchWheels()
    {
        // Find all WheelComponent components in children
        WheelComponent[] wheelComponents = GetComponentsInChildren<WheelComponent>();
        
        if (wheelComponents.Length == 0)
        {
            Debug.LogWarning("No WheelComponent found in children!");
            return;
        }
        
        // Create new wheels array
        wheels = new WheelReference[wheelComponents.Length];
        
        for (int i = 0; i < wheelComponents.Length; i++)
        {
            wheels[i] = new WheelReference();
            wheels[i].wheel = wheelComponents[i].transform;
            
            // Set default keys based on wheel position or index
            // You can customize this logic based on your wheel naming convention
            if (i == 0) // First wheel - default WASD
            {
                wheels[i].forwardsKey = KeyCode.W;
                wheels[i].backwardsKey = KeyCode.S;
                wheels[i].leftKey = KeyCode.A;
                wheels[i].rightKey = KeyCode.D;
            }
            else // Additional wheels - use arrow keys or other keys
            {
                wheels[i].forwardsKey = KeyCode.UpArrow;
                wheels[i].backwardsKey = KeyCode.DownArrow;
                wheels[i].leftKey = KeyCode.LeftArrow;
                wheels[i].rightKey = KeyCode.RightArrow;
            }
        }
        
        Debug.Log($"Auto-search complete! Found {wheelComponents.Length} wheels.");
    }

    // if autoSearchWheels is true, call AutoSearchWheels() and set to false, should run even when the scene is not play mode
    void OnValidate()
    {
        if (autoSearchWheels)
        {
            AutoSearchWheels();
            autoSearchWheels = false;
        }
    }

    public int sectionCount = 1;
    public float massInKg = 100.0f;

    void Awake()
    {
        foreach (WheelReference child in wheels)
        {
            child.wheel.GetComponent<WheelComponent>().parentRigidbody = GetComponent<Rigidbody>();
            child.wheel.GetComponent<WheelComponent>().vehicleController = this;
        }
    }

    void Start()
    {
        // add center of mass offset to current center of mass
        GetComponent<Rigidbody>().centerOfMass += centerOfMass;
    }

    void Update()
    {
        foreach (WheelReference child in wheels)
        {
            child.wheel.GetComponent<WheelComponent>().input = new Vector2(
                (Input.GetKey(child.rightKey) ? 1 : Input.GetKey(child.leftKey) ? -1 : 0) * child.turnAngle,
                (Input.GetKey(child.forwardsKey) ? 1 : Input.GetKey(child.backwardsKey) ? -1 : 0) * child.torque
            );
        }
    }
}
