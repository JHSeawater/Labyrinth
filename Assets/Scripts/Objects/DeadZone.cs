using UnityEngine;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Play)
            {
                Debug.LogWarning("[DeadZone] Player out of bounds. Triggering FastRetry.", this);
                GameManager.Instance.GameOver();
            }
        }
    }
}
