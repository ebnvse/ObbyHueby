using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Здоровье игрока")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Управление и движение")]
    [Tooltip("Перетащи сюда свой скрипт передвижения игрока, чтобы он отключался при смерти")]
    public MonoBehaviour movementScript; 

    [Header("Звуки игрока")]
    public AudioSource audioSource;
    public AudioClip[] hurtSounds;         // Голос: вскрики / кряхтение при боли
    public AudioClip[] bodyImpactSounds;   // Звуки самого попадания по телу / броне игрока
    public AudioClip[] deathSounds;        // Звуки смерти игрока

    [Header("Смерть и возрождение")]
    public GameObject deathPanel;
    public Animator animator;
    public string deathStateName = "Death";
    public int deathLayerIndex = 0;
    public float respawnHeightY = 1f;

    [Header("UI Здоровья")]
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    [Header("Характеристики игрока")]
    public int AtkDmg_Player = 20;
    public int Def_Player = 10;
    public int AtkSpped_Player = 1;
    public int Speed_Player = 5;

    private CharacterController characterController;
    private Rigidbody rb;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    public int CalculationAtackDamagePlayer(int Enemy_Def, int Player_AtackDamage)
    {
        int damage = Player_AtackDamage - Enemy_Def;
        return Mathf.Max(1, damage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();
        Debug.Log($"Игрок получил {damage} урона! Осталось ХП: {currentHealth}/{maxHealth}");

        // Звук физического удара по телу/броне играет при ЛЮБОМ попадании (даже смертельном)
        PlayBodyImpactSound();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Вскрик персонажа играет, если он выжил после удара
            PlayHurtSound();
        }
    }

    private void PlayBodyImpactSound()
    {
        if (audioSource != null && bodyImpactSounds != null && bodyImpactSounds.Length > 0)
        {
            AudioClip clip = bodyImpactSounds[Random.Range(0, bodyImpactSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayHurtSound()
    {
        if (audioSource != null && hurtSounds != null && hurtSounds.Length > 0)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayDeathSound()
    {
        if (audioSource != null && deathSounds != null && deathSounds.Length > 0)
        {
            AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        else
        {
            PlayHurtSound();
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
            rb.linearVelocity = Vector3.zero; // Для Unity 2022 и старше замени на rb.velocity = Vector3.zero;
        }

        PlayDeathSound();

        if (animator != null && !string.IsNullOrEmpty(deathStateName))
        {
            animator.Play(deathStateName, deathLayerIndex, 0f);
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

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
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
            rb.linearVelocity = Vector3.zero; // Для Unity 2022 и старше замени на rb.velocity = Vector3.zero;
        }

        Debug.Log("Игрок телепортирован в (0, 0)!");
    }
}
