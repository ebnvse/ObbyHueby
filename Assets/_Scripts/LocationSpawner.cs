using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationSpawner : MonoBehaviour
{
    [Header("Кого спавним в этой локации")]
    [Tooltip("Префабы врагов со скриптом Enemy (или дочерним объектом с ним)")]
    public GameObject[] locationMonsters;

    [Header("Маршрут и точки спавна")]
    [Tooltip("Точки появления и патрулирования для этой локации")]
    public Transform[] waypoints;

    [Header("Настройки спавна")]
    public int maxMonstersInLocation = 5;
    public float spawnInterval = 4f;
    [Tooltip("Спавнить только когда игрок рядом (оптимизация)")]
    public bool spawnOnlyWhenPlayerNear = true;
    public float activationRadius = 40f;
    [Tooltip("Дополнительный запас расстояния перед удалением мобов, чтобы они не мигали на границе сферы")]
    public float despawnBuffer = 5f;

    [Header("Опционально: ссылки со сцены")]
    [Tooltip("Объект, который включится при смерти игрока от рук заспавненного врага")]
    public GameObject objectOnPlayerKill;

    private List<GameObject> spawnedMonsters = new List<GameObject>();
    private Transform player;
    public Billboard3DTextManager billboard3DTextManager;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Очищаем список от уже убитых врагов
            spawnedMonsters.RemoveAll(m => m == null);

            if (spawnOnlyWhenPlayerNear && player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                // Если игрок ушел за пределы сферы (+ буфер) — удаляем всех мобов этой локации
                if (distanceToPlayer > activationRadius + despawnBuffer)
                {
                    if (spawnedMonsters.Count > 0)
                    {
                        DespawnAllMonsters();
                    }
                    continue;
                }

                // Если игрок еще не вошел в зону активации — не спавним
                if (distanceToPlayer > activationRadius)
                {
                    continue;
                }
            }

            if (spawnedMonsters.Count < maxMonstersInLocation)
            {
                SpawnMonster();
            }
        }
    }

    public void DespawnAllMonsters()
    {
        for (int i = 0; i < spawnedMonsters.Count; i++)
        {
            if (spawnedMonsters[i] != null)
            {
                Destroy(spawnedMonsters[i]);
            }
        }

        spawnedMonsters.Clear();

        if (billboard3DTextManager != null)
        {
            billboard3DTextManager.RefreshList();
        }
    }

    private void SpawnMonster()
    {
        if (locationMonsters.Length == 0 || waypoints.Length == 0) return;

        GameObject prefab = locationMonsters[Random.Range(0, locationMonsters.Length)];
        Transform spawnPoint = waypoints[Random.Range(0, waypoints.Length)];

        GameObject spawnedObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        spawnedMonsters.Add(spawnedObj);

        Enemy enemyScript = spawnedObj.GetComponentInChildren<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.patrolPoints = waypoints;

            if (billboard3DTextManager != null)
            {
                billboard3DTextManager.RefreshList();
            }

            if (player != null)
            {
                enemyScript.player = player;
            }

            if (objectOnPlayerKill != null)
            {
                enemyScript.objectOnPlayerKill = objectOnPlayerKill;
            }
        }
        else
        {
            Debug.LogWarning($"На префабе {prefab.name} не найден скрипт Enemy!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Желтая сфера — радиус активации спавна
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        // Красная сфера — граница, за которой мобы удаляются
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationRadius + despawnBuffer);

        if (waypoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in waypoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.4f);
                }
            }
        }
    }
}
