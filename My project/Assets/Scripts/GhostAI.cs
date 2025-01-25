using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GhostAI : MonoBehaviour
{
    public delegate void GhostDestroyedHandler(GameObject ghost);
    public event GhostDestroyedHandler OnGhostDestroyed;

    private Transform target; // Reference to the player's Transform
    public float moveSpeed = 2f; // Speed of the ghost
    public float cellSize = 1f; // Size of each maze cell
    public LayerMask wallLayer; // Layer mask for detecting walls

    private Vector3 moveDirection = Vector3.zero;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SnapToCellCenter();

        if (target != null)
        {
            StartCoroutine(UpdateTargetDirection());
        }
    }

    private void FixedUpdate()
    {
        if (CanMoveInDirection(moveDirection))
        {
            rb.velocity = moveDirection * moveSpeed;
            AlignToCenterLine();
        }
        else
        {
            rb.velocity = Vector3.zero;
        }
    }

    public void SetTarget(Transform playerTransform)
    {
        target = playerTransform;
    }

    private IEnumerator UpdateTargetDirection()
    {
        while (true)
        {
            if (target != null)
            {
                Vector3 targetDirection = GetChaseDirection();

                if (CanMoveInDirection(targetDirection))
                {
                    moveDirection = targetDirection;
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private Vector3 GetChaseDirection()
    {
        Vector3 directionToPlayer = target.position - transform.position;

        Vector3[] possibleDirections = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        Vector3 bestDirection = Vector3.zero;
        float shortestDistance = float.MaxValue;

        foreach (var direction in possibleDirections)
        {
            if (CanMoveInDirection(direction))
            {
                Vector3 potentialPosition = transform.position + direction * cellSize;
                float distance = Vector3.Distance(potentialPosition, target.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestDirection = direction;
                }
            }
        }

        return bestDirection;
    }

    private bool CanMoveInDirection(Vector3 direction)
    {
        Vector3 rayOrigin = SnapToCell(transform.position);
        Ray ray = new Ray(rayOrigin, direction);
        float rayDistance = cellSize * 0.5f;

        return !Physics.Raycast(ray, rayDistance, wallLayer);
    }

    private void AlignToCenterLine()
    {
        if (Mathf.Abs(rb.velocity.magnitude) > 0.01f)
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
        transform.position = SnapToCell(transform.position);
    }

    private Vector3 SnapToCell(Vector3 position)
    {
        position.x = Mathf.Round(position.x / cellSize) * cellSize;
        position.z = Mathf.Round(position.z / cellSize) * cellSize;
        return position;
    }

    private void OnDestroy()
    {
        OnGhostDestroyed?.Invoke(gameObject);
    }
}