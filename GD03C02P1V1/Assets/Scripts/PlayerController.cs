using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float attackRadius = 1f;
    public float attackDamage = 1f;
    public float attackCooldown = 0.5f;
    public float attackHeightOffset = 0f;
    
    [Header("Lunge Visual")]
    public float lungeDistance = 2f;
    public float lungeDuration = 0.15f;
    private bool _isLunging = false;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private float _lastAttackTime;
    private CombatManager _combatManager;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _combatManager = FindAnyObjectByType<CombatManager>();
    }

    private void Update()
    {
        // Ground check
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Small downward force to keep grounded
        }

        // Handle Manual Attack
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !_isLunging)
        {
            if (_combatManager == null || !_combatManager.IsSlomoActive)
            {
                if (Time.time - _lastAttackTime >= attackCooldown)
                {
                    PerformManualAttack();
                    StartCoroutine(LungeRoutine());
                }
            }
        }

        var kb = UnityEngine.InputSystem.Keyboard.current;

        // Handle Jumping
        if (kb != null && kb.spaceKey.wasPressedThisFrame && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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

    private void PerformManualAttack()
    {
        _lastAttackTime = Time.time;
        
        // Attack enemies in front of the player
        Vector3 sphereCastStart = transform.position + Vector3.up * attackHeightOffset; // Adjust height locally
        
        RaycastHit[] hits = Physics.SphereCastAll(sphereCastStart, attackRadius, transform.forward, attackRange);
        
        foreach (var hit in hits)
        {
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    private IEnumerator LungeRoutine()
    {
        _isLunging = true;
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + transform.forward * lungeDistance;

        float timePushed = 0f;
        float halfDuration = lungeDuration / 2f;

        // Move forward
        while (timePushed < halfDuration)
        {
            timePushed += Time.deltaTime;
            float t = timePushed / halfDuration;
            
            // We use CharacterController.Move so we don't snap through walls
            Vector3 targetFramePos = Vector3.Lerp(startPos, targetPos, t);
            _controller.Move(targetFramePos - transform.position);
            
            yield return null;
        }

        // Return to where we ended up (allows movement during lunge)
        timePushed = 0f;
        Vector3 peakPos = transform.position;
        // The return target is essentially backward of where we are now by the lunge distance
        // (This handles if the player slid along a wall during the lunge)
        Vector3 returnTarget = peakPos - transform.forward * lungeDistance; 

        while (timePushed < halfDuration)
        {
            timePushed += Time.deltaTime;
            float t = timePushed / halfDuration;
            
            Vector3 targetFramePos = Vector3.Lerp(peakPos, returnTarget, t);
            _controller.Move(targetFramePos - transform.position);
            
            yield return null;
        }

        _isLunging = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 start = transform.position + Vector3.up * attackHeightOffset;
        Gizmos.DrawWireSphere(start, attackRadius);
        Gizmos.DrawWireSphere(start + transform.forward * attackRange, attackRadius);
        Gizmos.DrawLine(start, start + transform.forward * attackRange);
    }
}
