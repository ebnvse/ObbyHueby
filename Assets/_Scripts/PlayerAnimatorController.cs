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

    [Header("Настройки спец. атаки (Ульты)")]
    [Tooltip("Название состояния специальной атаки в Animator")]
    public string abilityAttackStateName = "MassSwordAtack";
    [Tooltip("Индекс слоя аниматора для ульты (например, 2)")]
    public int abilityLayerIndex = 2;

    [Header("Настройки парирования")]
    [Tooltip("Название стейта анимации поднятия руки в Animator")]
    public string parryStateName = "parry";
    [Tooltip("Слой аниматора для парирования и обычных атак (1-й слой верхнего тела)")]
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

    // Слой 1: Обычная атака и Парирование
    private bool isPlayingAttack;
    private bool attackJustStarted;
    private bool wasParrying;
    private string currentAttackName;
    private int lastAttackIndex = -1;
    private float upperLayerWeight;

    // Отдельный слой: Спец. атака (Ульта)
    private bool isPlayingAbility;
    private bool abilityJustStarted;
    private float abilityLayerWeight;

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
        {
            attackController.OnAttackStarted += HandleAttackTriggered;
            attackController.OnAbilityAttackStarted += HandleAbilityAttackTriggered;
        }
    }

    private void OnDisable()
    {
        if (attackController != null)
        {
            attackController.OnAttackStarted -= HandleAttackTriggered;
            attackController.OnAbilityAttackStarted -= HandleAbilityAttackTriggered;
        }
    }

    private void Update()
    {
        if (animator == null || playerController == null)
            return;

        if (playerStats != null && playerStats.isDead)
        {
            PlayBaseAnimation(DeathHash, 0.15f);
            isPlayingAttack = false;
            attackJustStarted = false;
            isPlayingAbility = false;
            abilityJustStarted = false;
            wasParrying = false;

            upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, 0f, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(parryLayerIndex, upperLayerWeight);

            abilityLayerWeight = Mathf.MoveTowards(abilityLayerWeight, 0f, blendSpeed * Time.deltaTime);
            animator.SetLayerWeight(abilityLayerIndex, abilityLayerWeight);
            return;
        }

        UpdateBaseMovement();
        UpdateUpperBodyActions();
        UpdateAbilityLayer();
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

        if (parryController != null && parryController.IsParrying)
            return;

        SetAttackSpeed();

        int index = Random.Range(0, attackStateNames.Length);
        if (attackStateNames.Length > 1 && index == lastAttackIndex)
        {
            index = (index + 1) % attackStateNames.Length;
        }

        lastAttackIndex = index;
        currentAttackName = attackStateNames[index];

        isPlayingAttack = true;
        attackJustStarted = true;
        isPlayingAbility = false;
        abilityJustStarted = false;

        upperLayerWeight = 1f;
        animator.SetLayerWeight(parryLayerIndex, 1f);

        animator.Play(currentAttackName, parryLayerIndex, 0f);
    }

    private void HandleAbilityAttackTriggered()
    {
        if (animator == null || string.IsNullOrEmpty(abilityAttackStateName))
            return;

        if (playerStats != null && playerStats.isDead)
            return;

        if (parryController != null && parryController.IsParrying)
            return;

        SetAttackSpeed();

        isPlayingAbility = true;
        abilityJustStarted = true;
        isPlayingAttack = false;
        attackJustStarted = false;

        abilityLayerWeight = 1f;
        animator.SetLayerWeight(abilityLayerIndex, 1f);

        animator.Play(abilityAttackStateName, abilityLayerIndex, 0f);
    }

    private void SetAttackSpeed()
    {
        float speedMult = 1f;
        if (playerStats != null)
        {
            speedMult = Mathf.Clamp(playerStats.AtkSpped_Player, 1f, maxAnimSpeedMultiplier);
        }
        animator.SetFloat(AttackSpeedParamHash, speedMult);
    }

    private void UpdateUpperBodyActions()
    {
        bool isParrying = parryController != null && parryController.IsParrying;

        if (isParrying)
        {
            isPlayingAttack = false;
            attackJustStarted = false;

            if (!wasParrying)
            {
                animator.CrossFadeInFixedTime(parryHash, 0.1f, parryLayerIndex, 0f);
            }
        }
        else if (isPlayingAttack)
        {
            if (attackJustStarted)
            {
                attackJustStarted = false;
            }
            else
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(parryLayerIndex);
                if (state.IsName(currentAttackName) && state.normalizedTime >= 0.8f)
                {
                    isPlayingAttack = false;
                }
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

    private void UpdateAbilityLayer()
    {
        bool isParrying = parryController != null && parryController.IsParrying;

        if (isParrying)
        {
            isPlayingAbility = false;
            abilityJustStarted = false;
        }
        else if (isPlayingAbility)
        {
            if (abilityJustStarted)
            {
                abilityJustStarted = false;
            }
            else
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(abilityLayerIndex);
                if (state.IsName(abilityAttackStateName) && state.normalizedTime >= 0.85f)
                {
                    isPlayingAbility = false;
                }
            }
        }

        float currentSpeedMult = (!isParrying && playerStats != null)
            ? Mathf.Clamp(playerStats.AtkSpped_Player, 1f, maxAnimSpeedMultiplier)
            : 1f;

        float targetWeight = (isPlayingAbility && !isParrying) ? 1f : 0f;
        abilityLayerWeight = Mathf.MoveTowards(abilityLayerWeight, targetWeight, (blendSpeed * currentSpeedMult) * Time.deltaTime);
        animator.SetLayerWeight(abilityLayerIndex, abilityLayerWeight);
    }
}
