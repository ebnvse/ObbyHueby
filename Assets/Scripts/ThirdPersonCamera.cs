using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    [Header("Target Lock")]
    public PlayerController playerController;
    [Tooltip("Скорость плавного поворота и смещения камеры в таргете")]
    public float lockOnRotationSpeed = 8f;
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

    void Start()
    {
        currentDistance = distance;

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

        float targetDistance = distance;
        float targetRightOffset = 0f;

        if (isLockedOn)
        {
            // Блокируем ручное вращение (ПКМ и сенсор)
            activeTouchId = -1;

            Vector3 dirToEnemy = playerController.currentTarget.position - target.position;
            dirToEnemy.y = 0f;

            if (dirToEnemy.sqrMagnitude > 0.001f)
            {
                float targetYaw = Quaternion.LookRotation(dirToEnemy).eulerAngles.y;
                yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * lockOnRotationSpeed);
            }

            pitch = Mathf.LerpAngle(pitch, lockOnPitch, Time.deltaTime * lockOnRotationSpeed);

            // Задаём целевую дистанцию 5 и смещение вправо
            targetDistance = lockOnDistance;
            targetRightOffset = lockOnRightOffset;
        }
        else
        {
            HandleMouseRotation();
            HandleTouchRotation();
            HandleZoom();
        }

        // Плавное изменение дистанции и бокового смещения
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * lockOnRotationSpeed);
        currentRightOffset = Mathf.Lerp(currentRightOffset, targetRightOffset, Time.deltaTime * lockOnRotationSpeed);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Базовая точка над персонажем
        Vector3 basePivot = target.position + Vector3.up * heightOffset;
        Vector3 rightVector = rotation * Vector3.right;
        Vector3 desiredPivot = basePivot + rightVector * currentRightOffset;

        // Проверяем, нет ли стены справа при смещении камеры
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

        // Проверка столкновений камеры со стенами сзади
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
