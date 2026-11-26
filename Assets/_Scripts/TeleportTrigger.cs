using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Ищем PlayerStats на вошедшем объекте или его родителе
        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TeleportToSpawn();
        }
    }
}
