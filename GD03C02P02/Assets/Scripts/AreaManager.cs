using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AreaManager : MonoBehaviour
{
    [SerializeField] private TrailController trailController;
    [SerializeField] private Material fillMaterial;
    [SerializeField] private float displayDuration = 1.0f;
    [SerializeField] private float yOffset = 0.05f; // Keep it slightly above ground
    
    [Header("Damage Settings")]
    [SerializeField] private float damagePerAreaUnit = 5f;
    [SerializeField] private float minDamage = 10f;

    private MeshFilter meshFilter;
    private Mesh mesh;
    private float clearTimer;

    private void Awake()
    {
        // Since we are using world space coordinates for vertices,
        // this object must be at (0,0,0) with no rotation.
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;

        if (trailController == null)
            trailController = FindFirstObjectByType<TrailController>();

        if (trailController != null)
            trailController.OnLoopClosed += HandleLoopClosed;
    }

    private void OnDestroy()
    {
        if (trailController != null)
            trailController.OnLoopClosed -= HandleLoopClosed;
    }

    private void Update()
    {
        if (clearTimer > 0)
        {
            clearTimer -= Time.deltaTime;
            if (clearTimer <= 0)
            {
                mesh.Clear();
            }
        }
    }

    private void HandleLoopClosed(List<Vector3> points)
    {
        if (points.Count < 3) return;

        // Apply Y offset to all points so it's above the floor
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Vector3(points[i].x, points[i].y + yOffset, points[i].z);
        }

        float area = CalculateArea(points);
        Debug.Log($"Loop Closed! Area: {area}");

        GenerateMesh(points);
        clearTimer = displayDuration;

        // Apply damage to enemies inside the loop
        ApplyDamageToEnclosedEnemies(points, area);
    }

    private void ApplyDamageToEnclosedEnemies(List<Vector3> polygonPoints, float area)
    {
        float damageToDeal = Mathf.Max(minDamage, area * damagePerAreaUnit);
        
        // Convert polygon points to 2D for the check
        List<Vector2> poly2D = new List<Vector2>();
        foreach (var p in polygonPoints) poly2D.Add(new Vector2(p.x, p.z));

        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int enemiesHit = 0;

        foreach (Enemy enemy in allEnemies)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector2 enemyPos2D = new Vector2(enemyPos.x, enemyPos.z);

            if (IsPointInPolygon(enemyPos2D, poly2D))
            {
                enemy.TakeDamage(damageToDeal);
                enemiesHit++;
            }
        }

        if (enemiesHit > 0)
        {
            Debug.Log($"Dealt {damageToDeal:F1} damage to {enemiesHit} enclosed enemies!");
        }
    }

    private bool IsPointInPolygon(Vector2 p, List<Vector2> poly)
    {
        // Ray casting algorithm (Jordan curve theorem)
        bool inside = false;
        int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private float CalculateArea(List<Vector3> points)
    {
        // Shoelace formula for area calculation on XZ plane
        float area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[(i + 1) % points.Count];
            area += (p1.x * p2.z) - (p2.x * p1.z);
        }
        return Mathf.Abs(area * 0.5f);
    }

    private void GenerateMesh(List<Vector3> points)
    {
        mesh.Clear();

        // Convert 3D points to 2D for triangulation (XZ plane)
        List<Vector2> points2D = new List<Vector2>();
        foreach (var p in points)
        {
            points2D.Add(new Vector2(p.x, p.z));
        }

        // Triangulate using Ear Clipping
        int[] indices = TriangulateEarClipping(points2D);

        if (indices == null || indices.Length == 0) return;

        // Create the mesh
        Vector3[] vertices = points.ToArray();
        
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private int[] TriangulateEarClipping(List<Vector2> points)
    {
        // Basic ear-clipping triangulation (handles simple non-convex polygons)
        List<int> indices = new List<int>();
        List<int> remainingIndices = new List<int>();
        for (int i = 0; i < points.Count - 1; i++) remainingIndices.Add(i); // Ignore the last point if it's a duplicate of the first

        // Handle case where first and last points are the same
        if (Vector2.Distance(points[0], points[points.Count - 1]) < 0.01f)
        {
            // Already handled by loop limit above
        }
        else
        {
            remainingIndices.Add(points.Count - 1);
        }

        // Ensure CCW orientation
        if (!IsClockwise(points, remainingIndices))
        {
            remainingIndices.Reverse();
        }

        int iterations = 0;
        while (remainingIndices.Count >= 3 && iterations < 1000)
        {
            iterations++;
            bool earFound = false;

            for (int i = 0; i < remainingIndices.Count; i++)
            {
                int prev = remainingIndices[(i + remainingIndices.Count - 1) % remainingIndices.Count];
                int curr = remainingIndices[i];
                int next = remainingIndices[(i + 1) % remainingIndices.Count];

                if (IsEar(prev, curr, next, remainingIndices, points))
                {
                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);
                    remainingIndices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound) break;
        }

        return indices.ToArray();
    }

    private bool IsEar(int pIdx, int cIdx, int nIdx, List<int> remaining, List<Vector2> points)
    {
        Vector2 a = points[pIdx];
        Vector2 b = points[cIdx];
        Vector2 c = points[nIdx];

        // Must be convex
        if (CrossProduct(a, b, c) <= 0) return false;

        // No other points inside triangle
        for (int i = 0; i < remaining.Count; i++)
        {
            int idx = remaining[i];
            if (idx == pIdx || idx == cIdx || idx == nIdx) continue;
            if (IsPointInTriangle(points[idx], a, b, c)) return false;
        }

        return true;
    }

    private bool IsClockwise(List<Vector2> points, List<int> indices)
    {
        float area = 0;
        for (int i = 0; i < indices.Count; i++)
        {
            Vector2 p1 = points[indices[i]];
            Vector2 p2 = points[indices[(i + 1) % indices.Count]];
            area += (p2.x - p1.x) * (p2.y + p1.y);
        }
        return area > 0;
    }

    private float CrossProduct(Vector2 a, Vector2 b, Vector2 c)
    {
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return area;
    }

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = CrossProduct(p, a, b);
        float d2 = CrossProduct(p, b, c);
        float d3 = CrossProduct(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }
}
