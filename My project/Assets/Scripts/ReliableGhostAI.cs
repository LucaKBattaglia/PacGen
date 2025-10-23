using UnityEngine;
using System.Collections.Generic;

public class ReliableGhostAI : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float moveSpeed = 3f;
    public float cellSize = 1f;
    
    [Header("Target Settings")]
    public Transform target; // Player transform
    public float updateInterval = 0.2f; // How often to update direction
    
    [Header("Movement Settings")]
    public float smoothMovementThreshold = 0.8f; // How close to center before changing direction
    
    private Vector3 currentDirection = Vector3.zero;
    private Vector3 nextDirection = Vector3.zero;
    private bool isAtIntersection = false;
    private float lastUpdateTime = 0f;
    
    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    
    // Pathfinding
    private List<Vector3> currentPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private HistoryAwarePathfinding pathfinder;
    private Vector3 lastTargetPosition;
    private float lastPathUpdateTime = 0f;
    private float pathUpdateInterval = 0.5f;
    
    // Anti-loop system
    private Vector3 lastGridPosition;
    private Vector3 currentGridPosition;
    private int stuckCounter = 0;
    private Vector3 forcedDirection = Vector3.zero;
    private float forcedDirectionTime = 0f;
    
    // Simple 2-cell backtracking prevention
    private Vector3 secondLastGridPosition;
    
    void Start()
    {
        // Find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Find pathfinder
        pathfinder = FindObjectOfType<HistoryAwarePathfinding>();
        if (pathfinder == null)
        {
            Debug.LogError("HistoryAwarePathfinding not found! Please add a HistoryAwarePathfinding to your scene.");
        }
        
        // Start at cell center
        SnapToCellCenter();
    }
    
    void Update()
    {
        if (target == null || pathfinder == null) return;
        
        // Track current grid position
        currentGridPosition = SnapToGrid(transform.position);
        
        // Check if we're at an intersection
        CheckIntersection();
        
        // Check for stuck behavior (oscillating between two positions)
        CheckForStuckBehavior();
        
        // Update path if target moved significantly or enough time has passed
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval || 
            Vector3.Distance(target.position, lastTargetPosition) > cellSize * 1.5f)
        {
            UpdatePath();
            lastTargetPosition = target.position;
            lastPathUpdateTime = Time.time;
        }
        
        // Update direction at intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDirection();
            lastUpdateTime = Time.time;
        }
        
        // Handle movement
        HandleMovement();
    }
    
    void CheckIntersection()
    {
        Vector3 pos = transform.position;
        float tolerance = 0.1f;
        
        bool centeredX = Mathf.Abs(pos.x - Mathf.Round(pos.x / cellSize) * cellSize) < tolerance;
        bool centeredZ = Mathf.Abs(pos.z - Mathf.Round(pos.z / cellSize) * cellSize) < tolerance;
        
        isAtIntersection = centeredX && centeredZ;
    }
    
    void CheckForStuckBehavior()
    {
        // Check if we're oscillating between two positions
        if (Vector3.Distance(currentGridPosition, lastGridPosition) < 0.1f)
        {
            stuckCounter++;
        }
        else
        {
            stuckCounter = 0;
            lastGridPosition = currentGridPosition;
        }
        
        // If stuck for too long, force a direction
        if (stuckCounter > 10) // Adjust this value as needed
        {
            Debug.Log("Ghost is stuck, forcing direction");
            ForceDirection();
            stuckCounter = 0;
        }
    }
    
    void UpdateMovementHistory(Vector3 direction)
    {
        // Update the 2-cell tracking system
        secondLastGridPosition = lastGridPosition;
        lastGridPosition = currentGridPosition;
    }
    
    void ForceDirection()
    {
        // Force the ghost to choose a direction and stick with it
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        List<Vector3> validDirections = new List<Vector3>();
        
        foreach (Vector3 dir in directions)
        {
            if (CanMoveInDirection(dir) && !IsBacktracking(dir))
            {
                validDirections.Add(dir);
            }
        }
        
        // If no non-backtracking directions available, allow any valid direction
        if (validDirections.Count == 0)
        {
            foreach (Vector3 dir in directions)
            {
                if (CanMoveInDirection(dir))
                {
                    validDirections.Add(dir);
                }
            }
        }
        
        if (validDirections.Count > 0)
        {
            // Choose a random valid direction
            forcedDirection = validDirections[Random.Range(0, validDirections.Count)];
            forcedDirectionTime = Time.time + 2f; // Force this direction for 2 seconds
            Debug.Log($"Forced direction: {forcedDirection}");
        }
    }
    
    void UpdatePath()
    {
        if (pathfinder == null) return;
        
        // Create a simple movement history from our 2-cell tracking
        List<Vector3> simpleHistory = new List<Vector3>();
        if (lastGridPosition != Vector3.zero)
        {
            simpleHistory.Add(lastGridPosition);
        }
        if (secondLastGridPosition != Vector3.zero)
        {
            simpleHistory.Add(secondLastGridPosition);
        }
        
        currentPath = pathfinder.FindPath(transform.position, target.position, simpleHistory);
        currentPathIndex = 0;
        
        if (currentPath.Count > 0)
        {
            Debug.Log($"New path found with {currentPath.Count} waypoints");
        }
        else
        {
            Debug.Log("No path found, will use direct movement");
        }
    }
    
    void UpdateDirection()
    {
        if (!isAtIntersection) return;
        
        Vector3 bestDirection = GetBestDirection();
        
        if (bestDirection != Vector3.zero)
        {
            nextDirection = bestDirection;
        }
    }
    
    Vector3 GetBestDirection()
    {
        // If we're in forced direction mode, use that
        if (Time.time < forcedDirectionTime && forcedDirection != Vector3.zero)
        {
            if (CanMoveInDirection(forcedDirection))
            {
                return forcedDirection;
            }
            else
            {
                // Forced direction is blocked, cancel it
                forcedDirection = Vector3.zero;
                forcedDirectionTime = 0f;
            }
        }
        
        // If we have a valid path, follow it
        if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            Vector3 nextWaypoint = currentPath[currentPathIndex];
            Vector3 direction = (nextWaypoint - transform.position).normalized;
            
            // Check if we've reached the current waypoint
            if (Vector3.Distance(transform.position, nextWaypoint) < cellSize * 0.5f)
            {
                currentPathIndex++;
                if (currentPathIndex < currentPath.Count)
                {
                    nextWaypoint = currentPath[currentPathIndex];
                    direction = (nextWaypoint - transform.position).normalized;
                }
            }
            
            // Convert to grid direction
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            Vector3 bestDir = Vector3.zero;
            float bestDot = -1f;
            
            foreach (Vector3 dir in directions)
            {
                if (CanMoveInDirection(dir) && !IsRecentlyVisited(dir) && !IsBacktracking(dir))
                {
                    float dot = Vector3.Dot(dir, direction);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestDir = dir;
                    }
                }
            }
            
            if (bestDir != Vector3.zero)
            {
                return bestDir;
            }
        }
        
        // Fallback to direct direction if no path or path failed
        return GetDirectDirection();
    }
    
    bool IsRecentlyVisited(Vector3 direction)
    {
        // Check if moving in this direction would take us back to where we just were
        Vector3 nextPosition = transform.position + direction * cellSize;
        Vector3 nextGridPosition = SnapToGrid(nextPosition);
        
        return Vector3.Distance(nextGridPosition, lastGridPosition) < 0.1f;
    }
    
    bool IsBacktracking(Vector3 direction)
    {
        // Check if this direction would take us back to the last 2 grid positions
        Vector3 nextPosition = transform.position + direction * cellSize;
        Vector3 nextGridPosition = SnapToGrid(nextPosition);
        
        // Check against last 2 positions
        if (Vector3.Distance(nextGridPosition, lastGridPosition) < 0.1f)
        {
            return true;
        }
        
        if (Vector3.Distance(nextGridPosition, secondLastGridPosition) < 0.1f)
        {
            return true;
        }
        
        return false;
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / cellSize) * cellSize,
            position.y,
            Mathf.Round(position.z / cellSize) * cellSize
        );
    }
    
    Vector3 GetDirectDirection()
    {
        if (target == null) return Vector3.zero;
        
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Vector3 bestDir = Vector3.zero;
        float shortestDistance = float.MaxValue;
        
        foreach (Vector3 dir in directions)
        {
            if (CanMoveInDirection(dir) && !IsRecentlyVisited(dir) && !IsBacktracking(dir))
            {
                Vector3 nextPos = transform.position + dir * cellSize;
                float distance = Vector3.Distance(nextPos, target.position);
                
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestDir = dir;
                }
            }
        }
        
        // If all directions are recently visited or backtracking, allow any valid direction
        if (bestDir == Vector3.zero)
        {
            foreach (Vector3 dir in directions)
            {
                if (CanMoveInDirection(dir))
                {
                    return dir;
                }
            }
        }
        
        return bestDir;
    }
    
    bool CanMoveInDirection(Vector3 direction)
    {
        Vector3 rayOrigin = transform.position;
        Ray ray = new Ray(rayOrigin, direction);
        float rayDistance = cellSize * 0.6f;
        
        return !Physics.Raycast(ray, rayDistance, pathfinder.wallLayer);
    }
    
    void HandleMovement()
    {
        if (isAtIntersection && nextDirection != Vector3.zero)
        {
            if (CanMoveInDirection(nextDirection))
            {
                currentDirection = nextDirection;
                nextDirection = Vector3.zero;
                isMoving = true;
                targetPosition = transform.position + currentDirection * cellSize;
                
                // Update movement history
                UpdateMovementHistory(currentDirection);
            }
        }
        
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }
    
    void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // Check if we've reached the target position
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }
    
    void SnapToCellCenter()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / cellSize) * cellSize;
        pos.z = Mathf.Round(pos.z / cellSize) * cellSize;
        transform.position = pos;
    }
    
    public void SetTarget(Transform playerTransform)
    {
        target = playerTransform;
    }
    
    void OnDrawGizmos()
    {
        if (target != null)
        {
            // Draw current path
            if (currentPath.Count > 0)
            {
                Gizmos.color = Color.green;
                Vector3 prevPos = transform.position;
                
                for (int i = currentPathIndex; i < currentPath.Count; i++)
                {
                    Gizmos.DrawLine(prevPos, currentPath[i]);
                    Gizmos.DrawWireSphere(currentPath[i], 0.1f);
                    prevPos = currentPath[i];
                }
            }
            else
            {
                // Fallback to direct line if no path
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
        
        if (isAtIntersection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}
