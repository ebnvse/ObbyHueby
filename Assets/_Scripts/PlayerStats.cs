using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using YG;

public class PlayerStats : MonoBehaviour
{
    public static event Action OnStatsUpdated;

    [Header("Характеристики игрока")]
    public int AtkDmg_Player = 20;
    public int Def_Player = 10;
    public float AtkSpped_Player = 1f;
    public float baseAttackCooldown = 0.5f;
    public float Speed_Player = 5;
    public TreeTalent treeTalent;
    public PlayerController playerController;
    public AttackController attackController;
    public ParryController parryController;
    
    [Header("Критический урон")]
    public float Critical_Chance_multiplier = 1.5f;
    public float Critical_Chance = 0.1f;

    [Header("Здоровье игрока")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Управление и движение")]
    public MonoBehaviour movementScript; 

    [Header("Уровень и опыт")]
    public int NextLevelPlayer;
    public TextMeshProUGUI NextLevelPlayer_Text;
    public int currentLevelPlayer = 1;
    public int currentExp = 0;
    public int expToNextLevel = 50;
    public Image expFillImage;
    public TextMeshProUGUI currentLevelPlayer_Text;
    public TextMeshProUGUI currentExpPlayer_Text;

    [Header("Настройки прогрессии опыта")]
    public int baseExpReward = 100;
    [Range(0.5f, 1f)] public float expGainMultiplierPerLevel = 0.98f;

    [Header("Звуковые события (SoundEvents)")]
    public SoundEvent hurtVoiceSound;
    public SoundEvent critSound;
    public SoundEvent bodyImpactSound;
    public SoundEvent deathSound;
    public SoundEvent levelUpSound;

    [Header("Смерть и возрождение")]
    public GameObject deathPanel;
    public float respawnHeightY = 1f;

    [Header("UI Здоровья")]
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    [Header("UI Статистики персонажа")]
    public TextMeshProUGUI AtkDmg_Player_Text;
    public TextMeshProUGUI Def_Player_Text;
    public TextMeshProUGUI AtkSpped_Player_Text;
    public TextMeshProUGUI Speed_Player_Text;
    public TextMeshProUGUI Critical_Chance_Text;

    [Header("UI Статистики персонажа в панельке")]
    public TextMeshProUGUI Panel_AtkDmg_Player_Text;

    [Header("Всплывающий 2D урон")]
    public DamagePopup2D damagePopupPrefab;
    public Canvas mainUICanvas; 
    public RectTransform spawnPoint;

    private CharacterController characterController;
    private Rigidbody rb;
    public BuffManager buffManager;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        YG2.onGetSDKData += LoadStats;
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= LoadStats;
    }

    private void Start()
    {
        isDead = false;

        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (YG2.isSDKEnabled)
        {
            LoadStats();
        }
        else
        {
            ApplyLoadedStatsToGame();
        }
    }

    public void LoadStats()
    {
        if (YG2.saves.level > 0)
        {
            currentLevelPlayer = YG2.saves.level;
            currentExp = YG2.saves.currentExp;
            expToNextLevel = YG2.saves.expToNextLevel > 0 ? YG2.saves.expToNextLevel : 50;
            AtkDmg_Player = YG2.saves.AtkDmg_Player;
            Def_Player = YG2.saves.Def_Player;
            AtkSpped_Player = YG2.saves.AtkSpped_Player > 0 ? YG2.saves.AtkSpped_Player : 1f;
            Speed_Player = YG2.saves.Speed_Player > 0 ? YG2.saves.Speed_Player : 5f;
        }

        ApplyLoadedStatsToGame();
    }

    private void ApplyLoadedStatsToGame()
    {
        maxHealth = 100 + (currentLevelPlayer - 1) * 45;
        currentHealth = maxHealth;
        NextLevelPlayer = currentLevelPlayer + 1;

        ApplyAttackSpeed();
        UpdateHealthUI();
        UpdatePlayerStatsUI();
        UpdateExpUIImmediate();

        PlayerEvents.InvokePlayerLevelChanged(currentLevelPlayer);
        OnStatsUpdated?.Invoke();
    }

    public void SaveStats()
    {
        int bonusAtk = buffManager != null ? buffManager.activeBonusAtk : 0;
        int bonusDef = buffManager != null ? buffManager.activeBonusDef : 0;
        float bonusAtkSpeed = buffManager != null ? buffManager.activeBonusAtkSpeed : 0f;
        float bonusSpeed = buffManager != null ? buffManager.activeBonusSpeed : 0f;
        
        YG2.saves.level = currentLevelPlayer;
        YG2.saves.currentExp = currentExp;
        YG2.saves.expToNextLevel = expToNextLevel;
        YG2.saves.AtkDmg_Player = AtkDmg_Player;
        YG2.saves.Def_Player = Def_Player;
        YG2.saves.AtkSpped_Player = AtkSpped_Player;
        YG2.saves.Speed_Player = Speed_Player;

        YG2.SaveProgress();
        OnStatsUpdated?.Invoke();
    }

    public void ApplyAttackSpeed()
    {
        if (attackController == null) return;

        float safeSpeed = Mathf.Clamp(AtkSpped_Player, 1f, 10f);
        attackController.attackCooldown = baseAttackCooldown / safeSpeed;
    }

    public int CalculationAtackDamagePlayer(int Enemy_Def, int Player_AtackDamage, float critChanse, float critMultipler, int enemyLevel, float damageMultiplier = 1f)
    {
        int levelDifference = currentLevelPlayer - enemyLevel;
        int levelBonusDamage = levelDifference > 0 ? levelDifference * 5 : 0;
        int totalAttack = Player_AtackDamage + levelBonusDamage;

        bool isCrit = UnityEngine.Random.value < critChanse;
        int damage;

        if (isCrit)
        {
            if (critSound != null) critSound.Play(transform.position, 0.4f);
            int critDamage = Mathf.RoundToInt(totalAttack * critMultipler);
            damage = critDamage - Enemy_Def;
        }
        else
        {
            damage = totalAttack - Enemy_Def;
        }

        int finalDamage = Mathf.Max(1, damage);

        if (levelDifference >= 5)
            finalDamage = Mathf.RoundToInt(finalDamage * 1.7f);

        finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * damageMultiplier));
        ShowDamagePopup(finalDamage, isCrit);

        return finalDamage;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || currentHealth <= 0) return;

        if (parryController != null && parryController.IsParrying)
        {
            parryController.PlayParrySound();
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthUI();

        if (bodyImpactSound != null) bodyImpactSound.Play(transform.position);

        if (currentHealth <= 0)
            Die();
        else if (hurtVoiceSound != null)
            hurtVoiceSound.Play(transform.position);
    }

    public void UpdateHealthUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = (float)currentHealth / maxHealth;

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }

    private void Die()
    {
        isDead = true;
        if (movementScript != null) movementScript.enabled = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (deathSound != null) deathSound.Play(transform.position);
        else if (hurtVoiceSound != null) hurtVoiceSound.Play(transform.position);

        if (deathPanel != null) deathPanel.SetActive(true);
    }

    public void RespawnButton()
    {
        TeleportToSpawn();
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        if (movementScript != null) movementScript.enabled = true;
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    public void TeleportToSpawn()
    {
        Vector3 targetPosition = new Vector3(0f, respawnHeightY, 0f);

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = targetPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = targetPosition;
        }

        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    public void UpdatePlayerStatsUI()
    {
        if (AtkDmg_Player_Text != null) AtkDmg_Player_Text.text = $"Атака: {AtkDmg_Player}";
        if (Def_Player_Text != null) Def_Player_Text.text = $"Защита: {Def_Player}";
        if (AtkSpped_Player_Text != null) AtkSpped_Player_Text.text = $"Скорость атаки: {AtkSpped_Player:0.#}";
        if (Speed_Player_Text != null) Speed_Player_Text.text = $"Скорость бега: {Speed_Player}";
        
        int critPercent = Mathf.RoundToInt(Critical_Chance * 100f);
        if (Critical_Chance_Text != null) Critical_Chance_Text.text = $"Шанс Крит. Атаки: {critPercent}%";
        if (Panel_AtkDmg_Player_Text != null) Panel_AtkDmg_Player_Text.text = $"Атака: {AtkDmg_Player}";
    }

    public void ShowDamagePopup(int damage, bool isCrit)
    {
        if (damagePopupPrefab == null || mainUICanvas == null) return;

        DamagePopup2D popup = Instantiate(damagePopupPrefab, mainUICanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchoredPosition = spawnPoint != null ? spawnPoint.anchoredPosition : Vector2.zero;
        popup.Setup(damage, isCrit);
    }

    public void UpdateExpUIImmediate()
    {
        if (expToNextLevel <= 0) return;

        float ratio = Mathf.Clamp01((float)currentExp / expToNextLevel);
        if (expFillImage != null) expFillImage.fillAmount = ratio;
        if (currentExpPlayer_Text != null) currentExpPlayer_Text.text = $"{Mathf.RoundToInt(ratio * 100f)}%";

        UpdateLevelTexts();
    }

    private void UpdateLevelTexts()
    {
        if (currentLevelPlayer_Text != null) currentLevelPlayer_Text.text = currentLevelPlayer.ToString();
        if (NextLevelPlayer_Text != null) NextLevelPlayer_Text.text = NextLevelPlayer.ToString();
    }

    public void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevelPlayer++;
        NextLevelPlayer = currentLevelPlayer + 1;
        expToNextLevel += 10;

        if (currentLevelPlayer % 5 == 0)
            treeTalent?.AddSkillPoints(1);

        maxHealth += 45;
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (levelUpSound != null) levelUpSound.Play2D();

        PlayerEvents.InvokePlayerLevelChanged(currentLevelPlayer);
        SaveStats();
    }

    public void AddExpToPlayer(int enemyLevel)
    {
        float penaltyFactor = Mathf.Pow(expGainMultiplierPerLevel, currentLevelPlayer - 1);
        float rawExp = baseExpReward * penaltyFactor;

        int levelDiff = currentLevelPlayer - enemyLevel;

        if (levelDiff >= 7) return;
        else if (levelDiff >= 5) rawExp *= 0.1f;
        else if (levelDiff >= 3) rawExp *= 0.7f;

        int gainedExp = Mathf.Max(1, Mathf.RoundToInt(rawExp));
        currentExp += gainedExp;

        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }

        UpdateExpUIImmediate();
    }
}
