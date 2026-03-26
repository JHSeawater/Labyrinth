using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Reference Resolution (Target)")]
    [Tooltip("기획된 세로 기준 해상도의 가로 픽셀 (예: 1080)")]
    public float targetWidth = 1080f;
    [Tooltip("기획된 세로 기준 해상도의 세로 픽셀 (예: 1920)")]
    public float targetHeight = 1920f;
    
    [Tooltip("1080x1920 화면일 때의 기본 직교 크기(Orthographic Size)")]
    public float defaultOrthoSize = 10f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        AdjustCameraViewport();
    }

    /// <summary>
    /// 기기의 가로/세로 비율에 맞춰 미로가 화면 바깥으로 잘리지 않도록 카메라 줌(Size)을 동적 조절합니다.
    /// </summary>
    private void AdjustCameraViewport()
    {
        if (_cam == null || !_cam.orthographic) return;

        // 현재 기기의 화면 비율
        float currentAspectRatio = (float)Screen.width / Screen.height;
        // 목표 화면 비율 (1080 / 1920 = 0.5625)
        float targetAspectRatio = targetWidth / targetHeight;

        // 아이폰/갤럭시 폰 등 가로 폭이 기획된 1080x1920보다 좁고 더 길쭉한 경우 (예: 19.5:9 비율)
        if (currentAspectRatio < targetAspectRatio)
        {
            // 가로가 좁아진 만큼 카메라 뷰를 뒤로 빼서(Size 확대) 좌우가 잘리지 않도록 보정
            float aspectMultiplier = targetAspectRatio / currentAspectRatio;
            _cam.orthographicSize = defaultOrthoSize * aspectMultiplier;
        }
        else
        {
            // 태블릿(아이패드 등 4:3)처럼 가로 폭이 널널한 경우 기본 사이즈 유지 (위아래 빈 공간 발생)
            _cam.orthographicSize = defaultOrthoSize;
        }

        Debug.Log($"[CameraController] Size Adjusted. Screen: {Screen.width}x{Screen.height}, Ratio: {currentAspectRatio:F2}, OrthoSize: {_cam.orthographicSize:F2}");
    }
}
