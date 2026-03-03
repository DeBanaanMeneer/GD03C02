using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    public CombatManager combatManager;
    
    [Header("UI Elements")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI combatLogText;

    private int _lastHealth = -1;

    private void Update()
    {
        if (combatManager == null) return;

        // Update Health UI
        if (_lastHealth != combatManager.playerHealth)
        {
            _lastHealth = combatManager.playerHealth;
            if (healthText != null)
            {
                healthText.text = $"Player Health: {_lastHealth}";
            }
        }
    }

    // A simple method to be called from CombatManager to show results
    public void ShowCombatResult(int enemiesKilled, int selfDamage, int corners)
    {
        if (combatLogText != null)
        {
            combatLogText.text = $"Drawn Corners: {corners}\nEnemies Killed: {enemiesKilled}\nSelf Damage: {selfDamage}";
            
            // Clear message after 3 seconds
            CancelInvoke(nameof(ClearCombatLog));
            Invoke(nameof(ClearCombatLog), 3f);
        }
    }

    private void ClearCombatLog()
    {
        if (combatLogText != null)
            combatLogText.text = "";
    }
}
