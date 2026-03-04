using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyWander : MonoBehaviour
{
    [Header("Wander Settings")]
    public float moveSpeed = 2f;
    public float changeDirectionInterval = 3f;
    
    private Rigidbody rb;
    private Vector3 currentDirection;
    private float directionTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze rotation so the capsule doesn't tip over
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        ChooseNewDirection();
    }

    void Update()
    {
        directionTimer -= Time.deltaTime;
        if (directionTimer <= 0f)
        {
            ChooseNewDirection();
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
}
