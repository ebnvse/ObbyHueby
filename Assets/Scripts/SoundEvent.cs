using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewSoundEvent", menuName = "Audio/Sound Event")]
public class SoundEvent : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;

    [Header("Настройки громкости и тона")]
    [Range(0f, 1f)] public float minVolume = 0.9f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;

    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    [Header("3D настройки")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 0 = 2D (UI/голос), 1 = 3D (мир)
    public float maxDistance = 25f;

    // Воспроизведение в 3D-позиции с опциональным множителем громкости
    public void Play(Vector3 position, float volumeMultiplier = 1f)
    {
        if (AudioPoolManager.Instance != null)
        {
            AudioPoolManager.Instance.PlaySound(this, position, volumeMultiplier);
        }
    }

    // Перегрузка для 2D-звуков (например, интерфейс или звуки в голове игрока)
    public void Play2D(float volumeMultiplier = 1f)
    {
        Play(Vector3.zero, volumeMultiplier);
    }
}
