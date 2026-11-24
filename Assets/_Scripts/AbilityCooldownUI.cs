using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldownUI : MonoBehaviour
{
    [Header("Ссылки")]
    public AttackController attackController;
    public Image cooldownImage;

    private void Awake()
    {
        if (cooldownImage == null)
        {
            cooldownImage = GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (attackController == null || cooldownImage == null)
            return;

        // Плавно меняет fillAmount от 0 (на КД) до 1 (готово)
        cooldownImage.fillAmount = attackController.AbilityCooldownProgress;
    }
}
