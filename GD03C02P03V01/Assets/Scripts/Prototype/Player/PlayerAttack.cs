using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackRadius = 1f;
    public LayerMask enemyLayer;
    public float attackCooldown = 0.5f;

    [Header("Swipe Attack Settings")]
    public float swipeRange = 2f;
    public float swipeWidth = 3f;
    public float swipeCooldown = 1f;
    
    [Header("Lunge Visual Settings")]
    public float lungeDuration = 0.1f;
    public float returnDuration = 0.15f;

    private float lastAttackTime;
    private float lastSwipeTime;
    private bool isAttacking;

    void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current != null && !isAttacking)
        {
            // Left Click - Lunge Attack
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(AttackRoutine());
            }

            // Right Click - Swipe Attack
            if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame && Time.time >= lastSwipeTime + swipeCooldown)
            {
                StartCoroutine(SwipeRoutine());
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Briefly disable normal movement
        PlayerController controller = GetComponent<PlayerController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (controller != null) controller.enabled = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * attackRange;

        // Lunge Forward
        float elapsedTime = 0f;
        while (elapsedTime < lungeDuration)
        {
            float t = elapsedTime / lungeDuration;
            // Use smooth step for a punchy feel
            t = t * t * (3f - 2f * t);

            Vector3 stepPos = Vector3.Lerp(startPos, targetPos, t);
            if (rb != null) rb.MovePosition(stepPos);
            else transform.position = stepPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Exact hit calculation at the target position
        PerformDamage(targetPos);

        // Return Back
        elapsedTime = 0f;
        while (elapsedTime < returnDuration)
        {
            float t = elapsedTime / returnDuration;
            t = t * t * (3f - 2f * t);

            Vector3 stepPos = Vector3.Lerp(targetPos, startPos, t);
            if (rb != null) rb.MovePosition(stepPos);
            else transform.position = stepPos;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap perfectly back
        if (rb != null) rb.MovePosition(startPos);
        else transform.position = startPos;

        if (controller != null) controller.enabled = true;
        isAttacking = false;
    }

    private void PerformDamage(Vector3 damageCenter)
    {
        Collider[] hitColliders = Physics.OverlapSphere(damageCenter, attackRadius, enemyLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            EnemyHealth enemy = hitCollider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeHit();
                Debug.Log("Hit enemy: " + enemy.gameObject.name);
            }
        }
    }

    private IEnumerator SwipeRoutine()
    {
        isAttacking = true;
        lastSwipeTime = Time.time;

        // Briefly wait to sell the "windup"
        yield return new WaitForSeconds(0.1f);

        // Perform wide box overlap for "swipe" damage
        Vector3 swipeBoxCenter = transform.position + transform.forward * swipeRange * 0.5f;
        Vector3 swipeBoxHalfExtents = new Vector3(swipeWidth * 0.5f, 1f, swipeRange * 0.5f);
        
        Collider[] hitColliders = Physics.OverlapBox(swipeBoxCenter, swipeBoxHalfExtents, transform.rotation, enemyLayer);
        int hitCount = 0;

        foreach (Collider hitCollider in hitColliders)
        {
            EnemyHealth enemy = hitCollider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeHit();
                hitCount++;
            }
        }
        
        Debug.Log($"Swipe attacked and hit {hitCount} enemies!");

        // Brief delay for the follow-through
        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
    }

    // Visualize the attack range in the editor
    private void OnDrawGizmosSelected()
    {
        // Lunge Attack Sphere
        Gizmos.color = Color.red;
        Vector3 targetPos = transform.position + transform.forward * attackRange;
        Gizmos.DrawWireSphere(targetPos, attackRadius);
        Gizmos.DrawLine(transform.position, targetPos);

        // Swipe Attack Box
        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.forward * swipeRange * 0.5f, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(swipeWidth, 2f, swipeRange));
    }
}
