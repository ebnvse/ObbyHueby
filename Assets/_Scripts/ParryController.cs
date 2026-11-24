using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ParryController : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerStats playerStats;
    public PlayerController playerController;

    [Header("Настройки ввода")]
    [Tooltip("Разрешить защиту зажатием правой кнопки мыши на ПК")]
    public bool allowRightMouseParry = true;

    [Header("Состояние (для проверки из других скриптов)")]
    [SerializeField] private bool isParrying;

    [Header("Звуки")]
    [Tooltip("Звук когда тебя бьют, а ты парировал")]
    [SerializeField] private SoundEvent parrySound;

    public bool IsParrying => isParrying;

    private bool isMobileButtonHeld;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (playerStats != null && playerStats.isDead)
        {
            isParrying = false;
            isMobileButtonHeld = false;
            return;
        }

        bool isMouseHeld = allowRightMouseParry && 
                           Mouse.current != null && 
                           Mouse.current.rightButton.isPressed && 
                           !IsPointerOverUI();

        if (playerController != null && playerController.HasTarget)
        {
            isParrying = isMouseHeld || isMobileButtonHeld;
        }
        else
        {
            // Сбрасываем блок, если цель пропала или погибла
            isParrying = false;
        }
    }

    // Вызывается из PlayerStats в момент, когда враг ударил в блок
    public void PlayParrySound()
    {
        if (parrySound != null)
        {
            parrySound.Play(transform.position);
        }
    }

    public void OnParryButtonDown()
    {
        if (playerStats != null && playerStats.isDead) return;
        isMobileButtonHeld = true;
    }

    public void OnParryButtonUp()
    {
        isMobileButtonHeld = false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}
