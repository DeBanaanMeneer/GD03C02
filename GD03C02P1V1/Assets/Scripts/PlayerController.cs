using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Camera & Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    // We store pitch (up/down look) locally to clamp it
    private float _cameraPitch = 0f;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        
        // If camera isn't set, try to find the main camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Lock cursor to center for standard look controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Ground check
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Small downward force to keep grounded
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        var kb = UnityEngine.InputSystem.Keyboard.current;

        // Allow unlocking cursor when we want to draw in slomo (or holding a key)
        // For now, if we hold shift (slomo mode), we unlock the cursor so we can draw.
        bool holdToDraw = kb != null && kb.leftShiftKey.isPressed;
        
        if (holdToDraw)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Handle Mouse Look
            if (mouse != null)
            {
                float mouseX = mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
                float mouseY = mouse.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

                // Rotate Character Left/Right
                transform.Rotate(Vector3.up * mouseX);

                // Rotate Camera Up/Down
                if (cameraTransform != null)
                {
                    _cameraPitch -= mouseY;
                    _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);
                    cameraTransform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
                }
            }
        }

        // Handle WASD Movement
        float horizontal = 0f;
        float vertical = 0f;

        if (kb != null)
        {
            if (kb.wKey.isPressed) vertical += 1f;
            if (kb.sKey.isPressed) vertical -= 1f;
            if (kb.dKey.isPressed) horizontal += 1f;
            if (kb.aKey.isPressed) horizontal -= 1f;
        }

        // Create movement vector relative to the character's facing direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        if (move.magnitude > 1f) move.Normalize(); // Prevent faster diagonal movement

        _controller.Move(move * moveSpeed * Time.deltaTime);

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
