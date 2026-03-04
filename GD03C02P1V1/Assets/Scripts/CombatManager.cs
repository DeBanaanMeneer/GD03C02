using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Combat Settings")]
    public float groupingRadius = 5f;
    public float slowMotionTimeScale = 0.1f;
    public int playerHealth = 10;
    
    [Header("Input")]
    public UnityEngine.InputSystem.Key groupingKey = UnityEngine.InputSystem.Key.LeftShift;

    [Header("Visuals")]
    public GameObject groupingIndicatorPrefab; // E.g., a simple cylinder/ring with a transparent material
    public GameUI gameUI; // Reference to the UI manager
    private GameObject _currentIndicator;

    // State
    private bool _isSlomoActive = false;
    public bool IsSlomoActive => _isSlomoActive; // Added so ShapeDrawer can read it
    private List<EnemyAI> _groupedEnemies = new List<EnemyAI>();

    private void Start()
    {
        // Automatically find GameUI if not assigned
        if (gameUI == null)
        {
            gameUI = FindObjectOfType<GameUI>();
            if (gameUI == null)
            {
                Debug.LogWarning("[CombatManager] No GameUI found in the scene! Combat logs will not be displayed.");
            }
        }

        if (groupingIndicatorPrefab != null)
        {
            _currentIndicator = Instantiate(groupingIndicatorPrefab, transform.position, Quaternion.identity);
            _currentIndicator.SetActive(false);
            
            // Scale indicator to match radius (assuming default cylinder is 1x2x1 diameter)
            _currentIndicator.transform.localScale = new Vector3(groupingRadius * 2, 0.1f, groupingRadius * 2);
        }
    }

    private void Update()
    {
        // Toggle slomo
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current[groupingKey].wasPressedThisFrame)
        {
            if (!_isSlomoActive)
                ActivateSlomo();
            else
                DeactivateSlomo();
        }

        if (_isSlomoActive)
        {
            // Keep indicator under player
            if (_currentIndicator != null)
            {
                _currentIndicator.transform.position = transform.position + Vector3.up * 0.05f;
            }

            // In a real scenario, you might constantly update the group or only group on initial press.
            // Let's do it continuously for now while slomo is active
            UpdateGroupedEnemies();
        }
    }

    private void ActivateSlomo()
    {
        _isSlomoActive = true;
        Time.timeScale = slowMotionTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Keep physics smooth

        if (_currentIndicator != null)
            _currentIndicator.SetActive(true);

        UpdateGroupedEnemies();
        
        Debug.Log("Slomo Activated! Grouped Enemies: " + _groupedEnemies.Count);
    }

    private void DeactivateSlomo()
    {
        _isSlomoActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (_currentIndicator != null)
            _currentIndicator.SetActive(false);

        // Clear grouped state
        foreach (var enemy in _groupedEnemies)
        {
            if (enemy != null)
                enemy.isGrouped = false;
        }
        _groupedEnemies.Clear();
        
        Debug.Log("Slomo Deactivated.");
    }

    private void UpdateGroupedEnemies()
    {
        // Clear previous state safely
        foreach (var enemy in _groupedEnemies)
        {
            if (enemy != null)
                enemy.isGrouped = false;
        }
        _groupedEnemies.Clear();

        // Find enemies in radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, groupingRadius);
        foreach (var hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                enemy.isGrouped = true;
                _groupedEnemies.Add(enemy);
            }
        }
    }

    // Called by the ShapeDrawer when a shape is completed
    public void ResolveCombat(int drawnCorners)
    {
        if (!_isSlomoActive) return;

        int enemyCount = _groupedEnemies.Count;

        if (enemyCount == 0)
        {
            Debug.Log($"Drawn {drawnCorners} corners, but no enemies grouped! Wasted shape punished: took {drawnCorners} damage.");
            playerHealth -= drawnCorners;
            if (playerHealth <= 0)
            {
                Debug.Log("<color=red>PLAYER DIED!</color>");
                DeactivateSlomo();
                
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Stop the game natively
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif

                Destroy(gameObject);
                return;
            }
            
            // Update UI
            if (gameUI != null)
            {
                gameUI.ShowCombatResult(0, drawnCorners, drawnCorners);
            }

            DeactivateSlomo();
            return;
        }

        // Calculation based on requirement: 
        // Example: 3 enemies and player draws pentagon (5). Kills 3 enemies, hits themself for 2 damage.
        
        int damageToDeal = Mathf.Min(drawnCorners, enemyCount);
        int selfDamage = Mathf.Max(0, drawnCorners - enemyCount);

        Debug.Log($"<color=orange>COMBAT RESOLUTION:</color> Grouped: {enemyCount} | Corners: {drawnCorners} -> Kills: {damageToDeal}, Self-Damage: {selfDamage}");

        // Kill enemies (just destroy them for now)
        for (int i = 0; i < damageToDeal; i++)
        {
            if (i < _groupedEnemies.Count && _groupedEnemies[i] != null)
            {
                Destroy(_groupedEnemies[i].gameObject);
            }
        }

        // Apply Self Damage
        if (selfDamage > 0)
        {
            playerHealth -= selfDamage;
            Debug.Log($"<color=red>Ouch! Took {selfDamage} damage. Current Health: {playerHealth}</color>");
            if (playerHealth <= 0)
            {
                Debug.Log("<color=red>PLAYER DIED!</color>");
                DeactivateSlomo();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Stop the game natively
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                
                Destroy(gameObject);
                return;
            }
        }

        // Update UI
        if (gameUI != null)
        {
            gameUI.ShowCombatResult(damageToDeal, selfDamage, drawnCorners);
        }

        DeactivateSlomo();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, groupingRadius);
    }
}
