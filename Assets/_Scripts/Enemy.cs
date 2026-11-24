using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Aggressive, // 1) Бежит за игроком, как только увидит
        Neutral     // 2) Бежит за игроком только если тот его атаковал
    }

    [Header("Тип поведения")]
    public EnemyType enemyType = EnemyType.Aggressive;

    [Header("Ссылки и статы")]
    public Transform player;
    public EnemyStats enemyStats;
    public Transform[] patrolPoints;

    [Header("Событие при убийстве игрока")]
    [Tooltip("Этот объект включится на 5 секунд, если именно этот враг убьет игрока")]
    public GameObject objectOnPlayerKill;
    public float showObjectDuration = 5f;

    private PlayerStats playerStats;

    [Header("Настройки обзора и погони")]
    public float sightRange = 12f;
    public float loseTargetRange = 20f;
    public float attackRange = 2f;
    public float waitTimeAtPoint = 2f;

    [Header("Настройки атаки")]
    public float attackCooldown = 1.5f;
    public float damageDelay = 0.4f;
    private float nextAttackTime = 0f;

    [Header("Скорость")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 5f;
    public float rotationSpeed = 6f;

    // --- Свойства и события для EnemyAnimator ---
    public bool IsWalking { get; private set; }
    public bool IsRunning { get; private set; }
    
    public event Action OnAttackTriggered;
    public event Action OnHitTriggered;
    // --------------------------------------------

    private NavMeshAgent agent;
    private int currentPatrolIndex = -1;
    private float waitTimer = 0f;
    private bool isProvoked = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyStats == null)
            enemyStats = GetComponentInParent<EnemyStats>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player != null)
        {
            playerStats = player.GetComponentInParent<PlayerStats>();
        }
    }

    private void Start()
    {
        if (objectOnPlayerKill != null)
        {
            objectOnPlayerKill.SetActive(false);
        }

        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (ShouldChasePlayer(distanceToPlayer))
        {
            ChasePlayer(distanceToPlayer);
        }
        else
        {
            Patrol();
        }
    }

    private void SetMovementState(bool walking, bool running)
    {
        IsWalking = walking;
        IsRunning = running;
    }

    private bool ShouldChasePlayer(float distance)
    {
        if (playerStats != null && playerStats.isDead)
        {
            isProvoked = false;
            return false;
        }

        if (distance > loseTargetRange)
        {
            isProvoked = false;
            return false;
        }

        if (enemyType == EnemyType.Aggressive)
        {
            return distance <= sightRange;
        }

        if (enemyType == EnemyType.Neutral)
        {
            return isProvoked;
        }

        return false;
    }

    private void ChasePlayer(float distance)
    {
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange;

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            LookAtPlayer();
            
            SetMovementState(false, true); // Бег
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            LookAtPlayer();
            
            SetMovementState(false, false); // Стоит на месте
            TryAttackPlayer();
        }
    }

    private void TryAttackPlayer()
    {
        if (playerStats != null && playerStats.isDead) return;

        if (Time.time >= nextAttackTime)
        {
            float actualCooldown = (enemyStats != null && enemyStats.AtkSpped_Enemy > 0) 
                ? (attackCooldown / enemyStats.AtkSpped_Enemy) 
                : attackCooldown;

            nextAttackTime = Time.time + actualCooldown;

            // Сообщаем аниматору о начале атаки
            OnAttackTriggered?.Invoke();

            StartCoroutine(DealDamageWithDelay());
        }
    }

    private IEnumerator DealDamageWithDelay()
    {
        yield return new WaitForSeconds(damageDelay);

        if (player == null || (playerStats != null && playerStats.isDead)) yield break;

        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= attackRange + 0.5f)
        {
            int enemyAtk = enemyStats != null ? enemyStats.AtkDmg_Enemy : 10;
            int playerDef = playerStats != null ? playerStats.Def_Player : 0;

            int finalDamage = enemyStats != null 
                ? enemyStats.CalculationAtackDamageEnemy(enemyAtk, playerDef) 
                : Mathf.Max(1, enemyAtk - playerDef);

            if (enemyStats != null && playerStats != null)
            {
                int levelDifference = enemyStats.EnemyLevel - playerStats.currentLevelPlayer;

                if (levelDifference >= 5)
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * 1.7f);
                }
                else if (levelDifference <= -10)
                {
                    finalDamage = 0;
                }
                else if (levelDifference < 0)
                {
                    int levelsAboveEnemy = Mathf.Abs(levelDifference);
                    int damageReduction = levelsAboveEnemy * 3;
                    finalDamage = Mathf.Max(1, finalDamage - damageReduction);
                }
            }

            if (playerStats != null && finalDamage > 0)
            {
                playerStats.TakeDamage(finalDamage);

                if (playerStats.isDead && objectOnPlayerKill != null)
                {
                    StartCoroutine(ActivateObjectTemporarily(objectOnPlayerKill, showObjectDuration));
                }
            }
        }
    }

    private IEnumerator ActivateObjectTemporarily(GameObject targetObj, float duration)
    {
        targetObj.SetActive(true);
        yield return new WaitForSeconds(duration);

        if (targetObj != null)
        {
            targetObj.SetActive(false);
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            SetMovementState(false, false);
            return;
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            SetMovementState(false, false);

            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                waitTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
        else
        {
            agent.isStopped = false;
            SetMovementState(true, false);
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (patrolPoints.Length == 1)
        {
            currentPatrolIndex = 0;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            return;
        }

        int nextIndex = UnityEngine.Random.Range(0, patrolPoints.Length);

        if (nextIndex == currentPatrolIndex)
        {
            nextIndex = (nextIndex + 1) % patrolPoints.Length;
        }

        currentPatrolIndex = nextIndex;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void OnTakeDamage()
    {
        if (playerStats != null && playerStats.isDead) return;

        isProvoked = true;
        
        // Сообщаем аниматору о получении урона
        OnHitTriggered?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyType == EnemyType.Aggressive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
