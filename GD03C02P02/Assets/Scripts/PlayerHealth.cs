using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private CameraShake cameraShake;

    public System.Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)

    private void Awake()
    {
        currentHealth = maxHealth;
        if (cameraShake == null) cameraShake = Camera.main.GetComponent<CameraShake>();
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Current HP: {currentHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (cameraShake != null)
        {
            cameraShake.Shake(0.15f, 0.2f);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player DIED! Game Over.");
        // We could trigger a game over screen or restart the level here.
        // For now, let's just slow down time to signify a loss.
        Time.timeScale = 0.5f; 
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}
