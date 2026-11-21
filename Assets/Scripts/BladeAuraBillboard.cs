using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BladeAuraBillboard : MonoBehaviour
{
    public enum BladeAxis { Up, Forward, Right }
    
    [Tooltip("Вдоль какой оси меча направлено лезвие (обычно Up — зелёная стрелка, или Forward — синяя)")]
    public BladeAxis bladeAxis = BladeAxis.Up;

    [Header("Плавное скрытие при взгляде вдоль лезвия")]
    [Range(0f, 1f)]
    [Tooltip("При каком значении угла ореол полностью исчезает (0.85 = начинает гаснуть при взгляде в острие)")]
    public float fadeStartThreshold = 0.75f;
    [Range(0f, 1f)]
    public float fadeEndThreshold = 0.95f;

    private Camera mainCam;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        Vector3 toCamera = (mainCam.transform.position - transform.position).normalized;
        Vector3 axisDirection = GetBladeDirection().normalized;

        // 1. Поворачиваем спрайт лицом к камере вокруг оси лезвия
        Vector3 forward = Vector3.Cross(axisDirection, Vector3.Cross(toCamera, axisDirection));
        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward, axisDirection);
        }

        // 2. Считаем, насколько сильно камера смотрит "вдоль" лезвия (0 = строго сбоку, 1 = строго в острие/рукоять)
        float alignment = Mathf.Abs(Vector3.Dot(toCamera, axisDirection));

        // Плавно переводим угол в множитель видимости от 1 (видно) до 0 (невидимо)
        float visibility = 1f - Mathf.InverseLerp(fadeStartThreshold, fadeEndThreshold, alignment);

        // Применяем прозрачность к текущему цвету спрайта (не ломая оттенок из Sharpening)
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = visibility;
            spriteRenderer.color = c;
            
            // Полностью отключаем отрисовку, если прозрачность 0 (экономит ресурсы телефона)
            spriteRenderer.enabled = visibility > 0.01f;
        }
    }

    private Vector3 GetBladeDirection()
    {
        Transform refTransform = transform.parent != null ? transform.parent : transform;
        switch (bladeAxis)
        {
            case BladeAxis.Forward: return refTransform.forward;
            case BladeAxis.Right: return refTransform.right;
            default: return refTransform.up;
        }
    }
}
