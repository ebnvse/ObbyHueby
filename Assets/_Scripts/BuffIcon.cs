using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffIconUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI timerText;

    private float remainingTime;
    private bool isInitialized = false;

    public void Setup(Sprite icon, float duration)
    {
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        remainingTime = duration;
        isInitialized = true;
        UpdateTimerText();
    }

    private void Update()
    {
        if (!isInitialized) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            Destroy(gameObject); // Удаляем префаб с панели по истечении времени
            return;
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            // Округляем вверх (Mathf.CeilToInt), чтобы таймер шел от 60 до 1, а не висел на 0 последнюю секунду
            timerText.text = $"{Mathf.CeilToInt(remainingTime)}с";
        }
    }
}
