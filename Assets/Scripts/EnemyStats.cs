using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyStats : MonoBehaviour
{
    [Header("Здоровье врага")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead;

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

    private void Awake()
    {
        enemyAI = GetComponentInParent<Enemy>();

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Прячем полоску здоровья на старте игры
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(false);
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

        // Включаем полоску здоровья при первом получении урона
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
