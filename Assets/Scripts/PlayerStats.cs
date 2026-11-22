using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerStats : MonoBehaviour
{
    [Header("Характеристики игрока")]
    public int AtkDmg_Player = 20;
    public int Def_Player = 10;
    public float AtkSpped_Player = 1;
    public int Speed_Player = 5;
    public TreeTalent treeTalent;
    public PlayerController playerController;
    
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

    [Header("Настройки прогрессии и анимации опыта")]
    [Tooltip("Базовый опыт за убийство врага на 1 уровне")]
    public int baseExpReward = 25;
    [Tooltip("На сколько снижается получаемый опыт с каждым уровнем (0.9 = каждый лвл дает на 10% меньше опыта)")]
    [Range(0.5f, 1f)] public float expGainMultiplierPerLevel = 0.9f;
    [Tooltip("Скорость плавного заполнения полоски опыта")]
    public float expFillSpeed = 3f;

    private Coroutine expAnimationCoroutine;

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

        UpdateHealthUI();
        UpdatePlayerStatsUI();
        UpdateExpUIImmediate();

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    public int CalculationAtackDamagePlayer(int Enemy_Def, int Player_AtackDamage, float critChanse, float critMultipler)
    {
        bool isCrit = Random.value < critChanse;
        int damage;

        if (isCrit)
        {
            if (critSound != null)
            {
                critSound.Play(transform.position, 0.4f);
            }

            int critDamage = Mathf.RoundToInt(Player_AtackDamage * critMultipler);
            damage = critDamage - Enemy_Def;
        }
        else
        {
            damage = Player_AtackDamage - Enemy_Def;
        }

        int finalDamage = Mathf.Max(1, damage);
        ShowDamagePopup(finalDamage, isCrit);

        return finalDamage;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || currentHealth <= 0) return;

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
        AtkSpped_Player_Text.text = $"Скорость атаки:" + AtkSpped_Player.ToString();
        Speed_Player_Text.text = $"Скорость бега:" + Speed_Player.ToString();
        int critPercent = Mathf.RoundToInt(Critical_Chance * 100f);
        Critical_Chance_Text.text = $"Шанс Крит. Атаки: {critPercent}%";
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

        if (levelUpSound != null)
        {
            levelUpSound.Play2D();
        }
    }

    public void AddExpToPlayer()
    {
        float penaltyFactor = Mathf.Pow(expGainMultiplierPerLevel, currentLevelPlayer - 1);
        int gainedExp = Mathf.Max(1, Mathf.RoundToInt(baseExpReward * penaltyFactor));

        currentExp += gainedExp;

        int levelsGained = 0;
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
            levelsGained++;
        }

        float targetRatio = expToNextLevel > 0 ? Mathf.Clamp01((float)currentExp / expToNextLevel) : 0f;

        if (expAnimationCoroutine != null)
        {
            StopCoroutine(expAnimationCoroutine);
        }
        expAnimationCoroutine = StartCoroutine(AnimateExpBarRoutine(levelsGained, targetRatio));
    }

    private IEnumerator AnimateExpBarRoutine(int levelsGained, float targetRatio)
    {
        for (int i = 0; i < levelsGained; i++)
        {
            yield return StartCoroutine(FillBarToTarget(1f));

            if (expFillImage != null)
                expFillImage.fillAmount = 0f;

            if (currentExpPlayer_Text != null)
                currentExpPlayer_Text.text = "0%";
        }

        UpdateLevelTexts();

        yield return StartCoroutine(FillBarToTarget(targetRatio));
    }

    private IEnumerator FillBarToTarget(float targetFill)
    {
        if (expFillImage == null)
        {
            if (currentExpPlayer_Text != null)
                currentExpPlayer_Text.text = $"{Mathf.RoundToInt(targetFill * 100f)}%";
            yield break;
        }

        while (!Mathf.Approximately(expFillImage.fillAmount, targetFill))
        {
            expFillImage.fillAmount = Mathf.MoveTowards(
                expFillImage.fillAmount,
                targetFill,
                expFillSpeed * Time.deltaTime
            );

            if (currentExpPlayer_Text != null)
            {
                currentExpPlayer_Text.text = $"{Mathf.RoundToInt(expFillImage.fillAmount * 100f)}%";
            }

            yield return null;
        }

        expFillImage.fillAmount = targetFill;
        if (currentExpPlayer_Text != null)
        {
            currentExpPlayer_Text.text = $"{Mathf.RoundToInt(targetFill * 100f)}%";
        }
    }
}
