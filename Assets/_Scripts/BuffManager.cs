using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using YG;

public class BuffManager : MonoBehaviour
{
    public PlayerStats playerStats;

    [Header("UI отображения активных бафов")]
    [Tooltip("Панель на Canvas с Horizontal Layout Group, куда будут спавниться иконки")]
    public Transform buffIconsContainer;
    [Tooltip("Префаб иконки бафа со скриптом BuffIconUI")]
    public BuffIconUI buffIconPrefab;

    [Header("Картинки (Sprite) для каждого бафа")]
    public Sprite speedBuffSprite;
    public Sprite attackSpeedBuffSprite;
    public Sprite attackBuffSprite;
    public Sprite defBuffSprite;

    [Header("ID рекламы за вознаграждение")]
    public string rewardID1 = "SpeedBoost";
    public string rewardID2 = "AttackSpeed";
    public string rewardID3 = "AttackBoost";
    public string rewardID4 = "DefBoost";

    [Header("Текущие активные бонусы")]
    public int activeBonusAtk = 0;
    public int activeBonusDef = 0;
    public float activeBonusAtkSpeed = 0f;
    public float activeBonusSpeed = 0f;

    [Header("Кнопки бафов (0-Скорость, 1-Скор.атаки, 2-Урон, 3-Защита)")]
    public List<Button> buttons = new List<Button>();

    private readonly HashSet<string> activeBuffs = new HashSet<string>();

    public void TakePlayerBuff(string buffName, float value, float time)
    {
        if (playerStats == null || activeBuffs.Contains(buffName)) return;

        StartCoroutine(BuffRoutine(buffName, value, time));
    }

    private IEnumerator BuffRoutine(string buffName, float value, float time)
    {
        activeBuffs.Add(buffName);

        // Спавним иконку бафа на панель
        SpawnBuffIcon(buffName, time);

        ApplyStat(buffName, value, true);
        yield return new WaitForSeconds(time);

        ApplyStat(buffName, -value, false);
        activeBuffs.Remove(buffName);
    }

    private void SpawnBuffIcon(string buffName, float duration)
    {
        if (buffIconPrefab == null || buffIconsContainer == null) return;

        Sprite iconToUse = null;
        switch (buffName)
        {
            case "SpeedBoost": iconToUse = speedBuffSprite; break;
            case "AttackSpeed": iconToUse = attackSpeedBuffSprite; break;
            case "AttackBoost": iconToUse = attackBuffSprite; break;
            case "DefBoost": iconToUse = defBuffSprite; break;
        }

        BuffIconUI spawnedIcon = Instantiate(buffIconPrefab, buffIconsContainer);
        spawnedIcon.Setup(iconToUse, duration);
    }

    private void ApplyStat(string buffName, float value, bool isBuffActive)
    {
        switch (buffName)
        {
            case "SpeedBoost":
                activeBonusSpeed += value;
                playerStats.Speed_Player += value;
                if (playerStats.playerController != null)
                {
                    playerStats.playerController.runSpeed += value; // Прибавляем напрямую, чтобы не сбивалась базовая скорость
                }
                SetButtonState(0, !isBuffActive);
                break;

            case "AttackSpeed":
                activeBonusAtkSpeed += value;
                playerStats.AtkSpped_Player += value;
                playerStats.ApplyAttackSpeed();
                SetButtonState(1, !isBuffActive);
                break;

            case "AttackBoost":
                int intAtk = Mathf.RoundToInt(value);
                activeBonusAtk += intAtk;
                playerStats.AtkDmg_Player += intAtk;
                SetButtonState(2, !isBuffActive);
                break;

            case "DefBoost":
                int intDef = Mathf.RoundToInt(value);
                activeBonusDef += intDef;
                playerStats.Def_Player += intDef;
                SetButtonState(3, !isBuffActive);
                break;
        }

        playerStats.UpdatePlayerStatsUI();
    }

    private void SetButtonState(int index, bool isInteractable)
    {
        if (buttons != null && index >= 0 && index < buttons.Count && buttons[index] != null)
        {
            buttons[index].interactable = isInteractable;
        }
    }

    public void BuffOfSpeed()
    {
        if (activeBuffs.Contains("SpeedBoost")) return;
        YG2.RewardedAdvShow(rewardID1, () => TakePlayerBuff("SpeedBoost", 30f, 60f));
    }

    public void BuffOfAtkSpeed()
    {
        if (activeBuffs.Contains("AttackSpeed")) return;
        YG2.RewardedAdvShow(rewardID2, () => TakePlayerBuff("AttackSpeed", 2f, 60f));
    }

    public void BuffOfDamage()
    {
        if (activeBuffs.Contains("AttackBoost")) return;
        YG2.RewardedAdvShow(rewardID3, () => TakePlayerBuff("AttackBoost", 120f, 60f));
    }

    public void BuffOfDef()
    {
        if (activeBuffs.Contains("DefBoost")) return;
        YG2.RewardedAdvShow(rewardID4, () => TakePlayerBuff("DefBoost", 25f, 60f));
    }
}
