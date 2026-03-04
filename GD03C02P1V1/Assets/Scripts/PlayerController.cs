using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Ground check
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Small downward force to keep grounded
        }

        var kb = UnityEngine.InputSystem.Keyboard.current;

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
