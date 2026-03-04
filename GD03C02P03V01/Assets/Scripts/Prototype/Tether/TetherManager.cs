using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TetherManager : MonoBehaviour
{
    [Header("Tether Settings")]
    public float tagRange = 5f;
    public LayerMask enemyLayer;

    private List<Tetherable> tetheredEnemies = new List<Tetherable>();
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    void Update()
    {
        // Tag enemy when pressing 'E'
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryTagEnemy();
            }

            // Retract tether when pressing 'R'
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RetractTether();
            }
        }

        UpdateVisualConnections();
    }

    private void TryTagEnemy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, tagRange, enemyLayer);

        Tetherable closestUntaggedCustomer = null;
        float closestDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            Tetherable tetherable = col.GetComponent<Tetherable>();
            if (tetherable != null && !tetherable.isTethered)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestUntaggedCustomer = tetherable;
                }
            }
        }

        if (closestUntaggedCustomer != null)
        {
            closestUntaggedCustomer.SetTethered(true);
            tetheredEnemies.Add(closestUntaggedCustomer);
            Debug.Log($"Tagged {closestUntaggedCustomer.gameObject.name}! Total tethered: {tetheredEnemies.Count}");
        }
    }

    private void UpdateVisualConnections()
    {
        if (tetheredEnemies.Count < 1)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // We need points for all enemies
        lineRenderer.positionCount = tetheredEnemies.Count;

        for (int i = 0; i < tetheredEnemies.Count; i++)
        {
            // IMPORTANT: Check if the enemy still exists (wasn't destroyed)
            if (tetheredEnemies[i] != null)
            {
                lineRenderer.SetPosition(i, tetheredEnemies[i].transform.position);
            }
        }
    }

    // Draw the tag range sphere in the editor for easy debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tagRange);
    }

    private void RetractTether()
    {
        if (tetheredEnemies.Count < 1) return;

        // 1. Find the middle enemy in the list
        int middleIndex = tetheredEnemies.Count / 2;
        Tetherable middleEnemy = tetheredEnemies[middleIndex];

        if (middleEnemy != null)
        {
            // First, stun the middle enemy so it waits for everyone else
            middleEnemy.StartHitstun();

            // 2. Apply physics pull to all valid tethered enemies EXCEPT the middle one
            float pullForce = 50f; // Increased force for continuous pulling
            
            foreach (var enemy in tetheredEnemies)
            {
                if (enemy != null)
                {
                    if (enemy != middleEnemy)
                    {
                        // Tell the enemy to pull towards the middle enemy's transform
                        enemy.PullTowards(middleEnemy.transform, pullForce);
                    }
                    enemy.SetTethered(false); // Reset their visual state
                }
            }
        }

        // 3. Clear the tether list and reset visuals
        tetheredEnemies.Clear();
        UpdateVisualConnections(); // This will zero out the LineRenderer
        Debug.Log("Tether Retracted!");
    }
}
