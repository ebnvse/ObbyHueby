using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    // Шансы для уровней от +0 -> +1 до +15 -> +16
    private readonly int[] upgradeChances = { 100, 100, 100, 95, 92, 90, 88, 85, 82, 80, 77, 75, 72, 70, 68, 65 };

    [Header("Свечение меча (как в L2)")]
    public Renderer bladeAuraRenderer;
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

    // Внутренние состояния и кэш
    private Coroutine sharpenCoroutine;
    private Coroutine successEffectCoroutine;
    private Coroutine failedEffectCoroutine;
    private bool isSharpening = false;
    private int totalAddedDamage = 0; // Точный трекер добавленного урона

    private Material cachedAuraMat;
    private WaitForSeconds waitSuccessEffect;
    private WaitForSeconds waitFailedEffect;
    private WaitForSeconds waitCooldown;

    private void Awake()
    {
        // Кэшируем задержки для корутин (0 мусора для сборщика мусора)
        waitSuccessEffect = new WaitForSeconds(0.4f);
        waitFailedEffect = new WaitForSeconds(0.5f);
        waitCooldown = new WaitForSeconds(postSharpenCooldown);

        if (bladeAuraRenderer != null)
        {
            cachedAuraMat = bladeAuraRenderer.material;
        }
    }

    private void Start()
    {
        // На случай, если в инспекторе уже выставлен уровень > 0 при старте игры
        totalAddedDamage = currentUpgrade * bonusPerLevel;

        HideAllEffects();
        ResetProgressUI();
        UpdateUI();
        UpdateWeaponGlow();
    }

    // Вешаем этот метод на OnClick кнопки "Точить"
    public void StartSharpening()
    {
        // Мгновенная блокировка повторных вызовов в этом же кадре
        if (isSharpening || sharpenCoroutine != null)
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

        isSharpening = true;
        UpdateButtonsState();
        sharpenCoroutine = StartCoroutine(SharpenRoutine());
    }

    // Вешаем этот метод на OnClick кнопки "Отмена"
    public void CancelSharpening()
    {
        if (!isSharpening && sharpenCoroutine == null)
            return;

        if (sharpenCoroutine != null)
        {
            StopCoroutine(sharpenCoroutine);
            sharpenCoroutine = null;
        }

        ResetProgressUI();
        LogDebug("Заточка отменена игроком.");
    }

    private IEnumerator SharpenRoutine()
    {
        float elapsed = 0f;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        // Защита от деления на ноль, если sharpenDuration <= 0
        if (sharpenDuration > 0f)
        {
            while (elapsed < sharpenDuration)
            {
                elapsed += Time.deltaTime;
                if (progressFillImage != null)
                {
                    progressFillImage.fillAmount = Mathf.Clamp01(elapsed / sharpenDuration);
                }
                yield return null;
            }
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 1f;
        }

        ExecuteSharpening();

        if (playerStats != null)
        {
            playerStats.UpdatePlayerStatsUI();
        }

        // Небольшая пауза перед разблокировкой кнопки, чтобы погасить спам-клики
        if (postSharpenCooldown > 0f)
        {
            yield return waitCooldown;
        }

        ResetProgressUI();
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

        // Списываем валюту
        money.current_money -= upgradeCost;

        int chanceIndex = Mathf.Clamp(currentUpgrade, 0, upgradeChances.Length - 1);
        int chance = upgradeChances[chanceIndex];
        int roll = Random.Range(1, 101);

        if (roll <= chance)
        {
            // --- УСПЕХ ---
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
            // --- НЕУДАЧА ---
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

        // Выключаем эффект провала, если он ещё горел от прошлой попытки
        if (FailedSharpening_UiEffect != null) FailedSharpening_UiEffect.SetActive(false);

        if (successEffectCoroutine != null)
        {
            StopCoroutine(successEffectCoroutine);
        }

        SeccsesSharpening_UiEffect.SetActive(true);
        successEffectCoroutine = StartCoroutine(OffEffectSharpAnim());
    }

    private void TriggerFailedEffect()
    {
        if (FailedSharpening_UiEffect == null) return;

        // Выключаем эффект успеха, если он ещё горел от прошлой попытки
        if (SeccsesSharpening_UiEffect != null) SeccsesSharpening_UiEffect.SetActive(false);

        if (failedEffectCoroutine != null)
        {
            StopCoroutine(failedEffectCoroutine);
        }

        FailedSharpening_UiEffect.SetActive(true);
        failedEffectCoroutine = StartCoroutine(OffFailedEffectSharpAnim());
    }

    private IEnumerator OffEffectSharpAnim()
    {
        yield return waitSuccessEffect;
        if (SeccsesSharpening_UiEffect != null)
        {
            SeccsesSharpening_UiEffect.SetActive(false);
        }
        successEffectCoroutine = null;
    }

    private IEnumerator OffFailedEffectSharpAnim()
    {
        yield return waitFailedEffect;
        if (FailedSharpening_UiEffect != null)
        {
            FailedSharpening_UiEffect.SetActive(false);
        }
        failedEffectCoroutine = null;
    }

    private void HideAllEffects()
    {
        if (successEffectCoroutine != null)
        {
            StopCoroutine(successEffectCoroutine);
            successEffectCoroutine = null;
        }
        if (failedEffectCoroutine != null)
        {
            StopCoroutine(failedEffectCoroutine);
            failedEffectCoroutine = null;
        }
        if (SeccsesSharpening_UiEffect != null) SeccsesSharpening_UiEffect.SetActive(false);
        if (FailedSharpening_UiEffect != null) FailedSharpening_UiEffect.SetActive(false);
    }

    private void ResetProgressUI()
    {
        isSharpening = false;
        sharpenCoroutine = null;

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

        UpdateButtonsState();
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

        if (bladeAuraRenderer != null)
        {
            bladeAuraRenderer.gameObject.SetActive(shouldGlow);

            if (shouldGlow && cachedAuraMat != null)
            {
                Color auraColor = new Color(
                    Mathf.Clamp01(targetColor.r),
                    Mathf.Clamp01(targetColor.g),
                    Mathf.Clamp01(targetColor.b),
                    auraAlpha
                );

                if (cachedAuraMat.HasProperty("_TintColor"))
                {
                    cachedAuraMat.SetColor("_TintColor", auraColor);
                }
                else
                {
                    cachedAuraMat.color = auraColor;
                }
            }
        }
    }

    private void OnDisable()
    {
        // При закрытии окна сбрасываем и заточку, и зависшие UI-эффекты
        if (isSharpening || sharpenCoroutine != null)
        {
            CancelSharpening();
        }
        HideAllEffects();
    }

    private void OnDestroy()
    {
        if (cachedAuraMat != null)
        {
            Destroy(cachedAuraMat);
        }
    }

    // Логи будут работать только в редакторе Unity и не замедлят финальный билд
    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
}
