using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameUI : MonoBehaviour
{
    public CombatManager combatManager;
    
    private UIDocument _uiDocument;
    private Label _healthLabel;
    private Label _combatLogLabel;

    private int _lastHealth = -1;

    private void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        FindLabels();
    }

    private void FindLabels()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
        
        var root = _uiDocument.rootVisualElement;

        // Query for our defined UI elements by their exact names
        _healthLabel = root.Q<Label>("HealthLabel");
        _combatLogLabel = root.Q<Label>("CombatLogLabel");

        if (_combatLogLabel == null || _healthLabel == null)
        {
            Debug.LogWarning("[GameUI] Failed to find labels. Printing all available labels in UI:");
            var allLabels = root.Query<Label>().ToList();
            if (allLabels.Count == 0)
            {
                Debug.LogWarning(" -> There are 0 Labels in this UI Document! Did you add them in UI Builder, save it, and assign the UXML to the UIDocument 'Source Asset' slot?");
            }
            foreach (var lbl in allLabels)
            {
                Debug.Log($" -> Found Label with name: '{lbl.name}'");
            }
        }
    }

    private void Update()
    {
        if (combatManager == null) return;

        if (_healthLabel == null) 
        {
            FindLabels(); // Try to find it again just in case it loaded late
            if (_healthLabel == null) return;
        }

        // Update Health UI if it changed
        if (_lastHealth != combatManager.playerHealth)
        {
            _lastHealth = combatManager.playerHealth;
            _healthLabel.text = $"Player Health: {_lastHealth}";
        }
    }

    // A simple method to be called from CombatManager to show results
    public void ShowCombatResult(int enemiesKilled, int selfDamage, int corners)
    {
        if (_combatLogLabel == null) FindLabels();

        if (_combatLogLabel != null)
        {
            _combatLogLabel.text = $"Drawn Corners: {corners}\nEnemies Killed: {enemiesKilled}\nSelf Damage: {selfDamage}";
            
            // Clear message after 3 seconds
            CancelInvoke(nameof(ClearCombatLog));
            Invoke(nameof(ClearCombatLog), 3f);
        }
        else
        {
            Debug.LogWarning("[GameUI] Tried to show combat log, but the 'CombatLogLabel' doesn't exist to print it!");
        }
    }

    // A simpler method for printing custom warnings like "Shape too small"
    public void ShowCustomMessage(string message)
    {
        if (_combatLogLabel == null) FindLabels();

        if (_combatLogLabel != null)
        {
            _combatLogLabel.text = message;
            
            CancelInvoke(nameof(ClearCombatLog));
            Invoke(nameof(ClearCombatLog), 3f);
        }
    }

    private void ClearCombatLog()
    {
        if (_combatLogLabel != null)
        {
            _combatLogLabel.text = "";
        }
    }
}
