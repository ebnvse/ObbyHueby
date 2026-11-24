using UnityEngine;
using UnityEngine.EventSystems;

public class ParryButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Перетащи сюда игрока со скриптом ParryController")]
    public ParryController parryController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (parryController != null)
            parryController.OnParryButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (parryController != null)
            parryController.OnParryButtonUp();
    }

    // Сбрасываем защиту, если палец или курсор соскользнул с кнопки за её пределы
    public void OnPointerExit(PointerEventData eventData)
    {
        if (parryController != null)
            parryController.OnParryButtonUp();
    }
}
