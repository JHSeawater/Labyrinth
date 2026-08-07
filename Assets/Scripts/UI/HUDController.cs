using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD. `GameManager.OnStateChanged`를 구독해 일시정지 패널을 토글하고,
/// Play 중에는 경과 시간을 표시합니다.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI _timerText;

    [Header("Pause")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _retryButton;

    // 소수 첫째 자리까지만 표시하므로 1초에 10번만 갱신하면 된다.
    // (매 프레임 SetText하면 값이 그대로여도 TMP 메시가 재생성된다)
    private int _lastShownTenths = -1;

    private void Awake()
    {
        // 버튼 콜백은 코드로 연결한다. 인스펙터 persistent listener는 배선이 빠져도
        // 콘솔 에러 없이 조용히 통과하는데, 이 프로젝트가 반복해서 당한 실패 유형이다.
        if (_pauseButton != null) _pauseButton.onClick.AddListener(OnPausePressed);
        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumePressed);
        if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryPressed);
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (_pauseButton != null) _pauseButton.onClick.RemoveListener(OnPausePressed);
        if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumePressed);
        if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryPressed);
    }

    private void Start()
    {
        // 이벤트는 구독 이후의 "변화"만 알려주므로, 초기 표시는 현재 값을 직접 읽어 맞춘다.
        HandleStateChanged(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Play);
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || _timerText == null) return;
        if (gm.CurrentState != GameState.Play) return;

        int tenths = Mathf.FloorToInt(gm.PlayTimer * 10f);
        if (tenths == _lastShownTenths) return;
        _lastShownTenths = tenths;

        // `_timerText.text = value.ToString("F1")` 대신 SetText(format, arg)를 쓰는 이유:
        // 전자는 매번 string을 새로 만들지만, 후자는 TMP 내부 char 버퍼에 직접 쓴다.
        // (CLAUDE.md §4 — Update 내 string 조합 금지)
        // ⚠️ 실제 할당량은 미측정. 에디터에서는 GC 실측 수단이 없어 확인 불가 —
        //    Phase 14 프로파일링에서 실기기 Profiler로 확인할 것.
        _timerText.SetText("{0:1}", tenths * 0.1f);
    }

    private void HandleStateChanged(GameState state)
    {
        if (_pausePanel != null) _pausePanel.SetActive(state == GameState.Pause);
        if (_pauseButton != null) _pauseButton.gameObject.SetActive(state == GameState.Play);

        // 재시작으로 타이머가 0으로 돌아가면 캐시를 무효화해 즉시 다시 그린다.
        if (state == GameState.Play) _lastShownTenths = -1;
    }

    private void OnPausePressed()
    {
        if (GameManager.Instance != null) GameManager.Instance.Pause();
    }

    private void OnResumePressed()
    {
        if (GameManager.Instance != null) GameManager.Instance.Resume();
    }

    private void OnRetryPressed()
    {
        if (GameManager.Instance != null) GameManager.Instance.FastRetry();
    }
}
