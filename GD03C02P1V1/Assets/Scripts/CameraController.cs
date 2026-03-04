using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The player GameObject to follow.")]
    public Transform playerTransform;
    
    [Tooltip("The local offset from the player's position (e.g., eye height).")]
    public Vector3 positionOffset = new Vector3(0f, 0.6f, 0f);

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;

    private float _cameraPitch = 0f;
    private CombatManager _combatManager;

    private void Start()
    {
        _combatManager = FindObjectOfType<CombatManager>();

        if (playerTransform == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("[CameraController] No PlayerTransform assigned and couldn't find a PlayerController in the scene!");
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        bool isSlomoActive = _combatManager != null && _combatManager.IsSlomoActive;

        if (isSlomoActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var mouse = Mouse.current;
            if (mouse != null && playerTransform != null)
            {
                float mouseX = mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
                float mouseY = mouse.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

                // Rotate Character Left/Right (The camera will follow this rotation automatically at the end of LateUpdate)
                playerTransform.Rotate(Vector3.up * mouseX);

                // Rotate Camera Pitch Up/Down internally
                _cameraPitch -= mouseY;
                _cameraPitch = Mathf.Clamp(_cameraPitch, -80f, 80f);
            }
        }

        // Apply following and rotation every frame (even in slomo, to keep it attached if the player moves)
        if (playerTransform != null)
        {
            // 1. Follow the player's position, applying the offset relative to the player's rotation
            transform.position = playerTransform.position + playerTransform.TransformVector(positionOffset);
            
            // 2. Match the player's Y rotation, but apply our own X pitch for looking up/down
            transform.rotation = Quaternion.Euler(_cameraPitch, playerTransform.eulerAngles.y, 0f);
        }
    }
}
