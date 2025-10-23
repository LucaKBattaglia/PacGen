// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class GhostSpawner : MonoBehaviour
// {
//     public GameObject ghostPrefab; // Reference to the ghost prefab
//     public Transform spawnPoint;  // Location where ghosts spawn
//     public int maxGhosts = 4;     // Maximum number of ghosts allowed at once
//     public float spawnCooldown = 3f; // Time between each ghost spawn

//     private List<GameObject> activeGhosts = new List<GameObject>(); // List of active ghosts
//     private bool isSpawning = false;
//     private Transform playerTransform; // Reference to the player's Transform

//     void Start()
//     {
//         // Find the player in the scene by tag
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player != null)
//         {
//             playerTransform = player.transform;
//         }
//         else
//         {
//             Debug.LogError("Player not found! Ensure the player has the 'Player' tag.");
//         }

//         StartCoroutine(SpawnGhosts());
//     }

//     private IEnumerator SpawnGhosts()
//     {
//         while (true)
//         {
//             // Check if the number of active ghosts is less than the max allowed
//             if (activeGhosts.Count < maxGhosts && !isSpawning)
//             {
//                 isSpawning = true;

//                 // Spawn a new ghost
//                 GameObject newGhost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);

//                 // Pass the player's Transform to the ghost
//                 ReliableGhostAI ghostComponent = newGhost.GetComponent<ReliableGhostAI>();
//                 if (ghostComponent != null && playerTransform != null)
//                 {
//                     ghostComponent.SetTarget(playerTransform);
//                 }

//                 // Add the ghost to the list of active ghosts
//                 activeGhosts.Add(newGhost);

//                 // Wait for the cooldown before allowing another spawn
//                 yield return new WaitForSeconds(spawnCooldown);

//                 isSpawning = false;
//             }
//             else
//             {
//                 // Wait briefly before checking again
//                 yield return new WaitForSeconds(0.1f);
//             }
//         }
//     }
// }


