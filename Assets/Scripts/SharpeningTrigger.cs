using UnityEngine;

public class SharpeningTrigger : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Перетащи сюда UI-панель заточки (Canvas или Panel)")]
    public GameObject sharpeningPanel;

    [Tooltip("Тег объекта игрока")]
    public string playerTag = "Player";

    private void Start()
    {
        // На старте игры прячем панель заточки
        if (sharpeningPanel != null)
        {
            sharpeningPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            if (sharpeningPanel != null)
            {
                sharpeningPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(playerTag))
        {
            if (sharpeningPanel != null)
            {
                sharpeningPanel.SetActive(false);
            }
        }
    }
}
