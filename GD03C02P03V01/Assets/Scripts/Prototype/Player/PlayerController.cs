using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
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
        }

        // Normalize so diagonal movement isn't faster
        movementInput = new Vector3(moveX, 0f, moveZ).normalized;
    }

    void FixedUpdate()
    {
        // Apply movement vector to the Rigidbody
        rb.MovePosition(rb.position + movementInput * moveSpeed * Time.fixedDeltaTime);
    }
}
