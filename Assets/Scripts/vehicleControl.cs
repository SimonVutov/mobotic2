using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WheelReference
{
    public Transform wheel;
    public string tag = "wheel";
    [HideInInspector] public WheelComponent wheelComponent;
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
            wheels[i].wheelComponent = wheelComponents[i];
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
            // Get the WheelComponent and store it in the wheelComponent field
            child.wheelComponent = child.wheel.GetComponent<WheelComponent>();
            
            if (child.wheelComponent != null)
            {
                child.wheelComponent.vehicleController = this;
            }
            else
            {
                Debug.LogError($"WheelComponent not found on {child.wheel.name}!");
            }
        }
    }

    void Start()
    {
        // add center of mass offset to current center of mass
        GetComponent<Rigidbody>().centerOfMass += centerOfMass;
    }
}
