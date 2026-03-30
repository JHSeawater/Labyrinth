using System.Collections;
using UnityEngine;

public enum GameState { Play, Pause, GameOver, Clear }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private WorldRotationController _worldRotationController;
    [SerializeField] private InputController _inputController;
    [SerializeField] private StageData _currentStageData;

    private PlayerBall[] _activeBalls;
    private int _totalBallsCount = 0;
    private int _reachedBallsCount = 0;
    private float _playTimer = 0f;

    public GameState CurrentState { get; private set; } = GameState.Play;
    
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
            CurrentState = GameState.Clear;
            
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
        CurrentState = GameState.GameOver;

        FastRetry();
    }

    private IEnumerator ClearDelayRoutine()
    {
        int earnedStars = CalculateStars();
        Debug.LogWarning($"[Stage Clear] Time: {_playTimer:F1}s | Stars: {earnedStars}개 획득!", this);
        
        yield return _clearDelayCache;
        // 추후 로비로 넘어가는 등 분기 추가를 위한 자리
        FastRetry();
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
        CurrentState = GameState.Play;
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
            _inputController.ResetInput();
            
        if (_worldRotationController != null)
            _worldRotationController.FastReset();
    }
}
