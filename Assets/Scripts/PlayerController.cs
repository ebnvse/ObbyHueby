using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

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
    private float stepTimer;
    private Sounds soundsManager;

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

    [Header("Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentMoveDirection;

    private float horizontal;
    private float vertical;
    private bool isRunning;

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
        soundsManager = GetComponent<Sounds>();
    }

    public void MobileChangeMoveState()
    {
        isRunningBittonWasPressed = !isRunningBittonWasPressed;
    }

    void Update()
    {
        ReadInput();
        HandleTargetLock();
        CalculateMovement();
        HandleJumpAndGravity();
        ApplyFinalMovement();
        HandleFootsteps();
        UpdateAnimations();
    }

    void ReadInput()
    {
        float keyboardHorizontal = 0;
        float keyboardVertical = 0;

        isRunning = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) keyboardHorizontal = -1;
            if (Keyboard.current.dKey.isPressed) keyboardHorizontal = 1;
            if (Keyboard.current.wKey.isPressed) keyboardVertical = 1;
            if (Keyboard.current.sKey.isPressed) keyboardVertical = -1;

            isRunning = isRunningBittonWasPressed || Keyboard.current.leftShiftKey.isPressed;
        }

        float joystickHorizontal = 0;
        float joystickVertical = 0;

        if (joystick != null)
        {
            joystickHorizontal = joystick.Horizontal;
            joystickVertical = joystick.Vertical;
        }

        horizontal = Mathf.Clamp(keyboardHorizontal + joystickHorizontal, -1f, 1f);
        vertical = Mathf.Clamp(keyboardVertical + joystickVertical, -1f, 1f);

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

                // Ограничиваем входной вектор стика/клавиатуры, чтобы по диагонали не бежать быстрее
                Vector2 clampedInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

                // Движение вперёд/назад идёт с полной скоростью, а влево/вправо — с множителем targetStrafeSpeedMultiplier
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

    public void ClickMobileButtonJump()
    {
        JumpBittonWasPressed = true;
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
            animator.SetBool("Jump", false);
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
            animator.SetBool("Jump", true);
        }

        if ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) || JumpBittonWasPressed)
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

        if (!isGrounded || !isMoving || soundsManager == null)
        {
            stepTimer = 0f;
            return;
        }

        float currentInterval = isRunning ? runStepInterval : walkStepInterval;
        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            float stepVolume = isRunning ? 0.9f : 0.6f;
            soundsManager.PlayRandomFootstep(stepVolume);
            stepTimer = currentInterval;
        }
    }

    void UpdateAnimations()
    {
        bool hasTarget = currentTarget != null;
        bool isMoving = Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f;

        bool isWalkingBack = false;
        bool isWalkingRight = false;
        bool isWalkingLeft = false;

        if (hasTarget && isMoving)
        {
            // Если отклонение назад сильнее, чем вбок — идём назад
            if (vertical < -0.1f && Mathf.Abs(vertical) >= Mathf.Abs(horizontal))
            {
                isWalkingBack = true;
            }
            // Шаг вправо
            else if (horizontal > 0.05f)
            {
                isWalkingRight = true;
            }
            // Шаг влево
            else if (horizontal < -0.05f)
            {
                isWalkingLeft = true;
            }
        }

        animator.SetBool("Target", hasTarget);
        animator.SetBool("TargetWalkBack", isWalkingBack);
        animator.SetBool("TargetWalkRight", isWalkingRight);
        animator.SetBool("TargeWalkLeft", isWalkingLeft);

        // Если стоим или используем специальную таргет-анимацию — обычный Walk/Run выключены
        if (!isMoving || isWalkingBack || isWalkingRight || isWalkingLeft)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }
        else if (isRunning)
        {
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
        }
        else
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsRunning", false);
        }
    }

}
