using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathPlanner : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f;
    public LayerMask wallLayer = -1;
    
    [Header("Debug")]
    public bool showDebugGrid = false;
    public bool showConnections = false;
    
    // Navigation grid
    private Dictionary<Vector2Int, NavNode> navigationGrid = new Dictionary<Vector2Int, NavNode>();
    private Vector2Int gridSize;
    
    public class NavNode
    {
        public Vector2Int gridPosition;
        public Vector3 worldPosition;
        public bool isWalkable;
        public List<NavNode> neighbors = new List<NavNode>();
        
        public NavNode(Vector2Int gridPos, Vector3 worldPos, bool walkable)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;
            isWalkable = walkable;
        }
    }
    
    void Start()
    {
        if (wallLayer == -1)
        {
            wallLayer = LayerMask.GetMask("Wall");
        }
    }
    
    public void GenerateNavigationGrid(int mazeWidth, int mazeDepth)
    {
        navigationGrid.Clear();
        gridSize = new Vector2Int(mazeWidth, mazeDepth);
        
        Debug.Log($"Generating navigation grid for maze {mazeWidth}x{mazeDepth}");
        
        // Create all nodes
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeDepth; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(x * cellSize, 0, z * cellSize);
                bool isWalkable = IsCellWalkable(worldPos);
                
                NavNode node = new NavNode(gridPos, worldPos, isWalkable);
                navigationGrid[gridPos] = node;
            }
        }
        
        // Connect walkable nodes
        ConnectNeighbors();
        
        Debug.Log($"Navigation grid generated with {navigationGrid.Count} nodes");
    }
    
    bool IsCellWalkable(Vector3 worldPos)
    {
        // A cell is walkable if it's not completely surrounded by walls
        // Check if there's at least one open direction
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
        
        // Cell is walkable if it has at least one open direction
        return openDirections > 0;
    }
    
    void ConnectNeighbors()
    {
        foreach (var kvp in navigationGrid)
        {
            NavNode node = kvp.Value;
            if (!node.isWalkable) continue;
            
            // Check 4 directions
            Vector2Int[] directions = {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0), // Left
                new Vector2Int(0, 1),  // Forward
                new Vector2Int(0, -1)  // Back
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = node.gridPosition + dir;
                
                if (navigationGrid.ContainsKey(neighborPos))
                {
                    NavNode neighbor = navigationGrid[neighborPos];
                    if (neighbor.isWalkable)
                    {
                        // Check if there's a direct path (no wall between them)
                        if (IsDirectPath(node.worldPosition, neighbor.worldPosition))
                        {
                            node.neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }
    }
    
    bool IsDirectPath(Vector3 from, Vector3 to)
    {
        // Check if two adjacent cells are connected (no wall between them)
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);
        
        // Only check for walls if the distance is exactly one cell
        if (Mathf.Abs(distance - cellSize) < 0.1f)
        {
            Ray ray = new Ray(from, direction);
            return !Physics.Raycast(ray, distance * 0.8f, wallLayer);
        }
        
        return false; // Not adjacent cells
    }
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        Vector2Int startGrid = WorldToGrid(start);
        Vector2Int endGrid = WorldToGrid(end);
        
        if (!navigationGrid.ContainsKey(startGrid) || !navigationGrid.ContainsKey(endGrid))
        {
            Debug.LogWarning("Start or end position not in navigation grid");
            return new List<Vector3>();
        }
        
        NavNode startNode = navigationGrid[startGrid];
        NavNode endNode = navigationGrid[endGrid];
        
        if (!startNode.isWalkable || !endNode.isWalkable)
        {
            Debug.LogWarning("Start or end position is not walkable");
            return new List<Vector3>();
        }
        
        // Try to find path to exact target
        List<Vector3> path = AStarPathfinding(startNode, endNode);
        
        // If no path found, try to find path to closest reachable node
        if (path.Count == 0)
        {
            Debug.Log("No direct path found, searching for closest reachable node");
            NavNode closestNode = FindClosestReachableNode(startNode, endNode);
            if (closestNode != null)
            {
                path = AStarPathfinding(startNode, closestNode);
                Debug.Log($"Found path to closest node at {closestNode.worldPosition}");
            }
        }
        
        return path;
    }
    
    NavNode FindClosestReachableNode(NavNode start, NavNode target)
    {
        // Use BFS to find all reachable nodes from start
        Queue<NavNode> queue = new Queue<NavNode>();
        HashSet<NavNode> visited = new HashSet<NavNode>();
        Dictionary<NavNode, NavNode> cameFrom = new Dictionary<NavNode, NavNode>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        NavNode closestNode = null;
        float closestDistance = float.MaxValue;
        
        int maxIterations = 5000; // Prevent infinite loops
        int iterations = 0;
        
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            NavNode current = queue.Dequeue();
            
            // Check if this node is closer to target
            float distance = Vector3.Distance(current.worldPosition, target.worldPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = current;
            }
            
            foreach (NavNode neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return closestNode;
    }
    
    List<Vector3> AStarPathfinding(NavNode start, NavNode goal)
    {
        List<Vector3> path = new List<Vector3>();
        
        // Try A* first
        path = TryAStar(start, goal);
        if (path.Count > 0)
        {
            return path;
        }
        
        // If A* fails, use BFS as fallback
        Debug.Log("A* failed, using BFS fallback");
        path = TryBFS(start, goal);
        if (path.Count > 0)
        {
            return path;
        }
        
        // If BFS fails, use greedy approach
        Debug.Log("BFS failed, using greedy approach");
        path = TryGreedy(start, goal);
        
        return path;
    }
    
    List<Vector3> TryAStar(NavNode start, NavNode goal)
    {
        List<Vector3> path = new List<Vector3>();
        
        // A* pathfinding
        List<NavNode> openSet = new List<NavNode>();
        HashSet<NavNode> closedSet = new HashSet<NavNode>();
        Dictionary<NavNode, NavNode> cameFrom = new Dictionary<NavNode, NavNode>();
        Dictionary<NavNode, float> gScore = new Dictionary<NavNode, float>();
        Dictionary<NavNode, float> fScore = new Dictionary<NavNode, float>();
        
        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        
        int maxIterations = 1000; // Prevent infinite loops
        int iterations = 0;
        
        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            // Find node with lowest fScore
            NavNode current = openSet.OrderBy(n => fScore.ContainsKey(n) ? fScore[n] : float.MaxValue).First();
            
            if (current == goal)
            {
                // Reconstruct path
                while (current != null)
                {
                    path.Add(current.worldPosition);
                    current = cameFrom.ContainsKey(current) ? cameFrom[current] : null;
                }
                path.Reverse();
                return path;
            }
            
            openSet.Remove(current);
            closedSet.Add(current);
            
            foreach (NavNode neighbor in current.neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;
                
                float tentativeGScore = gScore[current] + Vector3.Distance(current.worldPosition, neighbor.worldPosition);
                
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore[neighbor])
                {
                    continue;
                }
                
                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
            }
        }
        
        return path; // No path found
    }
    
    List<Vector3> TryBFS(NavNode start, NavNode goal)
    {
        List<Vector3> path = new List<Vector3>();
        
        Queue<NavNode> queue = new Queue<NavNode>();
        HashSet<NavNode> visited = new HashSet<NavNode>();
        Dictionary<NavNode, NavNode> cameFrom = new Dictionary<NavNode, NavNode>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        int maxIterations = 2000; // Prevent infinite loops
        int iterations = 0;
        
        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            NavNode current = queue.Dequeue();
            
            if (current == goal)
            {
                // Reconstruct path
                NavNode node = goal;
                while (node != null)
                {
                    path.Add(node.worldPosition);
                    node = cameFrom.ContainsKey(node) ? cameFrom[node] : null;
                }
                path.Reverse();
                return path;
            }
            
            foreach (NavNode neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return path;
    }
    
    List<Vector3> TryGreedy(NavNode start, NavNode goal)
    {
        List<Vector3> path = new List<Vector3>();
        NavNode current = start;
        HashSet<NavNode> visited = new HashSet<NavNode>();
        
        path.Add(current.worldPosition);
        visited.Add(current);
        
        int maxSteps = 100; // Prevent infinite loops
        int steps = 0;
        
        while (current != goal && steps < maxSteps)
        {
            steps++;
            
            // Find the neighbor closest to goal
            NavNode bestNeighbor = null;
            float bestDistance = float.MaxValue;
            
            foreach (NavNode neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    float distance = Vector3.Distance(neighbor.worldPosition, goal.worldPosition);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestNeighbor = neighbor;
                    }
                }
            }
            
            if (bestNeighbor == null)
            {
                // No unvisited neighbors, try any neighbor
                foreach (NavNode neighbor in current.neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        bestNeighbor = neighbor;
                        break;
                    }
                }
            }
            
            if (bestNeighbor == null)
            {
                // No neighbors available, path failed
                break;
            }
            
            current = bestNeighbor;
            path.Add(current.worldPosition);
            visited.Add(current);
        }
        
        return path;
    }
    
    float Heuristic(NavNode a, NavNode b)
    {
        return Vector3.Distance(a.worldPosition, b.worldPosition);
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
        return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
    }
    
    public NavNode GetNodeAt(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        return navigationGrid.ContainsKey(gridPos) ? navigationGrid[gridPos] : null;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGrid) return;
        
        foreach (var kvp in navigationGrid)
        {
            NavNode node = kvp.Value;
            
            if (node.isWalkable)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * cellSize * 0.8f);
                
                if (showConnections)
                {
                    Gizmos.color = Color.blue;
                    foreach (NavNode neighbor in node.neighbors)
                    {
                        Gizmos.DrawLine(node.worldPosition, neighbor.worldPosition);
                    }
                }
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(node.worldPosition, Vector3.one * cellSize * 0.8f);
            }
        }
        
        // Debug: Show current ghost and player positions
        if (Application.isPlaying)
        {
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
        }
    }
}
