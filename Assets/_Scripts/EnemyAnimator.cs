using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Enemy))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator animator;
    private Enemy enemy;

    [Header("Настройки плавности (CrossFade)")]
    public float movementFadeDuration = 0.15f;
    public float actionFadeDuration = 0.15f;

    [Header("Слой 0: Движение (Base Layer)")]
    public int movementLayer = 0;
    public string idleState = "EnemyIdle";
    public string walkState = "EnemyWalk";
    public string runState = "EnemyRun";

    [Header("Слой 1: Атаки (Attack Layer)")]
    public int attackLayer = 1;
    public string attackEmptyState = "Empty";
    public string[] attackStates = { "EnemyAtack", "EnemyAtack2" };

    [Header("Слой 2: Получение урона (Hit Layer)")]
    public int hitLayer = 2;
    public string hitEmptyState = "Empty";
    public string hitState = "EnemyGetHit";

    private int idleHash;
    private int walkHash;
    private int runHash;
    private int attackEmptyHash;
    private int[] attackHashes;
    private int hitEmptyHash;
    private int hitHash;

    private int currentMovementHash;
    private int lastAttackIndex = -1;
    private bool isAttacking;

    private Coroutine attackRoutine;
    private Coroutine hitRoutine;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorHashes();
    }

    private void CacheAnimatorHashes()
    {
        idleHash = Animator.StringToHash(idleState);
        walkHash = Animator.StringToHash(walkState);
        runHash = Animator.StringToHash(runState);

        attackEmptyHash = Animator.StringToHash(attackEmptyState);
        attackHashes = new int[attackStates.Length];
        for (int i = 0; i < attackStates.Length; i++)
        {
            attackHashes[i] = Animator.StringToHash(attackStates[i]);
        }

        hitEmptyHash = Animator.StringToHash(hitEmptyState);
        hitHash = Animator.StringToHash(hitState);
    }

    private void OnEnable()
    {
        enemy.OnAttackTriggered += PlayAttack;
        enemy.OnHitTriggered += PlayHit;
    }

    private void OnDisable()
    {
        enemy.OnAttackTriggered -= PlayAttack;
        enemy.OnHitTriggered -= PlayHit;
    }

    private void Update()
    {
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (animator == null || isAttacking) return;

        int targetHash = idleHash;

        if (enemy.IsRunning)
            targetHash = runHash;
        else if (enemy.IsWalking)
            targetHash = walkHash;

        if (currentMovementHash == targetHash) return;

        currentMovementHash = targetHash;
        animator.CrossFadeInFixedTime(targetHash, movementFadeDuration, movementLayer);
    }

    private void PlayAttack()
    {
        if (animator == null || attackHashes.Length == 0) return;

        int index = Random.Range(0, attackHashes.Length);
        if (attackHashes.Length > 1 && index == lastAttackIndex)
        {
            index = (index + 1) % attackHashes.Length;
        }
        lastAttackIndex = index;

        int selectedHash = attackHashes[index];

        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(AttackSequence(selectedHash));
    }

    private IEnumerator AttackSequence(int attackHash)
    {
        isAttacking = true;

        // Запускаем выбранную атаку на Слою 1 с 0-й секунды
        animator.CrossFadeInFixedTime(attackHash, actionFadeDuration, attackLayer, 0f);

        // 1. Ждём, пока аниматор гарантированно войдёт в стейт атаки (или начнёт переход в него)
        while (true)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(attackLayer);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(attackLayer);

            // Как только текущий стейт стал нашей атакой и переход закончился — выходим из ожидания старта
            if (current.shortNameHash == attackHash && !animator.IsInTransition(attackLayer))
                break;

            yield return null;
        }

        // 2. Теперь ждём, пока клип атаки доиграет до конца (normalizedTime >= 0.9f)
        while (true)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(attackLayer);
            
            if (current.shortNameHash != attackHash || current.normalizedTime >= 0.9f)
                break;

            yield return null;
        }

        // 3. Плавно уводим слой атак обратно в Empty, чтобы он перестал влиять на скелет
        animator.CrossFadeInFixedTime(attackEmptyHash, actionFadeDuration, attackLayer, 0f);

        // 4. Возвращаем базовый слой в EnemyIdle
        currentMovementHash = idleHash;
        animator.CrossFadeInFixedTime(idleHash, actionFadeDuration, movementLayer);

        isAttacking = false;
    }

    private void PlayHit()
    {
        if (animator == null) return;

        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(HitSequence());
    }

    private IEnumerator HitSequence()
    {
        animator.CrossFadeInFixedTime(hitHash, actionFadeDuration, hitLayer, 0f);

        while (true)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(hitLayer);
            if (current.shortNameHash == hitHash && !animator.IsInTransition(hitLayer))
                break;

            yield return null;
        }

        while (true)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(hitLayer);
            if (current.shortNameHash != hitHash || current.normalizedTime >= 0.9f)
                break;

            yield return null;
        }

        animator.CrossFadeInFixedTime(hitEmptyHash, actionFadeDuration, hitLayer, 0f);
    }
}
