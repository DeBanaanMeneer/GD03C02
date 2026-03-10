using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer = ~0; // Default to all layers
    private bool isGrounded;

    private Rigidbody rb;
    private Vector3 movementInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze rotation so the capsule doesn't tip over
        rb.constraints = RigidbodyConstraints.FreezeRotation; 
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // Get Input using the new Input System's static Keyboard class
        // (This is a quick way to prototype without setting up Input Actions)
        float moveX = 0f;
        float moveZ = 0f;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveX += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveX -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveZ += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveZ -= 1f;

            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        // Get camera relative directions
        Transform cameraTransform = Camera.main.transform;
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten directions on the XZ plane
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate relative movement direction
        Vector3 moveDirection = (forward * moveZ + right * moveX).normalized;
        movementInput = moveDirection;

        // Rotate player to always face the camera's forward direction (like a 3rd person shooter)
        // so they strafe on A/D and walk backwards on S.
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Apply movement vector to the Rigidbody
        rb.MovePosition(rb.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }
}
