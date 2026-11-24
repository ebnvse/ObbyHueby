using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Sharpening : MonoBehaviour
{
    [Header("Ссылки на системы")]
    public PlayerStats playerStats;
    public Money money;
    public UISoundPlayer uISoundPlayer;

    [Header("UI элементы")]
    public TextMeshProUGUI currentUpgrade_Text;
    [Tooltip("Image с типом Filled (Horizontal / Radial), который заполняется от 0 до 1")]
    public Image progressFillImage;
    [Tooltip("Кнопка запуска заточки")]
    public Button sharpenButton;
    [Tooltip("Кнопка отмены заточки (всегда видна на экране)")]
    public Button cancelButton;
    public GameObject SeccsesSharpening_UiEffect;
    public GameObject FailedSharpening_UiEffect;

    [Header("Настройки заточки")]
    [Tooltip("Время заполнения полоски заточки в секундах")]
    public float sharpenDuration = 1f;
    [Tooltip("Микро-задержка перед следующей заточкой (защита от случайного двойного клика)")]
    public float postSharpenCooldown = 0.15f;
    public int currentUpgrade = 0;
    public int maxUpgrade = 16;   
    public int safeUpgrade = 3;    
    public int bonusPerLevel = 2;
    public int upgradeCost = 5;

    private readonly int[] upgradeChances = { 100, 100, 100, 95, 92, 90, 88, 85, 82, 80, 77, 75, 72, 70, 68, 65 };

    [Header("Свечение меча (через Material из Assets)")]
    public Material bladeAuraMaterial;
    public GameObject bladeAuraObject;
    [Range(0.1f, 1f)]
    public float auraAlpha = 0.85f;

    [Tooltip("Цвета для уровней от +0 до +16 (всего 17 элементов)")]
    [ColorUsage(true, true)]
    public Color[] upgradeGlowColors = new Color[]
    {
        Color.black, Color.black, Color.black, Color.black, // +0..+3 (выкл)
        Color.cyan, Color.cyan, // +4..+5
        Color.blue, Color.blue, Color.blue, Color.blue, Color.blue, Color.blue, // +6..+11
        Color.red, Color.red, Color.red, Color.red, // +12..+15
        new Color(1f, 0.2f, 0.8f) // +16
    };

    private bool isSharpening = false;
    private float sharpenTimer = 0f;
    private float nextAllowedSharpenTime = 0f;
    private int lastClickFrame = -1;
    private int totalAddedDamage = 0;

    private float successEffectTimer = 0f;
    private float failedEffectTimer = 0f;

    private void Start()
    {
        totalAddedDamage = currentUpgrade * bonusPerLevel;

        HideAllEffects();
        ResetProgressUI();
        UpdateUI();
        UpdateWeaponGlow();
    }

    private void Update()
    {
        if (successEffectTimer > 0f)
        {
            successEffectTimer -= Time.unscaledDeltaTime;
            if (successEffectTimer <= 0f && SeccsesSharpening_UiEffect != null)
            {
                SeccsesSharpening_UiEffect.SetActive(false);
            }
        }

        if (failedEffectTimer > 0f)
        {
            failedEffectTimer -= Time.unscaledDeltaTime;
            if (failedEffectTimer <= 0f && FailedSharpening_UiEffect != null)
            {
                FailedSharpening_UiEffect.SetActive(false);
            }
        }

        if (!isSharpening) return;

        float currentDuration = Mathf.Max(0.05f, sharpenDuration);
        sharpenTimer += Time.unscaledDeltaTime;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = Mathf.Clamp01(sharpenTimer / currentDuration);
        }

        if (sharpenTimer >= currentDuration)
        {
            FinishSharpening();
        }
    }

    public void SetSharpenDuration(float newDuration)
    {
        sharpenDuration = Mathf.Max(0.05f, newDuration);
    }

    public void ResetUpgrade()
    {
        currentUpgrade = 0;
        totalAddedDamage = 0;

        ResetProgressUI();
        HideAllEffects();
        UpdateWeaponGlow();
        UpdateUI();
    }

    public void StartSharpening()
    {
        if (Time.frameCount == lastClickFrame)
            return;
        lastClickFrame = Time.frameCount;

        if (isSharpening || Time.unscaledTime < nextAllowedSharpenTime)
            return;

        if (currentUpgrade >= maxUpgrade)
        {
            LogDebug($"Оружие уже достигло максимальной заточки (+{maxUpgrade})!");
            return;
        }

        if (money == null)
        {
            Debug.LogError("[Sharpening] Не назначен компонент Money!");
            return;
        }

        if (money.current_money < upgradeCost)
        {
            LogDebug("Недостаточно денег для заточки!");
            return;
        }

        if (uISoundPlayer != null)
        {
            uISoundPlayer.PlaySoundByIndex(4);
        }

        isSharpening = true;
        sharpenTimer = 0f;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        UpdateButtonsState();
    }

    public void CancelSharpening()
    {
        if (!isSharpening) return;

        ResetProgressUI();
        LogDebug("Заточка отменена игроком.");
    }

    private void FinishSharpening()
    {
        isSharpening = false;
        sharpenTimer = 0f;
        nextAllowedSharpenTime = Time.unscaledTime + postSharpenCooldown;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        ExecuteSharpening();

        if (playerStats != null)
        {
            playerStats.UpdatePlayerStatsUI();
        }

        UpdateButtonsState();
    }

    private void ExecuteSharpening()
    {
        if (currentUpgrade >= maxUpgrade || money == null)
            return;

        if (money.current_money < upgradeCost)
        {
            LogDebug("Недостаточно денег для заточки!");
            return;
        }

        money.current_money -= upgradeCost;

        int chanceIndex = Mathf.Clamp(currentUpgrade, 0, upgradeChances.Length - 1);
        int chance = upgradeChances[chanceIndex];
        int roll = Random.Range(1, 101);

        if (roll <= chance)
        {
            currentUpgrade++;
            
            if (playerStats != null)
            {
                playerStats.AtkDmg_Player += bonusPerLevel;
                totalAddedDamage += bonusPerLevel;
            }

            TriggerSuccessEffect();
            if (uISoundPlayer != null) uISoundPlayer.PlaySeccsedSharpeningSounds();
            
            UpdateWeaponGlow();
            UpdateUI();
            LogDebug($"Успех! Оружие заточено на +{currentUpgrade}.");
        }
        else
        {
            TriggerFailedEffect();
            if (uISoundPlayer != null) uISoundPlayer.PlayFailedSharpeningSounds();

            if (currentUpgrade < safeUpgrade)
            {
                LogDebug($"Неудача, но заточка безопасная! Уровень остался +{currentUpgrade}.");
            }
            else
            {
                if (playerStats != null)
                {
                    playerStats.AtkDmg_Player -= totalAddedDamage;
                    totalAddedDamage = 0;
                }

                currentUpgrade = 0;
                UpdateWeaponGlow();
                UpdateUI();
                LogDebug("Провал! Заточка слетела до +0.");
            }
        }
    }

    private void TriggerSuccessEffect()
    {
        if (SeccsesSharpening_UiEffect == null) return;

        failedEffectTimer = 0f;
        if (FailedSharpening_UiEffect != null) FailedSharpening_UiEffect.SetActive(false);

        SeccsesSharpening_UiEffect.SetActive(true);
        successEffectTimer = 0.4f;
    }

    private void TriggerFailedEffect()
    {
        if (FailedSharpening_UiEffect == null) return;

        successEffectTimer = 0f;
        if (SeccsesSharpening_UiEffect != null) SeccsesSharpening_UiEffect.SetActive(false);

        FailedSharpening_UiEffect.SetActive(true);
        failedEffectTimer = 0.5f;
    }

    private void HideAllEffects()
    {
        successEffectTimer = 0f;
        failedEffectTimer = 0f;

        if (SeccsesSharpening_UiEffect != null) SeccsesSharpening_UiEffect.SetActive(false);
        if (FailedSharpening_UiEffect != null) FailedSharpening_UiEffect.SetActive(false);
    }

    private void ResetProgressUI()
    {
        isSharpening = false;
        sharpenTimer = 0f;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        if (sharpenButton != null)
        {
            sharpenButton.interactable = !isSharpening && (currentUpgrade < maxUpgrade);
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = isSharpening;
        }
    }

    private void UpdateUI()
    {
        if (currentUpgrade_Text != null)
        {
            currentUpgrade_Text.text = $"+{currentUpgrade}";
        }
    }

    public void RefreshWeaponGlow()
    {
        UpdateWeaponGlow();
    }

    private void UpdateWeaponGlow()
    {
        Color targetColor = Color.black;
        if (upgradeGlowColors != null && upgradeGlowColors.Length > 0)
        {
            int colorIndex = Mathf.Clamp(currentUpgrade, 0, upgradeGlowColors.Length - 1);
            targetColor = upgradeGlowColors[colorIndex];
        }

        bool shouldGlow = currentUpgrade >= 4 && targetColor.maxColorComponent > 0.001f;

        if (bladeAuraObject != null)
        {
            bladeAuraObject.SetActive(shouldGlow);
        }

        if (bladeAuraMaterial != null)
        {
            Color auraColor = shouldGlow
                ? new Color(Mathf.Clamp01(targetColor.r), Mathf.Clamp01(targetColor.g), Mathf.Clamp01(targetColor.b), auraAlpha)
                : new Color(0f, 0f, 0f, 0f);

            if (bladeAuraMaterial.HasProperty("_TintColor"))
            {
                bladeAuraMaterial.SetColor("_TintColor", auraColor);
            }
            else if (bladeAuraMaterial.HasProperty("_Color"))
            {
                bladeAuraMaterial.SetColor("_Color", auraColor);
            }
            else
            {
                bladeAuraMaterial.color = auraColor;
            }
        }
    }

    private void OnEnable()
    {
        ResetProgressUI();
        UpdateUI();
    }

    private void OnDisable()
    {
        ResetProgressUI();
        HideAllEffects();
    }

    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
}
