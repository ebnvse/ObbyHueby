using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;
    public PlayerController playerController;
    public AttackController attackController;
    public ParryController parryController;
    public PlayerStats playerStats;

    [Header("Настройки атаки")]
    [Tooltip("Точные названия состояний атак на 1-м слое Animator")]
    public string[] attackStateNames = { "Attack1", "Attack2" };
    public float blendSpeed = 10f;
    [Tooltip("Максимальный множитель скорости анимации удара (чтобы на 10 уровне перс не махал слишком дёргано)")]
    public float maxAnimSpeedMultiplier = 4f;

    [Header("Настройки парирования")]
    [Tooltip("Название стейта анимации поднятия руки в Animator")]
    public string parryStateName = "parry";
    [Tooltip("Слой аниматора для парирования (можно использовать 1-й слой верхнего тела)")]
    public int parryLayerIndex = 1;

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

    // Параметр скорости атаки в окне Animator
    private readonly int AttackSpeedParamHash = Animator.StringToHash("AttackSpeed");

    private int parryHash;
    private int currentBaseAnim;

    // Слой 1: Атака и Парирование (Верх тела)
    private bool isPlayingAttack;
    private bool wasParrying;
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

        if (parryController == null)
            parryController = GetComponentInChildren<ParryController>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        parryHash = Animator.StringToHash(parryStateName);
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

        if (playerStats != null && playerStats.isDead)
        {
            PlayBaseAnimation(DeathHash, 0.15f);
            isPlayingAttack = false;
            wasParrying = false;
            upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, 0f, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(1, upperLayerWeight);
            return;
        }

        UpdateBaseMovement();
        UpdateUpperBodyActions();
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

        if (v < -0.1f && Mathf.Abs(v) >= Mathf.Abs(h))
        {
            return TargetWalkBackHash;
        }
        if (h > 0.05f)
        {
            return TargetWalkRightHash;
        }
        if (h < -0.05f)
        {
            return TargetWalkLeftHash;
        }

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

        // Не атакуем, если игрок сейчас держит блок/парирование
        if (parryController != null && parryController.IsParrying)
            return;

        float speedMult = 1f;
        if (playerStats != null)
        {
            speedMult = Mathf.Clamp(playerStats.AtkSpped_Player, 1f, maxAnimSpeedMultiplier);
        }
        animator.SetFloat(AttackSpeedParamHash, speedMult);

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

    private void UpdateUpperBodyActions()
    {
        bool isParrying = parryController != null && parryController.IsParrying;

        if (isParrying)
        {
            // Парирование прерывает текущую атаку
            isPlayingAttack = false;

            if (!wasParrying)
            {
                animator.CrossFadeInFixedTime(parryHash, 0.1f, parryLayerIndex, 0f);
            }
        }
        else if (isPlayingAttack)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
            if (state.IsName(currentAttackName) && state.normalizedTime >= 0.8f)
            {
                isPlayingAttack = false;
            }
        }

        wasParrying = isParrying;

        float currentSpeedMult = (!isParrying && playerStats != null)
            ? Mathf.Clamp(playerStats.AtkSpped_Player, 1f, maxAnimSpeedMultiplier)
            : 1f;

        float targetWeight = (isPlayingAttack || isParrying) ? 1f : 0f;
        upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, targetWeight, (blendSpeed * currentSpeedMult) * Time.deltaTime);
        animator.SetLayerWeight(parryLayerIndex, upperLayerWeight);
    }
}
