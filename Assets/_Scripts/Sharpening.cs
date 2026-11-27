using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class Sharpening : MonoBehaviour
{
    [Header("Ссылки на системы")]
    public PlayerStats playerStats;
    public Money money;
    public UISoundPlayer uISoundPlayer;

    [Header("UI элементы")]
    public TextMeshProUGUI currentUpgrade_Text;
    public Image progressFillImage;
    public Button sharpenButton;
    public Button cancelButton;
    public GameObject SeccsesSharpening_UiEffect;
    public GameObject FailedSharpening_UiEffect;

    [Header("Настройки заточки")]
    public float sharpenDuration = 1f;
    public float postSharpenCooldown = 0.15f;
    public int currentUpgrade = 0;
    public int maxUpgrade = 16;
    public int safeUpgrade = 3;
    public int bonusPerLevel = 2;
    public int upgradeCost = 5;

    private readonly int[] upgradeChances =
        { 100, 100, 100, 95, 92, 90, 88, 85, 82, 80, 77, 75, 72, 70, 68, 65 };

    [Header("Свечение меча")]
    public Material bladeAuraMaterial;
    public GameObject bladeAuraObject;
    [Range(0.1f, 1f)] public float auraAlpha = 0.85f;

    [ColorUsage(true, true)]
    public Color[] upgradeGlowColors = new Color[]
    {
        Color.black, Color.black, Color.black, Color.black,
        Color.cyan, Color.cyan,
        Color.blue, Color.blue, Color.blue, Color.blue, Color.blue, Color.blue,
        Color.red, Color.red, Color.red, Color.red,
        new Color(1f, 0.2f, 0.8f)
    };

    private bool isSharpening;
    private bool hasLoadedSharpening;
    private float sharpenTimer;
    private float nextAllowedSharpenTime;
    private int lastClickFrame = -1;
    private int totalAddedDamage;
    private float successEffectTimer;
    private float failedEffectTimer;

    private void OnEnable()
    {
        YG2.onGetSDKData += LoadSharpening;
        ResetProgressUI();
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= LoadSharpening;
        ResetProgressUI();
        HideAllEffects();
        RefreshWeaponGlow();
    }

    private void Start()
    {
        if (YG2.isSDKEnabled || Application.isEditor || 
            Application.platform != RuntimePlatform.WebGLPlayer)
        {
            LoadSharpening();
        }

        UpdateUI();
        RefreshWeaponGlow();
    }


    public void LoadSharpening()
    {
        if (hasLoadedSharpening) return;

        currentUpgrade = Mathf.Clamp(YG2.saves.currentSharpening_index, 0, maxUpgrade);
        totalAddedDamage = currentUpgrade * bonusPerLevel;
        hasLoadedSharpening = true;

        // AtkDmg_Player уже хранится как итоговая атака.
        // Не прибавляем бонус повторно, иначе урон будет удваиваться.
        UpdateUI();
        UpdateWeaponGlow();
    }

    private void Update()
    {
        if (successEffectTimer > 0f)
        {
            successEffectTimer -= Time.unscaledDeltaTime;
            if (successEffectTimer <= 0f && SeccsesSharpening_UiEffect != null)
                SeccsesSharpening_UiEffect.SetActive(false);
        }

        if (failedEffectTimer > 0f)
        {
            failedEffectTimer -= Time.unscaledDeltaTime;
            if (failedEffectTimer <= 0f && FailedSharpening_UiEffect != null)
                FailedSharpening_UiEffect.SetActive(false);
        }

        if (!isSharpening) return;

        float duration = Mathf.Max(0.05f, sharpenDuration);
        sharpenTimer += Time.unscaledDeltaTime;

        if (progressFillImage != null)
            progressFillImage.fillAmount = Mathf.Clamp01(sharpenTimer / duration);

        if (sharpenTimer >= duration)
            FinishSharpening();
    }

    public void SetSharpenDuration(float newDuration)
    {
        sharpenDuration = Mathf.Max(0.05f, newDuration);
    }

    public void ResetUpgrade()
    {
        if (!hasLoadedSharpening)
        {
            Debug.LogWarning("[Sharpening] Сброс отклонён: сохранение ещё не загружено.");
            return;
        }

        if (playerStats != null && totalAddedDamage > 0)
            playerStats.AtkDmg_Player = Mathf.Max(1, playerStats.AtkDmg_Player - totalAddedDamage);
        currentUpgrade = 0;
        totalAddedDamage = 0;

        ResetProgressUI();
        HideAllEffects();
        UpdateWeaponGlow();
        UpdateUI();

        YG2.saves.currentSharpening_index = 0;
        if (playerStats != null)
            playerStats.SaveStats();
        else if (money != null)
            money.SaveMoney();
        else
            YG2.SaveProgress();
    }

    public void StartSharpening()
    {
        if (!hasLoadedSharpening || (playerStats != null && !playerStats.HasLoadedProgress))
            return;

        if (Time.frameCount == lastClickFrame) return;
        lastClickFrame = Time.frameCount;

        if (isSharpening || Time.unscaledTime < nextAllowedSharpenTime) return;

        if (currentUpgrade >= maxUpgrade)
        {
            LogDebug($"Достигнут максимальный уровень заточки (+{maxUpgrade}).");
            return;
        }

        if (money == null)
        {
            Debug.LogError("[Sharpening] Не назначен компонент Money!");
            return;
        }

        if (money.current_money < upgradeCost) return;

        if (uISoundPlayer != null)
            uISoundPlayer.PlaySoundByIndex(4);

        isSharpening = true;
        sharpenTimer = 0f;

        if (progressFillImage != null)
            progressFillImage.fillAmount = 0f;

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
            progressFillImage.fillAmount = 0f;

        ExecuteSharpening();

        if (playerStats != null)
            playerStats.UpdatePlayerStatsUI();

        UpdateButtonsState();
    }

    private void ExecuteSharpening()
    {
        if (currentUpgrade >= maxUpgrade || money == null) return;
        if (money.current_money < upgradeCost) return;

        // Списание денег происходит после завершения анимации.
        money.current_money -= upgradeCost;
        money.WriteMoneyToSaves();

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
            LogDebug($"Успех! Уровень заточки: +{currentUpgrade}.");
        }
        else
        {
            TriggerFailedEffect();
            if (uISoundPlayer != null) uISoundPlayer.PlayFailedSharpeningSounds();

            if (currentUpgrade >= safeUpgrade)
            {
                if (playerStats != null)
                    playerStats.AtkDmg_Player = Mathf.Max(1, playerStats.AtkDmg_Player - totalAddedDamage);

                totalAddedDamage = 0;
                currentUpgrade = 0;
                UpdateWeaponGlow();
                LogDebug("Неудача: заточка сброшена до +0.");
            }
            else
            {
                LogDebug($"Неудача, безопасный уровень сохранён: +{currentUpgrade}.");
            }
        }

        YG2.saves.currentSharpening_index = currentUpgrade;

        if (playerStats != null)
            playerStats.SaveStats();
        else
            money.SaveMoney();

        UpdateUI();
    }

    private void TriggerSuccessEffect()
    {
        if (SeccsesSharpening_UiEffect == null) return;
        failedEffectTimer = 0f;
        if (FailedSharpening_UiEffect != null)
            FailedSharpening_UiEffect.SetActive(false);

        SeccsesSharpening_UiEffect.SetActive(true);
        successEffectTimer = 0.4f;
    }

    private void TriggerFailedEffect()
    {
        if (FailedSharpening_UiEffect == null) return;
        successEffectTimer = 0f;
        if (SeccsesSharpening_UiEffect != null)
            SeccsesSharpening_UiEffect.SetActive(false);

        FailedSharpening_UiEffect.SetActive(true);
        failedEffectTimer = 0.5f;
    }

    private void HideAllEffects()
    {
        successEffectTimer = 0f;
        failedEffectTimer = 0f;

        if (SeccsesSharpening_UiEffect != null)
            SeccsesSharpening_UiEffect.SetActive(false);
        if (FailedSharpening_UiEffect != null)
            FailedSharpening_UiEffect.SetActive(false);
    }

    private void ResetProgressUI()
    {
        isSharpening = false;
        sharpenTimer = 0f;
        if (progressFillImage != null)
            progressFillImage.fillAmount = 0f;

        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        if (sharpenButton != null)
            sharpenButton.interactable = hasLoadedSharpening && !isSharpening &&
                                         currentUpgrade < maxUpgrade;

        if (cancelButton != null)
            cancelButton.interactable = isSharpening;
    }

    private void UpdateUI()
    {
        if (currentUpgrade_Text != null)
            currentUpgrade_Text.text = $"+{currentUpgrade}";
        UpdateButtonsState();
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
            bladeAuraObject.SetActive(shouldGlow);

        if (bladeAuraMaterial != null)
        {
            Color auraColor = shouldGlow
                ? new Color(Mathf.Clamp01(targetColor.r), Mathf.Clamp01(targetColor.g),
                    Mathf.Clamp01(targetColor.b), auraAlpha)
                : new Color(0f, 0f, 0f, 0f);

            if (bladeAuraMaterial.HasProperty("_TintColor"))
                bladeAuraMaterial.SetColor("_TintColor", auraColor);
            else if (bladeAuraMaterial.HasProperty("_Color"))
                bladeAuraMaterial.SetColor("_Color", auraColor);
            else
                bladeAuraMaterial.color = auraColor;
        }
    }

    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
}