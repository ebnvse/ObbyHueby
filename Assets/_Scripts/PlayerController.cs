using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Gamepad Settings")]
    [Range(0.05f, 0.5f)]
    public float stickDeadzone = 0.15f;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    [Header("Target Lock System")]
    public Transform currentTarget;
    public float lockOnRadius = 8f;
    public float unlockRadius = 11f;
    public float lockOnRotationSpeed = 12f;
    public LayerMask enemyLayer;

    [Header("Footstep Settings")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    public SoundEvent footstepSound;
    private float stepTimer;

    [Range(0.1f, 1f)]
    [Tooltip("Множитель скорости движения влево и вправо в режиме таргета (0.6 = 60% от обычной скорости)")]
    public float targetStrafeSpeedMultiplier = 0.6f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -20f;
    [Tooltip("Ускоряет падение вниз (2 = в два раза быстрее, убирает эффект лунной гравитации)")]
    public float fallMultiplier = 2f;
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.15f;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    [Header("Animator Properties")]
    public bool IsMoving => Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f;
    public bool IsRunning => isRunning;
    public bool IsGrounded => isGrounded;
    public bool HasTarget => currentTarget != null;
    public float InputHorizontal => horizontal;
    public float InputVertical => vertical;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMoveDirection;

    private float horizontal;
    private float vertical;
    public bool isRunning;

    [Header("Rotation Settings")]
    public float rotationOffset = 180f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Mobile Controls")]
    public Joystick joystick;

    public bool isRunningBittonWasPressed;
    public bool JumpBittonWasPressed;

    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // При старте включаем режим бега по умолчанию
        isRunningBittonWasPressed = true;
        isRunning = true;
    }

    public void MobileChangeMoveState()
    {
        if (currentTarget != null) return;
        isRunningBittonWasPressed = !isRunningBittonWasPressed;
    }

    public void ClickMobileButtonJump()
    {
        if (currentTarget != null) return;
        JumpBittonWasPressed = true;
    }

    void Update()
    {
        ReadInput();
        HandleTargetLock();
        CalculateMovement();
        HandleJumpAndGravity();
        ApplyFinalMovement();
        HandleFootsteps();
    }

    void ReadInput()
    {
        float keyboardHorizontal = 0;
        float keyboardVertical = 0;
        float gamepadHorizontal = 0;
        float gamepadVertical = 0;

        bool hasTarget = currentTarget != null;

        // Переключение бега на геймпаде (нажатие на левый стик L3)
        if (!hasTarget && Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame)
        {
            isRunningBittonWasPressed = !isRunningBittonWasPressed;
        }

        if (hasTarget)
        {
            // В бою принудительно переводим на шаг, но запоминаем "бег" для выхода из боя
            isRunning = false;
            isRunningBittonWasPressed = true;
        }
        else
        {
            isRunning = isRunningBittonWasPressed;
        }

        // Клавиатура
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) keyboardHorizontal = -1;
            if (Keyboard.current.dKey.isPressed) keyboardHorizontal = 1;
            if (Keyboard.current.wKey.isPressed) keyboardVertical = 1;
            if (Keyboard.current.sKey.isPressed) keyboardVertical = -1;
        }

        // Геймпад (левый стик с мертвой зоной)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.magnitude >= stickDeadzone)
            {
                gamepadHorizontal = stick.x;
                gamepadVertical = stick.y;
            }
        }

        // Мобильный джойстик
        float joystickHorizontal = 0;
        float joystickVertical = 0;

        if (joystick != null)
        {
            joystickHorizontal = joystick.Horizontal;
            joystickVertical = joystick.Vertical;
        }

        horizontal = Mathf.Clamp(keyboardHorizontal + gamepadHorizontal + joystickHorizontal, -1f, 1f);
        vertical = Mathf.Clamp(keyboardVertical + gamepadVertical + joystickVertical, -1f, 1f);

        if (joystickHorizontal != 0 || joystickVertical != 0)
        {
            Vector2 direction = new Vector2(horizontal, vertical).normalized;
            horizontal = direction.x;
            vertical = direction.y;
        }
    }

    void HandleTargetLock()
    {
        if (currentTarget != null)
        {
            EnemyStats enemyStats = currentTarget.GetComponentInParent<EnemyStats>();
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            if (!currentTarget.gameObject.activeInHierarchy ||
                (enemyStats != null && enemyStats.currentHealth <= 0) ||
                distanceToTarget > unlockRadius)
            {
                currentTarget = null;
            }
        }

        if (currentTarget == null)
        {
            int mask = enemyLayer.value == 0 ? ~0 : enemyLayer.value;
            Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRadius, mask);

            float closestDistance = Mathf.Infinity;
            Transform bestTarget = null;

            foreach (var hit in hits)
            {
                EnemyStats enemy = hit.GetComponentInParent<EnemyStats>();
                if (enemy != null && enemy.currentHealth > 0)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestTarget = enemy.transform;
                    }
                }
            }

            currentTarget = bestTarget;
        }
    }

    void CalculateMovement()
    {
        if (currentTarget != null)
        {
            Vector3 directionToEnemy = currentTarget.position - transform.position;
            directionToEnemy.y = 0f;

            if (directionToEnemy.sqrMagnitude > 0.001f)
            {
                Vector3 forwardToEnemy = directionToEnemy.normalized;
                Vector3 rightFromEnemy = Vector3.Cross(Vector3.up, forwardToEnemy);

                Vector2 clampedInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

                Vector3 forwardMove = forwardToEnemy * clampedInput.y;
                Vector3 strafeMove = rightFromEnemy * (clampedInput.x * targetStrafeSpeedMultiplier);

                currentMoveDirection = forwardMove + strafeMove;

                Quaternion targetRotation =
                    Quaternion.LookRotation(forwardToEnemy) *
                    Quaternion.Euler(0, rotationOffset, 0);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    lockOnRotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;

            cameraForward.Normalize();
            cameraRight.Normalize();

            currentMoveDirection = Vector3.ClampMagnitude(cameraForward * vertical + cameraRight * horizontal, 1f);
            rotationSpeed = isRunning ? 10f : 5f;

            if (currentMoveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(currentMoveDirection) *
                    Quaternion.Euler(0, rotationOffset, 0);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    void HandleJumpAndGravity()
    {
        Vector3 capsuleCenter = transform.position + controller.center;
        float castDistance = (controller.height * 0.5f) - controller.radius + groundDistance;

        bool sphereHit = Physics.SphereCast(
            capsuleCenter,
            controller.radius * 0.95f,
            Vector3.down,
            out _,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = controller.isGrounded || sphereHit;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepadJump = currentTarget == null && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (keyboardJump || gamepadJump || JumpBittonWasPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isGrounded = false;
        }

        if (!isGrounded && velocity.y < 0f)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        velocity.y = Mathf.Max(velocity.y, -45f);
        JumpBittonWasPressed = false;
    }

    void ApplyFinalMovement()
    {
        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 finalMotion = (currentMoveDirection * speed) + (Vector3.up * velocity.y);

        controller.Move(finalMotion * Time.deltaTime);
    }

    void HandleFootsteps()
    {
        bool isMoving = horizontal != 0 || vertical != 0;

        if (!isGrounded || !isMoving || footstepSound == null)
        {
            stepTimer = 0f;
            return;
        }
        float currentInterval = isRunning ? runStepInterval : walkStepInterval;
        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            float stepVolume = isRunning ? 0.9f : 0.6f;
            footstepSound.Play(transform.position, stepVolume);
            stepTimer = currentInterval;
        }
    }
}
