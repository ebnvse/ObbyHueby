using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;
    public PlayerController playerController;
    public AttackController attackController;
    public PlayerStats playerStats;

    [Header("Настройки атаки")]
    [Tooltip("Точные названия состояний атак на 1-м слое Animator")]
    public string[] attackStateNames = { "Attack1", "Attack2" };
    public float blendSpeed = 10f;

    // Слой 0: Движение, прыжок, таргет-лок и смерть
    private readonly int IdleHash = Animator.StringToHash("Idle");
    private readonly int IdleTargetHash = Animator.StringToHash("IdleTarget");
    private readonly int WalkHash = Animator.StringToHash("Walk");
    private readonly int RunHash = Animator.StringToHash("Run");
    private readonly int JumpHash = Animator.StringToHash("Jump");

    private readonly int TargetWalkBackHash = Animator.StringToHash("TargetWalkBack");
    private readonly int TargetWalkLeftHash = Animator.StringToHash("TargetWalkLeft");
    private readonly int TargetWalkRightHash = Animator.StringToHash("TargetWalkRight");

    private readonly int DeathHash = Animator.StringToHash("Death");

    private int currentBaseAnim;

    // Слой 1: Атака (Верх тела)
    private bool isPlayingAttack;
    private string currentAttackName;
    private int lastAttackIndex = -1;
    private float upperLayerWeight;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (attackController == null)
            attackController = GetComponentInChildren<AttackController>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (attackController != null)
            attackController.OnAttackStarted += HandleAttackTriggered;
    }

    private void OnDisable()
    {
        if (attackController != null)
            attackController.OnAttackStarted -= HandleAttackTriggered;
    }

    private void Update()
    {
        if (animator == null || playerController == null)
            return;

        // Если игрок мертв — играем Death и плавно выключаем слой атак
        if (playerStats != null && playerStats.isDead)
        {
            PlayBaseAnimation(DeathHash, 0.15f);
            isPlayingAttack = false;
            upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, 0f, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(1, upperLayerWeight);
            return;
        }

        UpdateBaseMovement();
        UpdateUpperBodyAttack();
    }

    private void UpdateBaseMovement()
    {
        int targetAnim;

        if (!playerController.IsGrounded)
        {
            targetAnim = JumpHash;
        }
        else if (playerController.IsMoving)
        {
            if (playerController.HasTarget)
            {
                targetAnim = GetTargetMovementHash();
            }
            else
            {
                targetAnim = playerController.IsRunning ? RunHash : WalkHash;
            }
        }
        else
        {
            targetAnim = playerController.HasTarget ? IdleTargetHash : IdleHash;
        }

        PlayBaseAnimation(targetAnim, 0.15f);
    }

    private int GetTargetMovementHash()
    {
        float h = playerController.InputHorizontal;
        float v = playerController.InputVertical;

        // Если отклонение назад сильнее, чем вбок — идем назад
        if (v < -0.1f && Mathf.Abs(v) >= Mathf.Abs(h))
        {
            return TargetWalkBackHash;
        }
        // Шаг вправо
        if (h > 0.05f)
        {
            return TargetWalkRightHash;
        }
        // Шаг влево
        if (h < -0.05f)
        {
            return TargetWalkLeftHash;
        }

        // Движение вперед на врага в таргете — обычный бег или шаг
        return playerController.IsRunning ? RunHash : WalkHash;
    }

    private void PlayBaseAnimation(int animHash, float transitionDuration)
    {
        if (currentBaseAnim != animHash)
        {
            currentBaseAnim = animHash;
            animator.CrossFadeInFixedTime(currentBaseAnim, transitionDuration, 0);
        }
    }

    private void HandleAttackTriggered()
    {
        if (animator == null || attackStateNames == null || attackStateNames.Length == 0)
            return;

        if (playerStats != null && playerStats.isDead)
            return;

        int index = Random.Range(0, attackStateNames.Length);
        if (attackStateNames.Length > 1 && index == lastAttackIndex)
        {
            index = (index + 1) % attackStateNames.Length;
        }

        lastAttackIndex = index;
        currentAttackName = attackStateNames[index];

        isPlayingAttack = true;
        animator.Play(currentAttackName, 1, 0f);
    }

    private void UpdateUpperBodyAttack()
    {
        if (isPlayingAttack)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
            if (state.IsName(currentAttackName) && state.normalizedTime >= 0.8f)
            {
                isPlayingAttack = false;
            }
        }

        float targetWeight = isPlayingAttack ? 1f : 0f;
        upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, targetWeight, blendSpeed * Time.deltaTime);
        animator.SetLayerWeight(1, upperLayerWeight);
    }
}
