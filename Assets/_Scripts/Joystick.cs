using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Элементы джойстика")]
    public RectTransform background;
    public RectTransform handle;

    [Header("Настройки")]
    [Tooltip("Скрывать джойстик, когда палец не касается экрана")]
    public bool hideOnRelease = true;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    private Vector2 inputVector;
    private RectTransform touchZone;
    private bool isDragging = false;

    private void Awake()
    {
        touchZone = GetComponent<RectTransform>();

        if (hideOnRelease && background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        // Переводим координаты касания экрана в локальные координаты сенсорной зоны
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchZone,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            // Перемещаем основу джойстика прямо под палец
            background.anchoredPosition = localPoint;
        }

        if (hideOnRelease && background != null)
        {
            background.gameObject.SetActive(true);
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 position))
        {
            float radius = background.sizeDelta.x / 2f;

            inputVector = position / radius;
            inputVector = Vector2.ClampMagnitude(inputVector, 1f);

            Horizontal = inputVector.x;
            Vertical = inputVector.y;

            handle.anchoredPosition =
                inputVector * (radius - handle.sizeDelta.x / 2f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        inputVector = Vector2.zero;

        Horizontal = 0;
        Vertical = 0;

        handle.anchoredPosition = Vector2.zero;

        if (hideOnRelease && background != null)
        {
            background.gameObject.SetActive(false);
        }
    }
}
