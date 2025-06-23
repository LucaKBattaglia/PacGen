using UnityEngine;
using UnityEngine.AI;

public class AgentNav : MonoBehaviour
{
    // Reference to the Player's GameObject
    [SerializeField] private GameObject Player;

    // Reference to the NavMeshAgent
    private NavMeshAgent agent;

    // Size of each grid cell (must match maze and player settings)
    [SerializeField] private float cellSize = 1f;

    // Fixed movement speed
    [SerializeField] private float moveSpeed = 4f;

    // Start is called before the first frame update
    private void Start()
    {
        // Get the NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name);
            return;
        }

        // Configure NavMeshAgent for consistent speed and sharp turns
        agent.speed = moveSpeed;
        agent.acceleration = 1000f; // High acceleration for instant speed changes
        agent.angularSpeed = 360f; // High angular speed for instant turns
        agent.stoppingDistance = 0f; // Stop exactly at waypoints
        agent.autoBraking = true; // Brake immediately
        agent.radius = 0.4f; // Slightly smaller than cell to avoid wall collisions
        agent.height = 1f; // Adjust based on your ghost model
        //agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQuality; // Better wall avoidance
    }

    // Update is called once per frame
    private void Update()
    {
        if (agent == null || Player == null)
            return;

        // Set the agent's destination to the player's position
        Vector3 target = Player.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 1f, NavMesh.AllAreas))
        {
            agent.SetDestination(target);
        }
        // Align to grid centerline to mimic Pacman movement
        AlignToGridCenterLine();
    }

    // Align the agent to the nearest centerline of the maze
    private void AlignToGridCenterLine()
    {
        if (agent.velocity == null || agent.velocity.magnitude <= 0.01f)
            return; // Skip if not moving

        Vector3 position = agent.transform.position;
        // Use agent's velocity to determine primary movement direction
        Vector3 velocity = agent.velocity.normalized;

        // Calculate new position
        Vector3 newPosition = position;
        // Determine primary movement direction (X or Y axis)
        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.z))
        {
            // Moving primarily along X-axis, align Z to grid
            newPosition.z = Mathf.Round(position.z / cellSize) * cellSize;
        }
        else
        {
            // Moving primarily along Z-axis, align X to grid
            newPosition.x = Mathf.Round(position.x / cellSize) * cellSize;
        }

        // Validate position to prevent NaN or invalid values
        if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z) ||
            float.IsInfinity(newPosition.x) || float.IsInfinity(newPosition.y) || float.IsInfinity(newPosition.z))
        {
            Debug.LogWarning("Invalid position calculated in AlignToCenterLine: " + newPosition);
            return;
        }

        // Only warp if the position has changed significantly to avoid jitter
        if (Vector3.Distance(position, newPosition) > 0.01f)
        {
            // Use Warp to reposition, but ensure it's on the NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(newPosition, out navHit, 0.1f, NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
            else
            {
                Debug.LogWarning("Could not warp to position, not on NavMesh: " + newPosition);
            }
        }
    }
}