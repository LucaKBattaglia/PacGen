using UnityEngine;

public class SimpleGhostAI : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float moveSpeed = 3f;
    public float cellSize = 1f;
    public LayerMask wallLayer = -1;
    
    [Header("Target Settings")]
    public Transform target; // Player transform
    public float updateInterval = 0.3f; // How often to update direction
    
    private Vector3 currentDirection = Vector3.zero;
    private Vector3 nextDirection = Vector3.zero;
    private bool isAtIntersection = false;
    private float lastUpdateTime = 0f;
    
    // Movement state
    private bool isMoving = false;
    private Vector3 targetPosition;
    
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
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
        
        if (isAtIntersection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }
}

