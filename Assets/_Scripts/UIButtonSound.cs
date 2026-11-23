using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Настройки звука")]
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Вариативность тона (Pitch)")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private static AudioSource uiAudioSource;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        // Создаем общий 2D AudioSource для всего интерфейса, если его еще нет
        if (uiAudioSource == null)
        {
            GameObject audioObj = new GameObject("UI_AudioSource");
            DontDestroyOnLoad(audioObj);
            uiAudioSource = audioObj.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f; // Строго 2D звук, чтобы громкость не зависела от камеры
        }

        // Подписываемся на нажатие кнопки автоматически
        button.onClick.AddListener(PlayClickSound);
    }

    private void OnDestroy()
    {
        // Отписываемся при удалении объекта во избежание утечек памяти
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        if (clickSound == null || uiAudioSource == null)
            return;

        uiAudioSource.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;
        uiAudioSource.PlayOneShot(clickSound, volume);
    }
}
