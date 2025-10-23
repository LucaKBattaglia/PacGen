using UnityEngine;
using System.Collections.Generic;

public class ImprovedGhostAI : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float moveSpeed = 3f;
    public float cellSize = 1f;
    
    [Header("Target Settings")]
    public Transform target; // Player transform
    public float updateInterval = 0.2f; // How often to update direction
    
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
    private RobustPathfinding pathfinder;
    private Vector3 lastTargetPosition;
    private float lastPathUpdateTime = 0f;
    private float pathUpdateInterval = 1f; // Update path every 1 second
    
    // Grid tracking
    private Vector2Int lastPlayerGridPos = Vector2Int.zero;
    private bool hasReachedTarget = false;
    
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
        pathfinder = FindObjectOfType<RobustPathfinding>();
        if (pathfinder == null)
        {
            Debug.LogError("RobustPathfinding not found! Please add a RobustPathfinding to your scene.");
        }
        
        // Start at cell center
        SnapToCellCenter();
    }
    
    void Update()
    {
        if (target == null || pathfinder == null) return;
        
        // Check if we're at an intersection
        CheckIntersection();
        
        // Check if we've reached the target grid position
        CheckIfReachedTarget();
        
        // Update path if needed
        if (ShouldUpdatePath())
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
    
    void CheckIfReachedTarget()
    {
        if (currentPath.Count > 0 && currentPathIndex >= currentPath.Count - 1)
        {
            // We've reached the end of our current path
            Vector3 currentPos = transform.position;
            Vector3 targetPos = currentPath[currentPath.Count - 1];
            
            if (Vector3.Distance(currentPos, targetPos) < cellSize * 0.5f)
            {
                hasReachedTarget = true;
                Debug.Log("Reached target grid position, can track new player position");
            }
        }
    }
    
    bool ShouldUpdatePath()
    {
        // Update path if:
        // 1. We don't have a current path
        // 2. We've reached our target and player has moved
        // 3. Enough time has passed since last update
        
        if (currentPath.Count == 0) return true;
        if (hasReachedTarget && Vector3.Distance(target.position, lastTargetPosition) > cellSize * 0.5f) return true;
        if (Time.time - lastPathUpdateTime >= pathUpdateInterval) return true;
        
        return false;
    }
    
    void UpdatePath()
    {
        if (pathfinder == null) return;
        
        currentPath = pathfinder.FindPath(transform.position, target.position);
        currentPathIndex = 0;
        hasReachedTarget = false;
        
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
                if (CanMoveInDirection(dir))
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
    
    Vector3 GetDirectDirection()
    {
        if (target == null) return Vector3.zero;
        
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Vector3 bestDir = Vector3.zero;
        float shortestDistance = float.MaxValue;
        
        foreach (Vector3 dir in directions)
        {
            if (CanMoveInDirection(dir))
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

