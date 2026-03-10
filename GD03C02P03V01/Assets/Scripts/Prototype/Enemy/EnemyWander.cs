using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyWander : MonoBehaviour
{
    [Header("Wander Settings")]
    public float moveSpeed = 2f;
    public float changeDirectionInterval = 3f;
    
    [Header("Damage Settings")]
    public float damageRadius = 1.5f;
    public float damageAmount = 10f;
    public float damageCooldown = 1f;
    public float attackWindupTime = 2f; // Time player must be near before taking damage
    public LayerMask playerLayer;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float loseTargetRadius = 15f;

    private float lastDamageTime;
    private float timeNearPlayer; // Tracks how long player has been in radius
    private Rigidbody rb;
    private Vector3 currentDirection;
    private float directionTimer;
    private Transform currentTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze rotation so the capsule doesn't tip over
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        ChooseNewDirection();
    }

    void Update()
    {
        // Target Detection
        HandleTargeting();

        // Behavior selection
        if (currentTarget != null)
        {
            // Chase logic
            Vector3 directionToTarget = (currentTarget.position - transform.position);
            directionToTarget.y = 0f; // Keep it on the XZ plane
            currentDirection = directionToTarget.normalized;
        }
        else
        {
            // Wander logic
            directionTimer -= Time.deltaTime;
            if (directionTimer <= 0f)
            {
                ChooseNewDirection();
            }
        }

        // Damage Player Logic... (rest of the check)
        CheckDamageProximity();
    }

    private void HandleTargeting()
    {
        if (currentTarget == null)
        {
            // Search for target within detection radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
            if (hitColliders.Length > 0)
            {
                currentTarget = hitColliders[0].transform;
                Debug.Log($"{gameObject.name} found player!");
            }
        }
        else
        {
            // Check if existing target is too far away
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > loseTargetRadius)
            {
                currentTarget = null;
                ChooseNewDirection(); // Start wandering immediately in a new direction
                Debug.Log($"{gameObject.name} lost player (too far)!");
            }
        }
    }

    private void CheckDamageProximity()
    {
        // Check if player is near for damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, playerLayer);
        
        if (hitColliders.Length > 0)
        {
            // Player is inside the damage radius
            timeNearPlayer += Time.deltaTime;

            // Attack Logic
            if (timeNearPlayer >= attackWindupTime && Time.time >= lastDamageTime + damageCooldown)
            {
                foreach (Collider hitCollider in hitColliders)
                {
                    PlayerHealth playerParams = hitCollider.GetComponent<PlayerHealth>();
                    if (playerParams != null)
                    {
                        playerParams.TakeDamage(damageAmount);
                        lastDamageTime = Time.time;
                        Debug.Log("Enemy damaged player after being near for 2s!");
                        break; // Only damage one player per tick
                    }
                }
            }
        }
        else
        {
            // Reset the timer if the player leaves the radius
            timeNearPlayer = 0f;
        }
    }

    void FixedUpdate()
    {
        // Apply movement vector to the Rigidbody
        rb.MovePosition(rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime);
    }

    private void ChooseNewDirection()
    {
        // Pick a random angle in radians
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        
        // Convert angle back to a Vector3 direction on the XZ plane
        currentDirection = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle)).normalized;
        
        // Reset timer
        directionTimer = changeDirectionInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, damageRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
    }
}
