using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    private float _lastFeedbackTime = -1f;
    private const float FEEDBACK_COOLDOWN = 0.1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 충돌 피드백. 0.1초 내부 쿨타임으로 진동 모터 폭주·사운드 깨짐을 막습니다(TDD §8).
    /// ⚠️ <paramref name="intensity"/>는 아직 쓰이지 않는다 — `Handheld.Vibrate()`에 세기 인자가 없어,
    /// 실제 스케일링은 Android `VibrationEffect` / iOS Core Haptics 연동과 함께 Phase 13에서 처리한다.
    /// </summary>
    public void PlayHaptic(float intensity)
    {
        if (Time.time - _lastFeedbackTime < FEEDBACK_COOLDOWN)
            return;

        _lastFeedbackTime = Time.time;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
