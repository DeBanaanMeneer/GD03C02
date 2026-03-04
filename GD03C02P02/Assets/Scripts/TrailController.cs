using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrailController : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private float pointSpacing = 0.5f;
    [SerializeField] private float trailLifetime = 5f; // How long a point lasts before fading
    
    // An event you can subscribe to when a loop is successfully closed (e.g., from an Area/Damage manager)
    public System.Action<List<Vector3>> OnLoopClosed;
    
    private LineRenderer lineRenderer;
    private PlayerMovement playerMovement;

    public bool IsActive => playerMovement != null && playerMovement.IsTrailActive;

    public List<Vector3> GetPoints()
    {
        List<Vector3> pts = new List<Vector3>();
        foreach (var p in trailPoints) pts.Add(p.Position);
        return pts;
    }
    
    // Struct to hold a point and when it was created
    private struct TrailPoint
    {
        public Vector3 Position;
        public float CreatedTime;

        public TrailPoint(Vector3 position, float createdTime)
        {
            Position = position;
            CreatedTime = createdTime;
        }
    }

    private List<TrailPoint> trailPoints = new List<TrailPoint>();

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        
        playerMovement = GetComponent<PlayerMovement>();
        
        // Some basic LineRenderer setup (can also be done in inspector)
        lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        bool isActive = playerMovement != null && playerMovement.IsTrailActive;

        if (isActive)
        {
            AddPointIfMovedFarEnough();
            RemoveOldPoints();
            
            // Check for intersections between the newest segment and older segments
            if (CheckForLoopIntersection(out List<Vector3> loopPoints))
            {
                // A loop was closed! Notify listeners.
                OnLoopClosed?.Invoke(loopPoints);

                // Clear the trail points so the player has to start a new trail.
                trailPoints.Clear();
            }
        }
        else
        {
            // If trail is not active, clear it (or you could let it fade normally, but typically releasing the key cancels)
            if (trailPoints.Count > 0)
            {
                trailPoints.Clear();
            }
        }

        UpdateLineRenderer();
    }

    private void AddPointIfMovedFarEnough()
    {
        Vector3 currentPos = transform.position;

        if (trailPoints.Count == 0)
        {
            trailPoints.Add(new TrailPoint(currentPos, Time.time));
            return;
        }

        Vector3 lastPoint = trailPoints[trailPoints.Count - 1].Position;
        float sqrDistance = (currentPos - lastPoint).sqrMagnitude;

        // If we moved further than the spacing squared, add a new point
        if (sqrDistance >= pointSpacing * pointSpacing)
        {
            trailPoints.Add(new TrailPoint(currentPos, Time.time));
        }
    }

    private void RemoveOldPoints()
    {
        if (trailPoints.Count == 0) return;

        float currentTime = Time.time;
        int removeCount = 0;

        // Count how many points have expired
        for (int i = 0; i < trailPoints.Count; i++)
        {
            if (currentTime - trailPoints[i].CreatedTime > trailLifetime)
            {
                removeCount++;
            }
            else
            {
                // Since points are added sequentially in time, we can stop checking
                break;
            }
        }

        if (removeCount > 0)
        {
            trailPoints.RemoveRange(0, removeCount);
        }
    }

    private void UpdateLineRenderer()
    {
        if (trailPoints.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // Add the current player position to visually connect the player to the trail
        int totalPoints = trailPoints.Count + 1;
        lineRenderer.positionCount = totalPoints;

        for (int i = 0; i < trailPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, trailPoints[i].Position);
        }

        // The exact current position is the head of the trail
        lineRenderer.SetPosition(trailPoints.Count, transform.position);
    }

    /// <summary>
    /// Checks if the most recent line segment (from the last point to the player's current position)
    /// intersects with any older segments in the trail.
    /// </summary>
    private bool CheckForLoopIntersection(out List<Vector3> outLoopPoints)
    {
        outLoopPoints = null;

        // We need at least 3 points to form a segment to check against (the current moving head + at least 2 older fixed points)
        if (trailPoints.Count < 3) return false;

        Vector3 currentPos = transform.position;
        Vector3 headStartPos = trailPoints[trailPoints.Count - 1].Position;

        // Note: we don't check the very last segment in the list, because it connects to headStartPos.
        // If we did, they would share a vertex and mathematically "intersect" at that vertex.
        for (int i = 0; i < trailPoints.Count - 2; i++)
        {
            Vector3 a1 = trailPoints[i].Position;
            Vector3 a2 = trailPoints[i + 1].Position;

            if (LineSegmentsIntersect(headStartPos, currentPos, a1, a2, out Vector3 intersectionPoint))
            {
                // We found an intersection! 
                // Build the list of points that make up the closed polygon.
                outLoopPoints = new List<Vector3>();

                // Add the exact point of intersection
                outLoopPoints.Add(intersectionPoint);

                // Add all the points that are "trapped" in the loop
                for (int j = i + 1; j < trailPoints.Count; j++)
                {
                    outLoopPoints.Add(trailPoints[j].Position);
                }

                // Close the loop back to the intersection point
                outLoopPoints.Add(intersectionPoint);

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if two 3D line segments (A and B) intersect on the XZ plane.
    /// Uses a standard 2D line intersection formula, ignoring the Y axis.
    /// </summary>
    private bool LineSegmentsIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;

        // Convert to 2D (X and Z)
        Vector2 p = new Vector2(a1.x, a1.z);
        Vector2 r = new Vector2(a2.x - a1.x, a2.z - a1.z);
        Vector2 q = new Vector2(b1.x, b1.z);
        Vector2 s = new Vector2(b2.x - b1.x, b2.z - b1.z);

        float rCrossS = r.x * s.y - r.y * s.x;
        Vector2 qMinusP = q - p;
        float qMinusPCrossR = qMinusP.x * r.y - qMinusP.y * r.x;

        // If rCrossS is zero, lines are collinear or parallel. We'll ignore those exact cases for simplicity.
        if (Mathf.Abs(rCrossS) < 0.0001f)
            return false;

        float t = (qMinusP.x * s.y - qMinusP.y * s.x) / rCrossS;
        float u = qMinusPCrossR / rCrossS;

        // If t and u are between 0 and 1, they intersect linearly inside both segments
        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersectionPoint = new Vector3(p.x + t * r.x, a1.y, p.y + t * r.y);
            return true;
        }

        return false;
    }
}
