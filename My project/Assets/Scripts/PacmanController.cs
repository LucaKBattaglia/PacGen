using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PacmanController : MonoBehaviour
{
    public float speed = 5f; // Movement speed of Pacman

    private Vector3 moveDirection = Vector3.forward; // Current movement direction
    private Vector3 queuedDirection = Vector3.zero; // Queued direction for next turn
    private Rigidbody rb; // Rigidbody component

    private float cellSize = 1f; // Size of each maze cell
    private LayerMask wallLayer; // Layer mask for detecting walls

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        wallLayer = LayerMask.GetMask("Wall"); // Ensure walls are on this layer
        SnapToCellCenter(); // Start Pacman centered in the grid
    }

    private void Update()
    {
        // Capture input and queue the movement direction
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            QueueDirection(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            QueueDirection(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            QueueDirection(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            QueueDirection(Vector3.right);
    }

    private void FixedUpdate()
    {
        // Try to apply the queued direction if possible
        if (queuedDirection != Vector3.zero && CanTurn(queuedDirection))
        {
            moveDirection = queuedDirection;
            queuedDirection = Vector3.zero;
        }

        // Move in the current direction if possible
        if (CanMoveInDirection(moveDirection))
        {
            rb.velocity = moveDirection * speed;
            AlignToCenterLine(); // Keep aligned to grid
        }
        else
        {
            rb.velocity = Vector3.zero; // Stop if blocked
        }
    }

    private void QueueDirection(Vector3 direction)
    {
        // Queue a new direction
        queuedDirection = direction;
    }

    private bool CanMoveInDirection(Vector3 direction)
    {
        // Check if there is a wall in the given direction
        Vector3 rayOrigin = SnapToCell(transform.position);
        Ray ray = new Ray(rayOrigin, direction);
        float rayDistance = cellSize * 0.5f; // Half the cell size

        return !Physics.Raycast(ray, rayDistance, wallLayer);
    }

    private bool CanTurn(Vector3 direction)
    {
        // Check if Pacman can turn into the queued direction
        Vector3 rayOrigin = SnapToCell(transform.position);
        Ray ray = new Ray(rayOrigin, direction);
        float rayDistance = cellSize * 0.75f;

        return !Physics.Raycast(ray, rayDistance, wallLayer);
    }

    private void AlignToCenterLine()
{
    // Align Pacman to the nearest centerline after it has moved
    if (Mathf.Abs(rb.velocity.magnitude) > 0.01f) // Ensure Pacman is moving
    {
        Vector3 position = transform.position;
        if (moveDirection == Vector3.forward || moveDirection == Vector3.back)
            position.x = Mathf.Round(position.x / cellSize) * cellSize;
        else if (moveDirection == Vector3.left || moveDirection == Vector3.right)
            position.z = Mathf.Round(position.z / cellSize) * cellSize;

        transform.position = position;
    }
}


    private void SnapToCellCenter()
    {
        // Snap Pacman to the center of the nearest cell
        transform.position = SnapToCell(transform.position);
    }

    private Vector3 SnapToCell(Vector3 position)
    {
        // Calculate the nearest cell center
        position.x = (position.x / cellSize) * cellSize;
        position.z = (position.z / cellSize) * cellSize;
        return position;
    }
}
