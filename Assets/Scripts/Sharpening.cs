using UnityEngine;
using TMPro;

public class Sharpening : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerStats playerStats;
    public Money money;
    public TextMeshProUGUI currentUpgrade_Text;

    [Header("Настройки заточки")]
    public int currentUpgrade = 0;
    public int maxUpgrade = 10;   
    public int safeUpgrade = 3;    
    public int bonusPerLevel = 2;
    public int upgradeCost = 5;

    // Для теста стоят 100%, потом верни { 100, 100, 100, 80, 60, 50, 40, 30, 20, 10 }
    private int[] upgradeChances = { 100, 100, 100, 90, 80, 75, 70, 65, 55, 50 };

    [Header("Свечение меча и аура (как в L2)")]
    public int materialIndex = 1;
    public Renderer weaponRenderer;       // MeshRenderer самого меча
    public Renderer bladeAuraRenderer;    // Перетащи сюда Quad "BladeAura"
    [Range(0.1f, 1f)]
    public float auraAlpha = 0.85f;       // Прозрачность ореола вокруг лезвия

    [Tooltip("Цвета для уровней от +0 до +10 (всего 11 элементов)")]
    [ColorUsage(true, true)]
    public Color[] upgradeGlowColors = new Color[]
    {
        Color.black, Color.black, Color.black, Color.black, // +0..+3 (выкл)
        Color.cyan, Color.cyan, Color.blue, Color.blue, Color.blue, Color.blue, // +4..+9
        Color.red // +10
    };


    private void Start()
    {
        UpdateUI();
        UpdateWeaponGlow();
    }

    public void SharpeningWeapon()
    {
        // ТЕСТОВЫЙ РЕЖИМ: при клике на +10 сбрасываем заточку на +0
        if (currentUpgrade >= maxUpgrade)
        {
            playerStats.AtkDmg_Player -= currentUpgrade * bonusPerLevel;
            currentUpgrade = 0;

            UpdateWeaponGlow();
            UpdateUI();
            Debug.Log($"[ТЕСТ] Достигнут максимум! Заточка сброшена на +0. Ваш урон: {playerStats.AtkDmg_Player}");
            return; 
        }

        if (money.current_money >= upgradeCost)
        {
            money.current_money -= upgradeCost;

            int chance = upgradeChances[currentUpgrade];
            int roll = Random.Range(1, 101);

            if (roll <= chance)
            {
                // УСПЕХ
                currentUpgrade++;
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
                }
                else
                {
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

    private void UpdateUI()
    {
        if (currentUpgrade_Text != null)
        {
            if (currentUpgrade >= maxUpgrade)
            {
                currentUpgrade_Text.text = $"Меч заточен на : +{currentUpgrade} Шанс: MAX";
            }
            else
            {
                int nextChance = upgradeChances[currentUpgrade];
                currentUpgrade_Text.text = $"Меч заточен на : +{currentUpgrade} Шанс: {nextChance}%";
            }
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

        // 1. Обновляем Emission на материале самого меча
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

        // 2. Включаем и красим ореол BladeAura вдоль лезвия
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
                    auraMat.SetColor("_TintColor", auraColor); // Для Legacy Shaders/Particles/Additive
                }
                else
                {
                    auraMat.color = auraColor; // Для Mobile/Particles/Additive
                }
            }
        }
    }
}
