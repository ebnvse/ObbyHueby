using UnityEngine;

public class BuyPets : MonoBehaviour
{
    public Money money;
    public GameObject PetOfAtackSpeed;
    public PlayerStats playerStats;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"));

        AttackController attackController = other.gameObject.GetComponentInChildren<AttackController>();
        

        Money money = other.gameObject.GetComponentInChildren<Money>();
        
        if(money.current_money >= 2000)
        {
            money.current_money -= 2000;
            PetOfAtackSpeed.SetActive(true);
            //attackController.attackCooldown = 0.2f;
            playerStats.AtkSpped_Player = Mathf.Clamp(playerStats.AtkSpped_Player + 5, 1f, 10f);
            playerStats.ApplyAttackSpeed();      // Пересчитываем реальный кулдаун
            playerStats.UpdatePlayerStatsUI();
            
        }
        

    }
}
