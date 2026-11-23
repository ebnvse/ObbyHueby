using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    [Header("Плавное следование за игроком")]
    [Tooltip("Задержка камеры вне боя (0 = жестко привязана)")]
    public float normalFollowSmoothTime = 0f;
    [Tooltip("Задержка камеры в бою / таргете")]
    public float lockOnFollowSmoothTime = 0.2f;

    [Header("Target Lock")]
    public PlayerController playerController;
    [Tooltip("Скорость поворота и перехода камеры В таргет")]
    public float lockOnEnterSpeed = 8f;
    [Tooltip("Скорость плавного возврата камеры ИЗ таргета после смерти врага (меньше = плавнее)")]
    public float lockOnExitSpeed = 2.5f;
    [Tooltip("Угол наклона камеры сверху над игроком во время таргета")]
    public float lockOnPitch = 25f;
    [Tooltip("Высота точки крепления камеры над игроком")]
    public float heightOffset = 1.6f;
    [Tooltip("Дистанция камеры в режиме таргета")]
    public float lockOnDistance = 5f;
    [Tooltip("Смещение камеры вправо в режиме таргета (вид из-за плеча)")]
    public float lockOnRightOffset = 0.8f;

    [Header("Rotation")]
    public float sensitivity = 0.15f;
    public float minVerticalAngle = -35f;
    public float maxVerticalAngle = 70f;

    [Header("Touch Controls")]
    [Range(0.1f, 0.9f)]
    public float touchAreaStart = 0.5f;

    private int activeTouchId = -1;

    [Header("Zoom")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 0.5f;

    [Header("Collision")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch;
    private float currentDistance;
    private float currentRightOffset;
    private float currentSmoothTime;

    private Vector3 smoothedTargetPos;
    private Vector3 followVelocity;

    void Start()
    {
        currentDistance = distance;
        currentSmoothTime = normalFollowSmoothTime;

        if (target != null)
        {
            smoothedTargetPos = target.position;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        if (playerController == null)
        {
            if (target != null)
                playerController = target.GetComponentInParent<PlayerController>();

            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isLockedOn = playerController != null && playerController.currentTarget != null;

        // Выбираем скорость перехода: быстрая при входе в бой, мягкая и медленная при выходе
        float transitionSpeed = isLockedOn ? lockOnEnterSpeed : lockOnExitSpeed;

        // Плавно переводим саму задержку следования (от 0.2 к 0), чтобы при смерти врага не было рывка позиции
        float targetSmoothTime = isLockedOn ? lockOnFollowSmoothTime : normalFollowSmoothTime;
        currentSmoothTime = Mathf.Lerp(currentSmoothTime, targetSmoothTime, Time.deltaTime * transitionSpeed);

        if (currentSmoothTime <= 0.005f)
        {
            smoothedTargetPos = target.position;
            followVelocity = Vector3.zero;
        }
        else
        {
            smoothedTargetPos = Vector3.SmoothDamp(
                smoothedTargetPos,
                target.position,
                ref followVelocity,
                currentSmoothTime
            );
        }

        float targetDistance = distance;
        float targetRightOffset = 0f;

        if (isLockedOn)
        {
            activeTouchId = -1;

            Vector3 dirToEnemy = playerController.currentTarget.position - smoothedTargetPos;
            dirToEnemy.y = 0f;

            if (dirToEnemy.sqrMagnitude > 0.001f)
            {
                float targetYaw = Quaternion.LookRotation(dirToEnemy).eulerAngles.y;
                yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * lockOnEnterSpeed);
            }

            pitch = Mathf.LerpAngle(pitch, lockOnPitch, Time.deltaTime * lockOnEnterSpeed);

            targetDistance = lockOnDistance;
            targetRightOffset = lockOnRightOffset;
        }
        else
        {
            HandleMouseRotation();
            HandleTouchRotation();
            HandleZoom();
        }

        // Возврат дистанции и бокового смещения идет со скоростью transitionSpeed (при выходе — медленно и плавно)
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * transitionSpeed);
        currentRightOffset = Mathf.Lerp(currentRightOffset, targetRightOffset, Time.deltaTime * transitionSpeed);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 basePivot = smoothedTargetPos + Vector3.up * heightOffset;
        Vector3 rightVector = rotation * Vector3.right;
        Vector3 desiredPivot = basePivot + rightVector * currentRightOffset;

        if (Mathf.Abs(currentRightOffset) > 0.01f)
        {
            Vector3 sideDir = rightVector * Mathf.Sign(currentRightOffset);
            float sideDist = Mathf.Abs(currentRightOffset);

            if (Physics.SphereCast(
                basePivot,
                collisionRadius,
                sideDir,
                out RaycastHit sideHit,
                sideDist,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                float safeOffset = Mathf.Max(0f, sideHit.distance - collisionRadius);
                desiredPivot = basePivot + sideDir * safeOffset;
            }
        }

        Vector3 direction = rotation * Vector3.back;
        float finalDistance = currentDistance;

        if (Physics.SphereCast(
            desiredPivot,
            collisionRadius,
            direction,
            out RaycastHit hit,
            currentDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            finalDistance = Mathf.Max(0.1f, hit.distance - collisionRadius);
        }

        transform.position = desiredPivot + direction * finalDistance;
        transform.rotation = rotation;
    }

    void HandleMouseRotation()
    {
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            RotateCamera(delta);
        }
    }

    void HandleTouchRotation()
    {
        if (Touchscreen.current == null)
            return;

        foreach (var touch in Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            Vector2 position = touch.position.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (activeTouchId == -1 && position.x > Screen.width * touchAreaStart)
                {
                    activeTouchId = touchId;
                }
            }

            if (touchId == activeTouchId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 delta = touch.delta.ReadValue();
                    RotateCamera(delta);
                }

                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    activeTouchId = -1;
                }
            }
        }
    }

    void RotateCamera(Vector2 delta)
    {
        yaw += delta.x * sensitivity;
        pitch -= delta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            distance -= scroll * zoomSpeed * 0.01f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}
