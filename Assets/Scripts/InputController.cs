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
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.finger.index))
                        continue;

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
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1)) return;
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

    private float GetAngleFromCenter(Vector2 screenPos)
    {
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = screenPos - center;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}
