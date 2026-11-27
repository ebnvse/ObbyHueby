using UnityEngine;

public class SquareZoneSpawner : MonoBehaviour
{
    // Создаем удобную связку "Точка + Враг" для инспектора
    [System.Serializable]
    public struct SpawnEntry
    {
        public Transform spawnPoint;   // Точка появления
        public GameObject enemyPrefab; // Конкретный враг для этой точки
    }

    [Header("Настройки спавна")]
    [SerializeField] private SpawnEntry[] spawnList; // Массив точек и врагов
    [SerializeField] private bool spawnOnce = true;  // Спавнить только один раз?

    private bool hasSpawned = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (spawnOnce && hasSpawned) return;

            SpawnEnemies();
            hasSpawned = true;
        }
    }

    private void SpawnEnemies()
    {
        if (spawnList == null || spawnList.Length == 0)
        {
            Debug.LogWarning("Список спавна пуст!");
            return;
        }

        // Проходим по каждому элементу массива и создаем соответствующего врага
        foreach (SpawnEntry entry in spawnList)
        {
            if (entry.enemyPrefab != null && entry.spawnPoint != null)
            {
                Instantiate(entry.enemyPrefab, entry.spawnPoint.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("В одном из элементов списка не назначен враг или точка спавна!");
            }
        }
    }
}