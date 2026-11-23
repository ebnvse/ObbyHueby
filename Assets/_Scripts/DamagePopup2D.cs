using UnityEngine;
using TMPro;

public class DamagePopup2D : MonoBehaviour
{
    [Header("Настройки цветов")]
    public Color normalDamageColor = Color.white;
    public Color critDamageColor = Color.red;

    [Header("Настройки дуги (подскок вправо, падение влево)")]
    public float initialVelocityX = 180f;     // Начальный рывок вправо
    public float horizontalDrag = 50f;       // Сила разворота влево при падении
    public float initialVelocityY = 300f;     // Начальный подскок вверх
    public float gravity = 650f;              // Сила падения вниз
    public float critScaleMultiplier = 1.35f; // Увеличение размера при крите

    [Header("Исчезновение")]
    public float lifetime = 1.1f;             // Сколько секунд живет текст
    public float fadeStartTime = 0.4f;        // Когда начинает плавно исчезать

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private Vector2 velocity;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponentInChildren <TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(int damageAmount, bool isCrit)
    {
        if (textMesh == null) return;

        textMesh.text = damageAmount.ToString();

        // Берем выбранный цвет и принудительно делаем его 100% непрозрачным
        Color chosenColor = isCrit ? critDamageColor : normalDamageColor;
        chosenColor.a = 1f;
        textMesh.color = chosenColor;
        textMesh.alpha = 1f;

        if (isCrit)
        {
            rectTransform.localScale *= critScaleMultiplier;
        }

        // Небольшой разброс, чтобы цифры при серии ударов не накладывались друг на друга
        float randomX = Random.Range(initialVelocityX * 0.85f, initialVelocityX * 1.15f);
        velocity = new Vector2(randomX, initialVelocityY);
    }

    private void Update()
    {
        // 1. Движение по дуге: сначала вправо-вверх, затем торможение по X и разворот влево на падении
        velocity.x -= horizontalDrag * Time.deltaTime;
        velocity.y -= gravity * Time.deltaTime;
        rectTransform.anchoredPosition += velocity * Time.deltaTime;

        // 2. Плавное выцветание (меняем только Alpha, не трогая сам цвет RGB)
        timer += Time.deltaTime;
        if (timer >= fadeStartTime && textMesh != null)
        {
            float fadeProgress = (timer - fadeStartTime) / (lifetime - fadeStartTime);
            textMesh.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
        }

        // 3. Удаление объекта
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
