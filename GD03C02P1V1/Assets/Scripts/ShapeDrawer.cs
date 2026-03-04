using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class ShapeDrawer : MonoBehaviour
{
    [Header("Drawing Settings")]
    public float minDistanceBetweenPoints = 5f; // Pixels
    public float rdpTolerance = 25f; // Tolerance for shape simplification (higher = fewer corners)
    public float minShapeSize = 100f; // Pixels - How wide/tall the drawing must be minimum
    
    [Header("References")]
    public CombatManager combatManager;

    private LineRenderer _lineRenderer;
    private List<Vector2> _drawnPoints = new List<Vector2>();
    private bool _isDrawing = false;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true; // Use world space for accurate perspective projection
    }

    private void Update()
    {
        // Only allow drawing if Slomo is active
        if (combatManager == null || !combatManager.IsSlomoActive)
        {
            if (_isDrawing) StopDrawing();
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Start drawing
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartDrawing(mouse.position.ReadValue());
        }
        // Continue drawing
        else if (mouse.leftButton.isPressed && _isDrawing)
        {
            ContinueDrawing(mouse.position.ReadValue());
        }
        // Stop drawing and Evaluate
        else if (mouse.leftButton.wasReleasedThisFrame && _isDrawing)
        {
            StopDrawingAndEvaluate();
        }
    }

    private void StartDrawing(Vector2 screenPosition)
    {
        _isDrawing = true;
        _drawnPoints.Clear();
        _lineRenderer.positionCount = 0;
        
        AddPoint(screenPosition);
    }

    private void ContinueDrawing(Vector2 screenPosition)
    {
        if (_drawnPoints.Count == 0) return;

        Vector2 lastPoint = _drawnPoints[_drawnPoints.Count - 1];
        if (Vector2.Distance(lastPoint, screenPosition) > minDistanceBetweenPoints)
        {
            AddPoint(screenPosition);
        }
    }

    private void AddPoint(Vector2 screenPosition)
    {
        _drawnPoints.Add(screenPosition);
        
        // To make the line perfectly align with the mouse in a 3D perspective camera:
        // We create a Ray from the mouse pointer.
        // We calculate precisely where that ray hits a flat plane strictly facing the camera.
        
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        Plane drawPlane = new Plane(Camera.main.transform.forward * -1, Camera.main.transform.position + Camera.main.transform.forward * 5f);
        
        if (drawPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            
            _lineRenderer.positionCount++;
            _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, worldPos);
        }
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        _drawnPoints.Clear();
        _lineRenderer.positionCount = 0;
    }

    private void StopDrawingAndEvaluate()
    {
        _isDrawing = false;

        if (_drawnPoints.Count < 3)
        {
            Debug.Log("Line too short to form a shape!");
            StopDrawing();
            return;
        }

        // 0. Verify the shape isn't too small
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var p in _drawnPoints)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float boundsWidth = maxX - minX;
        float boundsHeight = maxY - minY;

        if (boundsWidth < minShapeSize && boundsHeight < minShapeSize)
        {
            Debug.Log($"<color=yellow>Shape rejected! Too small. Width: {boundsWidth}, Height: {boundsHeight}</color>");
            if (combatManager != null && combatManager.gameUI != null)
            {
                combatManager.gameUI.ShowCustomMessage("Shape drawn too small!");
            }
            StopDrawing();
            return; // Don't even resolve combat
        }

        // 1. First, find the "Convex Hull" of the drawn points.
        // This acts like a rubber band stretched around the drawing.
        // It completely ignores any messy scribbling, overlapping lines, 
        // or tracing back over the same line inside the shape!
        List<Vector2> hullPoints = GetConvexHull(_drawnPoints);

        // 2. Simplify the outer hull curve using Ramer-Douglas-Peucker
        // This reduces the smooth curve of the hull down to sharp geometric corners.
        List<Vector2> simplifiedPoints = DouglasPeuckerReduction(hullPoints, rdpTolerance);

        // A closed shape (like a triangle) usually has the first and last point very close together.
        // So a Triangle drawn by a human might have 4 simplified points (Start, Corner 1, Corner 2, End(close to Start)).
        // We consider the number of corners as simplifiedPoints.Count - 1 (or just Count depending on how closed it is).

        int cornerCount = simplifiedPoints.Count;

        // If the start and end point are close, they form 1 continuous corner.
        if (Vector2.Distance(simplifiedPoints[0], simplifiedPoints[simplifiedPoints.Count - 1]) < 50f)
        {
            // It's a closed loop. The number of actual distinct corners/vertices is Count - 1.
            cornerCount -= 1; 
        }

        Debug.Log($"<color=cyan>Shape Recognized! Raw Points: {_drawnPoints.Count} | Simplified Points: {simplifiedPoints.Count} | Estimated Corners: {cornerCount}</color>");

        // 3. Send the corner count to Combat Manager
        if (combatManager != null)
        {
            combatManager.ResolveCombat(cornerCount);
        }

        // Clear line after a short delay or immediately
        StopDrawing();
    }

    #region Convex Hull (Gift Wrapping Algorithm)

    // Finds the outer outline of all points drawn.
    // This allows the player to scribble or overlap lines without penalty, 
    // because it treats the whole mess as a solid volume and only cares about the outer edge!
    private List<Vector2> GetConvexHull(List<Vector2> points)
    {
        if (points.Count < 3) return points;

        List<Vector2> hull = new List<Vector2>();

        // Find the leftmost point
        int l = 0;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].x < points[l].x)
            {
                l = i;
            }
        }

        int p = l, q;
        do
        {
            hull.Add(points[p]);
            q = (p + 1) % points.Count;

            for (int i = 0; i < points.Count; i++)
            {
                // If i is more counterclockwise than current q, then update q
                if (Orientation(points[p], points[i], points[q]) == 2)
                {
                    q = i;
                }
            }

            p = q;

        } while (p != l);

        return hull;
    }

    // 0 = Collinear, 1 = Clockwise, 2 = Counterclockwise
    private int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (Mathf.Abs(val) < 0.001f) return 0;  // Collinear
        return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
    }

    #endregion

    #region Ramer-Douglas-Peucker Algorithm
    
    // Reduces the number of points in a curve while retaining its shape
    private List<Vector2> DouglasPeuckerReduction(List<Vector2> points, float tolerance)
    {
        if (points == null || points.Count < 3)
            return points;

        int firstPoint = 0;
        int lastPoint = points.Count - 1;
        List<int> pointIndexsToKeep = new List<int> { firstPoint, lastPoint };

        while (points[firstPoint].Equals(points[lastPoint]))
        {
            lastPoint--;
        }

        DouglasPeuckerReductionRecursive(points, firstPoint, lastPoint, tolerance, ref pointIndexsToKeep);

        pointIndexsToKeep.Sort();
        List<Vector2> returnPoints = new List<Vector2>();

        foreach (int index in pointIndexsToKeep)
        {
            returnPoints.Add(points[index]);
        }

        return returnPoints;
    }

    private void DouglasPeuckerReductionRecursive(List<Vector2> points, int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndexsToKeep)
    {
        float maxDistance = 0;
        int indexFarthest = 0;

        for (int index = firstPoint; index < lastPoint; index++)
        {
            float distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexFarthest = index;
            }
        }

        if (maxDistance > tolerance && indexFarthest != 0)
        {
            pointIndexsToKeep.Add(indexFarthest);
            DouglasPeuckerReductionRecursive(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
            DouglasPeuckerReductionRecursive(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
        }
    }

    private float PerpendicularDistance(Vector2 Point1, Vector2 Point2, Vector2 Point)
    {
        float area = Mathf.Abs(.5f * (Point1.x * Point2.y + Point2.x * Point.y + Point.x * Point1.y - Point2.x * Point1.y - Point.x * Point2.y - Point1.x * Point.y));
        float bottom = Mathf.Sqrt(Mathf.Pow(Point1.x - Point2.x, 2) + Mathf.Pow(Point1.y - Point2.y, 2));
        float height = area / bottom * 2;
        return height;
    }

    #endregion
}
