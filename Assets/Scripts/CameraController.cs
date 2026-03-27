using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Reference Resolution (Target)")]
    [Tooltip("기획된 세로 기준 해상도의 가로 픽셀 (예: 1080)")]
    [SerializeField] private float targetWidth = 1080f;
    [Tooltip("기획된 세로 기준 해상도의 세로 픽셀 (예: 1920)")]
    [SerializeField] private float targetHeight = 1920f;
    
    [Tooltip("1080x1920 화면일 때의 기본 직교 크기(Orthographic Size)")]
    [SerializeField] private float defaultOrthoSize = 10f;

    private Camera _cam;
    // The current world rotation angle received from WorldRotationController
    private float _worldRotationAngle;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        AdjustCameraViewport();
    }

    /// <summary>
    /// Called by WorldRotationController every FixedUpdate to supply the current angle.
    /// Camera rotation is deferred to LateUpdate to prevent Jittering.
    /// </summary>
    public void SetWorldRotation(float angle)
    {
        _worldRotationAngle = angle;
    }

    /// <summary>
    /// Apply camera rotation AFTER all physics and logic have finished this frame.
    /// Camera rotates in the OPPOSITE direction (-angle) to create the illusion
    /// that the maze is rotating while it actually stays static.
    /// </summary>
    private void LateUpdate()
    {
        // Camera rotates counter to the world angle: maze appears to rotate to the user.
        transform.rotation = Quaternion.Euler(0f, 0f, -_worldRotationAngle);
    }

    /// <summary>
    /// 기기의 가로/세로 비율에 맞춰 미로가 화면 바깥으로 잘리지 않도록 카메라 줌(Size)을 동적 조절합니다.
    /// </summary>
    private void AdjustCameraViewport()
    {
        if (_cam == null || !_cam.orthographic) return;

        float currentAspectRatio = (float)Screen.width / Screen.height;
        float targetAspectRatio = targetWidth / targetHeight;

        if (currentAspectRatio < targetAspectRatio)
        {
            float aspectMultiplier = targetAspectRatio / currentAspectRatio;
            _cam.orthographicSize = defaultOrthoSize * aspectMultiplier;
        }
        else
        {
            _cam.orthographicSize = defaultOrthoSize;
        }

        Debug.Log($"[CameraController] Size Adjusted. Screen: {Screen.width}x{Screen.height}, Ratio: {currentAspectRatio:F2}, OrthoSize: {_cam.orthographicSize:F2}");
    }
}
