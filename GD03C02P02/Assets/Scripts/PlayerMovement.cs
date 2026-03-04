using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    private InputAction moveAction;
    private InputAction trailAction;

    public bool IsTrailActive => trailAction != null && trailAction.IsPressed();

    private void Awake()
    {
        // Setup input bindings for WASD, Arrows, and Gamepad
        moveAction = new InputAction("Move");
        
        // Setup trail activation binding
        trailAction = new InputAction("ActivateTrail", binding: "<Keyboard>/leftShift");
        trailAction.AddBinding("<Gamepad>/rightTrigger"); // Added gamepad support too
        
        // Add gamepad stick
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Add keyboard WASD composite
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        trailAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        trailAction.Disable();
    }

    private void Update()
    {
        // Get input from Input System
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontal = input.x;
        float vertical = input.y;

        // Calculate direction along the XZ plane
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.sqrMagnitude >= 0.1f)
        {
            // Calculate rotation towards the movement direction
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            
            // Smoothly rotate the player
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move the player forward
            transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
