using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreeTalent : MonoBehaviour
{
    [Header("Очки умений")]
    public int current_score;
    public TextMeshProUGUI scoreText;

    [Header("Сколько прибавлять за 1 очко")]
    public int damagePerc = 2;
    public int defPerc = 2;
    public int speedPerc = 1;
    public float atkSpeedPerc = 0.5f;
    public float maxAtkSpeed = 10f;

    [Header("Кнопки")]
    public Button Damage_Button;
    public Button Def_Button;
    public Button Speed_Button;
    public Button AtkSpeed_Button;

    [Header("PlayerStats")]
    public PlayerStats playerStats;

    private void Start()
    {
        if (Damage_Button != null)
            Damage_Button.onClick.AddListener(UpgradeDamage);

        if (Def_Button != null)
            Def_Button.onClick.AddListener(UpgradeDefense);

        if (Speed_Button != null)
            Speed_Button.onClick.AddListener(UpgradeSpeed);

        if (AtkSpeed_Button != null)
            AtkSpeed_Button.onClick.AddListener(UpgradeAtkSpeed);

        UpdateTalentUI();
    }

    public void UpgradeDamage()
    {
        if (!TrySpendPoint()) return;

        playerStats.AtkDmg_Player += damagePerc;
        ApplyChanges();
    }

    public void UpgradeDefense()
    {
        if (!TrySpendPoint()) return;

        playerStats.Def_Player += defPerc;
        ApplyChanges();
    }

    public void UpgradeSpeed()
    {
        if (!TrySpendPoint()) return;

        playerStats.Speed_Player += speedPerc;
        if (playerStats.playerController != null)
        {
            playerStats.playerController.runSpeed = playerStats.Speed_Player;
        }
        ApplyChanges();
    }

    public void UpgradeAtkSpeed()
    {
        if (playerStats == null || playerStats.AtkSpped_Player >= maxAtkSpeed) return;
        if (!TrySpendPoint()) return;

        playerStats.AtkSpped_Player = Mathf.Min(maxAtkSpeed, playerStats.AtkSpped_Player + atkSpeedPerc);
        playerStats.ApplyAttackSpeed();
        ApplyChanges();
    }

    public void AddSkillPoints(int amount = 1)
    {
        current_score += amount;
        UpdateTalentUI();
    }

    private bool TrySpendPoint()
    {
        if (current_score <= 0 || playerStats == null)
            return false;

        current_score--;
        return true;
    }

    private void ApplyChanges()
    {
        playerStats.UpdatePlayerStatsUI();
        UpdateTalentUI();
    }

    public void UpdateTalentUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Очки умений: {current_score}";
        }

        bool hasPoints = current_score > 0;

        if (Damage_Button != null) Damage_Button.interactable = hasPoints;
        if (Def_Button != null) Def_Button.interactable = hasPoints;
        if (Speed_Button != null) Speed_Button.interactable = hasPoints;
        if (AtkSpeed_Button != null)
            AtkSpeed_Button.interactable = hasPoints && (playerStats != null && playerStats.AtkSpped_Player < maxAtkSpeed);
    }
}
