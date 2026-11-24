using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Характеристики игрока")]
    public int AtkDmg_Player = 20;
    public int Def_Player = 10;
    public float AtkSpped_Player = 1f; // Шкала для игрока от 1 до 10
    public float baseAttackCooldown = 0.5f; // Кулдаун при скорости 1
    public int Speed_Player = 5;
    public TreeTalent treeTalent;
    public PlayerController playerController;
    public AttackController attackController;
    public ParryController parryController;
    
    [Header("Критический урон")]
    public float Critical_Chance_multiplier = 1.5f; // Множитель должен быть > 1 (например, 1.5 = +50% урона)
    public float Critical_Chance = 0.1f;            // 0.1 = 10% шанс крита

    [Header("Здоровье игрока")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Управление и движение")]
    [Tooltip("Перетащи сюда свой скрипт передвижения игрока, чтобы он отключался при смерти")]
    public MonoBehaviour movementScript; 

    [Header("Уровень и опыт")]
    public int NextLevelPlayer; // цифра возле хп
    public TextMeshProUGUI NextLevelPlayer_Text; // цифра возле хп
    public int currentLevelPlayer = 1;
    public int currentExp = 0;
    public int expToNextLevel = 50;
    public Image expFillImage; // Полоска заполнения опыта
    public TextMeshProUGUI currentLevelPlayer_Text;
    public TextMeshProUGUI currentExpPlayer_Text;

    [Header("Настройки прогрессии опыта")]
    [Tooltip("Базовый опыт за убийство врага на 1 уровне")]
    public int baseExpReward = 25;
    [Tooltip("На сколько снижается получаемый опыт с каждым уровнем (0.9 = каждый лвл дает на 10% меньше опыта)")]
    [Range(0.5f, 1f)] public float expGainMultiplierPerLevel = 0.9f;

    [Header("Звуковые события (SoundEvents)")]
    public SoundEvent hurtVoiceSound;     // Голос: вскрики / кряхтение при боли
    public SoundEvent critSound;          // Звук критического удара игрока
    public SoundEvent bodyImpactSound;    // Звук самого попадания по телу / броне игрока
    public SoundEvent deathSound;         // Звук смерти игрока
    public SoundEvent levelUpSound;       // Звук повышения уровня

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

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        NextLevelPlayer = currentLevelPlayer + 1;

        ApplyAttackSpeed();
        UpdateHealthUI();
        UpdatePlayerStatsUI();
        UpdateExpUIImmediate();

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
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

        // Если игрок старше врага — добавляем +5 к атаке за каждый уровень превосходства
        int levelBonusDamage = levelDifference > 0 ? levelDifference * 5 : 0;
        int totalAttack = Player_AtackDamage + levelBonusDamage;

        bool isCrit = UnityEngine.Random.value < critChanse;
        int damage;

        if (isCrit)
        {
            if (critSound != null)
            {
                critSound.Play(transform.position, 0.4f);
            }

            int critDamage = Mathf.RoundToInt(totalAttack * critMultipler);
            damage = critDamage - Enemy_Def;
        }
        else
        {
            damage = totalAttack - Enemy_Def;
        }

        int finalDamage = Mathf.Max(1, damage);

        // Ваш существующий бонус +70%, если игрок старше врага на 5 и более уровней
        if (levelDifference >= 5)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 1.7f);
        }

        finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * damageMultiplier));

        ShowDamagePopup(finalDamage, isCrit);

        return finalDamage;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || currentHealth <= 0) return;

        if(parryController.IsParrying)
        {
            parryController.PlayParrySound();
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();
        Debug.Log($"Игрок получил {damage} урона! Осталось ХП: {currentHealth}/{maxHealth}");

        if (bodyImpactSound != null)
        {
            bodyImpactSound.Play(transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (hurtVoiceSound != null)
            {
                hurtVoiceSound.Play(transform.position);
            }
        }
    }

    public void UpdateHealthUI()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Игрок погиб!");

        if (movementScript != null)
        {
            movementScript.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (deathSound != null)
        {
            deathSound.Play(transform.position);
        }
        else if (hurtVoiceSound != null)
        {
            hurtVoiceSound.Play(transform.position);
        }

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
    }

    public void RespawnButton()
    {
        TeleportToSpawn();

        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        if (movementScript != null)
        {
            movementScript.enabled = true;
        }

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        Debug.Log("Игрок телепортирован в (0, 0)!");
    }

    public void UpdatePlayerStatsUI()
    {
        AtkDmg_Player_Text.text = $"Атака:" + AtkDmg_Player.ToString();
        Def_Player_Text.text = $"Защита:" + Def_Player.ToString();
        AtkSpped_Player_Text.text = $"Скорость атаки: {AtkSpped_Player:0.#}";
        Speed_Player_Text.text = $"Скорость бега:" + Speed_Player.ToString();
        int critPercent = Mathf.RoundToInt(Critical_Chance * 100f);
        Critical_Chance_Text.text = $"Шанс Крит. Атаки: {critPercent}%"; 

        Panel_AtkDmg_Player_Text.text = $"Атака:" + AtkDmg_Player.ToString();
    }

    public void ShowDamagePopup(int damage, bool isCrit)
    {
        if (damagePopupPrefab == null || mainUICanvas == null) return;

        DamagePopup2D popup = Instantiate(damagePopupPrefab, mainUICanvas.transform);

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (spawnPoint != null)
        {
            popupRect.anchoredPosition = spawnPoint.anchoredPosition;
        }
        else
        {
            popupRect.anchoredPosition = Vector2.zero;
        }

        popup.Setup(damage, isCrit);
    }

    public void UpdateExpUIImmediate()
    {
        if (expToNextLevel <= 0) return;

        float ratio = Mathf.Clamp01((float)currentExp / expToNextLevel);

        if (expFillImage != null)
            expFillImage.fillAmount = ratio;

        if (currentExpPlayer_Text != null)
            currentExpPlayer_Text.text = $"{Mathf.RoundToInt(ratio * 100f)}%";

        UpdateLevelTexts();
    }

    private void UpdateLevelTexts()
    {
        if (currentLevelPlayer_Text != null)
            currentLevelPlayer_Text.text = currentLevelPlayer.ToString();

        if (NextLevelPlayer_Text != null)
            NextLevelPlayer_Text.text = NextLevelPlayer.ToString();
    }

    public void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevelPlayer++;
        NextLevelPlayer = currentLevelPlayer + 1;
        expToNextLevel += 10;
        treeTalent?.AddSkillPoints(1);

        maxHealth += 15;
        currentHealth += 15;
        UpdateHealthUI();

        if (levelUpSound != null)
        {
            levelUpSound.Play2D();
        }
    }

    public void AddExpToPlayer(int enemyLevel)
    {
        float penaltyFactor = Mathf.Pow(expGainMultiplierPerLevel, currentLevelPlayer - 1);
        int gainedExp = Mathf.Max(1, Mathf.RoundToInt(baseExpReward * penaltyFactor));

        if (currentLevelPlayer >= enemyLevel + 10)
        {
            return;
        }
        else if (currentLevelPlayer >= enemyLevel + 4)
        {
            gainedExp = Mathf.Max(1, Mathf.RoundToInt(gainedExp * 0.2f)); // срезаем 80%
        }

        currentExp += gainedExp;

        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }

        UpdateExpUIImmediate();
    }
}
