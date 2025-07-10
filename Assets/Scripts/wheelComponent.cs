// WHEELCOMPONENT.CS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelComponent : MonoBehaviour
{
    public string defaultTag = "wheel";
    public float wheelSizeOverride = -1f;
    [Header("Wheel Visual")]
    public Transform wheelVisual;  // Reference to existing wheel visual component
    private Transform wheelRim;    // Will be set to the first child of wheelVisual
    public bool hideWheelVisual = false;
    
    [Header("Wheel Configuration")]
    public bool freeRoll = false;  // If true, wheel will free roll; if false, wheel is powered
    public bool flipX = false;
    
    [Header("Suspension")]
    [Range(0.01f, 2f)] public float suspensionLength = 0.1f;
    
    [Header("Physics Properties")]
    [Range(0.1f, 5f)] public float frictionCoefficient = 1f;
    [Range(10f, 1000f)] public float suspensionForce = 90f;
    [Range(10f, 1000f)] public float maxGrip = 250f;
    [Range(0.1f, 30f)] public float wheelGrip = 15f;
    [Range(0.1f, 400f)] public float suspensionForceClamp = 200f;
    [Range(0.1f, 10f)] public float dampingAmount = 2.5f;
    [Range(0.01f, 1.0f)] public float dragCoefficient = 0.3f;
    [Range(0.001f, 0.1f)] public float rollingResistance = 0.015f;
    
    [Header("Enhanced Physics")]
    [Range(0.1f, 2f)] public float coefStaticFriction = 0.95f;
    [Range(0.1f, 2f)] public float coefKineticFriction = 0.35f;
    [Range(0.1f, 50f)] public float wheelGripX = 19f;    // Lateral grip
    [Range(0.1f, 50f)] public float wheelGripZ = 19f;   // Longitudinal grip
    [Range(0.1f, 50f)] public float wheelMass = 1f;    // Mass for inertia calculation
    [Range(0.1f, 10f)] public float brakeStrength = 0.5f;
    
    [HideInInspector] public Rigidbody parentRigidbody;

    // Hidden Public Variables
    public Vector2 input = Vector2.zero;
    [HideInInspector] public bool isMovingForward = false;
    [HideInInspector] public vehicleControl vehicleController;
    [HideInInspector] public float braking = 0f;

    // Enhanced Runtime Properties
    private Vector3 wheelWorldPosition;
    private Vector3 localVelocity;
    private Vector3 localSlipDirection;
    private Vector3 worldSlipDirection;
    private Vector3 suspensionForceDirection;
    private float lastSuspensionLength = 0f;
    private float torque = 0f;
    private float hitPointForce;
    private int rollingDirectionMultiplier = 1;
    private float lerpedInputX = 0f;
    
    // New enhanced physics properties
    private float angularVelocity = 0f;
    private float normalForce = 0f;
    private float slip = 0f;
    private float xSlipAngle = 0f;
    private bool isSliding = false;
    private float wheelInertia = 0f;

    private void Awake()
    {
        // Find parent rigidbody by traversing up the hierarchy
        FindParentRigidbody();
    }

    private void OnValidate()
    {
        // Set default values if not already set
        if (suspensionForce <= 0) suspensionForce = 270.0f;
        if (maxGrip <= 0) maxGrip = 400.0f;
        if (frictionCoefficient <= 0) frictionCoefficient = 1f;
        if (suspensionLength <= 0) suspensionLength = 0.1f;
        
        // Validate new physics properties
        if (coefStaticFriction < coefKineticFriction)
        {
            Debug.LogWarning("Static friction should be higher than kinetic friction for realistic physics");
        }
    }

    private void Start()
    {
        InitializeComponents();
        InitializeWheel();
        if (hideWheelVisual) wheelVisual.gameObject.SetActive(false);
        
        // Calculate wheel inertia for angular velocity calculations
        float wheelRadius = GetWheelRadius();
        wheelInertia = wheelMass * wheelRadius * wheelRadius / 2f;
    }

    private void FixedUpdate()
    {
        ApplyAerodynamicDrag();
        UpdateWheelPhysics();
        lerpedInputX = Mathf.Lerp(lerpedInputX, input.x, Time.fixedDeltaTime * 5);
    }

    #region Initialization Methods

    private void FindParentRigidbody()
    {
        // Start from the parent transform (one level up from this component)
        Transform currentTransform = transform.parent;
        
        // Traverse up the hierarchy until we find a rigidbody or reach the root
        while (currentTransform != null)
        {
            Rigidbody rb = currentTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                parentRigidbody = rb;
                Debug.Log($"WheelComponent on '{gameObject.name}' found rigidbody on '{rb.gameObject.name}'");
                return;
            }
            currentTransform = currentTransform.parent;
        }
        
        // If we reach here, no rigidbody was found
        Debug.LogError($"WheelComponent on {gameObject.name}: No Rigidbody found in the parent hierarchy. Please add a Rigidbody component to a parent GameObject.");
    }

    private void InitializeComponents()
    {
        // Find vehicle controller if needed
        if (vehicleController == null)
        {
            vehicleController = transform.root.GetComponent<vehicleControl>();
        }
    }

    private void InitializeWheel()
    {
        // Verify wheel visual is set
        if (wheelVisual == null)
        {
            Debug.LogError("Wheel visual is not assigned. Please assign a Transform for the wheel visual.");
            return;
        }

        // Get the first child of wheelVisual (the rim/tire that should rotate)
        if (wheelVisual.childCount > 0)
        {
            wheelRim = wheelVisual.GetChild(0);
        }
        else
        {
            Debug.LogError("Wheel visual has no children. Please add a child object to represent the rotating rim/tire.");
            return;
        }
        
        // Handle wheel visual flipping and set rolling direction multiplier
        if (flipX)
        {
            // Vector3 visualScale = wheelVisual.localScale;
            // visualScale.x = -Mathf.Abs(visualScale.x);
            // wheelVisual.localScale = visualScale;
            rollingDirectionMultiplier = -1; // Reverse the rolling direction
        }
        else
        {
            rollingDirectionMultiplier = 1;
        }
        
        // Set initial position (now using transform position as base)
        wheelWorldPosition = wheelVisual.position;
    }

    private float GetWheelRadius()
    {
        if (wheelVisual == null) return 0.3f; // Default fallback
        if (wheelSizeOverride > 0) return wheelSizeOverride;
        return this.transform.localScale.x * 0.5f;
    }

    #endregion

    #region Enhanced Physics Update Methods

    private void ApplyAerodynamicDrag()
    {
        if (parentRigidbody == null) return;
        
        Vector3 dragForce = -dragCoefficient * 
                            parentRigidbody.linearVelocity.sqrMagnitude * 
                            parentRigidbody.linearVelocity.normalized * 
                            Time.fixedDeltaTime;
        
        parentRigidbody.AddForce(dragForce);
    }

    private void UpdateWheelPhysics()
    {
        if (wheelVisual == null || parentRigidbody == null) return;

        // Update wheel world position
        wheelWorldPosition = transform.position;
        
        // Perform raycast for ground detection
        RaycastHit hit;
        float rayLength = GetWheelRadius() + suspensionLength;
        bool isGrounded = Physics.Raycast(wheelWorldPosition, -transform.up, out hit, rayLength);
        
        if (isGrounded)
        {
            UpdateGroundedWheel(hit);
        }
        else
        {
            UpdateAirborneWheel();
        }
        
        // Update wheel rotation
        UpdateWheelRotation();
    }

    private void UpdateGroundedWheel(RaycastHit hit)
    {
        // Calculate velocities
        Vector3 velocityAtWheel = parentRigidbody.GetPointVelocity(wheelWorldPosition);
        Vector3 worldVelAtHit = parentRigidbody.GetPointVelocity(hit.point);
        localVelocity = wheelVisual.InverseTransformDirection(velocityAtWheel);
        Vector3 localHitVelocity = wheelVisual.InverseTransformDirection(worldVelAtHit);
        
        // Update forward direction
        isMovingForward = localVelocity.z > 0.1f;
        
        // Calculate suspension forces
        UpdateSuspensionForces(hit);
        
        // Calculate wheel torque and angular velocity
        UpdateWheelTorque();
        
        // Calculate friction forces
        UpdateFrictionForces(localHitVelocity, hit.point);
        
        // Calculate slip angle
        UpdateSlipAngle();
        
        // Position wheel at contact point
        wheelVisual.position = hit.point + transform.up * GetWheelRadius();
        lastSuspensionLength = hit.distance;
    }

    private void UpdateAirborneWheel()
    {
        // Position wheel at maximum extension when no contact
        wheelVisual.position = wheelWorldPosition - transform.up * suspensionLength;
        suspensionForceDirection = Vector3.zero;
        normalForce = 0f;
        isSliding = false;
        slip = 0f;
        
        // Apply some drag to angular velocity when airborne
        angularVelocity *= 0.98f;
    }

    private void UpdateSuspensionForces(RaycastHit hit)
    {
        // Calculate compression and damping
        float compression = GetWheelRadius() + suspensionLength - hit.distance;
        float damping = (lastSuspensionLength - hit.distance) * dampingAmount;
        
        // Calculate normal force
        normalForce = (compression + damping) * suspensionForce;
        normalForce = Mathf.Clamp(normalForce, 0f, suspensionForceClamp);
        
        // Apply suspension force
        suspensionForceDirection = hit.normal * normalForce;
    }

    private void UpdateWheelTorque()
    {
        // Calculate torque from input (only if not free rolling)
        if (!freeRoll)
        {
            if (vehicleController != null)
            {
                torque = input.y * GetWheelRadius() * 
                        (vehicleController.sectionCount / vehicleController.massInKg) * 
                        maxGrip * 0.1f; // Scale factor for reasonable torque
            }
            else
            {
                torque = input.y * GetWheelRadius() * maxGrip * 0.1f;
            }
        }
        else
        {
            torque = 0f;
        }
    }

    private void UpdateFrictionForces(Vector3 localHitVelocity, Vector3 hitPoint)
    {
        // Calculate lateral and longitudinal friction forces
        float lateralFriction = -wheelGripX * localVelocity.x - 2f * localHitVelocity.x;
        float longitudinalFriction = -wheelGripZ * (localVelocity.z - angularVelocity * GetWheelRadius());
        
        // Apply rolling resistance torque
        float rollingResistanceTorque = 0f;
        if (normalForce > 0f)
        {
            float rollingResistanceForce = rollingResistance * normalForce;
            rollingResistanceTorque = rollingResistanceForce * GetWheelRadius();
            rollingResistanceTorque *= -Mathf.Sign(angularVelocity);
        }
        
        // Update angular velocity based on torque and friction
        float netTorque = torque - longitudinalFriction * GetWheelRadius() - rollingResistanceTorque;
        angularVelocity += netTorque / wheelInertia * Time.fixedDeltaTime;
        
        // Apply braking
        angularVelocity *= 1f - braking * brakeStrength * Time.fixedDeltaTime;
        
        // Calculate total local force
        Vector3 totalLocalForce = new Vector3(lateralFriction, 0f, longitudinalFriction) * 
                                 normalForce * coefStaticFriction * frictionCoefficient * Time.fixedDeltaTime;
        
        // Calculate maximum friction force
        float maxFrictionForce = normalForce * coefStaticFriction * frictionCoefficient;
        
        // Determine if wheel is sliding and calculate slip
        isSliding = totalLocalForce.magnitude > maxFrictionForce;
        slip = maxFrictionForce > 0f ? totalLocalForce.magnitude / maxFrictionForce : 0f;
        
        // Clamp force and apply kinetic friction if sliding
        totalLocalForce = Vector3.ClampMagnitude(totalLocalForce, maxFrictionForce);
        if (isSliding)
        {
            totalLocalForce *= (coefKineticFriction / coefStaticFriction);
        }
        
        // Convert to world space
        worldSlipDirection = wheelVisual.TransformDirection(totalLocalForce);
        
        // Apply forces to rigidbody
        parentRigidbody.AddForceAtPosition(suspensionForceDirection + worldSlipDirection, 
                                          hitPoint);
    }

    private void UpdateSlipAngle()
    {
        // Calculate slip angle only when moving with sufficient velocity
        if (localVelocity.magnitude > 0.5f)
        {
            // Calculate the velocity angle
            float velocityAngle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;
            
            // Current wheel steering angle (input.x is already in degrees from vehicleControl)
            float currentWheelAngle = lerpedInputX;
            
            // Slip angle is the difference between where we're going vs where we're pointed
            float rawSlipAngle = velocityAngle - currentWheelAngle;
            
            // Normalize angle to [-180, 180] range
            while (rawSlipAngle > 180f) rawSlipAngle -= 360f;
            while (rawSlipAngle < -180f) rawSlipAngle += 360f;
            
            // Apply smoothing to reduce jitter
            xSlipAngle = Mathf.Lerp(xSlipAngle, rawSlipAngle, Time.fixedDeltaTime * 10f);
        }
        else
        {
            xSlipAngle = Mathf.Lerp(xSlipAngle, 0f, Time.fixedDeltaTime * 5f);
        }
    }

    private void UpdateWheelRotation()
    {
        // STEP 1: Update the wheelVisual rotation (steering - Y axis only)
        if (!freeRoll)
        {
            // Only rotate around Y axis for steering (input.x is already in degrees from vehicleControl)
            Quaternion targetYRotation = Quaternion.Euler(0, -lerpedInputX, 0);
            wheelVisual.localRotation = Quaternion.Lerp(
                wheelVisual.localRotation,
                targetYRotation,
                Time.fixedDeltaTime * 15);
        }
        else if (parentRigidbody.linearVelocity.magnitude > 0.04f && 
                suspensionForceDirection != Vector3.zero)
        {
            // For free rolling wheels, align with velocity direction but only on Y axis
            Vector3 velocity = parentRigidbody.GetPointVelocity(wheelWorldPosition);
            if (velocity.magnitude > 0.1f)
            {
                // Project velocity onto XZ plane
                Vector3 velocityXZ = new Vector3(velocity.x, 0, velocity.z).normalized;
                
                // Convert to local direction relative to parent
                Vector3 localDir = transform.InverseTransformDirection(velocityXZ);
                
                // Calculate target Y rotation only
                float targetYAngle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                Quaternion targetYRotation = Quaternion.Euler(0, targetYAngle, 0);
                
                wheelVisual.localRotation = Quaternion.Lerp(
                    wheelVisual.localRotation,
                    targetYRotation,
                    Time.fixedDeltaTime * 15);
            }
        }

        // STEP 2: Update the wheelRim rotation (rolling motion)
        if (wheelRim != null)
        {
            // Use the calculated angular velocity for rim rotation
            float wheelRotationSpeed = angularVelocity * Mathf.Rad2Deg;
            
            // Apply the direction multiplier based on flipX setting
            wheelRotationSpeed *= rollingDirectionMultiplier;
            
            // Rotate the rim (first child) around its X axis
            wheelRim.Rotate(Vector3.right, wheelRotationSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    #endregion

    #region Public Interface Methods
    
    /// <summary>
    /// Get the current slip ratio of the wheel (0 = no slip, 1 = maximum grip, >1 = sliding)
    /// </summary>
    public float GetSlipRatio()
    {
        return slip;
    }
    
    /// <summary>
    /// Get the current slip angle in degrees
    /// </summary>
    public float GetSlipAngle()
    {
        return xSlipAngle;
    }
    
    /// <summary>
    /// Check if the wheel is currently sliding
    /// </summary>
    public bool IsSliding()
    {
        return isSliding;
    }
    
    /// <summary>
    /// Get the current angular velocity of the wheel in rad/s
    /// </summary>
    public float GetAngularVelocity()
    {
        return angularVelocity;
    }
    
    /// <summary>
    /// Get the current normal force on the wheel
    /// </summary>
    public float GetNormalForce()
    {
        return normalForce;
    }

    #endregion

    #region Debug Methods

    private void OnDrawGizmos()
    {
        // Draw wheel position
        Gizmos.color = Color.green;
        Vector3 gizmoPosition = Application.isPlaying ? wheelWorldPosition : transform.position;
        Gizmos.DrawSphere(gizmoPosition, 0.01f);
        
        // Draw suspension range
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(gizmoPosition, gizmoPosition - transform.up * (suspensionLength + GetWheelRadius()));

        if (!Application.isPlaying) return;

        // Draw suspension force
        if (suspensionForceDirection != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wheelWorldPosition, 
                            wheelWorldPosition + suspensionForceDirection * 0.01f);
        }

        // Draw slip direction
        if (worldSlipDirection != Vector3.zero)
        {
            Gizmos.color = isSliding ? Color.red : Color.yellow;
            Gizmos.DrawLine(wheelWorldPosition, 
                            wheelWorldPosition + worldSlipDirection * 0.01f);
        }
        
        // Draw slip angle indicator
        if (Mathf.Abs(xSlipAngle) > 1f)
        {
            Gizmos.color = Color.magenta;
            Vector3 slipDirection = Quaternion.Euler(0, xSlipAngle, 0) * transform.forward;
            Gizmos.DrawLine(wheelWorldPosition, wheelWorldPosition + slipDirection * 0.5f);
        }
    }

    #endregion
}