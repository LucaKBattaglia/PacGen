// using UnityEngine;
// using UnityEngine.UI;

// public class ScoreManager : MonoBehaviour
// {
//     public static ScoreManager Instance;
//     public int Score { get; private set; }
//     public Text scoreText;

//     private void Awake()
//     {
//         if (Instance == null)
//             Instance = this;
//         else
//             Destroy(gameObject);

//         ResetScore();
//     }

//     public void AddScore(int points)
//     {
//         Score += points;
//         scoreText.text = "Score: " + Score;
//     }

//     public void ResetScore()
//     {
//         Score = 0;
//         if (scoreText != null)
//             scoreText.text = "Score: 0";
//     }
// }
