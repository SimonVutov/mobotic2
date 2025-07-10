using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;          // Target to follow
    public float distance = 5.0f;     // Starting distance from the target
    public float zoomSpeed = 2.0f;    // Scroll wheel zoom speed
    public float minDistance = 2f;    // Min zoom
    public float maxDistance = 10f;   // Max zoom

    public float xSpeed = 120.0f;     // Horizontal rotation speed
    public float ySpeed = 120.0f;     // Vertical rotation speed

    public float yMinLimit = -20f;    // Minimum vertical angle
    public float yMaxLimit = 80f;     // Maximum vertical angle
    
    public bool requireRightClick = false; // Set to true to require right click for rotation

    private float x = 0.0f;           // Current horizontal rotation
    private float y = 0.0f;           // Current vertical rotation

    void Awake()
    {
        if (target == null)
        {
            // Search for a GameObject with vehicleControl component
            vehicleControl vehicleController = FindFirstObjectByType<vehicleControl>();
            if (vehicleController != null)
            {
                target = vehicleController.transform;
                Debug.Log("CameraController: Automatically found target with vehicleControl component.");
            }
            else
            {
                Debug.LogError("CameraController: No target set and no GameObject with vehicleControl component found.");
                enabled = false;
                return;
            }
        }
    }

    void Start()
    {
        if (target == null) return;

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // Always hide and lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Zoom in/out
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Check if we should rotate (based on requireRightClick setting)
        bool shouldRotate = requireRightClick ? Input.GetMouseButton(1) : true;
        
        if (shouldRotate)
        {
            // Remove Time.deltaTime for mouse input - mouse input is already frame-rate independent
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }

        // Always hide and lock the cursor, regardless of rotation state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }

    // Clamp vertical angle
    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}