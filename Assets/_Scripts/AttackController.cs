using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    public PlayerStats playerStats;
    public ParryController parryController;

    [Header("Звуки")]
    [Tooltip("Звук взмаха при начале атаки (AtackPlayer)")]
    [SerializeField] private SoundEvent attackSwingSound;
    [Tooltip("Звук попадания по врагу (PlayerTakeHit)")]
    [SerializeField] private SoundEvent attackHitSound;
    [SerializeField] private SoundEvent playerTakeSelfSoundAtack;
    [SerializeField] private SoundEvent playerTakeSelfSoundUlt;
    [SerializeField] private SoundEvent FlameSound;

    [Header("Управление (Мышь и Клавиатура)")]
    [Tooltip("Включите, если хотите также атаковать кликом левой кнопки мыши по экрану")]
    [SerializeField] public bool allowMouseClickAttack = false;
    [Tooltip("Разрешить обычную атаку с клавиатуры")]
    [SerializeField] public bool allowKeyboardAttack = true;
    [Tooltip("Клавиша обычной атаки на ПК")]
    [SerializeField] public Key keyboardAttackKey = Key.F;
    [Tooltip("Разрешить активацию абилки с клавиатуры")]
    [SerializeField] public bool allowKeyboardAbility = true;
    [Tooltip("Клавиша абилки на ПК")]
    [SerializeField] public Key keyboardAbilityKey = Key.Digit1;

    [Header("Управление (Геймпад)")]
    [Tooltip("Включите для атаки и абилки с геймпада")]
    [SerializeField] public bool allowGamepadAttack = true;
    [Tooltip("Кнопка геймпада для обычной атаки (по умолчанию ButtonWest / Квадрат / X)")]
    [SerializeField] public GamepadButton gamepadAttackButton = GamepadButton.West;
    [Tooltip("Кнопка геймпада для абилки (по умолчанию ButtonNorth / Треугольник / Y)")]
    [SerializeField] public GamepadButton gamepadAbilityButton = GamepadButton.North;

    public event System.Action OnAttackStarted;
    public event System.Action OnAbilityAttackStarted;
    public event System.Action<float> OnAbilityCooldownStarted;

    private bool isAttacking;
    private bool isAbilityOnCooldown;
    private float abilityCooldownTimer;

    private Coroutine currentAttackCoroutine;
    private Coroutine currentResetCoroutine;

    public bool IsAbilityOnCooldown => isAbilityOnCooldown;
    public float AbilityCooldownRemaining => Mathf.Max(0f, abilityCooldownTimer);
    public float AbilityCooldownProgress => abilityCooldown > 0f ? Mathf.Clamp01(1f - (abilityCooldownTimer / abilityCooldown)) : 1f;

    [Header("Настройки обычной атаки")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 15f;
    [Tooltip("Минимальное время между атаками в секундах")]
    [SerializeField] public float attackCooldown = 0.5f;
    [Tooltip("Задержка урона обычной атаки в секундах")]
    [SerializeField] private float normalDamageDelay = 0.3f;
    [Range(2, 50)]
    [SerializeField] private int rayCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    [Header("Настройки спец. атаки (360°)")]
    [Tooltip("Множитель урона спец. атаки (например, 2 = 200% от обычного урона)")]
    [SerializeField] public float abilityDamageMultiplier = 2f;
    [Tooltip("Время перезарядки спец. атаки в секундах")]
    [SerializeField] public float abilityCooldown = 5f;
    [Tooltip("Блокировка других атак на время анимации ульты")]
    [SerializeField] private float abilityAnimationLock = 0.6f;
    [SerializeField] private float abilityDistance = 15f;
    [Tooltip("Задержка урона спец. атаки (ульты) в секундах")]
    [SerializeField] private float abilityDamageDelay = 0.3f;
    [Range(12, 72)]
    [SerializeField] private int abilityRayCount = 36;
    [SerializeField] private float abilitySpreadAngle = 360f;

    [Header("Эффект спец. атаки (VFX)")]
    [Tooltip("Префаб эффекта, который появится при нажатии абилки")]
    [SerializeField] private GameObject abilityEffectPrefab;
    [Tooltip("Точка спавна эффекта (если пусто — спавнится в центре персонажа)")]
    [SerializeField] private Transform abilityEffectSpawnPoint;
    [Tooltip("Привязать ли эффект к персонажу, чтобы он двигался вместе с ним")]
    [SerializeField] private bool attachEffectToPlayer = false;
    [Tooltip("Через сколько секунд удалить объект эффекта")]
    [SerializeField] private float abilityEffectDuration = 3f;

    private readonly HashSet<EnemyStats> hitEnemies = new HashSet<EnemyStats>();

    private void Update()
    {
        if (isAbilityOnCooldown)
        {
            abilityCooldownTimer -= Time.deltaTime;
            if (abilityCooldownTimer <= 0f)
            {
                abilityCooldownTimer = 0f;
                isAbilityOnCooldown = false;
            }
        }

        if (playerStats != null && playerStats.isDead)
            return;

        if (allowMouseClickAttack && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            Attack();
        }

        if (Keyboard.current != null)
        {
            if (allowKeyboardAttack && keyboardAttackKey != Key.None && Keyboard.current[keyboardAttackKey].wasPressedThisFrame)
            {
                Attack();
            }

            if (allowKeyboardAbility && keyboardAbilityKey != Key.None)
            {
                bool abilityPressed = Keyboard.current[keyboardAbilityKey].wasPressedThisFrame ||
                                      (keyboardAbilityKey == Key.Digit1 && Keyboard.current.numpad1Key.wasPressedThisFrame);

                if (abilityPressed)
                {
                    AttackAbility();
                }
            }
        }

        if (allowGamepadAttack && Gamepad.current != null)
        {
            if (Gamepad.current[gamepadAttackButton].wasPressedThisFrame)
            {
                Attack();
            }
            else if (Gamepad.current[gamepadAbilityButton].wasPressedThisFrame)
            {
                AttackAbility();
            }
        }
    }

    public void ClickMobileButtonAttack()
    {
        Attack();
    }

    public void ClickMobileButtonAbility()
    {
        AttackAbility();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame &&
                    EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void Attack()
    {
        if (parryController != null && parryController.IsParrying) return;
        if (playerStats != null && playerStats.isDead) return;
        if (isAttacking) return;
        if (rayOrigin == null) return;

        isAttacking = true;
        OnAttackStarted?.Invoke();

        if (attackSwingSound != null)
        {
            attackSwingSound.Play(transform.position);
            if (playerTakeSelfSoundAtack != null) playerTakeSelfSoundAtack.Play(transform.position);
        }

        currentAttackCoroutine = StartCoroutine(NormalAttackRoutine());
        currentResetCoroutine = StartCoroutine(ResetAttackRoutine(attackCooldown));
    }

    private void AttackAbility()
    {
        if (parryController != null && parryController.IsParrying) return;
        if (playerStats != null && playerStats.isDead) return;
        if (isAbilityOnCooldown) return;
        if (rayOrigin == null) return;

        if (currentAttackCoroutine != null) StopCoroutine(currentAttackCoroutine);
        if (currentResetCoroutine != null) StopCoroutine(currentResetCoroutine);

        isAttacking = true;
        isAbilityOnCooldown = true;
        abilityCooldownTimer = abilityCooldown;

        OnAbilityAttackStarted?.Invoke();
        OnAbilityCooldownStarted?.Invoke(abilityCooldown);

        SpawnAbilityEffect();

        if (playerTakeSelfSoundUlt != null) playerTakeSelfSoundUlt.Play(transform.position);
        if (FlameSound != null) FlameSound.Play(transform.position);

        currentAttackCoroutine = StartCoroutine(AbilityAttackRoutine());
        currentResetCoroutine = StartCoroutine(ResetAttackRoutine(abilityAnimationLock));
    }

    private void SpawnAbilityEffect()
    {
        if (abilityEffectPrefab == null) return;

        Transform spawnTransform = abilityEffectSpawnPoint != null ? abilityEffectSpawnPoint : transform;
        GameObject effectInstance = Instantiate(abilityEffectPrefab, spawnTransform.position, spawnTransform.rotation);

        if (attachEffectToPlayer)
        {
            effectInstance.transform.SetParent(spawnTransform);
        }

        if (abilityEffectDuration > 0f)
        {
            Destroy(effectInstance, abilityEffectDuration);
        }
    }

    private IEnumerator ResetAttackRoutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
    }

    private IEnumerator NormalAttackRoutine()
    {
        yield return new WaitForSeconds(normalDamageDelay);
        ExecuteRayCastDamage(rayCount, spreadAngle, maxDistance, 1f, true);
    }

    private IEnumerator AbilityAttackRoutine()
    {
        yield return new WaitForSeconds(abilityDamageDelay);
        ExecuteRayCastDamage(abilityRayCount, abilitySpreadAngle, abilityDistance, abilityDamageMultiplier, false);
    }

    private void ExecuteRayCastDamage(int rays, float angleSpread, float distance, float damageMultiplier, bool canCrit)
    {
        if (playerStats != null && playerStats.isDead)
            return;

        hitEnemies.Clear();

        int count = Mathf.Max(2, rays);
        float startAngle = -angleSpread * 0.5f;
        float angleStep = (angleSpread >= 360f) ? (360f / count) : (angleSpread / (count - 1));

        float critChance = canCrit ? playerStats.Critical_Chance : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, rayOrigin.up) * rayOrigin.forward;

            if (Physics.Raycast(rayOrigin.position, direction, out RaycastHit hit, distance))
            {
                Debug.DrawLine(rayOrigin.position, hit.point, Color.red, 1f);

                EnemyStats enemyStats = hit.collider.GetComponentInParent<EnemyStats>();

                if (enemyStats != null && !enemyStats.isDead && hitEnemies.Add(enemyStats))
                {
                    int finalDamage = playerStats.CalculationAtackDamagePlayer(
                        enemyStats.Def_Enemy,
                        playerStats.AtkDmg_Player,
                        critChance,
                        playerStats.Critical_Chance_multiplier,
                        enemyStats.EnemyLevel,
                        damageMultiplier
                    );

                    enemyStats.TakeDamage(finalDamage);

                    if (enemyStats.isDead)
                    {
                        playerStats.AddExpToPlayer(enemyStats.EnemyLevel);
                    }

                    if (attackHitSound != null)
                    {
                        attackHitSound.Play(hit.point);
                    }
                }
            }
            else
            {
                Debug.DrawRay(rayOrigin.position, direction * distance, Color.red, 1f);
            }
        }
    }
}
