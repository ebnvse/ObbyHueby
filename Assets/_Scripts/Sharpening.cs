using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Sharpening : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerStats playerStats;
    public Money money;
    public TextMeshProUGUI currentUpgrade_Text;
    public UISoundPlayer uISoundPlayer;


    [Header("UI процесса заточки")]
    [Tooltip("Image с типом Filled (Horizontal / Radial), который заполняется от 0 до 1")]
    public Image progressFillImage;
    [Tooltip("Время заполнения полоски заточки в секундах")]
    public float sharpenDuration = 1f;
    [Tooltip("Кнопка запуска заточки")]
    public Button sharpenButton;
    [Tooltip("Кнопка отмены заточки (всегда видна на экране)")]
    public Button cancelButton;
    public GameObject SeccsesSharpening_UiEffect;
    public GameObject FailedSharpening_UiEffect;


    private Coroutine sharpenCoroutine;
    private bool isSharpening = false;

    [Header("Настройки заточки")]
    public int currentUpgrade = 0;
    public int maxUpgrade = 16;   
    public int safeUpgrade = 3;    
    public int bonusPerLevel = 2;
    public int upgradeCost = 5;

    // Лайтовые шансы для уровней от +0 -> +1 до +15 -> +16
    private int[] upgradeChances = { 100, 100, 100, 95, 92, 90, 88, 85, 82, 80, 77, 75, 72, 70, 68, 65 };

    [Header("Свечение меча и аура (как в L2)")]
    public int materialIndex = 1;
    public Renderer weaponRenderer;       // MeshRenderer самого меча
    public Renderer bladeAuraRenderer;    // Перетащи сюда Quad "BladeAura"
    [Range(0.1f, 1f)]
    public float auraAlpha = 0.85f;       // Прозрачность ореола вокруг лезвия

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

    private void Start()
    {
        ResetProgressUI();
        UpdateUI();
        UpdateWeaponGlow();
    }

    // Вешаем этот метод на OnClick кнопки "Точить"
    public void StartSharpening()
    {
        if (isSharpening)
            return;

        if (currentUpgrade >= maxUpgrade)
        {
            Debug.Log($"Оружие уже достигло максимальной заточки (+{maxUpgrade})!");
            return;
        }

        if (money.current_money < upgradeCost)
        {
            Debug.Log("Недостаточно денег для заточки!");
            return;
        }

        sharpenCoroutine = StartCoroutine(SharpenRoutine());
    }

    // Вешаем этот метод на OnClick кнопки "Отмена"
    public void CancelSharpening()
    {
        if (!isSharpening)
            return;

        if (sharpenCoroutine != null)
        {
            StopCoroutine(sharpenCoroutine);
            sharpenCoroutine = null;
        }

        ResetProgressUI();
        Debug.Log("Заточка отменена игроком.");
    }

    private IEnumerator SharpenRoutine()
    {
        isSharpening = true;

        if (sharpenButton != null) sharpenButton.interactable = false;

        float elapsed = 0f;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        while (elapsed < sharpenDuration)
        {
            elapsed += Time.deltaTime;
            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = Mathf.Clamp01(elapsed / sharpenDuration);
            }
            yield return null;
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 1f;
        }

        ExecuteSharpening();
        playerStats.UpdatePlayerStatsUI();
        ResetProgressUI();
    }
    public IEnumerator OffEffectSharpAnim()
    {
        yield return new WaitForSeconds(0.4f);
        SeccsesSharpening_UiEffect.SetActive(false);

    }
    public IEnumerator OffFailedEffectSharpAnim()
    {
        yield return new WaitForSeconds(0.5f);
        FailedSharpening_UiEffect.SetActive(false);

    }
    private void ExecuteSharpening()
    {
        
        if (currentUpgrade >= maxUpgrade)
            return;

        if (money.current_money >= upgradeCost)
        {
            money.current_money -= upgradeCost;
            

            int chanceIndex = Mathf.Clamp(currentUpgrade, 0, upgradeChances.Length - 1);
            int chance = upgradeChances[chanceIndex];
            int roll = Random.Range(1, 101);

            if (roll <= chance)
            {
                // УСПЕХ
                currentUpgrade++;
                SeccsesSharpening_UiEffect.SetActive(true);
                uISoundPlayer.PlaySeccsedSharpeningSounds();
                StartCoroutine(OffEffectSharpAnim());
                UpdateWeaponGlow();
                playerStats.AtkDmg_Player += bonusPerLevel;
                UpdateUI();
                Debug.Log($"Успех! Оружие заточено на +{currentUpgrade}. Ваш урон: {playerStats.AtkDmg_Player}");
                
            }
            else
            {
                // НЕУДАЧА
                if (currentUpgrade < safeUpgrade)
                {
                    Debug.Log($"Неудача, но заточка безопасная! Уровень остался +{currentUpgrade}.");
                    FailedSharpening_UiEffect.SetActive(true);
                    StartCoroutine(OffFailedEffectSharpAnim());
                }
                else
                {
                    FailedSharpening_UiEffect.SetActive(true);
                    StartCoroutine(OffFailedEffectSharpAnim());
                    uISoundPlayer.PlayFailedSharpeningSounds();
                    playerStats.AtkDmg_Player -= currentUpgrade * bonusPerLevel;
                    currentUpgrade = 0;
                    UpdateWeaponGlow();
                    UpdateUI();
                    Debug.Log($"Провал! Заточка слетела до +0. Ваш урон: {playerStats.AtkDmg_Player}");
                }
            }
        }
        else
        {
            Debug.Log("Недостаточно денег для заточки!");
        }
    }

    private void ResetProgressUI()
    {
        isSharpening = false;
        sharpenCoroutine = null;

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = 0f;
        }

        if (sharpenButton != null)
        {
            sharpenButton.interactable = currentUpgrade < maxUpgrade;
        }
    }

    private void UpdateUI()
    {
        if (currentUpgrade_Text != null)
        {
            if (currentUpgrade >= maxUpgrade)
            {
                currentUpgrade_Text.text = $"+{currentUpgrade}";
            }
            else
            {
                int chanceIndex = Mathf.Clamp(currentUpgrade, 0, upgradeChances.Length - 1);
                int nextChance = upgradeChances[chanceIndex];
                currentUpgrade_Text.text = $"+{currentUpgrade}";
            }
        }

        if (sharpenButton != null && !isSharpening)
        {
            sharpenButton.interactable = currentUpgrade < maxUpgrade;
        }
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

        if (weaponRenderer != null)
        {
            Material[] mats = weaponRenderer.materials;
            if (materialIndex >= 0 && materialIndex < mats.Length)
            {
                Material mat = mats[materialIndex];
                if (shouldGlow)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", targetColor);
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }

        if (bladeAuraRenderer != null)
        {
            bladeAuraRenderer.gameObject.SetActive(shouldGlow);

            if (shouldGlow)
            {
                Color auraColor = new Color(
                    Mathf.Clamp01(targetColor.r),
                    Mathf.Clamp01(targetColor.g),
                    Mathf.Clamp01(targetColor.b),
                    auraAlpha
                );

                Material auraMat = bladeAuraRenderer.material;
                if (auraMat.HasProperty("_TintColor"))
                {
                    auraMat.SetColor("_TintColor", auraColor);
                }
                else
                {
                    auraMat.color = auraColor;
                }
            }
        }
    }

    private void OnDisable()
    {
        if (isSharpening)
        {
            CancelSharpening();
        }
    }
}
