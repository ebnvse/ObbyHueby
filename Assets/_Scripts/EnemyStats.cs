using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyStats : MonoBehaviour
{
    [Header("Уровень")]
    public int EnemyLevel;
    public int levelDifferenceThreshold = 5;

    [Header("Здоровье врага")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead;
    public TextMeshProUGUI textEnemyLevel;

    [Header("UI Здоровья врага")]
    [Tooltip("Родительский объект всей полоски ХП (Canvas или фон), который прячется до первого удара")]
    public GameObject healthBarObject; 
    public Image healthFillImage;        // Image с типом Filled над головой врага
    public TextMeshProUGUI healthText;   // Текст здоровья врага

    [Header("Характеристики врага")]
    public int AtkDmg_Enemy = 15;
    public int Def_Enemy = 5;
    public int AtkSpped_Enemy = 1;
    public int Speed_Enemy = 5;

    [Header("Звуки врага")]
    public AudioSource audioSource;        // Найдется сам, если висит на враге
    public AudioClip[] enemyHurtSounds;    // Звуки получения урона врагом
    public AudioClip[] enemyDeathSounds;   // Звуки смерти врага

    private Enemy enemyAI;
    private PlayerStats playerStats;

    private void Awake()
    {
        enemyAI = GetComponentInParent<Enemy>();

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void OnEnable()
    {
        PlayerEvents.OnPlayerLevelChanged += UpdateLevelColor;
    }

    private void OnDisable()
    {
        PlayerEvents.OnPlayerLevelChanged -= UpdateLevelColor;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (healthBarObject != null)
        {
            healthBarObject.SetActive(false);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponentInParent<PlayerStats>();
        }

        int playerLvl = playerStats != null ? playerStats.currentLevelPlayer : 1;
        UpdateLevelColor(playerLvl);
    }

    public void UpdateLevelColor(int playerLevel)
    {
        if (textEnemyLevel == null) return;

        textEnemyLevel.gameObject.SetActive(true);
        textEnemyLevel.text = $"Уровень {EnemyLevel}";

        int levelDifference = EnemyLevel - playerLevel;

        // Красивый светло-синий (голубой) цвет, который хорошо читается на любом фоне
        Color lightBlue = new Color(0.3f, 0.75f, 1f);

        if (levelDifference < 0)
        {
            // Враг младше: плавный переход от белого (0) к светло-синему (-5 и ниже)
            float t = Mathf.Clamp01(Mathf.Abs(levelDifference) / 5f);
            textEnemyLevel.color = Color.Lerp(Color.white, lightBlue, t);
        }
        else if (levelDifference > 0)
        {
            // Враг старше: плавный переход от белого (0) к красному (+5 и выше)
            float t = Mathf.Clamp01(levelDifference / 5f);
            textEnemyLevel.color = Color.Lerp(Color.white, Color.red, t);
        }
        else
        {
            // Уровни полностью равны -> чисто белый
            textEnemyLevel.color = Color.white;
        }
    }

    public int CalculationAtackDamageEnemy(int Enemy_Atack, int Player_Def)
    {
        int damage = Enemy_Atack - Player_Def;
        return Mathf.Max(1, damage);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBarObject != null && !healthBarObject.activeSelf)
        {
            healthBarObject.SetActive(true);
        }

        UpdateHealthUI();
        Debug.Log($"Враг {gameObject.name} получил {damage} урона! Осталось ХП: {currentHealth}/{maxHealth}");

        if (enemyAI != null)
        {
            enemyAI.OnTakeDamage();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlayHurtSound();
        }
    }

    private void PlayHurtSound()
    {
        if (enemyHurtSounds != null && enemyHurtSounds.Length > 0)
        {
            AudioClip clip = enemyHurtSounds[Random.Range(0, enemyHurtSounds.Length)];

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
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
        Debug.Log($"Враг {gameObject.name} повержен!");

        if (enemyDeathSounds != null && enemyDeathSounds.Length > 0)
        {
            AudioClip clip = enemyDeathSounds[Random.Range(0, enemyDeathSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        Destroy(gameObject);
    }
}
