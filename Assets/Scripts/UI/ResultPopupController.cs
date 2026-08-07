using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 결과 팝업. 최종 타임과 획득 별을 표시합니다.
/// 패배(GameOver)는 GDD §2에 따라 팝업 없이 즉시 재시작하므로 여기서 다루지 않습니다.
/// </summary>
public class ResultPopupController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Image[] _starImages;

    [Header("Buttons")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _lobbyButton;

    [Header("Star Colors")]
    [SerializeField] private Color _starEarnedColor = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color _starEmptyColor = new Color(1f, 1f, 1f, 0.15f);

    private void Awake()
    {
        if (_retryButton != null) _retryButton.onClick.AddListener(OnRetryPressed);

        // [다음] / [로비]는 갈 곳(로비 씬·스테이지 레지스트리)이 Phase 11에서 생긴다.
        // 그때까지 눌러도 아무 일이 없으므로 비활성 상태로 의도를 드러낸다.
        if (_nextButton != null) _nextButton.interactable = false;
        if (_lobbyButton != null) _lobbyButton.interactable = false;
    }

    private void OnEnable()
    {
        GameManager.OnStageCleared += HandleStageCleared;
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStageCleared -= HandleStageCleared;
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetryPressed);
    }

    private void Start()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void HandleStageCleared(int stars, float clearTime)
    {
        // HUD와 동일하게 0.1초 단위 버림. TMP의 {0:1} 포맷은 반올림(내부에서 0.05를 더한 뒤 자름)이라
        // 원시값을 그대로 넘기면 HUD(버림) 표시와 0.1초 어긋나 보인다.
        if (_timeText != null) _timeText.SetText("{0:1}s", Mathf.FloorToInt(clearTime * 10f) * 0.1f);

        if (_starImages != null)
        {
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].color = i < stars ? _starEarnedColor : _starEmptyColor;
            }
        }

        if (_panel != null) _panel.SetActive(true);
    }

    private void HandleStateChanged(GameState state)
    {
        // 재시작으로 Play에 돌아가면 결과 팝업을 닫는다.
        if (state == GameState.Play && _panel != null) _panel.SetActive(false);
    }

    private void OnRetryPressed()
    {
        // 상태 전이 이벤트에 기대지 않고 직접 닫는다.
        // SetState()는 값이 실제로 달라질 때만 발행하므로, 이미 Play인 상태에서
        // FastRetry()가 불리면 OnStateChanged가 오지 않아 팝업이 열린 채 남는다.
        if (_panel != null) _panel.SetActive(false);

        if (GameManager.Instance != null) GameManager.Instance.FastRetry();
    }
}
