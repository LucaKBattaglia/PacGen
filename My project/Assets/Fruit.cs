using UnityEngine;

public class Fruit : MonoBehaviour
{
    public int fruitValue = 50;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //ScoreManager.Instance.AddScore(fruitValue);
            Destroy(gameObject);
        }
    }
}
