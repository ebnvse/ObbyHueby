using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    public PlayerStats playerStats;

    [Header("Звуки")]
    [Tooltip("Звук взмаха при начале атаки (AtackPlayer)")]
    [SerializeField] private SoundEvent attackSwingSound;
    [Tooltip("Звук попадания по врагу (PlayerTakeHit)")]
    [SerializeField] private SoundEvent attackHitSound;

    [Header("Управление")]
    [Tooltip("Включите, если хотите также атаковать кликом мыши по экрану (на ПК)")]
    [SerializeField] public bool allowMouseClickAttack = false;

    public event System.Action OnAttackStarted;

    private bool isAttacking;

    [Header("Настройки атаки")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxDistance = 15f;
    [Tooltip("Минимальное время между атаками в секундах")]
    [SerializeField] public float attackCooldown = 0.5f;

    [Header("Задержка урона")]
    [SerializeField] private float damageDelay = 0.3f;

    [Header("Настройки веера")]
    [Range(2, 50)]
    [SerializeField] private int rayCount = 5;
    [SerializeField] private float spreadAngle = 45f;




    private readonly HashSet<EnemyStats> hitEnemies = new HashSet<EnemyStats>();

    private void Update()
    {
        if (playerStats != null && playerStats.isDead)
            return;

        if (allowMouseClickAttack && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            Attack();
        }
    }

    public void ClickMobileButtonAttack()
    {
        Attack();
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
        if (playerStats != null && playerStats.isDead)
            return;

        if (isAttacking)
            return;

        if (rayOrigin == null)
            return;

        isAttacking = true;
        OnAttackStarted?.Invoke();

        if (attackSwingSound != null)
        {
            attackSwingSound.Play(transform.position);
        }

        StartCoroutine(AttackWithDelay());
        StartCoroutine(ResetAttackRoutine());
    }

    private IEnumerator ResetAttackRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private IEnumerator AttackWithDelay()
    {
        yield return new WaitForSeconds(damageDelay);

        if (playerStats != null && playerStats.isDead)
            yield break;

        hitEnemies.Clear();

        int count = Mathf.Max(2, rayCount);
        float startAngle = -spreadAngle * 0.5f;
        float angleStep = spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, rayOrigin.up) * rayOrigin.forward;

            if (Physics.Raycast(rayOrigin.position, direction, out RaycastHit hit, maxDistance))
            {
                Debug.DrawLine(rayOrigin.position, hit.point, Color.red, 1f);

                EnemyStats enemyStats = hit.collider.GetComponentInParent<EnemyStats>();

                if (enemyStats != null && hitEnemies.Add(enemyStats))
                {
                    int finalDamage = playerStats.CalculationAtackDamagePlayer(enemyStats.Def_Enemy,
                     playerStats.AtkDmg_Player, playerStats.Critical_Chance, playerStats.Critical_Chance_multiplier);

                    enemyStats.TakeDamage(finalDamage);
                    if (enemyStats.isDead == true)
                    {
                        playerStats.AddExpToPlayer();
                    }

                    if (attackHitSound != null)
                    {
                        attackHitSound.Play(hit.point);
                    }
                }
            }
            else
            {
                Debug.DrawRay(rayOrigin.position, direction * maxDistance, Color.red, 1f);
            }
        }
    }
}
