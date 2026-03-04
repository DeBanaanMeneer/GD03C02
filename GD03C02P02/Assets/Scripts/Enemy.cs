using UnityEngine;
using System.Collections;

public enum EnemyState { Idle, Wander, Chase, Flee, Hurt, Dead }

public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float fleeSpeed = 8f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float fleeThresholdRange = 8f; // Increased range to react sooner
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float minWanderWait = 1f;
    [SerializeField] private float maxWanderWait = 3f;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRate = 1f; // Seconds between attacks
    private float nextAttackTime;

    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;

    private Vector3 targetWanderPos;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Transform player;
    private PlayerHealth playerHealth;
    private TrailController playerTrail;
    private Vector3 escapeTarget;

    private void Awake()
    {
        currentHealth = maxHealth;
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerTrail = playerObj.GetComponent<TrailController>();
        }
    }

    private void Start()
    {
        StartCoroutine(WanderRoutine());
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead) return;

        UpdateThreatDetection();
        CheckForAttack();

        switch (currentState)
        {
            case EnemyState.Wander:
                MoveTowards(targetWanderPos, walkSpeed);
                if (Vector3.Distance(transform.position, targetWanderPos) < 0.2f)
                {
                    currentState = EnemyState.Idle;
                }
                break;

            case EnemyState.Chase:
                if (player != null)
                {
                    MoveTowards(player.position, chaseSpeed);
                }
                break;

            case EnemyState.Flee:
                MoveTowards(escapeTarget, fleeSpeed);
                // Check if we reached escape target or threat is gone
                if (Vector3.Distance(transform.position, escapeTarget) < 1f)
                {
                    currentState = EnemyState.Idle;
                }
                break;
        }
    }

    private void UpdateThreatDetection()
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Hurt) return;
        
        // 1. Check for Flee Threat (The Trail) - UPDATED TO RECALCULATE EVERY FRAME
        if (playerTrail != null && playerTrail.IsActive)
        {
            var trailPoints = playerTrail.GetPoints();
            if (trailPoints != null && trailPoints.Count >= 3)
            {
                Vector3 fleeVector = Vector3.zero;
                int pointsInFleeRange = 0;

                foreach (var p in trailPoints)
                {
                    float dist = Vector3.Distance(transform.position, p);
                    if (dist < fleeThresholdRange)
                    {
                        // Stronger weight for closer points
                        float weight = 1.0f - (dist / fleeThresholdRange);
                        fleeVector += (transform.position - p).normalized * weight;
                        pointsInFleeRange++;
                    }
                }

                if (pointsInFleeRange > 0)
                {
                    fleeVector.Normalize();
                    fleeVector.y = 0;
                    
                    // Continuously update escape target while fleeing
                    escapeTarget = transform.position + fleeVector * 10f;
                    currentState = EnemyState.Flee;
                    return; 
                }
            }
        }

        // If we were fleeing but no trail is in range anymore, go back to Idle
        if (currentState == EnemyState.Flee)
        {
            currentState = EnemyState.Idle;
        }

        // 2. Check for Chase Threat (The Player)
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= detectionRange)
            {
                currentState = EnemyState.Chase;
            }
            else if (currentState == EnemyState.Chase)
            {
                currentState = EnemyState.Idle;
            }
        }
    }

    private void CheckForAttack()
    {
        if (player == null || playerHealth == null || currentState == EnemyState.Dead || currentState == EnemyState.Flee || currentState == EnemyState.Hurt)
            return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            playerHealth.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // Stay on floor
        
        if (direction != Vector3.zero)
        {
            Vector3 moveStep = direction * speed * Time.deltaTime;
            Vector3 potentialPosition = transform.position + moveStep;

            // Check if this move would cross the player's trail
            if (playerTrail != null)
            {
                var trailPoints = playerTrail.GetPoints();
                bool willCross = false;

                // Check intersection against every segment of the trail
                for (int i = 0; i < trailPoints.Count - 1; i++)
                {
                    if (LineSegmentsIntersect(transform.position, potentialPosition, trailPoints[i], trailPoints[i+1], out _))
                    {
                        willCross = true;
                        break;
                    }
                }

                if (willCross)
                {
                    // Don't cross! 
                    // We could implement sliding here, but for now just stopping is a clear "barrier" effect.
                    return;
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            transform.Translate(moveStep, Space.World);
        }
    }

    /// <summary>
    /// Helper to check if two line segments intersect on the XZ plane.
    /// </summary>
    private bool LineSegmentsIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;

        Vector2 p = new Vector2(a1.x, a1.z);
        Vector2 r = new Vector2(a2.x - a1.x, a2.z - a1.z);
        Vector2 q = new Vector2(b1.x, b1.z);
        Vector2 s = new Vector2(b2.x - b1.x, b2.z - b1.z);

        float rCrossS = r.x * s.y - r.y * s.x;
        Vector2 qMinusP = q - p;
        float qMinusPCrossR = qMinusP.x * r.y - qMinusP.y * r.x;

        if (Mathf.Abs(rCrossS) < 0.0001f) return false;

        float t = (qMinusP.x * s.y - qMinusP.y * s.x) / rCrossS;
        float u = qMinusPCrossR / rCrossS;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersectionPoint = new Vector3(p.x + t * r.x, a1.y, p.y + t * r.y);
            return true;
        }

        return false;
    }

    private IEnumerator WanderRoutine()
    {
        while (currentState != EnemyState.Dead)
        {
            if (currentState == EnemyState.Idle)
            {
                yield return new WaitForSeconds(Random.Range(minWanderWait, maxWanderWait));
                // Only pick a new wander spot if we aren't chasing the player now
                if (currentState == EnemyState.Idle) 
                {
                    targetWanderPos = GetRandomWanderPos();
                    currentState = EnemyState.Wander;
                }
            }
            yield return null;
        }
    }

    private Vector3 GetRandomWanderPos()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        randomDir.y = transform.position.y;
        return randomDir;
    }

    public void TakeDamage(float damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        Debug.Log($"Enemy {name} took {damage} damage. HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HurtEffect());
        }
    }

    private IEnumerator HurtEffect()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            enemyRenderer.material.color = originalColor;
        }
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        Debug.Log($"Enemy {name} died!");
        
        // Simple scale down effect
        StartCoroutine(DieSequence());
    }

    private IEnumerator DieSequence()
    {
        float timer = 0;
        Vector3 startScale = transform.localScale;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / 0.5f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
