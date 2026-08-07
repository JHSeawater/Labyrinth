using System;
using System.Collections;
using UnityEngine;

public enum GameState { Play, Pause, GameOver, Clear }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 상태가 실제로 바뀐 순간에만 1회 발행됩니다. (HUD·결과 팝업 등 UI가 구독)
    /// static인 이유: 구독자(UI)의 OnEnable이 GameManager.Awake보다 먼저 돌더라도
    /// 구독이 조용히 실패하지 않게 하기 위함. 실행 순서에 의존하지 않는다.
    /// 구독자는 ① OnEnable에서 구독 + Start에서 CurrentState를 1회 직접 읽어 초기 표시를 맞추고,
    ///          ② OnDisable/OnDestroy에서 반드시 해제(-=)한다. (CLAUDE.md §4)
    /// </summary>
    public static event Action<GameState> OnStateChanged;

    /// <summary>
    /// 클리어 연출 대기가 끝나고 결과를 표시할 시점에 발행됩니다. (획득 별 수, 클리어 타임)
    /// 패배(GameOver)는 GDD §2에 따라 팝업 없이 즉시 재시작하므로 이 이벤트를 쓰지 않는다.
    /// </summary>
    public static event Action<int, float> OnStageCleared;

    [SerializeField] private WorldRotationController _worldRotationController;
    [SerializeField] private InputController _inputController;
    [SerializeField] private StageData _currentStageData;

    private PlayerBall[] _activeBalls;
    private int _totalBallsCount = 0;
    private int _reachedBallsCount = 0;
    private float _playTimer = 0f;

    public GameState CurrentState { get; private set; } = GameState.Play;

    /// <summary>현재 스테이지 경과 시간(초). HUD 표시용.</summary>
    public float PlayTimer => _playTimer;
    
    private WaitForSeconds _clearDelayCache;
    private const float CLEAR_DELAY_TIME = 1.5f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _activeBalls = FindObjectsByType<PlayerBall>(FindObjectsSortMode.None);
        _totalBallsCount = _activeBalls != null ? _activeBalls.Length : 0;

        _clearDelayCache = new WaitForSeconds(CLEAR_DELAY_TIME);
    }

    private void OnDestroy()
    {
        // 싱글턴 중복으로 파기되는 쪽은 전역 상태를 건드리면 안 된다.
        if (Instance != this) return;

        Instance = null;
        // 현재 프로젝트는 도메인 리로드가 켜져 있어 static이 자동 초기화되지만,
        // Fast Enter Play Mode를 켜는 순간 구독자가 세션을 넘어 살아남는다.
        OnStateChanged = null;
        OnStageCleared = null;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 값이 실제로 달라졌을 때만 상태를 바꾸고 이벤트를 1회 발행합니다.
    /// </summary>
    private void SetState(GameState next)
    {
        if (CurrentState == next) return;

        CurrentState = next;
        OnStateChanged?.Invoke(next);
    }

    private void Update()
    {
        if (CurrentState == GameState.Play)
        {
            _playTimer += Time.deltaTime;
        }
    }

    public void OnBallReachedGoal()
    {
        if (CurrentState != GameState.Play) return;
        
        _reachedBallsCount++;

        // 모든 공이 골에 도달해야만 승리(Clear) 판정
        if (_reachedBallsCount >= _totalBallsCount)
        {
            SetState(GameState.Clear);

            if (_inputController != null)
            {
                _inputController.ResetInput();
            }

            StartCoroutine(ClearDelayRoutine());
        }
    }

    public void GameOver()
    {
        if (CurrentState != GameState.Play) return;
        SetState(GameState.GameOver);

        // GDD §2: 패배는 결과 화면 없이 "즉시 Fast Retry(같은 자리에서 재도전)".
        // 로딩도 팝업도 없는 즉시 반복이 코어 루프의 의도이므로 이 호출은 유지한다.
        // (결과 화면은 승리 전용 — ClearDelayRoutine 참조)
        FastRetry();
    }

    /// <summary>
    /// 일시정지. Play 상태에서만 진입하며, GameOver/Clear 중 호출은 무시됩니다.
    /// timeScale로 FixedUpdate를 멈춰 물리와 중력 회전을 동시에 동결한다.
    /// </summary>
    public void Pause()
    {
        if (CurrentState != GameState.Play) return;

        Time.timeScale = 0f;
        // Update()는 timeScale 0에서도 계속 돌기 때문에, 잠그지 않으면
        // 일시정지 중 드래그가 목표 각도에 누적되어 재개 순간 미로가 튄다.
        if (_inputController != null) _inputController.SetInputEnabled(false);

        SetState(GameState.Pause);
    }

    /// <summary>
    /// 일시정지 해제. Pause 상태에서만 동작합니다.
    /// </summary>
    public void Resume()
    {
        if (CurrentState != GameState.Pause) return;

        Time.timeScale = 1f;
        if (_inputController != null) _inputController.SetInputEnabled(true);

        SetState(GameState.Play);
    }

    private IEnumerator ClearDelayRoutine()
    {
        // 상태가 Clear라 Update()가 타이머를 더 올리지 않지만, 의도를 분명히 하려고 여기서 확정한다.
        int earnedStars = CalculateStars();
        float clearTime = _playTimer;

        // 마지막 공이 골에 들어가는 장면을 보여준 뒤 결과를 띄운다.
        yield return _clearDelayCache;

        if (OnStageCleared != null)
        {
            OnStageCleared.Invoke(earnedStars, clearTime);
        }
        else
        {
            // 결과 팝업이 배선되지 않으면 재시작 수단이 없어 소프트락이 된다.
            // 조용히 멈추는 대신 경고를 남기고 자동 복구한다.
            Debug.LogWarning("[GameManager] OnStageCleared 구독자가 없습니다. 결과 팝업 배선을 확인하세요. 자동 재시작합니다.", this);
            FastRetry();
        }
    }

    private int CalculateStars()
    {
        if (_currentStageData == null) return 1;

        if (_playTimer <= _currentStageData.TimeLimitFor3Stars) return 3;
        if (_playTimer <= _currentStageData.TimeLimitFor2Stars) return 2;
        return 1;
    }

    public void FastRetry()
    {
        StopAllCoroutines();

        // 일시정지 상태에서 재시작해도 시간이 멈춘 채 남지 않도록 먼저 되돌린다.
        Time.timeScale = 1f;

        SetState(GameState.Play);
        _reachedBallsCount = 0;
        _playTimer = 0f;

        // 모든 공을 강제 초기화 및 재활성화
        if (_activeBalls != null)
        {
            foreach (var ball in _activeBalls)
            {
                if (ball != null) ball.FastReset();
            }
        }

        if (_inputController != null)
        {
            _inputController.SetInputEnabled(true);
            _inputController.ResetInput();
        }

        if (_worldRotationController != null)
            _worldRotationController.FastReset();
    }
}
