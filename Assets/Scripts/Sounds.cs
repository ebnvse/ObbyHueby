using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Sounds : MonoBehaviour
{
    [Header("Звуки шагов")]
    public AudioClip[] footstepSounds;

    [Header("Звуки ударов / атак")]
    public AudioClip[] attackSounds;

    [Header("Звуки монеток")]
    public AudioClip CoinSounds;

    public AudioSource audioSrc;
    private int lastStepIndex = -1;
    private int lastAttackIndex = -1;

    private void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
    }

    // Базовый метод для любого клипа
    public void PlaySound(AudioClip clip, float volume = 1f, float minPitch = 0.90f, float maxPitch = 1.2f)
    {
        if (clip == null || audioSrc == null) return;

        audioSrc.pitch = Random.Range(minPitch, maxPitch);
        audioSrc.PlayOneShot(clip, volume);
    }


    // Публичный метод для шагов (вызывается из PlayerController)
    public void PlayRandomFootstep(float volume = 1f, float minPitch = 0.90f, float maxPitch = 1.2f)
    {
        PlayRandomFromArray(footstepSounds, ref lastStepIndex, volume, minPitch, maxPitch);
    }

    // Публичный метод для ударов (вызывается из AttackController)
    public void PlayRandomAttack(float volume = 1f, float minPitch = 0.90f, float maxPitch = 1.2f)
    {
        PlayRandomFromArray(attackSounds, ref lastAttackIndex, volume, minPitch, maxPitch);
    }

    // Общая внутренняя логика выбора случайного звука без повторов
    private void PlayRandomFromArray(AudioClip[] clips, ref int lastIndex, float volume, float minPitch, float maxPitch)
    {
        if (clips == null || clips.Length == 0) return;

        int randomIndex = 0;

        if (clips.Length > 1)
        {
            do
            {
                randomIndex = Random.Range(0, clips.Length);
            }
            while (randomIndex == lastIndex);
        }

        lastIndex = randomIndex;
        PlaySound(clips[randomIndex], volume, minPitch, maxPitch);
    }
}
