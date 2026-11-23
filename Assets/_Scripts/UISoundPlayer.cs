using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UISoundPlayer : MonoBehaviour
{
    [SerializeField] private SoundLibrary soundLibrary;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Публичный метод с одним аргументом int — Unity UI поддерживает его в OnClick
    public void PlaySoundByIndex(int index)
    {
        if (soundLibrary == null) return;

        AudioClip clip = soundLibrary.GetSound(index);
        if (clip != null)
        {
            // PlayOneShot позволяет звукам накладываться друг на друга при быстрых кликах
            audioSource.PlayOneShot(clip);
        }
    }
    public void PlaySeccsedSharpeningSounds()
    {
        if (soundLibrary == null) return;

        AudioClip clip = soundLibrary.GetSound(3);
        if (clip != null)
        {
            // PlayOneShot позволяет звукам накладываться друг на друга при быстрых кликах
            audioSource.PlayOneShot(clip);
        }
    }
    public void PlayFailedSharpeningSounds()
    {
        if (soundLibrary == null) return;

        AudioClip clip = soundLibrary.GetSound(5);
        if (clip != null)
        {
            // PlayOneShot позволяет звукам накладываться друг на друга при быстрых кликах
            audioSource.PlayOneShot(clip);
        }
    }
}
