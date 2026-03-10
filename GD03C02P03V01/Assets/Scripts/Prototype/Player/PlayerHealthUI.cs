using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;

    private UIDocument uiDocument;
    private VisualElement healthBarFill;
    private Label healthText;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        healthText = root.Q<Label>("health-text");

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            // Initialize UI
            UpdateHealthUI(playerHealth.maxHealth, playerHealth.maxHealth);
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            float percentage = currentHealth / maxHealth * 100f;
            healthBarFill.style.width = Length.Percent(percentage);

            // Change color based on health percentage
            if (percentage > 50f)
            {
                healthBarFill.style.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f)); // Green
            }
            else if (percentage > 25f)
            {
                healthBarFill.style.backgroundColor = new StyleColor(new Color(0.8f, 0.8f, 0.2f)); // Yellow
            }
            else
            {
                healthBarFill.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f)); // Red
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}
