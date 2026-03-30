using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Play)
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Play)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}
