using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(40f, 40f);
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnInterval = 3f;

    private int currentEnemyCount = 0;
    private float nextSpawnTime;

    private void Update()
    {
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            0.5f, // Slightly above floor
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
        );

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemyObj.name = $"Enemy_{currentEnemyCount}";
        
        // Subscribe to death to manage count (Skipped for prototype)
        // We will just let them spawn indefinitely up to maxEnemies for now
        
        currentEnemyCount++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
    }
}
