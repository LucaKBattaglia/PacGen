using UnityEngine;
using System.Collections.Generic;

public class SimplePathfinding : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1f;
    public LayerMask wallLayer = -1;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        
        // Simple approach: try to move toward target while avoiding walls
        Vector3 current = start;
        Vector3 target = end;
        
        // Snap to grid
        current = SnapToGrid(current);
        target = SnapToGrid(target);
        
        path.Add(current);
        
        int maxSteps = 50;
        int steps = 0;
        
        while (Vector3.Distance(current, target) > cellSize * 0.5f && steps < maxSteps)
        {
            steps++;
            
            Vector3 direction = (target - current).normalized;
            Vector3 nextMove = GetNextMove(current, direction);
            
            if (nextMove == current)
            {
                // Can't move directly, try alternative directions
                nextMove = GetAlternativeMove(current, target);
            }
            
            if (nextMove == current)
            {
                // Completely stuck, break
                break;
            }
            
            current = nextMove;
            path.Add(current);
        }
        
        return path;
    }
    
    Vector3 GetNextMove(Vector3 current, Vector3 direction)
    {
        // Try to move in the direction of the target
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Vector3 bestMove = current;
        float bestDot = -1f;
        
        foreach (Vector3 dir in directions)
        {
            if (CanMoveInDirection(current, dir))
            {
                float dot = Vector3.Dot(dir, direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestMove = current + dir * cellSize;
                }
            }
        }
        
        return bestMove;
    }
    
    Vector3 GetAlternativeMove(Vector3 current, Vector3 target)
    {
        // Try all 4 directions and pick the one that gets us closest to target
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Vector3 bestMove = current;
        float bestDistance = float.MaxValue;
        
        foreach (Vector3 dir in directions)
        {
            if (CanMoveInDirection(current, dir))
            {
                Vector3 potentialMove = current + dir * cellSize;
                float distance = Vector3.Distance(potentialMove, target);
                
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMove = potentialMove;
                }
            }
        }
        
        return bestMove;
    }
    
    bool CanMoveInDirection(Vector3 from, Vector3 direction)
    {
        Ray ray = new Ray(from, direction);
        return !Physics.Raycast(ray, cellSize * 0.6f, wallLayer);
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / cellSize) * cellSize,
            position.y,
            Mathf.Round(position.z / cellSize) * cellSize
        );
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
    }
}

