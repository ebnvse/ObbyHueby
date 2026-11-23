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

            if (spawnOnlyWhenPlayerNear && player != null)
            {
                if (Vector3.Distance(transform.position, player.position) > activationRadius)
                    continue;
            }

            // Очищаем список от убитых врагов
            spawnedMonsters.RemoveAll(m => m == null);

            if (spawnedMonsters.Count < maxMonstersInLocation)
            {
                SpawnMonster();
            }
        }
    }

    private void SpawnMonster()
    {
        if (locationMonsters.Length == 0 || waypoints.Length == 0) return;
        

        // Выбираем случайного врага и случайную стартовую точку
        GameObject prefab = locationMonsters[Random.Range(0, locationMonsters.Length)];
        Transform spawnPoint = waypoints[Random.Range(0, waypoints.Length)];

        GameObject spawnedObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        spawnedMonsters.Add(spawnedObj);

        // Ищем ваш скрипт Enemy (на корневом объекте или в дочерних)
        Enemy enemyScript = spawnedObj.GetComponentInChildren<Enemy>();
        if (enemyScript != null)
        {
            // Передаем точки патрулирования этой локации
            enemyScript.patrolPoints = waypoints;
            billboard3DTextManager.RefreshList();

            // Передаем игрока, чтобы враг не искал его каждый раз через FindGameObjectWithTag
            if (player != null)
            {
                enemyScript.player = player;
            }

            // Передаем объект со сцены, если он назначен в спавнере
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

    // Отрисовка радиуса активации и точек в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

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
