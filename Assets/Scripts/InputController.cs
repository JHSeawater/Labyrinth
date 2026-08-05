using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class InputController : MonoBehaviour
{
    [Header("References")]
    // Assigned in Inspector — avoids FindFirstObjectByType and prevents breakage on scene reset
    [SerializeField] private WorldRotationController _worldRotationController;
    
    private float _initialTouchAngle;
    private float _initialWorldAngle;
    private bool _isDragging = false;
    private int _currentTouchId = -1;
    private bool _isUsingTouch = false;

    // UI 판정용 레이캐스트 캐시 (최초 1회만 할당 → 드래그 시작마다 GC 발생 방지)
    private PointerEventData _uiPointerData;
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (_worldRotationController == null)
        {
            Debug.LogWarning("[InputController] WorldRotationController reference is missing! Assign it in the Inspector.", this);
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (_worldRotationController == null) return;

        // 1. 모바일 다중 터치 추적 (EnhancedTouch 기반, finger.index로 첫 번째 터치만 추적)
        if (Touch.activeTouches.Count > 0)
        {
            _isUsingTouch = true;
            for (int i = 0; i < Touch.activeTouches.Count; i++)
            {
                var touch = Touch.activeTouches[i];
                
                if (touch.phase == TouchPhase.Began)
                {
                    if (_isDragging) continue;
                    if (IsPointerOverUI(touch.screenPosition)) continue;

                    StartDrag(touch.screenPosition, touch.finger.index);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (_isDragging && _currentTouchId == touch.finger.index)
                        UpdateDrag(touch.screenPosition);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (_isDragging && _currentTouchId == touch.finger.index)
                        EndDrag();
                }
            }
        }
        // 2. 에디터용 마우스 처리
        else if (!_isUsingTouch && Mouse.current != null)
        {
            var mouse = Mouse.current;
            Vector2 mousePos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI(mousePos)) return;
                StartDrag(mousePos, -1);
            }
            else if (mouse.leftButton.isPressed)
            {
                if (_isDragging && _currentTouchId == -1) UpdateDrag(mousePos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (_isDragging && _currentTouchId == -1) EndDrag();
            }
        }
    }

    private void StartDrag(Vector2 screenPos, int fingerId)
    {
        _isDragging = true;
        _currentTouchId = fingerId;

        // Snap prevention: store both the touch angle and the maze angle at drag start
        // TargetAngle = InitialMazeAngle + (CurrentTouchAngle - InitialTouchAngle)
        _initialTouchAngle = GetAngleFromCenter(screenPos);
        _initialWorldAngle = _worldRotationController.GetCurrentAngle();
    }

    private void UpdateDrag(Vector2 screenPos)
    {
        float currentTouchAngle = GetAngleFromCenter(screenPos);
        // Apply snap-prevention formula
        float targetAngle = _initialWorldAngle + (currentTouchAngle - _initialTouchAngle);
        _worldRotationController.SetTargetAngle(targetAngle);
    }

    private void EndDrag()
    {
        _isDragging = false;
        _currentTouchId = -1;
    }

    /// <summary>
    /// 해당 스크린 좌표 아래에 UI가 있는지 직접 레이캐스트로 판정합니다. (true면 회전 조작을 무시)
    /// EventSystem.IsPointerOverGameObject()를 쓰지 않는 이유:
    ///  ① 그 값은 EventSystem.Update()가 채워둔 상태를 읽는데, EventSystem에는 실행 순서 지정이 없어
    ///     터치가 시작된 프레임에 아직 상태가 없을 수 있다(= 드래그가 그대로 새어나감).
    ///  ② InputSystemUIInputModule은 인자를 pointerId/touchId/deviceId로만 매칭하므로
    ///     EnhancedTouch의 finger.index(N번째 손가락 슬롯 번호)를 넘기면 매칭 자체가 되지 않는다.
    /// 드래그 시작 시점에만 호출되므로(프레임마다 아님) 레이캐스트 비용은 무시 가능하다.
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        if (_uiPointerData == null)
            _uiPointerData = new PointerEventData(EventSystem.current);

        _uiPointerData.position = screenPos;
        EventSystem.current.RaycastAll(_uiPointerData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }

    private float GetAngleFromCenter(Vector2 screenPos)
    {
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = screenPos - center;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Fast Retry 로직 호출 시 
    /// 에디터(마우스) 환경 버그 방지 및 모든 드래그 변수를 초기화합니다.
    /// </summary>
    public void ResetInput()
    {
        _isUsingTouch = false;
        _isDragging = false;
        _currentTouchId = -1;
        _initialTouchAngle = 0f;
        _initialWorldAngle = 0f;
    }
}

