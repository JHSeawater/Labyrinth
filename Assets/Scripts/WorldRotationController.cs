using UnityEngine;

/// <summary>
/// B-Method (Camera Illusion): Instead of physically rotating the maze,
/// this controller rotates the camera in reverse and syncs Physics2D.gravity direction.
/// The maze Transform stays fixed at (0,0,0), acting as a true Static collider.
/// </summary>
public class WorldRotationController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("미로가 목표 각도로 회전하는 속도 보간값 (클수록 빠름)")]
    [SerializeField] private float rotationSmoothness = 15f;
    [Tooltip("물리 터널링 버그 방지를 위한 초당 최대 회전각 상한선")]
    [SerializeField] private float maxAngularSpeed = 400f;
    [Tooltip("중력 크기 배율 (기본값: 1.0 = 9.81 m/s²)")]
    [SerializeField] private float gravityScale = 1.5f;

    [Header("References")]
    [SerializeField] private CameraController cameraController;

    private const float GravityMagnitude = 9.81f;

    private float _targetAngle;
    private float _currentAngle;
    // Reused gravity vector field (Vector2 is a struct; declared here to make intent explicit)
    private Vector2 _gravityVector;

    private void Awake()
    {
        if (cameraController == null)
        {
            Debug.LogWarning("[WorldRotationController] CameraController reference is missing! Assign it in the Inspector.", this);
        }

        _currentAngle = 0f;
        _targetAngle = 0f;

        // Ensure gravity starts pointing straight down
        ApplyGravity(0f);
    }

    /// <summary>
    /// Called by InputController to set the desired world rotation angle.
    /// </summary>
    public void SetTargetAngle(float newAngle)
    {
        _targetAngle = newAngle;
    }

    /// <summary>
    /// Returns the current interpolated world angle for camera sync.
    /// </summary>
    public float GetCurrentAngle()
    {
        return _currentAngle;
    }

    private void FixedUpdate()
    {
        // Smoothly interpolate toward target angle (Time.fixedDeltaTime rule)
        float nextAngle = Mathf.LerpAngle(_currentAngle, _targetAngle, rotationSmoothness * Time.fixedDeltaTime);

        // Clamp angular speed to prevent tunneling at high rotation speeds
        float angleDiff = Mathf.DeltaAngle(_currentAngle, nextAngle);
        float maxStep = maxAngularSpeed * Time.fixedDeltaTime;
        angleDiff = Mathf.Clamp(angleDiff, -maxStep, maxStep);

        _currentAngle += angleDiff;

        // Sync gravity direction so the ball always falls toward screen "down"
        ApplyGravity(_currentAngle);

        // Notify camera to rotate in the opposite direction (camera illusion)
        if (cameraController != null)
        {
            cameraController.SetWorldRotation(_currentAngle);
        }
    }

    /// <summary>
    /// Rotates the gravity vector to match the current world rotation angle.
    /// When angle=0, gravity is (0, -9.81). Rotating CW (+angle) tilts gravity accordingly.
    /// </summary>
    private void ApplyGravity(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float gravMag = GravityMagnitude * gravityScale;
        // Update fields directly; negate sin/cos so gravity points toward visual "down"
        _gravityVector.x = -Mathf.Sin(rad) * gravMag;
        _gravityVector.y = -Mathf.Cos(rad) * gravMag;
        Physics2D.gravity = _gravityVector;
    }
}
