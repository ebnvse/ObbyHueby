using UnityEngine;

public class Target : MonoBehaviour
{
    private EnemyStats enemyStats;

    private void Awake()
    {
        enemyStats = GetComponentInParent<EnemyStats>();
    }

    public void TakeDamage(int damage)
    {
        if (enemyStats != null)
        {
            enemyStats.TakeDamage(damage);
        }
    }
}
