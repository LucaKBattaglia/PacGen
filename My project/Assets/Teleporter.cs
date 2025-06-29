using System.Collections;
using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    public Transform targetLocation;
    private bool canTeleport = true;

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Ghost")) && canTeleport)
        {
            StartCoroutine(Teleport(other));
        }
    }

    private IEnumerator Teleport(Collider entity)
    {
        canTeleport = false;
        entity.transform.position = targetLocation.position;

        yield return new WaitForSeconds(1f);

        canTeleport = true;
    }
}
