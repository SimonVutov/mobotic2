using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class forklift : MonoBehaviour
{
    public float moveSpeed = 2f;
    public List<Piece> pieces;
    
    private float springForce = 210f;
    private float dampingForce = 9f;
    private float clampForce = 210f;
    
    [HideInInspector]
    public List<GameObject> piecesObjects = new List<GameObject>();
    public float input;
    

    
    private void Start()
    {
        foreach (Piece piece in pieces)
        {
            if (piece.forkPrefab == null)
            {
                Debug.LogError("Piece has no prefab assigned! Skipping.");
                continue;
            }
            
            piece.pieceObject = Instantiate(piece.forkPrefab, transform.position, transform.rotation);
            piecesObjects.Add(piece.pieceObject);
            
            piece.pieceObject.transform.localScale = piece.shape;
            piece.pieceObject.transform.position = transform.TransformPoint(piece.position);
            piece.pieceObject.transform.rotation = transform.rotation;
            
            // Remove rotation constraints, let physics handle it
            Rigidbody rb = piece.pieceObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                continue;
            }
            rb.inertiaTensor = piece.inertiaTensorMultiplier * Vector3.one;
            
            // If lockRotation is true, constrain the rigidbody rotation
            if (piece.lockRotation)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            
            // make forklift a child of the vehicle so that it gets deleted when the vehicle is destroyed
            piece.pieceObject.transform.parent = transform;
            
            Debug.Log($"Forklift piece instantiated: {piece.forkPrefab.name}");
        }
        
        Debug.Log($"Forklift initialized with {pieces.Count} pieces");
    }
    
    void Update()
    {
        input = Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0;
    }
    
    void FixedUpdate()
    {
        foreach (Piece piece in pieces)
        {
            piece.targetHeight = piece.targetHeight + input * moveSpeed;
            piece.targetHeight = Mathf.Clamp(piece.targetHeight, 0, piece.maxHeight);

            // Calculate target position in world space, using the piece's position offset
            Vector3 targetWorldPosition = transform.TransformPoint(new Vector3(
                piece.position.x,
                piece.position.y + piece.targetHeight,
                piece.position.z
            ));
            Vector3 currentWorldPosition = piece.pieceObject.transform.position;

            Rigidbody rb = piece.pieceObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                // piece is just a visual object, move it directly (no rigidbody)
                piece.pieceObject.transform.position = targetWorldPosition;
                piece.pieceObject.transform.rotation = transform.rotation;
                continue;
            }

            // --- Linear spring-damper for position ---
            Vector3 positionError = targetWorldPosition - currentWorldPosition;
            Vector3 velocity = rb.linearVelocity;

            Vector3 totalForce = (positionError * springForce) - (velocity * dampingForce);
            totalForce = Vector3.ClampMagnitude(totalForce, clampForce); // Prevent extreme forces

            // Apply equal and opposite forces
            rb.AddForce(totalForce, ForceMode.Force);
            GetComponent<Rigidbody>().AddForceAtPosition(-totalForce, piece.pieceObject.transform.position, ForceMode.Force);

            // --- Angular spring-damper for rotation ---
            Quaternion currentRotation = piece.pieceObject.transform.rotation;
            Quaternion targetRotation = transform.rotation;

            // Calculate the shortest rotation from current to target
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (Mathf.Abs(angle) < 0.01f || axis == Vector3.zero)
                axis = Vector3.zero;
            else
                axis.Normalize();

            // Spring torque to correct orientation
            float angularSpring = 1000f;
            float angularDamping = 40f;
            float maxTorque = 1000f;

            Vector3 angularError = axis * Mathf.Deg2Rad * angle;
            Vector3 angularVelocity = rb.angularVelocity;

            Vector3 springTorque = angularError * angularSpring;
            Vector3 dampingTorque = -angularVelocity * angularDamping;
            Vector3 totalTorque = springTorque + dampingTorque;

            totalTorque = Vector3.ClampMagnitude(totalTorque, maxTorque);

            rb.AddTorque(totalTorque, ForceMode.Force);

            // If lockRotation is true, directly set the rotation to match the parent (override physics)
            if (piece.lockRotation)
            {
                piece.pieceObject.transform.rotation = transform.rotation;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void DestroyForklift()
    {
        foreach (Piece piece in pieces)
        {
            if (piece.pieceObject != null)
            {
                Destroy(piece.pieceObject);
            }
        }
        pieces.Clear();
        piecesObjects.Clear();
    }
    
    private void OnDestroy()
    {
        // Destroy all pieces when the vehicle is destroyed
        DestroyAllPieces();
    }
    
    private void DestroyAllPieces()
    {
        foreach (GameObject pieceObject in piecesObjects)
        {
            if (pieceObject != null)
            {
                Destroy(pieceObject);
            }
        }
        pieces.Clear();
        piecesObjects.Clear();
    }
}

[System.Serializable]
public class Piece
{
    public float inertiaTensorMultiplier = 1f;
    public GameObject forkPrefab;
    public Vector3 position = new Vector3(0, 0, 0);
    public Vector3 shape = new Vector3(1, 1, 1);
    public float maxHeight = 4f;
    public bool lockRotation = false;
    [HideInInspector]
    public GameObject pieceObject;
    [HideInInspector]
    public float targetHeight;
    [HideInInspector]
    public Vector3 oldForce;
}
