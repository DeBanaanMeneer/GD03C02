using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHits = 3;
    private int currentHits;

    void Start()
    {
        currentHits = maxHits;
    }

    public void TakeHit()
    {
        currentHits--;

        if (currentHits <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Add any death effects or logic here (e.g., particles, sound)
        Destroy(gameObject);
    }
}
