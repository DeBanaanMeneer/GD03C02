using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HealthUIController : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    
    private VisualElement healthBarFill;
    private UIDocument uiDocument;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        healthBarFill = root.Q<VisualElement>("HealthBarFill");

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            // Initialize with actual values
            UpdateHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
        else
        {
            Debug.LogWarning("HealthUIController: PlayerHealth not found!");
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthBarFill == null) return;

        float percentage = Mathf.Clamp01(current / max) * 100f;
        healthBarFill.style.width = new Length(percentage, LengthUnit.Percent);
        
        // Dynamic color shifting (Green to Red)
        Color barColor = Color.Lerp(new Color(1f, 0.28f, 0.34f), new Color(0.2f, 1f, 0.4f), current / max);
        healthBarFill.style.backgroundColor = barColor;
    }
}
