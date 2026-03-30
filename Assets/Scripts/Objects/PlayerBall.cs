using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBall : MonoBehaviour
{
    [Tooltip("이 공의 고유 색상 속성 (Goal 색상과 매칭됨)")]
    [SerializeField] private ColorType _colorType = ColorType.Default;

    public ColorType ColorType => _colorType;

    private Rigidbody2D _rb;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        CacheInitialState();
    }

    /// <summary>
    /// 게임 재시작 시 돌아갈 초기 위치와 회전을 저장합니다.
    /// (GameManager가 아닌 각 공이 스스로의 상태를 캐싱)
    /// </summary>
    public void CacheInitialState()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    /// <summary>
    /// 물리 연산(속도)을 0으로 강제 초기화하고, 저장된 시작 상태로 즉시 복원합니다.
    /// </summary>
    public void FastReset()
    {
        // 핵심 원인: Goal 골인 시 gameObject가 비활성화(SetActive(false))된 상태입니다.
        // 비활성화된 오브젝트의 Rigidbody2D에는 position을 할당해도 변환(Transform)이 동기화되지 않고 무시됩니다.
        // 따라서 비활성 상태일 때는 Transform으로 먼저 이동시킨 뒤 활성화해야 합니다. (비활성 상태에서의 Transform 대입은 물리 부하/Jitter를 유발하지 않습니다)
        if (!gameObject.activeSelf)
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            gameObject.SetActive(true);
        }

        // 활성화(Active) 상태의 공(예: DeadZone에 빠져서 죽었을 때)은 Jitter 방지를 위해 _rb API 사용
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            
            _rb.position = _initialPosition;
            _rb.rotation = _initialRotation.eulerAngles.z;
        }
        else
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
        }
    }
}
