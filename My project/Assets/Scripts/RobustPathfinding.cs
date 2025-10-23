using UnityEngine;
using System.Collections.Generic;

public class RobustPathfinding : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1f;
    public LayerMask wallLayer = -1;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    // Track player's last known grid position
    private Vector2Int lastPlayerGridPos = Vector2Int.zero;
    private bool hasValidTarget = false;
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        Vector2Int startGrid = WorldToGrid(start);
        Vector2Int endGrid = WorldToGrid(end);
        
        // If we have a valid target and haven't reached it, continue with current path
        if (hasValidTarget && startGrid != lastPlayerGridPos)
        {
            // Continue moving toward the last known player position
            return FindPathToGrid(start, lastPlayerGridPos);
        }
        
        // Update target if player has moved to a new grid cell
        if (!hasValidTarget || endGrid != lastPlayerGridPos)
        {
            lastPlayerGridPos = endGrid;
            hasValidTarget = true;
            Debug.Log($"New target set: Grid position {endGrid}");
        }
        
        return FindPathToGrid(start, endGrid);
    }
    
    List<Vector3> FindPathToGrid(Vector3 start, Vector2Int targetGrid)
    {
        List<Vector3> path = new List<Vector3>();
        Vector2Int startGrid = WorldToGrid(start);
        
        // If we're already at the target, no path needed
        if (startGrid == targetGrid)
        {
            hasValidTarget = false; // Reached target, can track new position
            return path;
        }
        
        // Use BFS to find path to target grid
        path = BFS(startGrid, targetGrid);
        
        if (path.Count == 0)
        {
            Debug.LogWarning($"No path found from {startGrid} to {targetGrid}");
            // Try to find path to closest reachable cell
            path = FindPathToClosest(startGrid, targetGrid);
        }
        
        return path;
    }
    
    List<Vector3> BFS(Vector2Int start, Vector2Int target)
    {
        List<Vector3> path = new List<Vector3>();
        
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        int maxIterations = 2000;
        int iterations = 0;
        
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = queue.Dequeue();
            
            if (current == target)
            {
                // Reconstruct path
                Vector2Int node = target;
                while (node != start)
                {
                    path.Add(GridToWorld(node));
                    node = cameFrom.ContainsKey(node) ? cameFrom[node] : node;
                }
                path.Reverse();
                return path;
            }
            
            // Check all 4 directions
            Vector2Int[] directions = {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(0, 1),   // Forward
                new Vector2Int(0, -1)   // Back
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (!visited.Contains(next) && IsGridCellWalkable(next))
                {
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }
        
        return path;
    }
    
    List<Vector3> FindPathToClosest(Vector2Int start, Vector2Int target)
    {
        // Find the closest reachable cell to the target
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        Vector2Int closestCell = start;
        float closestDistance = float.MaxValue;
        
        int maxIterations = 1000;
        int iterations = 0;
        
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector2Int current = queue.Dequeue();
            
            // Check if this cell is closer to target
            float distance = Vector2Int.Distance(current, target);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCell = current;
            }
            
            // Check all 4 directions
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (!visited.Contains(next) && IsGridCellWalkable(next))
                {
                    visited.Add(next);
                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }
        }
        
        // Reconstruct path to closest cell
        List<Vector3> path = new List<Vector3>();
        Vector2Int node = closestCell;
        while (node != start)
        {
            path.Add(GridToWorld(node));
            node = cameFrom.ContainsKey(node) ? cameFrom[node] : node;
        }
        path.Reverse();
        
        return path;
    }
    
    bool IsGridCellWalkable(Vector2Int gridPos)
    {
        Vector3 worldPos = GridToWorld(gridPos);
        return IsCellWalkable(worldPos);
    }
    
    bool IsCellWalkable(Vector3 worldPos)
    {
        // Check if there's at least one open direction from this cell
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        int openDirections = 0;
        
        foreach (Vector3 dir in directions)
        {
            Ray ray = new Ray(worldPos, dir);
            if (!Physics.Raycast(ray, cellSize * 0.6f, wallLayer))
            {
                openDirections++;
            }
        }
        
        return openDirections > 0;
    }
    
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize)
        );
    }
    
    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize,
            0,
            gridPos.y * cellSize
        );
    }
    
    public void ResetTarget()
    {
        hasValidTarget = false;
        lastPlayerGridPos = Vector2Int.zero;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebug) return;
        
        // Show current ghost and player positions
        GameObject ghost = GameObject.FindGameObjectWithTag("Ghost");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (ghost != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(ghost.transform.position, Vector3.one * cellSize * 0.5f);
        }
        
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(player.transform.position, Vector3.one * cellSize * 0.5f);
        }
        
        // Show target grid position
        if (hasValidTarget)
        {
            Vector3 targetWorld = GridToWorld(lastPlayerGridPos);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(targetWorld, Vector3.one * cellSize * 0.8f);
        }
    }
}

