using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Tetherable : MonoBehaviour
{
    [SerializeField] private float hitstunDuration = 2f;
    private Rigidbody rb;
    public bool isTethered { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        isTethered = false;
    }

    // Called by the TetherManager when this enemy is successfully tagged
    public void SetTethered(bool state)
    {
        isTethered = state;
        
        // Optional visual feedback when tagged
        if (state)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red; // Temporary visual feedback
            }
        }
        else
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.white; // Reset visual feedback
            }
        }
    }

    // Called by the TetherManager when retracting
    public void PullTowards(Transform target, float forceMultiplier)
    {
        StartCoroutine(PullRoutine(target, forceMultiplier));
    }

    // Called exclusively on the middle enemy to stun it without pulling it
    public void StartHitstun()
    {
        StartCoroutine(HitstunRoutine());
    }

    private IEnumerator HitstunRoutine()
    {
        var wander = GetComponent<EnemyWander>();
        if (wander != null) wander.enabled = false;
        
        rb.linearVelocity = Vector3.zero;

        // Grouping finished! Hitstun for configured duration before wandering again
        yield return new WaitForSeconds(hitstunDuration);
        
        if (wander != null)
        {
            wander.enabled = true;
        }
    }

    private IEnumerator PullRoutine(Transform target, float pullSpeed)
    {
        // 1. Disable wandering so they stop fighting the pull
        var wander = GetComponent<EnemyWander>();
        if (wander != null) wander.enabled = false;

        // 2. Kill any existing bouncy physics momentum
        rb.linearVelocity = Vector3.zero;

        // 3. Move them directly towards the target to guarantee they arrive
        while (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            
            // Stop pulling when they form a tight group (1 unit away from center)
            if (distance <= 1.0f)
            {
                break; 
            }

            // MovePosition works perfectly with rigidbodies and ensures they slide together 
            // without overshooting or getting stuck halfway
            Vector3 nextPos = Vector3.MoveTowards(transform.position, target.position, pullSpeed * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);
            
            yield return new WaitForFixedUpdate();
        }
        
        // Grouping finished! Hitstun for configured duration before wandering again
        yield return new WaitForSeconds(hitstunDuration);
        
        if (wander != null)
        {
            wander.enabled = true;
        }
    }
}
