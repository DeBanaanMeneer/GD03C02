using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public float movementSpeed = 2.5f;
    public float detectionRange = 10f;
    public float stoppingDistance = 1.5f;

    [Header("State")]
    public bool isGrouped = false; // True if the player caught them in a slomo circle
    
    [Header("Wander Settings")]
    public float wanderDuration = 5f;
    public float wanderSpeedMultiplier = 1.5f;

    [Header("Combat Settings")]
    public float enemyHealth = 3f;
    public float damagePerAttack = 0.5f;
    public float attackCooldown = 1f;

    private Transform _playerTransform;
    private Rigidbody _rigidbody;
    private bool _isWandering = true;
    private Vector3 _wanderDirection;
    private float _spawnTime;
    private float _lastAttackTime;
    private CombatManager _combatManager;

    private void Start()
    {
        // Add Rigidbody if missing
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Find Player
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        _combatManager = FindAnyObjectByType<CombatManager>();

        // Initialize Wander State
        _isWandering = true;
        _spawnTime = Time.time;
        
        // Pick a random direction to wander
        Vector2 randomCircleDir = Random.insideUnitCircle.normalized;
        _wanderDirection = new Vector3(randomCircleDir.x, 0f, randomCircleDir.y);
    }

    private void Update()
    {
        // We remove the return statement here so they keep moving
        // But since Time.timeScale is small (e.g. 0.1), Time.deltaTime will be very small,
        // which makes them naturally move in slow motion!

        // Calculate distance to player (we need this for wandering early-exit too)
        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (_isWandering)
        {
            // Check if wander time is up OR player got too close
            if (Time.time - _spawnTime >= wanderDuration || distanceToPlayer <= detectionRange)
            {
                _isWandering = false;
                return; // Let the next frame handle chasing
            }

            // Move in wander direction
            if (_wanderDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_wanderDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                transform.position += transform.forward * (movementSpeed * wanderSpeedMultiplier) * Time.deltaTime;
            }
            return; // Skip chasing logic while wandering
        }

        // --- Chasing Logic ---

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > stoppingDistance)
            {
                // Rotate towards player
                Vector3 direction = (_playerTransform.position - transform.position).normalized;
                direction.y = 0; // Keep horizontal
                
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                    
                    // Move towards player
                    transform.position += transform.forward * movementSpeed * Time.deltaTime;
                }
            }
            else
            {
                // Attack Player
                if (Time.time - _lastAttackTime >= attackCooldown)
                {
                    _lastAttackTime = Time.time;
                    if (_combatManager != null)
                    {
                        _combatManager.TakeDamage(damagePerAttack);
                    }
                }
            }
        }
    }

    // Optional debug visual for the detection range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    public void TakeDamage(float amount)
    {
        enemyHealth -= amount;
        if (enemyHealth <= 0)
        {
            if (_combatManager != null && _combatManager.useEnergySystem)
            {
                // Grant energy for a manual kill
                _combatManager.currentSlomoEnergy += _combatManager.slomoEnergyGainPerKill;
                if (_combatManager.currentSlomoEnergy > _combatManager.maxSlomoEnergy)
                    _combatManager.currentSlomoEnergy = _combatManager.maxSlomoEnergy;
            }
            Destroy(gameObject);
        }
    }
}
