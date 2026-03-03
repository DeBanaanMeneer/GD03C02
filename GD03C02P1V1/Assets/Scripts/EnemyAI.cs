using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public float movementSpeed = 2.5f;
    public float detectionRange = 10f;
    public float stoppingDistance = 1.5f;

    [Header("State")]
    public bool isGrouped = false; // True if the player caught them in a slomo circle

    private Transform _playerTransform;
    private Rigidbody _rigidbody;

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
    }

    private void Update()
    {
        // We remove the return statement here so they keep moving
        // But since Time.timeScale is small (e.g. 0.1), Time.deltaTime will be very small,
        // which makes them naturally move in slow motion!

        if (_playerTransform == null)
            return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= detectionRange && distanceToPlayer > stoppingDistance)
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
    }

    // Optional debug visual for the detection range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
