using UnityEngine;
using System.Collections.Generic;

public class AgentNav : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float moveSpeed = 3f;
    public float cellSize = 1f;
    public LayerMask wallLayer = -1;
    
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
    private List<Vector3> pathToTarget = new List<Vector3>();
    private int currentPathIndex = 0;
    private Vector3 lastTargetPosition;
    
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
        
        // Set up wall layer
        if (wallLayer == -1)
        {
            wallLayer = LayerMask.GetMask("Wall");
        }
        
        // Start at cell center
        SnapToCellCenter();
    }
    
    void Update()
    {
        if (target == null) return;
        
        // Check if we're at an intersection
        CheckIntersection();
        
        // Update path if target moved significantly
        if (Vector3.Distance(target.position, lastTargetPosition) > cellSize * 2f)
        {
            CalculatePathToTarget();
            lastTargetPosition = target.position;
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
    
    void CalculatePathToTarget()
    {
        pathToTarget.Clear();
        currentPathIndex = 0;
        
        Vector3 startPos = GetCellPosition(transform.position);
        Vector3 targetPos = GetCellPosition(target.position);
        
        // Use BFS pathfinding to find path through maze
        pathToTarget = FindPath(startPos, targetPos);
        
        // If no path found, use simple fallback
        if (pathToTarget.Count == 0)
        {
            pathToTarget.Add(targetPos);
        }
    }
    
    List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        
        // Simple BFS pathfinding
        Queue<Vector3> queue = new Queue<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        HashSet<Vector3> visited = new HashSet<Vector3>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            Vector3 current = queue.Dequeue();
            
            if (Vector3.Distance(current, end) < cellSize * 0.5f)
            {
                // Reconstruct path
                Vector3 node = end;
                while (node != start)
                {
                    path.Add(node);
                    if (cameFrom.ContainsKey(node))
                        node = cameFrom[node];
                    else
                        break;
                }
                path.Reverse();
                return path;
            }
            
            // Check all 4 directions
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            
            foreach (Vector3 dir in directions)
            {
                Vector3 next = current + dir * cellSize;
                
                if (!visited.Contains(next) && IsValidCell(next))
                {
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }
        
        return path;
    }
    
    bool IsValidCell(Vector3 cellPos)
    {
        // Check if this cell position is walkable (no walls)
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        
        foreach (Vector3 dir in directions)
        {
            Ray ray = new Ray(cellPos, dir);
            if (Physics.Raycast(ray, cellSize * 0.6f, wallLayer))
            {
                return false;
            }
        }
        
        return true;
    }
    
    Vector3 GetCellPosition(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Round(worldPos.x / cellSize) * cellSize,
            worldPos.y,
            Mathf.Round(worldPos.z / cellSize) * cellSize
        );
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
        if (pathToTarget.Count == 0 || currentPathIndex >= pathToTarget.Count)
        {
            // Fallback to direct direction if no path
            return GetDirectDirection();
        }
        
        Vector3 nextTarget = pathToTarget[currentPathIndex];
        Vector3 direction = (nextTarget - transform.position).normalized;
        
        // Check if we've reached the current path point
        if (Vector3.Distance(transform.position, nextTarget) < cellSize * 0.5f)
        {
            currentPathIndex++;
            if (currentPathIndex < pathToTarget.Count)
            {
                nextTarget = pathToTarget[currentPathIndex];
                direction = (nextTarget - transform.position).normalized;
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
        
        return bestDir;
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
        
        return !Physics.Raycast(ray, rayDistance, wallLayer);
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
            // Draw path to target
            if (pathToTarget.Count > 0)
            {
                Gizmos.color = Color.blue;
                Vector3 prevPos = transform.position;
                
                for (int i = 0; i < pathToTarget.Count; i++)
                {
                    Gizmos.DrawLine(prevPos, pathToTarget[i]);
                    Gizmos.DrawWireSphere(pathToTarget[i], 0.1f);
                    prevPos = pathToTarget[i];
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
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}