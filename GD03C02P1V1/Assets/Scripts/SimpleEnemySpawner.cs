using UnityEngine;
using System.Collections;

public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject enemyPrefab;
    
    [Tooltip("Time in seconds between each spawn.")]
    public float spawnInterval = 3f;
    
    [Tooltip("Maximum number of enemies that can be spawned at once by this spawner. (0 = infinite)")]
    public int maxEnemies = 5;
    
    [Tooltip("Radius around the spawner where enemies will be spawned.")]
    public float spawnRadius = 2f;

    [Tooltip("How far away other colliders need to be before an enemy spawns here. (e.g. 1 unit)")]
    public float collisionCheckRadius = 1f;

    [Tooltip("How many times the spawner should try finding a safe spot before giving up for this cycle?")]
    public int maxSpawnAttempts = 10;

    [Tooltip("Layers to check for collisions (e.g. Default, Enemies, Player). Ensure 'Floor' or 'Ground' layer is UNCHECKED here so the sphere doesn't detect it!")]
    public LayerMask collisionMask = ~0; // Default to Everything

    [Header("Optional: Target Reference")]
    [Tooltip("Optional reference to the player to pass to the spawned enemy. If empty, the enemy's AI should find it.")]
    public Transform playerTransform;

    private int _currentEnemyCount = 0;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"[SimpleEnemySpawner] No Enemy Prefab assigned on {gameObject.name}!");
            return;
        }

        // Try to automatically find the player if not set
        if (playerTransform == null)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Check if we haven't reached the limit
            if (maxEnemies == 0 || _currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = Vector3.zero;
        bool foundSafePosition = false;

        // 1. Try multiple times to find a position that isn't overlapping anything
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 randomCircleDir = Random.insideUnitCircle * spawnRadius;
            spawnPos = transform.position + new Vector3(randomCircleDir.x, 0f, randomCircleDir.y);

            // Check if there are any colliders at this position using the collisionMask
            // We use CheckSphere to see if *anything* we care about is inside this space.
            if (!Physics.CheckSphere(spawnPos, collisionCheckRadius, collisionMask))
            {
                foundSafePosition = true;
                break; // Found a good spot!
            }
        }

        // 2. If we couldn't find a safe spot after max attempts, abort this spawn cycle.
        if (!foundSafePosition)
        {
            Debug.Log($"[SimpleEnemySpawner] Failed to find a safe spawn position after {maxSpawnAttempts} attempts. Trying again next cycle.");
            return;
        }

        // 3. Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        _currentEnemyCount++;

        // 4. Set up the EnemyAI if it exists
        EnemyAI ai = newEnemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            // If the AI script needs a target, you can optionally assign it here depending on its implementation.
            // ai.target = playerTransform; 
        }

        // 5. Listen for when this specific enemy is destroyed so we can decrement the count
        EnemyDeathTracker tracker = newEnemy.AddComponent<EnemyDeathTracker>();
        tracker.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath()
    {
        _currentEnemyCount--;
        // Ensure count doesn't go below 0
        _currentEnemyCount = Mathf.Max(0, _currentEnemyCount); 
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a visual indicator for where the enemies will spawn in the editor
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}

/// <summary>
/// A tiny helper class to notify the spawner when the enemy GameObject is destroyed.
/// We use this because `Destroy(gameObject)` is called by the Combat Manager.
/// </summary>
public class EnemyDeathTracker : MonoBehaviour
{
    public event System.Action OnDeath;

    private void OnDestroy()
    {
        // When the Combat Manager destroys this GameObject, this will fire.
        OnDeath?.Invoke();
    }
}
