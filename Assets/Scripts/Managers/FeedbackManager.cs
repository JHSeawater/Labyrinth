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

    public void PlayHaptic(float intensity)
    {
        if (Time.time - _lastFeedbackTime < FEEDBACK_COOLDOWN)
            return;

        _lastFeedbackTime = Time.time;

        Debug.LogWarning($"[FeedbackManager] PlayHaptic: intensity={intensity}", this);
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
