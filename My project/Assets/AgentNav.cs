using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentNav : MonoBehaviour
{
    // Reference to the Player's GameObject
    [SerializeField] private GameObject Player;

    // Reference to the NavMeshAgent
    private NavMeshAgent agent;

    // Variable to store the player's position (target destination)
    private Vector3 target;

    // Size of each grid cell
    public float cellSize = 1f;

    // Start is called before the first frame update
    void Start()
    {
        // Get the NavMeshAgent component on this object
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        // Set the target to the player's position
        target = Player.transform.position;

        // Snap agent to the center of the grid cell before updating the destination

        // Set the agent's destination to the target (player's position)
        agent.destination = target;
        SnapToCellCenter();
    }

    // Snap the agent to the center of the nearest cell
    private void SnapToCellCenter()
    {
        Vector3 position = transform.position;

        // Align the agent's position to the nearest grid cell center (X and Z axes)
        position.x = Mathf.Round(position.x / cellSize) * cellSize;
        position.z = Mathf.Round(position.z / cellSize) * cellSize;

        // Apply the snapped position to the agent
        transform.position = position;
    }
}
