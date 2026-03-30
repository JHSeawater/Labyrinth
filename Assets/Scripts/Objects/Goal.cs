using UnityEngine;

public enum ColorType { Default, Red, Blue, Green, Yellow }

public class Goal : MonoBehaviour
{
    [SerializeField] private ColorType _goalColorType = ColorType.Default;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerBall>(out var ball))
        {
            // 색상이 서로 일치할 때만 골인 판정 (오답일 경우 물리 벽으로 동작하여 튕겨냄)
            if (ball.ColorType == _goalColorType)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Play)
                {
                    // 1. 이벤트 우선 전달
                    GameManager.Instance.OnBallReachedGoal();
                    
                    // 2. 이후 비활성화 (물리 충돌이 시각적으로 나타나기 전 파기)
                    collision.gameObject.SetActive(false);
                }
            }
        }
    }
}
