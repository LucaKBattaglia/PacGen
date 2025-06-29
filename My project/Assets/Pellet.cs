using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int scoreValue = 10;

    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        //ScoreManager.Instance.AddScore(scoreValue);
        Debug.Log("pellet hit");
        Destroy(gameObject);
    }
}

}
