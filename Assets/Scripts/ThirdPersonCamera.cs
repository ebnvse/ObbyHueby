using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

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

    void Start()
    {
        currentDistance = distance;

        Vector3 angles = transform.eulerAngles;

        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleMouseRotation();
        HandleTouchRotation();
        HandleZoom();

        currentDistance = Mathf.Lerp(
            currentDistance,
            distance,
            Time.deltaTime * 10f
        );

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pivot = target.position + Vector3.up * 1.5f;
        Vector3 direction = rotation * Vector3.back;

        float finalDistance = currentDistance;

        // Проверка столкновений
        if (Physics.SphereCast(
            pivot,
            collisionRadius,
            direction,
            out RaycastHit hit,
            currentDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            finalDistance = Mathf.Max(
                0.1f,
                hit.distance - collisionRadius
            );
        }

        transform.position = pivot + direction * finalDistance;
        transform.rotation = rotation;
    }

    // Вращение мышью
    void HandleMouseRotation()
    {
        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            RotateCamera(delta);
        }
    }

    // Вращение пальцем
    void HandleTouchRotation()
    {
        if (Touchscreen.current == null)
            return;

        foreach (var touch in Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();

            var phase = touch.phase.ReadValue();

            Vector2 position = touch.position.ReadValue();

            // Начало касания
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Разрешаем вращение только справа экрана
                if (activeTouchId == -1 &&
                    position.x > Screen.width * touchAreaStart)
                {
                    activeTouchId = touchId;
                }
            }

            // Обработка активного пальца
            if (touchId == activeTouchId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 delta = touch.delta.ReadValue();

                    RotateCamera(delta);
                }

                // Завершение касания
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    activeTouchId = -1;
                }
            }
        }
    }

    // Общая логика вращения камеры
    void RotateCamera(Vector2 delta)
    {
        yaw += delta.x * sensitivity;

        pitch -= delta.y * sensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minVerticalAngle,
            maxVerticalAngle
        );
    }

    // Приближение и отдаление
    void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            distance -= scroll * zoomSpeed * 0.01f;
            distance = Mathf.Clamp(
                distance,
                minDistance,
                maxDistance
            );
        }
    }
}