using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The player GameObject to follow.")]
    public Transform playerTransform;

    [Header("Distance & Offset")]
    public float distance = 5f;
    public Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Look Settings")]
    public float mouseSensitivity = 10f;
    public float minPitch = -20f;
    public float maxPitch = 80f;

    private float _yaw = 0f;
    private float _pitch = 0f;
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
        }

        if (playerTransform != null)
        {
            _yaw = playerTransform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

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
            if (mouse != null)
            {
                float mouseX = mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
                float mouseY = mouse.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

                _yaw += mouseX;
                _pitch -= mouseY;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

                // Rotate Character Left/Right to match camera
                playerTransform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
        }

        CalculateCameraPositionAndRotation();
    }

    private void CalculateCameraPositionAndRotation()
    {
        // Target center position
        Vector3 targetCenter = playerTransform.position + targetOffset;

        // Desired camera rotation
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Offset backward by distance
        Vector3 position = targetCenter - (rotation * Vector3.forward * distance);

        // Apply rotation and position
        transform.position = position;
        transform.rotation = rotation;
    }
}
