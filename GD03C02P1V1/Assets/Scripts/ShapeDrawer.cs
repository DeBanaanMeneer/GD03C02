using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class ShapeDrawer : MonoBehaviour
{
    [Header("Drawing Settings")]
    public float minShapeSize = 100f; // Pixels - How wide/tall the drawing must be minimum
    public float autoCloseDistance = 50f; // If the final point is within 50px of the start point, don't count it as an extra corner
    public float minDistanceBetweenPoints = 5f; // Pixels for freehand trace
    
    [Header("Protractor Settings")]
    public float cornerAngleThreshold = 45f; // Degrees - How sharp a turn must be to automatically drop a corner
    public float minDistanceBeforeCornerCheck = 25f; // Pixels - Wait until they've drawn a bit of a straight line before measuring turns
    
    [Header("Sniper Mode (Sensitivity)")]
    [Range(0.1f, 1f)]
    public float drawSensitivity = 0.3f; // 1.0 is normal speed, 0.3 is 30% speed
    
    // We need to track a "virtual" mouse position because we can't easily force the OS hardware cursor to slow down in Unity without locking it.
    private Vector2 _virtualMousePosition;
    
    [Header("References")]
    public CombatManager combatManager;
    public UnityEngine.UI.Image customCursorImage;

    private LineRenderer _lineRenderer;
    private List<Vector2> _drawnPoints = new List<Vector2>(); // Freehand visual points
    private List<Vector2> _geometricAnchors = new List<Vector2>(); // Invisible sharp corners
    
    private bool _isDrawing = false;
    private bool _isDrawingModeActive = false;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true; // Use world space for accurate perspective projection
    }

    private void Update()
    {
        // Handle Slomo State Transitions for Virtual Cursor
        bool shouldBeDrawingMode = combatManager != null && combatManager.IsSlomoActive;

        if (shouldBeDrawingMode && !_isDrawingModeActive)
        {
            EnterDrawingMode();
        }
        else if (!shouldBeDrawingMode && _isDrawingModeActive)
        {
            // The moment Slomo ends, we Stop Drawing and Evaluate whatever we have!
            StopDrawingAndEvaluate();
            ExitDrawingMode();
            return;
        }

        if (!shouldBeDrawingMode) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Read raw mouse movement, apply our sensitivity multiplier
        Vector2 mouseDelta = mouse.delta.ReadValue() * drawSensitivity;
        
        // Move our virtual cursor
        _virtualMousePosition += mouseDelta;
        
        // Clamp it to the screen bounds
        _virtualMousePosition.x = Mathf.Clamp(_virtualMousePosition.x, 0, Screen.width);
        _virtualMousePosition.y = Mathf.Clamp(_virtualMousePosition.y, 0, Screen.height);

        if (customCursorImage != null)
        {
            customCursorImage.transform.position = _virtualMousePosition;
        }

        // Handle organic freehand drawing
        if (mouse.leftButton.wasPressedThisFrame)
        {
            StartFreehandDrawing(_virtualMousePosition);
        }
        else if (mouse.leftButton.isPressed && _isDrawing)
        {
            ContinueFreehandDrawing(_virtualMousePosition);
        }
    }

    private void StartFreehandDrawing(Vector2 screenPosition)
    {
        _isDrawing = true;
        _drawnPoints.Clear();
        _geometricAnchors.Clear();
        _lineRenderer.positionCount = 0;
        
        // Add the very first visual point and geometric anchor
        AddVisualPoint(screenPosition);
        AddGeometricAnchor(screenPosition);
    }

    private void ContinueFreehandDrawing(Vector2 screenPosition)
    {
        if (_drawnPoints.Count == 0 || _geometricAnchors.Count == 0) return;

        Vector2 lastVisualPoint = _drawnPoints[_drawnPoints.Count - 1];
        Vector2 lastAnchor = _geometricAnchors[_geometricAnchors.Count - 1];

        // 1. Draw Visual Freehand Line
        if (Vector2.Distance(lastVisualPoint, screenPosition) > minDistanceBetweenPoints)
        {
            AddVisualPoint(screenPosition);
            
            // 2. Protractor Math: Check if they just turned a sharp corner
            CheckForGeometricCorner(screenPosition, lastAnchor);
        }
    }

    private void CheckForGeometricCorner(Vector2 currentScreenPos, Vector2 lastAnchor)
    {
        // Don't calculate angles until they've drawn a long enough line from the last anchor
        // This prevents jittery mouse movements from dropping 50 corners instantly
        float distanceFromAnchor = Vector2.Distance(lastAnchor, currentScreenPos);
        if (distanceFromAnchor < minDistanceBeforeCornerCheck) return;

        // We need 3 points to check an angle: 
        // A) The last locked anchor
        // B) A point halfway between the anchor and the current cursor (the direction they *were* going)
        // C) The current cursor (the direction they *are* going)
        
        // Let's use the actual visual history to find their recent trajectory
        int recentPointIndex = _drawnPoints.Count - 5; // Look back 5 points for a smoother vector
        if (recentPointIndex <= 0) return;
        
        Vector2 recentTrajectoryPoint = _drawnPoints[recentPointIndex];
        
        // Vector from Last Anchor -> Recent Point = Their established line direction
        Vector2 establishedDirection = (recentTrajectoryPoint - lastAnchor).normalized;
        
        // Vector from Recent Point -> Current Cursor = Their brand new turn direction
        Vector2 currentDirection = (currentScreenPos - recentTrajectoryPoint).normalized;

        // Calculate the angle between where they were going and where they are going now
        float angleChange = Vector2.Angle(establishedDirection, currentDirection);

        if (angleChange >= cornerAngleThreshold)
        {
            // DEBOUNCE: Don't drop a new anchor if we just dropped one very close by (prevents corner spam)
            if (Vector2.Distance(lastAnchor, recentTrajectoryPoint) < minDistanceBeforeCornerCheck) return;

            // They made a sharp turn! Drop an anchor exactly at the "pivot" point (recentTrajectoryPoint)
            AddGeometricAnchor(recentTrajectoryPoint);
        }
    }

    private void AddVisualPoint(Vector2 screenPosition)
    {
        _drawnPoints.Add(screenPosition);
        
        Vector3 screenPosWithDepth = new Vector3(screenPosition.x, screenPosition.y, 5f); 
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosWithDepth);
        
        _lineRenderer.positionCount++;
        _lineRenderer.SetPosition(_lineRenderer.positionCount - 1, worldPos);
    }

    private void AddGeometricAnchor(Vector2 screenPosition)
    {
        _geometricAnchors.Add(screenPosition);
        
        // Optional: Play a nice "click" sound or spawn a particle effect here 
        // so the player feels the "Protractor Effect" locking in!
        Debug.Log($"<color=white>Anchor Dropped #{_geometricAnchors.Count}</color>");
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        _drawnPoints.Clear();
        _geometricAnchors.Clear();
        _lineRenderer.positionCount = 0;
    }

    private void EnterDrawingMode()
    {
        _isDrawingModeActive = true;
        
        // Lock the hardware cursor so we can rely entirely on mouse Deltas and build our own virtual cursor
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
        
        // Start the virtual cursor at the center of the screen
        _virtualMousePosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        if (customCursorImage != null) 
        {
            customCursorImage.gameObject.SetActive(true);
            customCursorImage.transform.position = _virtualMousePosition;
        }
    }

    private void ExitDrawingMode()
    {
        _isDrawingModeActive = false;
        
        if (customCursorImage != null) customCursorImage.gameObject.SetActive(false);
    }

    private void StopDrawingAndEvaluate()
    {
        _isDrawing = false;

        // Ensure they drew at least a few points freehand
        if (_drawnPoints.Count < 3)
        {
            Debug.Log("Shape too short!");
            StopDrawing();
            if (combatManager != null) combatManager.ForceDeactivateSlomo();
            return;
        }

        // 0. Manual-Close Check
        // The player must deliberately bring their cursor back to where they started.
        Vector2 startPoint = _drawnPoints[0];
        Vector2 endPoint = _drawnPoints[_drawnPoints.Count - 1];
        
        if (Vector2.Distance(endPoint, startPoint) > autoCloseDistance)
        {
            Debug.Log($"<color=orange>Shape rejected! Not closed manually. Distance: {Vector2.Distance(endPoint, startPoint)}</color>");
            if (combatManager != null)
            {
                if (combatManager.gameUI != null)
                {
                    combatManager.gameUI.ShowCustomMessage("Shape must be closed!");
                }
                combatManager.ForceDeactivateSlomo();
            }
            StopDrawing();
            return;
        }

        // 1. Verify the shape isn't too small
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
            if (combatManager != null)
            {
                if (combatManager.gameUI != null)
                {
                    combatManager.gameUI.ShowCustomMessage("Shape drawn too small!");
                }
                combatManager.ForceDeactivateSlomo();
            }
            StopDrawing();
            return; 
        }

        // 2. Count distinct corners geometrically!
        // The visible squiggly line might have 150 points.
        // We completely ignore it and ONLY count how many sharp corners our Protractor math found.
        
        // Always add the final close point as the last anchor to complete the shape correctly
        AddGeometricAnchor(endPoint);

        // Calculate corners smartly. We don't want to blindly count the Start point as a corner
        // because the player might have started drawing in the middle of a straight edge!
        int cornerCount = 0;
        
        if (_geometricAnchors.Count >= 3)
        {
            // The middle anchors are guaranteed to be sharp turns
            int middleTurns = _geometricAnchors.Count - 2;
            cornerCount += middleTurns;
            
            // Check the seam where the shape closes (Start/End points overlap)
            Vector2 startPos = _geometricAnchors[0];
            Vector2 firstTurn = _geometricAnchors[1];
            Vector2 lastTurn = _geometricAnchors[_geometricAnchors.Count - 2];
            
            Vector2 incomingDir = (startPos - lastTurn).normalized;
            Vector2 outgoingDir = (firstTurn - startPos).normalized;
            
            float seamAngle = Vector2.Angle(incomingDir, outgoingDir);
            
            if (seamAngle >= cornerAngleThreshold)
            {
                // The seam itself forms a sharp corner!
                cornerCount += 1;
            }
        }

        // Sanity Check: A closed shape must have at least 1 corner (a circle), or 3 (a triangle).
        // If they drew a perfect circle without tripping the AngleThreshold, they might have 0 corners.
        // A standard shape should have at least 3. Let's enforce that for combat purposes unless you want 1-corner magic circles.
        if (cornerCount < 3) 
        {
            // If they drew a circle, it didn't trigger sharp corners. Let's count it as a 1-corner attack (or generic spell).
            // This allows them to draw curved loops to damage 1 enemy quickly.
            cornerCount = 1; 
        }

        Debug.Log($"<color=cyan>HYBRID SHAPE RECOGNIZED! Raw Lines: {_drawnPoints.Count} | Sharp Geometric Corners: {cornerCount}</color>");

        // 3. Send the corner count to Combat Manager
        if (combatManager != null)
        {
            combatManager.ResolveCombat(cornerCount);
        }

        StopDrawing();
    }
}
